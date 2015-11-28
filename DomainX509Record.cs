// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


/*

Section 9.1.2 EME-PKCS1-v1_5
Steps:

   1. If the length of the message M is greater than emLen - 10 octets,
   output "message too long" and stop.

   2. Generate an octet string PS of length emLen-||M||-2 consisting of
   pseudorandomly generated nonzero octets. The length of PS will be at
   least 8 octets.

   3. Concatenate PS, the message M, and other padding to form the
   encoded message EM as:

   EM = 02 || PS || 00 || M

Concatenate PS, the message M, and other padding to form an
         encoded message EM of length k octets as

            EM = 0x00 || 0x02 || PS || 0x00 || M.



How does that pattern work with the CRT numbers?

"If the first octet of EM is not 02, or if there is no 00 octet to
   separate PS from M, output "decoding error" and stop."


9.2 Encoding methods for signatures.

That's where that zero comes from in Android stuff:
"version is the version number, for compatibility with future
   revisions of this document. It shall be 0 for this version of the document."


Section 7 of RFC 2437 Encryption Schemes.  That has to do with how the bit streams are parsed.

Section 8 signature schemes.

Section 9 Encoding methods.
The message representative EM will typically have some structure that
   can be verified by the decoding operation


"Steps:
   1. Apply the EME-PKCS1-v1_5 encoding operation (Section 9.1.2.1) to
   the message M to produce an encoded message EM of length k-1 octets:"



In the RFCs it says that how you get that CA's public key is "outside of the scope of this document".
*/




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
// https://tools.ietf.org/html/rfc3279



