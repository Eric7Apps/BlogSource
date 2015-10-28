// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// PKCS Public Key Cryptography Standards
// https://en.wikipedia.org/wiki/PKCS


// PKCS 1: RSA Cryptography Specifications Version 2.0
// http://tools.ietf.org/html/rfc2437

// PKCS 7:
// ftp://ftp.rsasecurity.com/pub/pkcs/ascii/pkcs-7.asc

// X.509:
// https://en.wikipedia.org/wiki/X.509
// http://tools.ietf.org/html/rfc5280

// There is a difference between the X.509 standard and a particular
// _profile_ in that standard.  See:
// https://www.cs.auckland.ac.nz/~pgut001/pubs/x509guide.txt

// ASN.1:
// https://en.wikipedia.org/wiki/Abstract_Syntax_Notation_One

// Distinguished Encoding Rules:
// https://en.wikipedia.org/wiki/Distinguished_Encoding_Rules

// Online Certificate Status Protocol (OCSP): RFC 2560

// "Procedures for identification and encoding of public key materials
// and digital signatures are defined in RFC3279, RFC4055, and
// RFC4491."


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace ExampleServer
{
  class DomainX509Record
  {
  // internal int Rank = 0; // Where it is ranked in popularity.  Google is 1.
  internal string DomainName = "";
  private ECTime ModifyTime;
     // This is the most recent certificate list
     // from the Server Certificate Message.
  private byte[] CertificateList;
  private string StatusString = "";
  private X509ObjectIDNames ObjectIDNames;
  private int MostRecentTagValue = 0;
  private Integer MostRecentIntegerValue;
  // private byte[] MostRecentBitStringValue;
  private string MostRecentObjectIDString = "";
  private IntegerMath IntMath;


  internal DomainX509Record()
    {
    ModifyTime = new ECTime();
    MostRecentIntegerValue = new Integer();
    }


  internal ulong GetModifyTimeIndex()
    {
    return ModifyTime.GetIndex();
    }



  internal string GetStatusString()
    {
    string Result = StatusString;
    StatusString = "";
    return Result;
    }



  internal bool ImportOriginalStringToObject( string Line )
    {
    try
    {
    if( Line.Length < 2 )
      return false;

    string[] SplitS = Line.Split( new Char[] { ',' } );
    if( SplitS.Length < 2 )
      return false;

    // int Field = 0;
    // Rank = Int32.Parse( SplitS[0] );
    DomainName = SplitS[1].Trim().ToLower();

    return true;
    }
    catch( Exception Except )
      {
      StatusString += "Exception in DomainX509Record.StringToObject().\r\n";
      StatusString += Except.Message + "\r\n";
      return false;
      }
    }


  internal string ObjectToString()
    {
    string Result = DomainName + "\t" +
      ModifyTime.GetIndex().ToString() + "\t";
      
    if( CertificateList == null )
      Result += " \t";
    else
      Result += Utility.BytesToLetterString( CertificateList );

    return Result;
    }


  internal void ParseAndAddOneCertificateList( byte[] ToAdd )
    {
    CertificateList = ToAdd;
    if( CertificateList == null )
      return;

    if( CertificateList.Length < 7 )
      {
      StatusString += "The certificate list length is not a reasonable number.\r\n";
      CertificateList = null;
      return;
      }

    StatusString += "Certificate buffer length is: " + CertificateList.Length.ToString() + "\r\n";

    // What is the standard that says what the first six bytes are?
    // The first three bytes are for the length of the whole thing.
    // Each certificate starts with 3 bytes giving its length.

    int LengthAll = CertificateList[0];
    LengthAll <<= 8;
    LengthAll |= CertificateList[1];
    LengthAll <<= 8;
    LengthAll |= CertificateList[2];

    StatusString += "LengthAll: " + LengthAll.ToString() + "\r\n";

    if( LengthAll > CertificateList.Length )
      {
      StatusString += "LengthAll is not right.\r\n";
      return;
      }

    int Index = 3;
    // while( don't do this forever )
    for( int Count = 0; Count < 1000; Count++ )
      {
      int LengthOneCert = CertificateList[Index];
      Index++;
      LengthOneCert <<= 8;
      LengthOneCert |= CertificateList[Index];
      Index++;
      LengthOneCert <<= 8;
      LengthOneCert |= CertificateList[Index];
      Index++;

      StatusString += "LengthOneCert: " + LengthOneCert.ToString() + "\r\n";

      if( LengthOneCert > LengthAll )
        {
        StatusString += "LengthOneCert is not right.\r\n";
        return;
        }

      byte[] OneCertificate = new byte[LengthOneCert];
      for(int CountBuf = 0; CountBuf < LengthOneCert; CountBuf++ )
        {
        OneCertificate[CountBuf] = CertificateList[Index];
        Index++;
        }

      StatusString += "Index after getting one certificate: " + Index.ToString() + "\r\n";

      if( Index >= CertificateList.Length )
        return;
         
      if( !ParseOneCertificate( OneCertificate ))
        return;

      }
    }



  // A certificate from a .cer file could be parsed with this too.
  internal bool ParseOneCertificate( byte[] OneCertificate )
    {
    if( OneCertificate == null )
      return false;

    // See if it's a reasonable number.
    if( OneCertificate.Length < 10 )
      {
      StatusString += "OneCertificate.Length < 10.\r\n";
      return false;
      }

    /*
    The ASN.1 description says:
    Certificate  ::=  SEQUENCE  {
        tbsCertificate       TBSCertificate,
        signatureAlgorithm   AlgorithmIdentifier,
        signatureValue       BIT STRING  }

   TBSCertificate  ::=  SEQUENCE  {
        version         [0]  EXPLICIT Version DEFAULT v1,
        serialNumber         CertificateSerialNumber,
        signature            AlgorithmIdentifier,
        issuer               Name,
        validity             Validity,
        subject              Name,
        subjectPublicKeyInfo SubjectPublicKeyInfo,
        issuerUniqueID  [1]  IMPLICIT UniqueIdentifier OPTIONAL,
                             -- If present, version MUST be v2 or v3
        subjectUniqueID [2]  IMPLICIT UniqueIdentifier OPTIONAL,
                             -- If present, version MUST be v2 or v3
        extensions      [3]  EXPLICIT Extensions OPTIONAL
                             -- If present, version MUST be v3
        }
    */

    int Index = 0;
    // Sequence that represents the whole certificate.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 16 ) // Sequence
      {
      StatusString += "This tag should be a sequence.\r\n";
      return false;
      }

    // Sequence that represents the TBS certificate.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 16 ) // Sequence
      {
      StatusString += "This second tag should be a sequence.\r\n";
      return false;
      }

    // This is a Context Specific tag.  What is this?
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 0 ) // Context specific.
      {
      StatusString += "The context specific tag wasn't there.\r\n";
      return false;
      }

    // This is the X.509 version. This code is only meant to parse
    // version 3 certificates.  Version 1 of X.509 starts at zero, so
    // version 3 has number 2 here.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 2 ) // Integer
      {
      StatusString += "The integer for the X.509 version wasn't there.\r\n";
      return false;
      }

    if( !MostRecentIntegerValue.IsEqualToULong( 2 ))
      {
      StatusString += "The X.509 version wasn't right.\r\n";
      return false;
      }

    // Serial Number.  This might be a 16-byte value, but RFC 5280 says
    // "... serialNumber values up to 20 octets.  Conforming CAs MUST NOT
    // use serialNumber values longer than 20 octets."
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 2 ) // Integer
      {
      StatusString += "The integer for the serial number wasn't there.\r\n";
      return false;
      }

    // Sequence that represents the Signature Algorithm used.
    // "RFCs 3279, 4055, and 4491 list supported signature algorithms"
    // The line of ASN.1 says: signature            AlgorithmIdentifier,
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 16 ) // Sequence
      {
      StatusString += "The signature sequence wasn't there.\r\n";
      return false;
      }

    // AlgorithmIdentifier  ::=  SEQUENCE  {
    //    algorithm               OBJECT IDENTIFIER,
    //    parameters              ANY DEFINED BY algorithm OPTIONAL  }

    // The line of ASN.1 says: signature            AlgorithmIdentifier
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 6 ) // Object Identifier
      {
      StatusString += "The signature object ID wasn't there.\r\n";
      return false;
      }

    // When I sent the ClientHello message I only specified that this client
    // can understand RSA with AES encryption, so the server has to pick from
    // only the ones I list that this client can support.
    // So it's returning an RSA type of certificate here.
    // If your client was supporting more encryption protocols then this
    // part would be more complicated and the server would return a wider
    // variety of things here, including possibly some parameters for
    // the next tag that follows this one.
    // StringValue is: 1.2.840.113549.1.1.5
    // OID Name is: RSA_SHA1RSA RFC 2437, 3370

    // This null is for the parameters associated with the signature algorithm.
    // There are none here, so it's null.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 5 ) // Null tag
      {
      StatusString += "The null parameter tag wasn't there.\r\n";
      return false;
      }

    // This is a sequence for the Issuer Name.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 16 ) // Sequence
      {
      StatusString += "The issuer name sequence wasn't there.\r\n";
      return false;
      }

    // Get all the parts of the name.  It's an indefinite number
    // of tags because it could have any number of parts in it,
    // like Country Name, State or Province Name, Organization Name, etc.
    // while( don't go forever )
    for( int Count = 0; Count < 100; Count++ )
      {
      Index = ProcessOneTag( Index, OneCertificate );
      if( Index < 1 )
        return false;

      // This is the Not-Before date/time that starts the Validity section.
      // It's either UTC Time or Generalized Time.
      if( (MostRecentTagValue == 23) || (MostRecentTagValue == 24) )
        break; // The UTC times follow the Name parts.

      }

    // Universal time is: YYYYMMDDHHMMSSZ
    // UTC time isYYMMDDHHMMSSZ
    // "Conforming systems MUST interpret the year field (YY) as follows:
    // Where YY is greater than or equal to 50, the year SHALL be
    //  interpreted as 19YY; and
    // Where YY is less than 50, the year SHALL be interpreted as 20YY."

    // This is the second and last part of the Validity section.
    // This is the Not-After date/time.
    Index = ProcessOneTag( Index, OneCertificate );
    if( !((MostRecentTagValue == 23) || (MostRecentTagValue == 24)))
      {
      StatusString += "The second time value wasn't there.\r\n";
      return false;
      }

    // Subject. This is another series of names with an undefined length.
    // The Subject is who you want to verify.  Are they really who they
    // say they are?  "The CA certifies the binding between the public key
    // material and the subject of the certificate."
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 16 ) // Sequence
      {
      StatusString += "The Subject Name sequence wasn't there.\r\n";
      return false;
      }

    // Get all the parts of the name.  It's an indefinite number
    // of tags.
    // while( don't go forever )
    for( int Count = 0; Count < 100; Count++ )
      {
      Index = ProcessOneTag( Index, OneCertificate );
      if( Index < 1 )
        return false;

      if( MostRecentTagValue == 6 ) // Object Identifier
        {
        // If it has gotten to the SubjectPublicKeyInfo part, then break.
        //  OID Name is: RSA_RSA RSA Encryption. RFC 2313, 2437, 3370.
        if( MostRecentObjectIDString == "1.2.840.113549.1.1.1" )
          break;

        }
      }

    //    subjectPublicKeyInfo SubjectPublicKeyInfo,
    //  OID Name is: RSA_RSA RSA Encryption. RFC 2313, 2437, 3370.
    // MostRecentObjectIDString == "1.2.840.113549.1.1.1" 

    // This null is for the parameters associated with the signature algorithm.
    // There are none here, so it's null.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 5 ) // Null tag
      {
      StatusString += "The null parameter tag wasn't there.\r\n";
      return false;
      }

    // This is 270 bytes or so.
    Index = ProcessOneTag( Index, OneCertificate );
    if( MostRecentTagValue != 3 ) // Bit string
      {
      StatusString += "The bit string wasn't there for the public key.\r\n";
      return false;
      }

    // Following this is UniqueIdentifier if it's there.  (Obsolete?)

    // Following this is the extension parts.

    StatusString += "Check to see where the end of the certificate is.\r\n";
    StatusString += "Index: " + Index.ToString() + "\r\n";
    return true;
    }



  private int ProcessOneTag( int Index, byte[] OneCertificate )
    {
    try
    {
    if( IntMath == null )
      IntMath = new IntegerMath();

    if( OneCertificate == null )
      return -1;

    if( ObjectIDNames == null )
      ObjectIDNames = new X509ObjectIDNames();

    if( Index < 0 )
      {
      StatusString += "Index < 0.\r\n";
      return -1;
      }

    if( Index >= OneCertificate.Length )
      {
      StatusString += "It's past the end of the buffer.\r\n";
      return -1;
      }

    StatusString += "Index: " + Index.ToString() + "\r\n";

    int Tag = OneCertificate[Index];
    Index++;

    int ClassBits = Tag & 0xC0;
    ClassBits >>= 6;
    StatusString += "ClassBits: " + ClassBits.ToString( "X0" ) + "\r\n";

    int PrimitiveOrConstructed = Tag & 0x20;
    if( (PrimitiveOrConstructed & 0x20) == 0 )
      StatusString += "Tag is a Primitive type.\r\n";
    else
      StatusString += "Tag is a Constructed type.\r\n";

    int TagVal = Tag & 0x1F; // Bottom 5 bits.

    int Length = OneCertificate[Index];
    Index++;
    if( (Length & 0x80) != 0 )
      {
      // Then it's in the long form.
      int HowManyBytes = Length & 0x7F;
      if( HowManyBytes > 4 )
        {
        StatusString += "This can't be right for HowManyBytes: " + HowManyBytes.ToString() + "\r\n";
        return -1;
        }

      StatusString += "Long form HowManyBytes: " + HowManyBytes.ToString() + "\r\n";

      Length = 0;
      for( int Count = 0; Count < HowManyBytes; Count++ )
        {
        Length <<= 8;
        Length |= OneCertificate[Index];
        Index++;
        }
      }
    else
      {
      StatusString += "Length is in short form.\r\n";
      }

    StatusString += "Length is: " + Length.ToString() + "\r\n";
    if( Length > OneCertificate.Length )
      {
      StatusString += "The Length is not a reasonable number.\r\n";
      return -1;
      }

    MostRecentTagValue = TagVal;
    switch( TagVal )
        {
        case 0:
          StatusString += "Apparently this is for a context specific tag.\r\n";
          break;

        case 1:
          StatusString += "Tag is Boolean.\r\n";
          break;

        case 2:
          StatusString += "Tag is Integer.\r\n";
          break;

        case 3:
          StatusString += "Tag is Bit String.\r\n";
          break;

        case 4:
          StatusString += "Tag is Octet String.\r\n";
          break;

        case 5:
          StatusString += "Tag is Null.\r\n";
          break;

        case 6:
          StatusString += "Tag is Object Indentifier.\r\n";
          break;

        case 7:
          StatusString += "Tag is Object Descriptor.\r\n";
          break;

        case 8:
          StatusString += "Tag is External.\r\n";
          break;

        case 9:
          StatusString += "Tag is Real/Float.\r\n";
          break;

        case 10:
          StatusString += "Tag is Enumerated.\r\n";
          break;

        case 11:
          StatusString += "Tag is Embedded PDV.\r\n";
          break;

        case 12:
          StatusString += "Tag is UTF8 String.\r\n";
          break;

        case 13:
          StatusString += "Tag is Relative OID.\r\n";
          break;

        case 14:
          StatusString += "Tag is Reserved.\r\n";
          break;

        case 15:
          StatusString += "Tag is Reserved.\r\n";
          break;

        case 16:
          StatusString += "Tag is Sequence.\r\n";
          break;

        case 17:
          StatusString += "Tag is Set.\r\n";
          break;

        case 18:
          StatusString += "Tag is Numeric String.\r\n";
          break;

        case 19:
          StatusString += "Tag is Printable String.\r\n";
          break;

        case 20:
          StatusString += "Tag is T61 String.\r\n";
          break;

        case 21:
          StatusString += "Tag is Videotex String.\r\n";
          break;

        case 22:
          StatusString += "Tag is IA5 String.\r\n";
          break;

        case 23:
          StatusString += "Tag is UTC Time.\r\n";
          break;

        case 24:
          StatusString += "Tag is Generalized Time.\r\n";
          break;

        case 25:
          StatusString += "Tag is Graphic String.\r\n";
          break;

        case 26:
          StatusString += "Tag is Visible String.\r\n";
          break;

        case 27:
          StatusString += "Tag is General String.\r\n";
          break;

        case 28:
          StatusString += "Tag is Universal String.\r\n";
          break;

        case 29:
          StatusString += "Tag is Character String.\r\n";
          break;

        case 30:
          StatusString += "Tag is BMP String.\r\n";
          break;

        case 31:
          StatusString += "Tag is in Long Form.\r\n";
          break;

        default:
          StatusString += "This is a bug for TagVal.\r\n";
          break;

        }

    if( // (TagVal == 0) || // That content specific tag.
        (TagVal == 1) || // Boolean
        (TagVal == 3) || // Bit String
        (TagVal == 4) || // Octet String
        (TagVal == 5) || // Null (Should have a length of zero.)
        (TagVal == 7) || // Object Descriptor
        // (TagVal == 8) || // External
        (TagVal == 9) || // Real/Float
        (TagVal == 10) || // Enumerated
        (TagVal == 11) || // Embedded PDV
        (TagVal == 12) || // UTF8 String
        (TagVal == 13) || // Relative OID
        // (TagVal == 14) || // Reserved
        // (TagVal == 15) || // Reserved
        // (TagVal == 16) || // Sequence
        // (TagVal == 17) || // Set
        (TagVal == 18) || // Numeric String
        (TagVal == 20) || // T61 String
        (TagVal == 21) || // Videotex String
        (TagVal == 22) || // IA5 String
        (TagVal == 23) || // UTC Time
        (TagVal == 24) || // Generalized Time
        (TagVal == 25) || // Graphic String
        (TagVal == 26) || // Visible String
        (TagVal == 27) || // General String
        (TagVal == 28) || // Universal String
        (TagVal == 29) || // Character String
        (TagVal == 30)) // BMP String
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        int ContentByte = OneCertificate[Index];
        Index++;
        StatusString += "  Byte: 0x" + ContentByte.ToString( "X2" ) + "\r\n";
        }
      }

    if( TagVal == 2 ) // Integer
      {
      // This length was already checked above to see if it's a reasonable
      // number.
      byte[] BytesToSet = new byte[Length];
      try
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        BytesToSet[Count] = OneCertificate[Index];
        Index++;
        }

      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: BytesToSet[Count] = OneCertificate[Index].\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }

      try
      {
      MostRecentIntegerValue.SetFromBigEndianByteArray( BytesToSet );
      // StatusString += "MostRecentIntegerValue: " + IntMath.ToString10( MostRecentIntegerValue ) + "\r\n";
      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: SetFromBigEndianByteArray().\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }
      }

    // The only types of strings allowed in the RFC are PrintableString and
    // UTF8String.
    if( TagVal == 19 ) // Printable String
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        char ContentChar = (char)OneCertificate[Index];
        Index++;

        // Make sure it has valid ASCII characters.
        if( ContentChar > 127 )
          continue;

        if( ContentChar < 32 ) // Space character.
          continue;

        StatusString += Char.ToString( ContentChar );
        }

      StatusString += "\r\n";
      }

    if( TagVal == 6 ) // Object Indentifier
      {
      if( Length < 2 )
        {
        StatusString += "The Length can't be right for this OID.\r\n";
        return -1;
        }

      byte[] CodedBytes = new byte[Length];
      for( int Count = 0; Count < Length; Count++ )
        {
        CodedBytes[Count] = OneCertificate[Index];
        Index++;
        }

      X509ObjectID X509ID = new X509ObjectID();
      if( !X509ID.MakeFromBytes( CodedBytes ))
        {
        StatusString += "X509ID.MakeFromBytes() returned false.\r\n";
        return -1;
        }

      MostRecentObjectIDString = X509ID.GetStringValue();
      StatusString += X509ID.GetStatusString();
      StatusString += "StringValue is: " + X509ID.GetStringValue() + "\r\n";
      StatusString += "OID Name is: " + ObjectIDNames.GetNameFromDictionary( X509ID.GetStringValue()) + "\r\n";
      }

    StatusString += "\r\n";
    return Index;

    }
    catch( Exception Except )
      {
      // Probably over-ran the buffer.
      StatusString += "Exception in DomainX509Record.ProcessOneTag().\r\n";
      StatusString += Except.Message + "\r\n";
      return -1;
      }
    }



  internal string GetCertificateBytesShowString()
    {
    if( CertificateList == null )
      return "";

    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < CertificateList.Length; Count++ )
      {
      SBuilder.Append( Count.ToString() + ") " + CertificateList[Count].ToString("X2") + "\r\n" );
      if( Count > 99 )
        break;

      }

    return SBuilder.ToString();
    }



  }
}
