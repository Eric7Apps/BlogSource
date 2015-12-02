// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace ExampleServer
{
  static class Utility
  {

  internal static string CleanAsciiString( string InString, int MaxLength )
    {
    if( InString == null )
      return "";

    StringBuilder SBuilder = new StringBuilder();
    
    for( int Count = 0; Count < InString.Length; Count++ )
      {
      if( Count >= MaxLength )
        break;

      if( InString[Count] > 127 )
        continue; // Don't want this character.

      if( InString[Count] < ' ' )
        continue; // Space is lowest ASCII character.
          
      SBuilder.Append( Char.ToString( InString[Count] ) );
      }

    string Result = SBuilder.ToString();
    // Result = Result.Replace( "\"", "" );
    return Result;
    }



  internal static string GetCleanUnicodeString( string InString, int HowLong )
    {
    if( InString == null )
      return "";

    if( InString.Length > HowLong )
      InString = InString.Remove( HowLong );

    InString = InString.Trim();

    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < InString.Length; Count++ )
      {
      char ToCheck = InString[Count];

      if( ToCheck < ' ' )
        continue; // Don't want this character.

      //  Don't go higher than D800 (Surrogates).
      if( ToCheck >= 0xD800 )
        continue;

      SBuilder.Append( Char.ToString( ToCheck ));
      }

    return SBuilder.ToString();
    }


  internal static string TruncateString( string InString, int HowLong )
    {
    if( InString.Length <= HowLong )
      return InString;

    return InString.Remove( HowLong );
    }



  // You could use Base64 instead.
  internal static string BytesToLetterString( byte[] InBytes )
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < InBytes.Length; Count++ )
      {
      uint ByteHigh = InBytes[Count];
      uint ByteLow = ByteHigh & 0x0F;
      ByteHigh >>= 4;
      SBuilder.Append( (char)('A' + (char)ByteHigh) );
      SBuilder.Append( (char)('A' + (char)ByteLow) );
      // MForm.ShowStatus( SBuilder.ToString() );
      }

    return SBuilder.ToString();
    }



  private static bool IsInLetterRange( uint Letter )
    {
    const uint MaxLetter = (uint)('A') + 15;
    const uint MinLetter = (uint)'A';

    if( Letter > MaxLetter )
      {
      // MForm.ShowStatus( "Letter > MaxLetter" );
      return false;
      }

    if( Letter < MinLetter )
      {
      // MForm.ShowStatus( "Letter < MinLetter" );
      return false;
      }

    return true;
    }



  internal static byte[] LetterStringToBytes( string InString )
    {
    try
    {

    if( InString == null )
      return null;

    if( InString.Length < 2 )
      return null;

    byte[] OutBytes;

    try
    {
    OutBytes = new byte[InString.Length >> 1];
    }
    catch( Exception )
      {
      return null;
      }

    int Where = 0;
    for( int Count = 0; Count < OutBytes.Length; Count++ )
      {
      uint Letter = InString[Where];
      if( !IsInLetterRange( Letter ))
        return null;

      uint ByteHigh = Letter - (uint)'A';
      ByteHigh <<= 4;
      Where++;
      Letter = InString[Where];
      if( !IsInLetterRange( Letter ))
        return null;

      uint ByteLow = Letter - (uint)'A';
      Where++;

      OutBytes[Count] = (byte)(ByteHigh | ByteLow);
      }

    return OutBytes;

    }
    catch( Exception )
      {
      return null;
      }
    }


  internal static void SortUintArray( ref uint[] ToSort )
    {
    int Last = ToSort.Length;
    while( true )
      {
      bool Swapped = false;
      for( int Count = 0; Count < (Last - 1); Count++ )
        {
        if( ToSort[Count] > ToSort[Count + 1] )
          {
          uint Temp = ToSort[Count];
          ToSort[Count] = ToSort[Count + 1];
          ToSort[Count + 1] = Temp;
          Swapped = true;
          }
        }

      if( !Swapped )
        break;

      }
    }


  /*
  internal static ulong[] MakeULongArrayFromUIntArray( uint[] ToCopy )
    {
    ulong[] Result = new ulong[ToCopy.Length];
    for( int Count = 0; Count < ToCopy.Length; Count++ )
      Result[Count] = ToCopy[Count];

    return Result;
    }
    */



  internal static uint[] MakeNoDuplicatesUIntArray( uint[] InArray )
    {
    // The InArray is expected to have values in it that are much
    // less than 0xFFFFFFFF.
    int InArrayLength = InArray.Length;
    uint[] Result = new uint[InArrayLength];
    for( uint CountStart = 0; CountStart < InArrayLength; CountStart++ )
      Result[CountStart] = InArray[CountStart];

    for( uint CountStart = 0; CountStart < InArrayLength; CountStart++ )
      {
      if( Result[CountStart] == 0xFFFFFFFF )
        continue;

      for( uint CountTest = CountStart + 1; CountTest < InArrayLength; CountTest++ )
        {
        if( Result[CountStart] == Result[CountTest] )
          Result[CountTest] = 0xFFFFFFFF;

        }
      }

    int MoveTo = 0;
    for( uint CountTest = 0; CountTest < InArrayLength; CountTest++ )
      {
      if( Result[CountTest] != 0xFFFFFFFF )
        {
        Result[MoveTo] = Result[CountTest];
        MoveTo++;
        }
      }

    Array.Resize( ref Result, MoveTo );
    return Result;
    }



  internal static uint[] RemoveThreeOutOfFourFromUIntArray( uint[] ToCopy, uint ModMask )
    {
    uint[] Result = new uint[ToCopy.Length];
    int Last = 0;
    for( int Count = 0; Count < ToCopy.Length; Count++ )
      {
      if( (Count & 0x3) == ModMask )
        {
        Result[Last] = ToCopy[Count];
        Last++;
        }
      }

    Array.Resize( ref Result, Last );
    return Result;
    }



  }
}