using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace ExampleServer
{
  internal struct BitStreamRec
    {
    internal byte[] BitStream;
    internal string OIDString;
    }



  class DomainX509Record
  {
  // internal int Rank = 0; // Where it is ranked in popularity.  Google is 1.
  internal string DomainName = "";
  private string PublicKeyModulus = "0";
  private string PublicKeyExponent = "0";
  private ECTime ModifyTime;
     // This is the most recent certificate list
     // from the Server Certificate Message.
  private byte[] CertificateList;
  private string StatusString = "";
  private X509ObjectIDNames ObjectIDNames; // This only gets created if it's needed to parse certificates.
  private int MostRecentTagValue = 0;
  private Integer MostRecentIntegerValue;
  private string MostRecentObjectIDString = "";
  private IntegerMath IntMath;
  private BitStreamRec[] BitStreamRecArray;
  private int BitStreamRecArrayLast = 0;



  private void DoGeneralConstructorThings()
    {
    ModifyTime = new ECTime();
    MostRecentIntegerValue = new Integer();
    BitStreamRecArray = new BitStreamRec[8];
    }


  internal DomainX509Record()
    {
    DoGeneralConstructorThings();
    }



  internal DomainX509Record( string UseDomainName )
    {
    DomainName = UseDomainName;
    DoGeneralConstructorThings();
    }


  internal ulong GetModifyTimeIndex()
    {
    return ModifyTime.GetIndex();
    }


  internal void SetModifyTimeToNow()
    {
    ModifyTime.SetToNow();
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



  internal void Copy( DomainX509Record ToCopy )
    {
    if( ToCopy.DomainName.Length > 2 )
      DomainName = ToCopy.DomainName;
      
    ModifyTime.Copy( ToCopy.ModifyTime );
    PublicKeyExponent = ToCopy.PublicKeyExponent;
    PublicKeyModulus = ToCopy.PublicKeyModulus;
    }


  internal string ObjectToString()
    {
    if( DomainName.Length < 2 )
      return "";

    if( PublicKeyModulus.Length < 1 )
      PublicKeyModulus = "0";

    if( PublicKeyExponent.Length < 1 )
      PublicKeyExponent = "0";

    string Result = DomainName + "\t" +
      ModifyTime.GetIndex().ToString() + "\t" +
      PublicKeyExponent + "\t" +
      PublicKeyModulus;
      
    // if( CertificateList == null )
      // Result += " \t";
    // else
      // Result += Utility.BytesToLetterString( CertificateList );

    return Result;
    }



  internal bool StringToObject( string InString )
    {
    try
    {
    string[] SplitS = InString.Split( new Char[] { '\t' } );
    if( SplitS.Length < 4 )
      return false;

    DomainName = Utility.CleanAsciiString( SplitS[0], 100 ).Trim();
    ModifyTime.SetFromIndex( (ulong)Int64.Parse( SplitS[1] ));
    PublicKeyExponent = Utility.CleanAsciiString( SplitS[2], 100000 ).Trim();
    PublicKeyModulus = Utility.CleanAsciiString( SplitS[3], 100000 ).Trim();

    // CertificateList 

    return true;
    }
    catch( Exception Except )
      {
      // DomainName = "";
      return false;
      }
    }



  internal void ParseAndAddOneCertificateList( byte[] ToAdd )
    {
    StatusString += "Top of parse and add.\r\n";

    CertificateList = ToAdd;
    if( CertificateList == null )
      {
      StatusString += "CertificateList == null.\r\n";
      return;
      }

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

      if( !ParseOneCertificate( OneCertificate ))
        return;

      if( Index >= CertificateList.Length )
        {
        StatusString += "Index >= CertificateList.Length: " + Index.ToString() + "\r\n";
        return;
        }
      }
    }



  // A certificate from a .cer file could be parsed with this too.
  internal bool ParseOneCertificate( byte[] OneCertificate )
    {
    StatusString += "Top of ParseOneCertificate().\r\n";

    BitStreamRecArrayLast = 0;

    if( OneCertificate == null )
      {
      StatusString += "OneCertificate == null\r\n";
      return false;
      }

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

    // StringValue is: 1.2.840.113549.1.1.5
    // OID Name is: RSA_SHA1RSA RFC 2437, 3370
    // When I sent the ClientHello message I only specified that this client
    // can understand RSA with AES encryption, so the server has to pick from
    // only the ones I list that this client can support.
    // So it's returning an RSA type here.
    // If your client was supporting more encryption protocols then this
    // part would be more complicated and the server would return a wider
    // variety of things here, including possibly some parameters for
    // the next tag that follows this one.

    // This null is for the parameters associated with the signature algorithm.
    // There are none here for RSA, so it's null.
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
        // It's looking for the RSA
        if( MostRecentObjectIDString == "1.2.840.113549.1.1.1" )
          break;

        }
      }


    // subjectPublicKeyInfo SubjectPublicKeyInfo,
    // OID Name is: RSA_RSA RSA Encryption. RFC 2313, 2437, 3370.
    // MostRecentObjectIDString == "1.2.840.113549.1.1.1" 

    // This null is for the parameters associated with the RSA public key.
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

    // Get the rest of the parts of the certificate.
    // while( don't go forever )
    for( int Count = 0; Count < 1000; Count++ )
      {
      Index = ProcessOneTag( Index, OneCertificate );
      if( Index < 1 )
        break; // The end of the certificate.

      }

    StatusString += "Check to see where the end of the certificate is.\r\n";
    // StatusString += "Index: " + Index.ToString() + "\r\n";

    ProcessBitStreamRecs();

    return true;
    }



  private void ShowTagType( int TagVal )
    {
    switch( TagVal )
      {
      case 0:
        StatusString += "Context specific tag.\r\n";
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
        StatusString += "Unknown TagVal is: " + TagVal.ToString() + "\r\n";
        break;

      }
    }




  private int ProcessOneTag( int Index, byte[] TagsBuffer )
    {
    try
    {
    StatusString += "Top of ProcessOneTag().\r\n";

    if( IntMath == null )
      IntMath = new IntegerMath();

    if( TagsBuffer == null )
      {
      StatusString += "Tags buffer was null.\r\n";
      return -1;
      }

    if( ObjectIDNames == null )
      ObjectIDNames = new X509ObjectIDNames();

    if( Index < 0 )
      {
      StatusString += "Index < 0.\r\n";
      return -1;
      }

    if( Index >= TagsBuffer.Length )
      {
      StatusString += "It's past the end of the buffer.\r\n";
      return -1;
      }

    StatusString += "Index: " + Index.ToString() + "\r\n";

    int Tag = TagsBuffer[Index];
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
    ShowTagType( TagVal );
    MostRecentTagValue = TagVal;

    int Length = TagsBuffer[Index];
    Index++;
    if( (Length & 0x80) != 0 )
      {
      // Then it's in the long form.
      int HowManyBytes = Length & 0x7F;
      if( HowManyBytes > 4 )
        throw( new Exception( "This can't be right for HowManyBytes: " + HowManyBytes.ToString() ));

      StatusString += "Long form HowManyBytes: " + HowManyBytes.ToString() + "\r\n";

      Length = 0;
      for( int Count = 0; Count < HowManyBytes; Count++ )
        {
        Length <<= 8;
        Length |= TagsBuffer[Index];
        Index++;
        }
      }
    else
      {
      StatusString += "Length is in short form.\r\n";
      }

    StatusString += "Length is: " + Length.ToString() + "\r\n";
    if( Length > TagsBuffer.Length )
      {
      StatusString += "The Length is not a reasonable number.\r\n";
      return -1;
      }

    if( // (TagVal == 0) || // That context specific tag.
        (TagVal == 1) || // Boolean
        // (TagVal == 4) || // Octet String
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
        // (TagVal == 22) || // IA5 String
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
        int ContentByte = TagsBuffer[Index];
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
        BytesToSet[Count] = TagsBuffer[Index];
        Index++;
        }

      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: BytesToSet[Count] = TagsBuffer[Index].\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }

      try
      {
      MostRecentIntegerValue.SetFromBigEndianByteArray( BytesToSet );
      StatusString += "MostRecentIntegerValue: " + IntMath.ToString10( MostRecentIntegerValue ) + "\r\n";
      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: SetFromBigEndianByteArray().\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }
      }

    if( TagVal == 3 ) // bit String
      {
      // This length was already checked above to see if it's a reasonable
      // number.
      byte[] BytesToSet = new byte[Length];
      try
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        BytesToSet[Count] = TagsBuffer[Index];
        Index++;
        }

      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: BytesToSet[Count] = TagsBuffer[Index].\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }

      try
      {
      BitStreamRec Rec = new BitStreamRec();
      Rec.BitStream = BytesToSet;
      Rec.OIDString = MostRecentObjectIDString;
      AddBitStreamRec( Rec );
      }
      catch( Exception Except )
        {
        // Probably over-ran the buffer.
        StatusString += "Exception at: AddBitStreamRec().\r\nIn DomainX509Record.ProcessOneTag().\r\n";
        StatusString += Except.Message + "\r\n";
        return -1;
        }
      }

    // Just a rough draft, to see what's in them.
    if( TagVal == 4 ) // Octet String
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        char ContentChar = (char)TagsBuffer[Index];
        Index++;

        // Make sure it has valid ASCII characters.
        if( ContentChar > 126 )
          continue;

        if( ContentChar < 32 ) // Space character.
          continue;

        StatusString += Char.ToString( ContentChar );
        }

      StatusString += "\r\n";
      }


    // UTF8String.

    if( TagVal == 19 ) // Printable String
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        char ContentChar = (char)TagsBuffer[Index];
        Index++;

        // Make sure it has valid ASCII characters.
        if( ContentChar > 126 ) // 127 is the ASCII DEL character.
          continue;

        if( ContentChar < 32 ) // Space character.
          continue;

        StatusString += Char.ToString( ContentChar );
        }

      StatusString += "\r\n";
      }

    if( TagVal == 22 ) // IA5 String
      {
      for( int Count = 0; Count < Length; Count++ )
        {
        char ContentChar = (char)TagsBuffer[Index];
        Index++;

        // Make sure it has valid ASCII characters.
        if( ContentChar > 126 )
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
        CodedBytes[Count] = TagsBuffer[Index];
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



  internal void AddBitStreamRec( BitStreamRec Rec )
    {
    // if( Rec == null )
      // return false;

    BitStreamRecArray[BitStreamRecArrayLast] = Rec;
    BitStreamRecArrayLast++;

    if( BitStreamRecArrayLast >= BitStreamRecArray.Length )
      {
      try
      {
      Array.Resize( ref BitStreamRecArray, BitStreamRecArray.Length + 8 );
      }
      catch( Exception Except )
        {
        throw( new Exception( "Couldn't resize the arrays for AddBitStreamRec(). " + Except.Message ));
        }
      }
    }



  private void ProcessBitStreamRecs()
    {
    for( int Count = 0; Count < BitStreamRecArrayLast; Count++ )
      {
      string OIDString = BitStreamRecArray[Count].OIDString;
      StatusString += "\r\n\r\nProcessing BitStream at: " + Count.ToString() + "\r\n" +
             "OIDString: " + OIDString + "\r\n" +
             "OID Name is: " + ObjectIDNames.GetNameFromDictionary( BitStreamRecArray[Count].OIDString ) + "\r\n";

      byte[] ByteArray = BitStreamRecArray[Count].BitStream;
      StatusString += "ByteArray.Length: " + ByteArray.Length.ToString() + "\r\n";

      if( OIDString == "1.2.840.113549.1.1.1" ) // The public keys.
        {
        if( Count == 0 )
          {
          ParsePublicKeys( ByteArray );
          continue;
          }

        ParseNotDone( ByteArray );
        continue;
        }

      // if( OIDString == "1.2.840.113549.1.1.2" ) // "RSA_MD2RSA";
      // if( OIDString == "1.2.840.113549.1.1.3" ) // RSA_MD4RSA
      // if( OIDString == "1.2.840.113549.1.1.4" ) // RSA_MD5RSA

      // RFC 2437, 3370";
      // SHA1 has a 20 byte hash.  But "rendered as hex, 40 digits long.
      // "Microsoft, Google and Mozilla have all announced that their
      // respective browsers will stop accepting SHA-1 SSL certificates by 2017."

      if( OIDString == "1.2.840.113549.1.1.5" ) // SHA1 with RSA
        {
        ParseSHA1WithRSA( ByteArray );
        continue;
        }

      // if( OIDString == "1.2.840.113549.1.1.11" ) // SHA256 with RSA

      // if( OIDString == "1.2.840.113549.1.1.12" ) // SHA384 with RSA

      }
    }



  private void ParseNotDone( byte[] ParseArray )
    {
    StatusString += "This byte array was not parsed.\r\n";
    }


  // OIDString: 1.2.840.113549.1.1.1
  private bool ParsePublicKeys( byte[] ParseArray )
    {
    StatusString += "Top of ParsePublicKeys().\r\n";

    if( ParseArray == null )
      return false;

    // See if it's a reasonable number.
    if( ParseArray.Length < 10 )
      {
      StatusString += "ParseArray.Length < 10.\r\n";
      return false;
      }

    int Index = 0;

    // Apparently this is not a context specific tag.
    // And its length of 48 makes no sense.
    Index = ProcessOneTag( Index, ParseArray );
    if( MostRecentTagValue != 0 ) // Context Specific tag
      {
      StatusString += "This tag should be a context specific tag.\r\n";
      return false;
      }

    // Apparently the first number is the version.
    // But it's not in the Android In-App billing version 1.5 public key.
    Index = ProcessOneTag( Index, ParseArray );
    if( MostRecentTagValue != 2 ) // Integer
      {
      StatusString += "This second tag should be an Integer.\r\n";
      return false;
      }

    // This is the Modulus.
    Index = ProcessOneTag( Index, ParseArray );
    if( MostRecentTagValue != 2 ) // Integer
      {
      StatusString += "An Integer tag wasn't there.\r\n";
      return false;
      }

    PublicKeyModulus = IntMath.ToString10( MostRecentIntegerValue );

    // This is the Exponent.
    Index = ProcessOneTag( Index, ParseArray );
    if( MostRecentTagValue != 2 ) // Integer
      {
      StatusString += "An Integer tag wasn't there.\r\n";
      return false;
      }

    PublicKeyExponent = IntMath.ToString10( MostRecentIntegerValue );
    return true;
    }



  private bool ParseSHA1WithRSA( byte[] ParseArray )
    {
    StatusString += "Top of ParseSHA1WithRSA().\r\n";

    if( ParseArray == null )
      return false;

    // See if it's a reasonable number.
    if( ParseArray.Length < 10 )
      {
      StatusString += "ParseArray.Length < 10.\r\n";
      return false;
      }

    int Index = 0;
    // See what's in it.
    for( int Count = 0; Count < ParseArray.Length; Count++ )
      {
      int ShowByte = ParseArray[Count];
      StatusString += "Byte at " + Count.ToString() + ") " + ShowByte.ToString() + "\r\n";
      }

    return true;
    }



  }
}
