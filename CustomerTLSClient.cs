// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Security.Cryptography;



// RFC 5246:
// The Transport Layer Security (TLS) Protocol Version 1.2
// https://tools.ietf.org/html/rfc5246

// https://en.wikipedia.org/wiki/Transport_Layer_Security



namespace ExampleServer
{
  class CustomerTLSClient
  {
  private NetworkStream NetStream;
  private TcpClient Client;
  private string StatusString = "";
  private ECTime LastReadWriteTime;
  private byte[] RawBuffer;
  private int RawBufferStart = 0;
  private int RawBufferEnd = 0;
  private byte[] TLSOuterRecordBuffer;
  private int TLSOuterRecordBufferLast = 0;
  private int OuterTLSContentType = 0;
  private int OuterTLSVersionMajor = 0;
  private int OuterTLSVersionMinor = 0;
  private int OuterTLSLength = 0;
  // Apparently some implementations go beyond this record length limit.
  // The length is a 16 bit number and so it could go up to 64K.
  private const int MaximumTLSRecordLength = 16384 + 5; // RFC says length must not exceed 2^14.
  private const int RawBufferLength = 1024 * 128;
  private RNGCryptoServiceProvider CryptoRand;
  private ProcessHandshake Handshake;
  private string DomainName = "";



  private CustomerTLSClient()
    {
    }


  internal CustomerTLSClient( string UseDomainName )
    {
    DomainName = UseDomainName;
    RawBuffer = new byte[RawBufferLength];
    TLSOuterRecordBuffer = new byte[MaximumTLSRecordLength];

    Handshake = new ProcessHandshake( DomainName );

    Client = new TcpClient();
    Client.ReceiveTimeout = 15 * 1000;
    Client.SendTimeout = 15 * 1000;
    LastReadWriteTime = new ECTime();
    LastReadWriteTime.SetToNow();
    CryptoRand = new RNGCryptoServiceProvider();
    }


  internal DomainX509Record GetX509Record()
    {
    if( Handshake == null )
      return null;

    return Handshake.GetX509Record();
    }


  internal void FreeEverything()
    {
    if( NetStream != null )
      {
      NetStream.Close();
      NetStream = null;
      }
        
    if( Client != null )
      {
      Client.Close();
      Client = null;
      }
    }



  internal string GetStatusString()
    {
    string Result = StatusString + Handshake.GetStatusString();
    StatusString = "";
    return Result;
    }



  internal double GetLastReadWriteTimeSecondsToNow()
    {
    return LastReadWriteTime.GetSecondsToNow();
    }



  internal bool IsShutDown()
    {
    if( Client == null )
      return true;

    if( !Client.Connected )
      {
      FreeEverything();
      return true;
      }

    return false;
    }



  private int GetAvailable()
    {
    if( IsShutDown())
      return 0;

    try
    {
    return Client.Available;
    }
    catch( Exception )
      {
      FreeEverything();
      return 0;
      }
    }



  internal bool Connect( string ServerIP, int ServerPort )
    {
    try
    {
    Client.Connect( ServerIP, ServerPort );
    }
    catch( Exception Except )
      {
      StatusString += "Could not connect to the server: " + ServerIP + "\r\n";
      StatusString += Except.Message + "\r\n";
      return false;
      }

    // Is this delay needed for a bug?  So Client.GetStream() works?
    // Or is that only for older versions of .NET?
    Thread.Sleep( 100 );

    try
    {
    NetStream = Client.GetStream();
    }
    catch( Exception Except )
      {
      StatusString += "Could not connect to the server (2): " + ServerIP + "\r\n";
      StatusString += Except.Message + "\r\n";
      NetStream = null;
      return false;
      }

    LastReadWriteTime.SetToNow();
    return true;
    }




  private bool WaitForData()
    {
    try
    {
    // Wait while data is not yet here.
    if( DataIsAvailable() )
      return true;

    Thread.Sleep( 100 );

    if( DataIsAvailable() )
      return true;

    return false;

    }
    catch
      {
      return false;
      }
    }




  private bool DataIsAvailable()
    {
    if( NetStream == null )
      return false;
    
    try
    {
    if( NetStream.DataAvailable )
      return true;

    return false;

    }
    catch
      {
      FreeEverything();
      return false;
      }
    }




