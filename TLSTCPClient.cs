// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;



// RFC 5246:
// The Transport Layer Security (TLS) Protocol Version 1.2
// https://tools.ietf.org/html/rfc5246

// https://en.wikipedia.org/wiki/Transport_Layer_Security

// X.509:
// https://en.wikipedia.org/wiki/X.509



namespace ExampleServer
{
  class TLSTCPClient
  {
  private MainForm MForm;
  private NetworkStream NetStream;
  private TcpClient Client;
  private ECTime LastTransactTime;
  private IAsyncResult AsyncResult = null;
  // The plus 5 is for the outer TLS header.
  private const int MaximumTLSRecordLength = 16384; // RFC says length must not exceed 2^14.
  private string RemoteAddress = "";
  // private ECTime StartTime;
  private const int RawBufferLength = MaximumTLSRecordLength * 4;
  private byte[] RawBuffer; // A circular buffer.
  private int RawBufferStart = 0;
  private int RawBufferEnd = 0;
  private byte[] TLSOuterRecordBuffer;
  private int TLSOuterRecordBufferLast = 0;
  private int OuterTLSContentType = 0;
  private int OuterTLSVersionMajor = 0;
  private int OuterTLSVersionMinor = 0;
  private int OuterTLSLength = 0;
  private bool HandshakeCompleted = false;
  private int HandshakeMessageNumber = 0;



  private TLSTCPClient()
    {
    }




  internal TLSTCPClient( MainForm UseForm, TcpClient UseClient, string UseAddress )
    {
    MForm = UseForm;
    RemoteAddress = UseAddress;

    RawBuffer = new byte[RawBufferLength];
    TLSOuterRecordBuffer = new byte[MaximumTLSRecordLength + 5]; // Plus 5 for the header.

    LastTransactTime = new ECTime();
    LastTransactTime.SetToNow();
    
    if( UseClient == null )
      Client = new TcpClient();
    else
      Client = UseClient;

    try
    {
    NetStream = Client.GetStream();
    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in Creating the NetStream:" );
      MForm.ShowTLSListenerFormStatus( Except.Message );
      NetStream = null;
      return;
      }

    Client.ReceiveTimeout = 3 * 1000;
    Client.SendTimeout = 3 * 1000;
    Client.SendBufferSize = 1024 * 32;
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




  internal ulong GetLastTransactTimeIndex()
    {
    return LastTransactTime.GetIndex();
    }



  internal double GetLastTransactSecondsToNow()
    {
    return LastTransactTime.GetSecondsToNow();
    }



  internal bool IsProcessingInBackground()
    {
    if( Client == null )
      return false;

    // AsyncResult doesn't get created unless an Async transfer gets started.
    if( AsyncResult == null )
      return false;

    // This is checked whenever it checks to see if it is shut down, which is very often.
    if( AsyncResult.IsCompleted )
      {
      AsyncResult = null;
      // Then give it time before it gets shut down.
      LastTransactTime.SetToNow();
      return false;
      }

    return true;
    }



  internal bool IsShutDown()
    {
    if( Client == null )
      return true;

    if( IsProcessingInBackground())
      return false;

    // This only knows if it's connected as of the last socket operation.
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
      return false;
      }
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

    LastTransactTime.SetToNow();

    // Notice that _anything_ that a hacker sends is added here.
    for( int Count = 0; Count < BytesRead; Count++ )
      {
      RawBuffer[RawBufferEnd] = TempRawData[Count];
      RawBufferEnd++;
      if( RawBufferEnd >= RawBufferLength )
        RawBufferEnd = 0; // Wrap around in a circle.

      if( RawBufferEnd == RawBufferStart )
        {
        // RawBufferLength is a constant set at compile time and it could be
        // something that would adapt to changes dynamically, or to an
        // administrator setting configuration options, but the question
        // is _why_ did it overflow.  Is it a Denial of Service attack?  Is it
        // because you need bigger EC2 instances with more processing power
        // because it can't keep up with a growing amount of customers? So
        // adapting dynamically for this buffer size is a complicated question.

        MForm.ShowTLSListenerFormStatus( "The RawBuffer overflowed in TLSTCPSocket." );
        // To Do: Send an alert to gracefully end it.
        FreeEverything(); // Close this connection.
        return false;
        }
      }

    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in TLSTCPClient.ReadToRawBuffer():" );
      MForm.ShowTLSListenerFormStatus( Except.Message );
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
        (ToCheck == 0x18))   // 24 Heartbeat
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

