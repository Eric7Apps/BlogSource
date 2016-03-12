// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;


namespace ExampleServer
{
  class Base10Number
  {
  // The number string 123456789 is put in the array of digits
  // like this:
  // D[0] = 9; // Least significant digit.
  // D[1] = 8;
  // D[2] = 7;
  // D[3] = 6;
  // D[4] = 5;
  // D[5] = 4;
  // D[6] = 3;
  // D[7] = 2;
  // D[8] = 1; // Most significant digit.

  private const int DigitArraySize = 4 * (((1024 * 16) / 32) + 1);
  private int Index = 0;
  private uint[] D;


  internal Base10Number()
    {
    D = new uint[DigitArraySize];
    }



  internal uint GetD( int Where )
    {
    // Range checks, etc.
    return D[Where];
    }



  internal int GetIndex()
    {
    return Index;
    }



  private uint ConvertDigit( char TheDigit )
    {
    // Convert one representation of digits to another.
    // If you know where these characters are in Unicode you can
    // subtract an offset to where they are.
    return (uint)TheDigit - 48;
    }




  internal void SetFromString( string ToSet )
    {
    int Where = 0;
    for( int Count = ToSet.Length - 1; Count >= 0; Count-- )
      {
      // If the number is too big to fit in the array then you could
      // either throw an exception or allocate a bigger array.
      if( Where >= DigitArraySize )
        throw( new Exception( "Base10Number: The number is too big for the array." ));

      char OneChar = ToSet[Count];
      // Ignore white space, commas, non digits, etc.
      if( (OneChar < '0') || (OneChar > '9'))
        continue;
        
      uint Digit = ConvertDigit( OneChar );

      // Test what GetDigit() returned.
      if( (Digit < 0) || (Digit > 9))
        throw( new Exception( "Base10Number: (Digit < 0) || (Digit > 9)) in SetFromString()." ));

      D[Where] = Digit;
      Where++;
      }

    if( Where == 0 )
      {
      // No valid digit was ever set.
      throw( new Exception( "Base10Number: Where == 0 in SetFromString()." ));
      }

    Index = Where - 1;
    }




  }
}