  internal bool SendBuffer( byte[] Buffer )
    {
    if( IsShutDown())
      return false;

    if( NetStream == null )
      {
      StatusString += "NetStream is null in SendBuffer().";
      return false;
      }

    try
    {
    NetStream.Write( Buffer, 0, Buffer.Length );
    }
    catch
      {
      StatusString += "Could not send Buffer.";
      return false;
      }
      
    LastReadWriteTime.SetToNow();
    return true;
    }



  internal bool ProcessOuterMessages()
    {
    // if( IsShutDown())
      // return false;

    ReadToRawBuffer();
    if( ReadNextOuterTLSRecord())
      {
      // There is a record to process.
      if( ProcessOneOuterTLSRecord())
        {

        }
      }

    return true;
    }



  private bool ReadToRawBuffer()
    {
    if( IsShutDown())
      return false;

    try
    {
    if( 0 == GetAvailable())
      return true; // No error.

    if( NetStream == null )
      return false; // NetStream = Client.GetStream();
      
    if( !DataIsAvailable() )
      return true;

    byte[] TempRawData = new byte[RawBufferLength];
    int BytesRead = NetStream.Read( TempRawData, 0, TempRawData.Length );
    if( BytesRead == 0 )
      return true; // Nothing to read.

    LastReadWriteTime.SetToNow();

    // Notice that _anything_ that a hacker sends is added here.
    for( int Count = 0; Count < BytesRead; Count++ )
      {
      RawBuffer[RawBufferEnd] = TempRawData[Count];
      RawBufferEnd++;
      if( RawBufferEnd >= RawBufferLength )
        RawBufferEnd = 0; // Wrap around in a circle.

      if( RawBufferEnd == RawBufferStart )
        {
        StatusString += "The RawBuffer overflowed in CustomerTLSSocket.";
        // To Do: Send an alert to gracefully end it.
        FreeEverything(); // Close this connection.
        return false;
        }
      }

    return true;

    }
    catch( Exception Except )
      {
      StatusString += "Exception in CustomerTLSClient.ReadToRawBuffer():";
      StatusString += Except.Message;
      FreeEverything();
      return false;
      }
    }



  private bool IsValidContentType( int ToCheck )
    {
    if( (ToCheck == 0x14) || // 20 ChangeCipherSpec
        (ToCheck == 0x15) || // 21 Alert
        (ToCheck == 0x16) || // 22 Handshake
        (ToCheck == 0x17) || // 23 Application
        (ToCheck == 0x18))   // 24 Heartbeat (As in the infamous Heartbleed bug.)
      return true;
    else
      return false;

    }