    MForm.ShowTLSListenerFormStatus( "RawBufferStart: " + RawBufferStart.ToString());
    MForm.ShowTLSListenerFormStatus( "RawBufferEnd: " + RawBufferEnd.ToString());
    MForm.ShowTLSListenerFormStatus( "HowManyInRawBuffer: " + HowManyInRawBuffer.ToString());

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
      MForm.ShowTLSListenerFormStatus( "Received an invalid content type in TLSTCPClient.ReadNextOuterTLSRecord()." );
      MForm.ShowTLSListenerFormStatus( "OuterTLSContentType: " + OuterTLSContentType.ToString());
      // To Do: Send an alert and gracefully shut it down.
      FreeEverything();
      return false;
      }
    else
      {
      MForm.ShowTLSListenerFormStatus( "Valid OuterTLSContentType: " + OuterTLSContentType.ToString());
      }

    // This is only written for TLS version 1.2 (RFC 5246), which means that it's
    // version 3.3 in the version major and minor fields.  But it says in the RFC 
    // that "a client that supports multiple versions of TLS may not know what
    // version will be employed before it receives the ServerHello".  And also 
    // it says "TLS servers compliant with this specification MUST accept any
    // value {03,XX} as the record layer version number for ClientHello."

    // So in this code only the major part of the version number is checked.
    OuterTLSVersionMajor = RawBuffer[TempStart];
    TLSOuterRecordBuffer[TLSOuterRecordBufferLast] = RawBuffer[TempStart];
    TLSOuterRecordBufferLast++;
    TempStart++;
    if( TempStart >= RawBufferLength )
      TempStart = 0; // Wrap it around.

    if( OuterTLSVersionMajor != 3 )
      {
      MForm.ShowTLSListenerFormStatus( "Received an invalid major version number in TLSTCPClient.ReadNextOuterTLSRecord()." );
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
    if( OuterTLSLength > MaximumTLSRecordLength )
      {
      MForm.ShowTLSListenerFormStatus( "Received an invalid message length in TLSTCPClient.ReadNextOuterTLSRecord()." );
      // To Do: Send an alert and gracefully shut it down.
      // "protocol_version" alert?
      FreeEverything();
      return false;
      }

    // If this was written in Java you'd have to worry about the sign of the byte
    // when it gets assigned in the integer value.  But the way this is done it can't
    // be less than zero here.
    if( OuterTLSLength < 0 )
      {
      MForm.ShowTLSListenerFormStatus( "OuterTLSLength < 0. This is a bug in TLSTCPClient.ReadNextOuterTLSRecord()." );
      return false;
      }

    // if( OuterTLSLength < 1 )
    // Some messages, depending on the content type, aren't supposed to be empty.

    if( OuterTLSLength > HowManyInRawBuffer )
      {
      // Not a full message to process yet, so return false.
      MForm.ShowTLSListenerFormStatus( "Haven't received all the bytes yet." );
      return false;
      }

    if( TLSOuterRecordBufferLast != 5 )
      {
      MForm.ShowTLSListenerFormStatus( "TLSOuterRecordBufferLast != 5. This is a bug in TLSTCPClient.ReadNextOuterTLSRecord()." );
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
    MForm.ShowTLSListenerFormStatus( "OuterTLSLength is: " + OuterTLSLength.ToString());
    MForm.ShowTLSListenerFormStatus( " " );
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in TLSTCPClient.ReadNextOuterTLSRecord():" );
      MForm.ShowTLSListenerFormStatus( Except.Message );
      FreeEverything();
      return false;
      }
    }



