// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

// http://www.x500standard.com/



namespace ExampleServer
{
  class ProcessHandshake
  {
  private string StatusString = "";
  private DomainX509Record X509Record;
  private string DomainName = "";


  private ProcessHandshake()
    {
    }


  internal ProcessHandshake( string UseDomainName )
    {
    DomainName = UseDomainName;
    }



  internal DomainX509Record GetX509Record()
    {
    return X509Record;
    }



  internal string GetStatusString()
    {
    string Result = StatusString;
    if( X509Record != null )
      Result += X509Record.GetStatusString();

    StatusString = "";
    return Result;
    }




  internal int ProcessX509Certificates( int Index, byte[] TLSOuterRecordBuffer, int TLSOuterRecordBufferLast )
    {
    StatusString += "Top of Certificate.\r\n";

    // This is a chain of certificates.
    int OriginalIndex = Index;
    StatusString += "\r\n";
    StatusString += "Processing Certificate message.\r\n";
    StatusString += "Index is: " + Index.ToString() + "\r\n";

    int MessageType = TLSOuterRecordBuffer[Index];
    Index++;
    if( MessageType != 11 )
      {
      StatusString += "This is a bug. MessageType != 11.\r\n";
      ShowBytesInBuffer( TLSOuterRecordBuffer, TLSOuterRecordBufferLast );
      return -1;
      }

    StatusString += "MessageType: " + MessageType.ToString() + "\r\n";

    int Length = TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Index++;

    StatusString += "Length is: " + Length.ToString() + "\r\n";
    if( TLSOuterRecordBufferLast < (Index + Length) )
      {
      StatusString += "TLSOuterRecordBufferLast < (Index + Length). Length is: " + Length.ToString() + "\r\n";
      ShowBytesInBuffer( TLSOuterRecordBuffer, TLSOuterRecordBufferLast );
      return -1;
      }

    // "Implementations MUST NOT send zero-length fragments of Handshake,
    // Alert, or ChangeCipherSpec content types."
    if( Length < 1 )
      {
      StatusString += "The length of this message was less than one. Length is: " + Length.ToString() + "\r\n";
      ShowBytesInBuffer( TLSOuterRecordBuffer, TLSOuterRecordBufferLast );
      throw( new Exception( "Length less than one." ));
      // return -1;
      }

    // This MoveTo gets returned as the index for the start of the 
    // next inner message.
    // The message type plus 3 bytes for the length is 4 bytes for the
    // header.  So add the 4 bytes here.
    int MoveTo = OriginalIndex + Length + 4;
    StatusString += "MoveTo is: " + MoveTo.ToString() + "\r\n";

    byte[] X509Buffer = new byte[Length];
    for( int Count = 0; Count < Length; Count++ )
      {
      X509Buffer[Count] = TLSOuterRecordBuffer[Index];
      Index++;
      }

    X509Record = new DomainX509Record( DomainName );
    X509Record.ParseAndAddOneCertificateList( X509Buffer );
    StatusString += X509Record.GetStatusString();

    StatusString += "Index after loop is: " + Index.ToString() + "\r\n";
    return -1;
    }


  private void ShowBytesInBuffer( byte[] Buffer, int Last )
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < Last; Count++ )
      {
      // 127 is the DEL character to Delete something.
      if( (Buffer[Count] >= 32) && (Buffer[Count] < 127))
        {
        SBuilder.Append( "ASCII: " + Char.ToString( (char)Buffer[Count] ) );
        }
      else
        {
        int ToShow = Buffer[Count];
        SBuilder.Append( "\r\nByte: " + ToShow.ToString() + "\r\n" );
        }
      }

