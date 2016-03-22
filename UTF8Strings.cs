// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
// using System.Windows.Forms;



/*
UTF-8 (UCS Transformation Format -8)
UCS: Universal Character Set

http://en.wikipedia.org/wiki/Unicode
http://en.wikipedia.org/wiki/UTF-8
http://en.wikipedia.org/wiki/Wide_character
http://en.wikipedia.org/wiki/UTF-16
http://en.wikipedia.org/wiki/Basic_Multilingual_Plane
http://en.wikipedia.org/wiki/Universal_Character_Set
*/



namespace ExampleServer
{
  static class UTF8Strings
  {

  internal static byte[] StringToBytes( string InString )
    {
    if( InString == null )
      return null;

    if( InString.Length == 0 )
      return null;

    // Bits
    //  7 	U+007F 	0xxxxxxx
    // 11 	U+07FF 	110xxxxx 	10xxxxxx
    // 16 	U+FFFF 	1110xxxx 	10xxxxxx 	10xxxxxx

    // 21 	U+1FFFFF 	11110xxx 	10xxxxxx 	10xxxxxx 	10xxxxxx
    // 26 	U+3FFFFFF 	111110xx 	10xxxxxx 	10xxxxxx 	10xxxxxx 	10xxxxxx
    // 31 	U+7FFFFFFF 	1111110x 	10xxxxxx 	10xxxxxx 	10xxxxxx 	10xxxxxx 	10x

    byte[] Result;
    try
    {
    Result = new byte[InString.Length * 3];
    }
    catch( Exception )
      {
      return null;
      }

    int Where = 0;
    for( int Count = 0; Count < InString.Length; Count++ )
      {
      char Character = InString[Count];
      if( Character <= 0x7F )
        {
        // Regular ASCII.
        Result[Where] = (byte)Character;
        Where++; 
        continue;
        }

      if( Character >= 0xD800 ) // High Surrogates
        {
        // Result[Where] = (byte)'#'; // Ignore anything above high surrogates. 
        // Where++;
        // continue;
        break; // This should have been cleaned out already.
        }

      // "the first byte unambiguously indicates the length of the
      // sequence in bytes."
      // "All continuation bytes (byte nos. 2–6 in the table above) have
      // 10 as their two most-significant bits."

     // character "¢" = code point U+00A2
     // = 00000000 10100010
     // → 11000010 10100010
     // → hexadecimal C2 A2
     // = 00000000 10 100010

      //  7 	U+007F 	0xxxxxxx
      // 11 	U+07FF 	110xxxxx 	10xxxxxx
      // 16 	U+FFFF 	1110xxxx 	10xxxxxx 	10xxxxxx
      if( (Character > 0x7F) && (Character <= 0x7FF) )
        {
        // Notice that this conversion from characters to bytes
        // doesn't involve characters over 0x7F.
        byte SmallByte = (byte)(Character & 0x3F); // Bottom 6 bits.
        byte BigByte = (byte)((Character >> 6) & 0x1F); // Big 5 bits.
        
        BigByte |= 0xC0; // Mark it as the beginning byte.
        SmallByte |= 0x80; // Mark it as a continuing byte.
        Result[Where] = BigByte; 
        Where++; 
        Result[Where] = SmallByte; 
        Where++; 
        }


      // 16 	U+FFFF 	1110xxxx 	10xxxxxx 	10xxxxxx
      if( Character > 0x7FF ) // && (Character < 0xD800) )
        {
        byte Byte3 = (byte)(Character & 0x3F); // Bottom 6 bits.
        byte Byte2 = (byte)((Character >> 6) & 0x3F); // Next 6 bits.
        byte BigByte = (byte)((Character >> 12) & 0x0F); // Biggest 4 bits.
        
        BigByte |= 0xE0; // Mark it as the beginning byte.
        Byte2 |= 0x80; // Mark it as a continuing byte.
        Byte3 |= 0x80; // Mark it as a continuing byte.
        Result[Where] = BigByte; 
        Where++; 
        Result[Where] = Byte2; 
        Where++; 
        Result[Where] = Byte3; 
        Where++; 
        }
      }

    Array.Resize( ref Result, Where );
    return Result;
    }





  internal static string BytesToString( byte[] InBytes, int MaxLen )
    {
    // int Test = 1;
    try
    {
    if( InBytes == null )
      return "";

    if( InBytes.Length == 0 )
      return "";

    if( InBytes[0] == 0 )
      return "";

    if( MaxLen > InBytes.Length )
      MaxLen = InBytes.Length;

    StringBuilder SBuilder = new StringBuilder( MaxLen );
    for( int Count = 0; Count < MaxLen; Count++ )
      {
      if( InBytes[Count] == 0 )
        break;

      if( (InBytes[Count] & 0x80) == 0 )
        {
        // It's regular ASCII.
        SBuilder.Append( (char)InBytes[Count] );
        continue;
        }

      if( (InBytes[Count] & 0xC0) == 0x80 )
        {
        // It's a continuing byte that has already been read below.
        continue;
        }

      if( (InBytes[Count] & 0xC0) == 0xC0 )
        {
        // It's a beginning byte.
        // A beginning byte is either 110xxxxx or 1110xxxx.
        if( (InBytes[Count] & 0xF0) == 0xE0 )
          {
          // Starts with 1110xxxx.
          // It's a 3-byte character.
          if( (Count + 2) >= MaxLen )
            break; // Ignore the garbage.

          char BigByte = (char)(InBytes[Count] & 0x0F); // Biggest 4 bits.
          char Byte2 = (char)(InBytes[Count + 1] & 0x3F); // Next 6 bits.
          char Byte3 = (char)(InBytes[Count + 2] & 0x3F); // Next 6 bits.

          char Character = (char)(BigByte << 12);
          Character |= (char)(Byte2 << 6);
          Character |= Byte3;
          if( Character < 0xD800 ) // High Surrogates
            SBuilder.Append( Character );

          }

        if( (InBytes[Count] & 0xE0) == 0xC0 )
          {
          // Starts with 110xxxxx.
          // It's a 2-byte character.
          if( (Count + 1) >= MaxLen )
            continue; // return ""; // Ignore the garbage.

          char BigByte = (char)(InBytes[Count] & 0x1F); // Biggest 5 bits.
          char Byte2 = (char)(InBytes[Count + 1] & 0x3F); // Next 6 bits.

          char Character = (char)(BigByte << 6);
          Character |= Byte2;

          if( Character < 0xD800 ) // High Surrogates
            SBuilder.Append( Character );

          }

        // If it doesn't match the two above it gets ignored.
        }
      }

    string Result = SBuilder.ToString();
    if( Result == null )
      return "";

    return Result;

    }
    catch( Exception Except )
      {
      return "Error in UTF8Strings.BytesToString(): " + Except.Message;
      // return "";
      }
    }




  }
}