  private bool ProcessOneOuterTLSRecord()
    {
    try
    {
    string ShowS = "Received a totally unverified outer message of type: ";
    if( OuterTLSContentType == 20 )
      {
      MForm.ShowTLSListenerFormStatus( ShowS + "ChangeCipherSpec" );
      return ProcessChangeCipherSpecMessage();
      }

    if( OuterTLSContentType == 21 )
      {
      MForm.ShowTLSListenerFormStatus( ShowS + "Alert" );
      return ProcessAlertMessage();
      }

    if( OuterTLSContentType == 22 )
      {
      MForm.ShowTLSListenerFormStatus( ShowS + "Handshake" );
      return ProcessHandshakeMessages();
      }

    if( OuterTLSContentType == 23 )
      {
      MForm.ShowTLSListenerFormStatus( ShowS + "Application" );
      return ProcessApplicationMessage();
      }

    if( OuterTLSContentType == 24 )
      {
      MForm.ShowTLSListenerFormStatus( ShowS + "Heartbeat" );
      }

    MForm.ShowTLSListenerFormStatus( "This is a bug. It should never happen since the content type was already checked." );
    MForm.ShowTLSListenerFormStatus( "OuterTLSContentType is: " + OuterTLSContentType.ToString());
    FreeEverything();
    return false;
    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in TLSTCPClient.ProcessOneOuterTLSRecord():" );
      MForm.ShowTLSListenerFormStatus( Except.Message );
      FreeEverything();
      return false;
      }
    }



  private bool ProcessChangeCipherSpecMessage()
    {
    return true;
    }


  private bool ProcessAlertMessage()
    {
    return true;
    }



  // If some of this looks like half-baked code that hardly works, that's
  // because it is.  It is exploratory.  It's just to see how TLS works.


  private bool ProcessHandshakeMessages()
    {
    int Index = 5;

    // There can be multiple inner messages of the same type in one outer TLS message.
    // While() there are still messages to process, but don't do it forever.
    // for( int Count = 0; Count < 100; Count++ )
      // {

    int MessageType = TLSOuterRecordBuffer[Index];
    switch( MessageType )
      {
      case 0: // HelloRequest
        return true;

      case 1: // ClientHello
        Index = ProcessHandshakeClientHello( Index );
        if( Index < 0 )
          return true; // No more messages.

        break;

      case 2: // ServerHello
        return true;

      case 4: // NewSessionTicket
        return true;

      case 11: // Certificate
        return true;

      case 12: // ServerKeyExchange
        return true;

      case 13: // CertificateRequest
        return true;

      case 14: // ServerHelloDone
        return true;

      case 15: // CertificateVerify
        return true;

      case 16: // ClientKeyExchange
        return true;

      case 20: // Finished
        return true;

      case 21: // CertificateUrl
        return true;

      case 22: // CertificateStatus
        return true;

      }
      // }

    return true;
    }