  private bool ReadNextOuterTLSRecord()
    {
    try
    {
    // Return true if a new record is ready.
    if( RawBufferEnd == RawBufferStart )
      return false; // Nothing to read.

    int HowManyInRawBuffer = RawBufferEnd - RawBufferStart;
    if( HowManyInRawBuffer < 0 ) // If RawBufferEnd wrapped around.
      HowManyInRawBuffer = (RawBufferEnd + RawBufferLength) - RawBufferStart;

    if( HowManyInRawBuffer < 5 )
      {
      // It doesn't even have the length yet.
      return false;
      }

    StatusString += "RawBufferStart: " + RawBufferStart.ToString() + "\r\n";
    StatusString += "RawBufferEnd: " + RawBufferEnd.ToString() + "\r\n";
    StatusString += "HowManyInRawBuffer: " + HowManyInRawBuffer.ToString() + "\r\n";

    int TempStart = RawBufferStart;
    TLSOuterRecordBufferLast = 0;

    // It says that the MAC is done on the whole message including the outer
    // header and a sequence number.

    OuterTLSContentType = RawBuffer[TempStart];
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    if( !IsValidContentType( OuterTLSContentType ))
      {
      StatusString += "Received an invalid content type in CustomerTLSClient.ReadNextOuterTLSRecord().\r\n";
      StatusString += "OuterTLSContentType: " + OuterTLSContentType.ToString() + "\r\n";
      // To Do: Send an alert and gracefully shut it down.
      FreeEverything();
      return false;
      }
    else
      {
      StatusString += "Valid OuterTLSContentType: " + OuterTLSContentType.ToString() + "\r\n";
      }

    // This is only written for TLS version 1.2 (RFC 5246), which means that it's
    // version 3.3 in the version major and minor fields.  But it says in the RFC 
    // that "a client that supports multiple versions of TLS may not know what"
    // version will be employed before it receives the ServerHello".  And also 
    // it says "TLS servers compliant with this specification MUST accept any
    // value {03,XX} as the record layer version number for ClientHello."

    OuterTLSVersionMajor = RawBuffer[TempStart];
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    if( OuterTLSVersionMajor != 3 )
      {
      StatusString += "Received an invalid major version number in CustomerTLSClient.ReadNextOuterTLSRecord().\r\n";
      // To Do: Send an alert and gracefully shut it down.
      // "protocol_version" alert?
      FreeEverything();
      return false;
      }

    OuterTLSVersionMinor = RawBuffer[TempStart];
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    // Get the high byte of the length.
    OuterTLSLength = RawBuffer[TempStart];
    OuterTLSLength <<= 8;
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    // Get the low byte of the length.
    // OR it with the high byte.
    OuterTLSLength |= RawBuffer[TempStart];
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    // RFC says length must not exceed 2^14, which is 16,384.
    // But you might have to allow longer lengths (up to 64K) to be compatible
    // with some bad implementations that are out there.
    if( OuterTLSLength > MaximumTLSRecordLength )
      {
      StatusString += "Received an invalid message length in CustomerTLSClient.ReadNextOuterTLSRecord().\r\n";
      // To Do: Send an alert and gracefully shut it down.
      // "protocol_version" alert?
      FreeEverything();
      return false;
      }

    if( OuterTLSLength < 0 )
      {
      StatusString += "OuterTLSLength < 0. This is a bug in CustomerTLSClient.ReadNextOuterTLSRecord().\r\n";
      return false;
      }

    // if( OuterTLSLength < 1 )
    // Some messages, depending on the content type, aren't supposed to be empty.

    if( OuterTLSLength > HowManyInRawBuffer )
      {
      // Not a full message to process yet, so return false.
      StatusString += "Haven't received all the bytes yet.\r\n";
      return false;
      }

    if( TLSOuterRecordBufferLast != 5 )
      {
      StatusString += "TLSOuterRecordBufferLast != 5. This is a bug in CustomerTLSClient.ReadNextOuterTLSRecord().\r\n";
      return false;
      }

    // This is a limited form of a while( it's not at the end yet ).
    for( int Count = 0; Count < RawBufferLength; Count++ )
      {
      TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
      TLSOuterRecordBufferLast++;
      TempStart++;
      if( TempStart >= RawBufferLength )
        TempStart = 0; // Wrap it around.

      // It's the 5 initial header bytes plus the length it says it is.
      if( TLSOuterRecordBufferLast >= (OuterTLSLength + 5) )
        break; // It got past the end of the record.

      }

    // Move RawBufferStart to the new starting position for the next message.
    // If it never got this far then RawBufferStart hasn't changed and it would
    // start reading next time from the same beginning position.
    RawBufferStart = TempStart;

    // Since HowManyInRawBuffer was checked against OuterTLSLength, this can't
    // have RawBufferStart going past RawBufferEnd.  And OuterTLSLength is at
    // least a reasonable number.  But if there are no more messages that came
    // in then RawBufferStart would be equal to RawBufferEnd here. Otherwise
    // there's another message, or part of a message, still in the raw buffer.

    // There is now a new raw and unchecked message in the TLSOuterRecordBuffer.
    // It is totally unverified and it could have been sent by a hacker.
    // There is no telling what's in it at this point.
    StatusString += "OuterTLSLength is: " + OuterTLSLength.ToString() + "\r\n";
    StatusString += "\r\n";
    return true;

    // Some servers don't put the handshake messages in separate
    // outer TLS messages like some other servers do.
    // Some will combine these inner messages in to one outer message.
    // ServerHello
    // Certificate
    // ServerHelloDone

    }
    catch( Exception Except )
      {
      StatusString += "Exception in CustomerTLSClient.ReadNextOuterTLSRecord():\r\n";
      StatusString += Except.Message;
      FreeEverything();
      return false;
      }
    }