    StatusString += SBuilder.ToString();
    }



  internal int ProcessServerHello( int Index, byte[] TLSOuterRecordBuffer, int TLSOuterRecordBufferLast )
    {
    try
    {
    int OriginalIndex = Index;
    StatusString += "\r\n";
    StatusString += "Processing ServerHello message.\r\n";
    StatusString += "Index is: " + Index.ToString() + "\r\n";

    // Check if it's at least a reasonable number.  How long is this?
    // It says in the RFC that "The server may return an empty session_id to
    // indicate that the session will not be cached and therefore cannot be
    // resumed."  So it could have a length of zero.
    // if( TLSOuterRecordBufferLast < What?) )
      // {
      // StatusString += "TLSOuterRecordBufferLast < (Index + the other parts) in ProcessHandshakeServerHello().";
      // return -1;
      // }

    int MessageType = TLSOuterRecordBuffer[Index];
    Index++;
    if( MessageType != 2 )
      {
      StatusString += "This is a bug. MessageType != 2.\r\n";
      return -1;
      }

    StatusString += "MessageType: " + MessageType.ToString() + "\r\n";

    // HandshakeMessageNumber++;

    int Length = TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Length <<= 8;
    Index++;
    Length |= TLSOuterRecordBuffer[Index];
    Index++;

    StatusString += "Length is: " + Length.ToString() + "\r\n";
    if( TLSOuterRecordBufferLast < (Index + Length) )
      {
      StatusString += "TLSOuterRecordBufferLast < (Index + Length). Length is: " + Length.ToString() + "\r\n";
      return -1;
      }

    // "Implementations MUST NOT send zero-length fragments of Handshake,
    // Alert, or ChangeCipherSpec content types."
    if( Length < 1 )
      return -1;

    // This MoveTo gets returned as the index for the start of the 
    // next inner message.
    // The message type plus 3 bytes for the length is 4 bytes for the
    // header.  So add the 4 bytes here.
    int MoveTo = OriginalIndex + Length + 4;
    StatusString += "MoveTo is: " + MoveTo.ToString() + "\r\n";

    // The server says what version of TLS it wants to use.
    int VersionMajor = TLSOuterRecordBuffer[Index];
    Index++;
    int VersionMinor = TLSOuterRecordBuffer[Index];
    Index++;

    StatusString += "VersionMajor: " + VersionMajor.ToString() + "\r\n";
    StatusString += "VersionMinor: " + VersionMinor.ToString() + "\r\n";

    if( VersionMajor != 3 )
      {
      StatusString += "The server didn't respond with major version 3.\r\n";
      return -1;
      }

    if( VersionMinor != 3 )
      {
      // So negotiate a lower version.  Not too low though.  (Too weak.)
      StatusString += "The server can't do TLS 1.2. It didn't respond with version 3.3.\r\n";
      return -1;
      }

    // Unix time is the first four bytes, then the other 28 are random.
    // "Clocks are not required to be set correctly by the basic TLS protocol."
    byte[] RandomBytes = new byte[32];
    for( int Count = 0; Count < 32; Count++ )
      {
      RandomBytes[Count] = TLSOuterRecordBuffer[Index];
      Index++;
      }

    int SessionIDLength = TLSOuterRecordBuffer[Index];
    Index++;

    if( SessionIDLength > 32 )
      {
      StatusString += "SessionIDLength is > 32: " + SessionIDLength.ToString() + "\r\n";
      return -1;
      }

    StatusString += "SessionIDLength: " + SessionIDLength.ToString() + "\r\n";
    // byte[] SessionIDArray;
    if( SessionIDLength > 0 )
      {
      // Fill up a byte array with the Session ID.
      // SessionIDArray = new byte[SessionIDLength];

      // What if SessionIDLength wasn't checked here?  Where would the Index go?
      for( int Count = 0; Count < SessionIDLength; Count++ )
        {
        if( Index >= TLSOuterRecordBufferLast )
          {
          StatusString += "Index >= TLSOuterRecordBufferLast.\r\n";
          return -1;
          }

        // SessionIDArray[Count] = TLSOuterRecordBuffer[Index];
        Index++;
        }
      }

    // There is no CipherSuiteLength for the ServerHello.
    // It's just the two bytes for the value of the Cipher suite.
    
    int CipherSuiteHigh = TLSOuterRecordBuffer[Index];
    Index++;
    int CipherSuiteLow = TLSOuterRecordBuffer[Index];
    Index++;

    StatusString += "CipherSuite is: 0x" + CipherSuiteHigh.ToString( "X2" ) +
                    ", 0x" + CipherSuiteLow.ToString( "X2" ) + "\r\n";

    int CompressionMethod = TLSOuterRecordBuffer[Index];
    Index++;

    // That's it unless there are extensions.

    if( CompressionMethod != 0 )
      {
      StatusString += "The Compression Method is not null.\r\n";
      return -1;
      }

    StatusString += "Index: " + Index.ToString() + "\r\n";
    StatusString += "MoveTo: " + MoveTo.ToString() + "\r\n";

    if( Index > MoveTo )
      {
      StatusString += "Index > MoveTo. This can't be right.\r\n";
      return -1;
      }

    if( Index < MoveTo )
      {
      StatusString += "This ServerHello message has extensions.\r\n";
      }
    else
      {
      // Then it must be equal here.
      StatusString += "No extensions in this ServerHello message.\r\n";
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

    StatusString += "ExtensionType: " + ExtensionType.ToString() + "\r\n";
    StatusString += "ExtensionType hex: 0x" + ExtensionType.ToString( "X2" ) + "\r\n";

    StatusString += "Index at the end is: " + Index.ToString() + "\r\n";

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
      StatusString += "Exception in CustomerTLSClient.ProcessHandshakeServerHello():\r\n";
      StatusString += Except.Message + "\r\n";
      return -1;
      }
    }



  internal bool SendCrudeClientHello( RNGCryptoServiceProvider CryptoRand, CustomerTLSClient CustomerTLS )
    {
    try
    {
    int LengthOfOuterMessage = 52 - 5;
    byte[] ToSendBuf = new byte[LengthOfOuterMessage + 5];

    // The first five bytes are the outer TLS record.
    ToSendBuf[0] = 22; // Content type is Handshake
    ToSendBuf[1] = 3;  // Version Major   TLS version 1.2 is version 3.3 (of SSL).
    ToSendBuf[2] = 3;  // Version Minor
    ToSendBuf[3] = (byte)(LengthOfOuterMessage >> 8);
    ToSendBuf[4] = (byte)(LengthOfOuterMessage);

    // Start of the inner ClientHello message.
    ToSendBuf[5] = 1; // Message type 1 is a  ClientHello message.

    int LengthOfClientHelloMessage = LengthOfOuterMessage - 4;
    ToSendBuf[6] = (byte)(LengthOfClientHelloMessage >> 16);
    ToSendBuf[7] = (byte)(LengthOfClientHelloMessage >> 8);
    ToSendBuf[8] = (byte)(LengthOfClientHelloMessage);
    ToSendBuf[9] = 3;   // Version Major
    ToSendBuf[10] = 3;  // Version Minor

    // This part for Unix time is not in TLS 1.3 because there's no point in having it.
    // But it is here in TLS 1.2.
    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    ulong UnixTime = RightNow.ToUnixTime();
    ToSendBuf[11] = (byte)(UnixTime >> 24);
    ToSendBuf[12] = (byte)(UnixTime >> 16);
    ToSendBuf[13] = (byte)(UnixTime >> 8);
    ToSendBuf[14] = (byte)(UnixTime);

    // These bytes have to be cryptographically random.
    // These are used later in generating the master secret and keys, etc.
    byte[] RandomBytes = new byte[28];
    CryptoRand.GetBytes( RandomBytes );

    int Index = 15;
    for( int Count = 0; Count < 28; Count++ )
      {
      ToSendBuf[Index] = RandomBytes[Count];
      Index++;
      }

    // Index is 15 + 28 = 43.
    // StatusString += "Index at the end of Random bytes is: " + Index.ToString() + "\r\n";
    // Index at the end of Random bytes is: 43

    ToSendBuf[43] = 0; // Session ID Length is zero. It's not resuming a session here.

    // A browser sends a lot more cipher suites than this so this is
    // usually a lot longer.  The algorithm you prefer to use should
    // be listed first.  So they are listed in order of preference.
    ToSendBuf[44] = 0; // Cipher Suites Length high byte
    ToSendBuf[45] = 4; // Cipher Suites Length
    ToSendBuf[46] = 0;
    ToSendBuf[47] = 0x35; // TLS_RSA_WITH_AES_256_CBC_SHA    = { 0x00,0x35 };
    ToSendBuf[48] = 0;
    ToSendBuf[49] = 0x3D; // TLS_RSA_WITH_AES_256_CBC_SHA256 = { 0x00,0x3D };
    ToSendBuf[50] = 1; // Compression Methods Length
    ToSendBuf[51] = 0; // Compression Method is null.

    // This message has no extensions, so that's all there is to send.
    return CustomerTLS.SendBuffer( ToSendBuf );

    }
    catch( Exception Except )
      {
      StatusString += "Exception in SendCrudeClientHello()\r\n";
      StatusString += Except.Message + "\r\n";
      return false;
      }
    }




  }
}