  private int ProcessHandshakeClientHello( int Index )
    {
    try
    {
    int OriginalIndex = Index;
    MForm.ShowTLSListenerFormStatus( " " );
    MForm.ShowTLSListenerFormStatus( "Processing ClientHello message." );
    MForm.ShowTLSListenerFormStatus( "Index is: " + Index.ToString());

    // Check if it's at least a reasonable number.
    if( TLSOuterRecordBufferLast < (Index + (32 + 4 + 4)) )
      {
      MForm.ShowTLSListenerFormStatus( "TLSOuterRecordBufferLast < (Index + 32) in ProcessHandshakeClientHello()." );
      return -1;
      }

    int MessageType = TLSOuterRecordBuffer[Index];
    Index++;
    if( MessageType != 1 )
      {
      MForm.ShowTLSListenerFormStatus( "This is a bug. MessageType != 1." );
      return -1;
      }

    MForm.ShowTLSListenerFormStatus( "MessageType: " + MessageType.ToString());

    // "When a client first connects to a server, it is required to send
    // the ClientHello as its first message."
    HandshakeMessageNumber++;
    if( HandshakeMessageNumber != 1 )
      {
      MForm.ShowTLSListenerFormStatus( "The ClientHello message is not the first message being received." );
      return -1;
      }

    int Length = TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Index++;

    if( TLSOuterRecordBufferLast < (Index + Length) )
      {
      MForm.ShowTLSListenerFormStatus( "TLSOuterRecordBufferLast < (Index + Length)." ); 
      return -1;
      }

    // This MoveTo gets returned as the index for the start of the 
    // next inner message.
    int MoveTo = OriginalIndex + Length;
    MForm.ShowTLSListenerFormStatus( "MoveTo is: " + MoveTo.ToString());

    MForm.ShowTLSListenerFormStatus( "Length is: " + Length.ToString());
    if( TLSOuterRecordBufferLast < (Index + Length) )
      {
      MForm.ShowTLSListenerFormStatus( "Length is: " + Length.ToString());
      return -1;
      }

    // "Implementations MUST NOT send zero-length fragments of Handshake,
    // Alert, or ChangeCipherSpec content types."
    if( Length < 1 )
      return -1;

    int VersionMajor = TLSOuterRecordBuffer[Index];
    Index++;
    int VersionMinor = TLSOuterRecordBuffer[Index];
    Index++;

    MForm.ShowTLSListenerFormStatus( "VersionMajor: " + VersionMajor.ToString());
    MForm.ShowTLSListenerFormStatus( "VersionMinor: " + VersionMinor.ToString());

    byte[] RandomBytes = new byte[32];
    // Unix time is the first four bytes, then the other 28 are random.
    // "Clocks are not required to be set correctly by the basic TLS protocol."
    for( int Count = 0; Count < 32; Count++ )
      {
      RandomBytes[Count] = TLSOuterRecordBuffer[Index];
      Index++;
      }

    int SessionIDLength = TLSOuterRecordBuffer[Index];
    Index++;

    if( SessionIDLength > 32 )
      {
      MForm.ShowTLSListenerFormStatus( "SessionIDLength is > 32: " + SessionIDLength.ToString());
      return -1;
      }

    MForm.ShowTLSListenerFormStatus( "SessionIDLength: " + SessionIDLength.ToString());
    // byte[] SessionIDArray;
    if( SessionIDLength > 0 )
      {
      // Fill up a byte array with the Session ID.
      // SessionIDArray = new byte[SessionIDLength];
      for( int Count = 0; Count < SessionIDLength; Count++ )
        {
        if( Index >= TLSOuterRecordBufferLast )
          {
          MForm.ShowTLSListenerFormStatus( "Index >= TLSOuterRecordBufferLast." );
          return -1;
          }

        // SessionIDArray[Count] = TLSOuterRecordBuffer[Index];
        Index++;
        }
      }

    // Two bytes for this length.
    int CipherSuitesLength = TLSOuterRecordBuffer[Index];
    Index++;
    CipherSuitesLength <<= 8;
    CipherSuitesLength |= TLSOuterRecordBuffer[Index];
    Index++;

    MForm.ShowTLSListenerFormStatus( "CipherSuitesLength: " + CipherSuitesLength.ToString());

    if( CipherSuitesLength  > Length )
      {
      MForm.ShowTLSListenerFormStatus( "CipherSuitesLength  > Length." );
      return -1;
      }

    if( CipherSuitesLength < 2 )
      {
      MForm.ShowTLSListenerFormStatus( "It has to send at least one cipher suite. CipherSuitesLength < 1." );
      return -1;
      }

    if( (CipherSuitesLength & 1) != 0 )
      {
      // It's a list of two-byte values so it can't be an odd number.
      MForm.ShowTLSListenerFormStatus( "CipherSuitesLength can't be an odd number." );
      return -1;
      }

    byte[] CipherSuitesArray = new byte[CipherSuitesLength];
    for( int Count = 0; Count < CipherSuitesLength; Count++ )
      {
      if( Index >= TLSOuterRecordBufferLast )
        {
        MForm.ShowTLSListenerFormStatus( "Index >= TLSOuterRecordBufferLast." );
        return -1;
        }

      // CipherSuitesArray[]: 000
      // CipherSuitesArray[]: 035
      // CipherSuite TLS_RSA_WITH_AES_256_CBC_SHA          = { 0x00,0x35 };

      CipherSuitesArray[Count] = TLSOuterRecordBuffer[Index];
      
      // If it's the first (even) byte in the pair.
      if( (Count & 1) == 0 )
        {
        // This is not true because in RFC 4492 it has things for Elliptic
        // Curve Cryptography (ECC) Cipher Suites.  Those start with 0xC0.
        // if( CipherSuitesArray[Count] != 0 )
          // MForm.ShowTLSListenerFormStatus( "None of the CipherSuite values has a non-zero first byte." );
          
        MForm.ShowTLSListenerFormStatus( " " );
        }

      MForm.ShowTLSListenerFormStatus( "CipherSuitesArray[]: 0x" + CipherSuitesArray[Count].ToString( "X2" ));
      Index++;
      }

    int CompressionMethodsLength = TLSOuterRecordBuffer[Index];
    Index++;
    if( CompressionMethodsLength < 1 )
      {
      // The only one defined so far in this RFC is null, but it should at least
      // have a length of one and show the null value for compression method.
      // TLS 1.3 drops support for compression.
      MForm.ShowTLSListenerFormStatus( "CompressionMethodsLength < 1" );
      return -1;
      }

    MForm.ShowTLSListenerFormStatus( "CompressionMethodsLength: " + CompressionMethodsLength.ToString());

    // Fill up a byte array with the compression methods.
    byte[] CompressionMethodsArray = new byte[CompressionMethodsLength];
    for( int Count = 0; Count < CompressionMethodsLength; Count++ )
      {
      if( Index >= TLSOuterRecordBufferLast )
        {
        MForm.ShowTLSListenerFormStatus( "Index >= TLSOuterRecordBufferLast." );
        return -1;
        }
        
      CompressionMethodsArray[Count] = TLSOuterRecordBuffer[Index];
      Index++;
      }

    if( Index < MoveTo )
      {
      MForm.ShowTLSListenerFormStatus( "This ClientHello message has extensions." );
      }
    else
      {
      MForm.ShowTLSListenerFormStatus( "No extensions in this ClientHello message." );
      return MoveTo;
      }

    // Transport Layer Extensions
    // https://tools.ietf.org/html/rfc6066
    // https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xml

    int ExtensionType = TLSOuterRecordBuffer[Index];
    Index++;
    ExtensionType <<= 8;
    ExtensionType |= TLSOuterRecordBuffer[Index];
    Index++;

    MForm.ShowTLSListenerFormStatus( "ExtensionType: " + ExtensionType.ToString());
    MForm.ShowTLSListenerFormStatus( "ExtensionType hex: 0x" + ExtensionType.ToString( "X2" ));

    MForm.ShowTLSListenerFormStatus( "Index at the end is: " + Index.ToString());

    // Ignore the extension for now:
    return MoveTo;

    // server_name(0)
    // max_fragment_length(1)
    // client_certificate_url(2)
    // trusted_ca_keys(3)
    // truncated_hmac(4)
    // status_request(5)
    // Signature algorithm 13

    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in TLSTCPClient.ProcessHandshakeClientHello():" );
      MForm.ShowTLSListenerFormStatus( Except.Message );
      FreeEverything();
      return -1;
      }
    }