  private bool ProcessOneOuterTLSRecord()
    {
    try
    {
    string ShowS = "Received an outer message of type: ";
    if( OuterTLSContentType == 20 )
      {
      StatusString += ShowS + "ChangeCipherSpec\r\n";
      return true; // ProcessChangeCipherSpecMessage();
      }

    if( OuterTLSContentType == 21 )
      {
      StatusString += ShowS + "Alert\r\n";
      return true; // ProcessAlertMessage();
      }

    if( OuterTLSContentType == 22 )
      {
      StatusString += ShowS + "Handshake\r\n";
      return ProcessHandshakeMessages();
      }

    if( OuterTLSContentType == 23 )
      {
      StatusString += ShowS + "Application\r\n";
      return true; // ProcessApplicationMessage();
      }

    if( OuterTLSContentType == 24 )
      {
      StatusString += ShowS + "Heartbeat\r\n";
      }

    StatusString += "This is a bug. It should never happen since the content type was already checked.\r\n";
    StatusString += "OuterTLSContentType is: " + OuterTLSContentType.ToString() + "\r\n";
    FreeEverything();
    return false;
    }
    catch( Exception Except )
      {
      StatusString += "Exception in CustomerTLSClient.ProcessOneOuterTLSRecord():\r\n";
      StatusString += Except.Message + "\r\n";
      FreeEverything();
      return false;
      }
    }



  private bool ProcessHandshakeMessages()
    {
    int Index = 5; // After the TLS outer header.

    // There can be multiple inner messages of the same type in one outer TLS message.
    // While() there are still messages to process, but don't do it forever.
    for( int Count = 0; Count < 100; Count++ ) // There won't be 100 messages in it.
      {
      if( Index >= TLSOuterRecordBufferLast )
        {
        StatusString += "No more inner messages to process.\r\n";
        return true; // It's done.
        }

      int MessageType = TLSOuterRecordBuffer[Index];
      switch( MessageType )
        {
        case 0: // HelloRequest
          StatusString += "Received a HelloRequest.\r\n";
          return true;

        case 1: // ClientHello
          StatusString += "It should not be getting a ClientHello on this side.\r\n";
          return true;

        case 2: // ServerHello
          // Get the Index that's at the end of this message.
          Index = Handshake.ProcessServerHello( Index, TLSOuterRecordBuffer, TLSOuterRecordBufferLast );
          if( Index < 1 )
            return true; // Usually it's an error.

          break; // Go around the loop again, where it checks the Index at the top.

        case 4: // NewSessionTicket
          StatusString += "Received a NewSessionTicket.\r\n";
          return true;

        case 11: // Certificate
          Index = Handshake.ProcessX509Certificates( Index, TLSOuterRecordBuffer, TLSOuterRecordBufferLast );
          if( Index < 1 )
            return true; // Usually it's an error.

          break; // Go around the loop again, where it checks the Index at the top.

        case 12: // ServerKeyExchange
          StatusString += "Received a ServerKeyExchange.\r\n";
          return true;

        case 13: // CertificateRequest
          StatusString += "Received a CertificateRequest.\r\n";
          return true;

        case 14: // ServerHelloDone
          StatusString += "Received a ServerHelloDone.\r\n";
          return true;

        case 15: // CertificateVerify
          StatusString += "Received a CertificateVerify.\r\n";
          return true;

        case 16: // ClientKeyExchange
          StatusString += "Received a ClientKeyExchange.\r\n";
          return true;

        case 20: // Finished
          StatusString += "Received a Finished.\r\n";
          return true;

        case 21: // CertificateUrl
          StatusString += "Received a CertificateUrl.\r\n";
          return true;

        case 22: // CertificateStatus
          StatusString += "Received a CertificateStatus.\r\n";
          return true;

        default:
          StatusString += "It didn't find a matching handshake type.\r\n";
          return false;

        }
      }

    StatusString += "It went around 100 times and never exited correctly.";
    return false;
    }




  internal bool SendCrudeClientHello()
    {
    return Handshake.SendCrudeClientHello( CryptoRand, this );
    }



  }
}