  /*
  // http://www.iana.org/assignments/tls-parameters/tls-parameters.xhtml

  From RFC 5246:

  CipherSuite TLS_NULL_WITH_NULL_NULL               = { 0x00,0x00 };
      CipherSuite TLS_RSA_WITH_NULL_MD5                 = { 0x00,0x01 };
      CipherSuite TLS_RSA_WITH_NULL_SHA                 = { 0x00,0x02 };
      CipherSuite TLS_RSA_WITH_NULL_SHA256              = { 0x00,0x3B };
      CipherSuite TLS_RSA_WITH_RC4_128_MD5              = { 0x00,0x04 };
      CipherSuite TLS_RSA_WITH_RC4_128_SHA              = { 0x00,0x05 };
      CipherSuite TLS_RSA_WITH_3DES_EDE_CBC_SHA         = { 0x00,0x0A };
      CipherSuite TLS_RSA_WITH_AES_128_CBC_SHA          = { 0x00,0x2F };
      CipherSuite TLS_RSA_WITH_AES_256_CBC_SHA          = { 0x00,0x35 };
      CipherSuite TLS_RSA_WITH_AES_128_CBC_SHA256       = { 0x00,0x3C };
      CipherSuite TLS_RSA_WITH_AES_256_CBC_SHA256       = { 0x00,0x3D };

    // Diffie-Hellman is in the RFC too.
  */




  private bool ProcessApplicationMessage()
    {
    if( !HandshakeCompleted )
      {
      MForm.ShowTLSListenerFormStatus( "It should not be receiving application messages if the handshake has not completed." );
      FreeEverything();
      return false;
      }

    return true;
    }

  private bool ProcessHeartbeatMessage()
    {
    return true;
    }


  internal bool WriteBytesAsync( byte[] Bytes )
    {
    // To Do: If it's busy writing in async then put it in an outgoing buffer.
    // if( IsProcessingInBackground())

    if( IsShutDown())
      return false;

    // This can only be called once.
    if( AsyncResult != null )
      {
      MForm.ShowTLSListenerFormStatus( "AsyncResult != null in WriteBytesAsync." );
      return false;
      }

    if( NetStream == null )
      return false;

    // This just means it's a writeable stream.  It's not a test 
    // for the write buffer being ready to write.
    // if( NetStream.CanWrite )

    try
    {
    AsyncResult = NetStream.BeginWrite( Bytes, 
                  0, 
                  Bytes.Length,
                  new AsyncCallback( TLSTCPClient.ProcessAsynchCallback ),
                  NetStream );

    LastTransactTime.SetToNow();
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowTLSListenerFormStatus( "Exception in WriteBytesAsync." );
      MForm.ShowTLSListenerFormStatus( Except.Message );
      FreeEverything();
      return false;
      }
    }




  static void ProcessAsynchCallback( IAsyncResult Result )
    {
    try
    {
    NetworkStream TheStream = (NetworkStream)(Result.AsyncState);
    // Did it send all the bytes?
    TheStream.EndWrite( Result );
    }
    catch( Exception )
      {
      // What?
      }
    }



  internal string GetRemoteAddress()
    {
    return RemoteAddress;
    }




// RFC for Heartbeat:
// https://tools.ietf.org/html/rfc6520




  // http://en.wikipedia.org/wiki/Internet_media_type
  internal void SendGenericWebResponse( byte[] Buffer, ulong ModifiedIndex, ulong UniqueEntity, string ContentType )
    {
    /*
    if( Client == null )
      return;

    try
    {
    // Set the initial UniqueEntity to the current date time index and then just
    // keep incrementing it.
    ECTime RightNow = new ECTime();
    RightNow.SetToNow();

    ECTime ExpireTime = new ECTime();
    ExpireTime.SetToNow();
    ExpireTime.AddSeconds( 120 );

    ECTime ModifiedTime = new ECTime( ModifiedIndex );

    // ETag is an Entity Tag.
    // "An entity tag MUST be unique across all versions of all entities
    // associated with a particular resource."
    string Header = "HTTP/1.1 200 OK\r\n" +
           "Date: " + RightNow.GetHTTPHeaderDateTime() + "\r\n" +
           "Server: Eric Example\r\n" +
           "Last-Modified: " + ModifiedTime.GetHTTPHeaderDateTime() + "\r\n" +
           "ETag: " + UniqueEntity.ToString() + "\r\n" +
           "Accept-Ranges: bytes\r\n" +
           "Content-Length: " + Buffer.Length.ToString() + "\r\n" +
           // "Cache-Control: max-age=5184000
           "Expires: " + ExpireTime.GetHTTPHeaderDateTime() + "\r\n" +
           "Keep-Alive: timeout=5, max=100\r\n" +
           "Connection: Keep-Alive\r\n" +
           "Content-Type: " + ContentType + "\r\n" +
           "\r\n"; // Empty line and then the actual bytes.

    byte[] HeaderBytes = UTF8Strings.StringToBytes( Header );
    if( HeaderBytes == null )
      return;

    byte[] AllSendBytes;

    if( GetIsHeadOnly() )
      AllSendBytes = new byte[HeaderBytes.Length];
    else
      AllSendBytes = new byte[HeaderBytes.Length + Buffer.Length];

    int Where = 0;
    for( int Count = 0; Count < HeaderBytes.Length; Count++ )
      {
      AllSendBytes[Where] = HeaderBytes[Count];
      Where++;
      }

    if( !GetIsHeadOnly() )
      {
      for( int Count = 0; Count < Buffer.Length; Count++ )
        {
        AllSendBytes[Where] = Buffer[Count];
        Where++;
        }
      }

    // This returns immediately.
    WriteBytesAsync( AllSendBytes );

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in SendHTMLOrText():" );
      MForm.ShowStatus( Except.Message );
      }
    */
    }



   // "All the major browsers are planning to stop accepting SHA-1 signatures by 2017."


  }
}
