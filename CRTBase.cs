// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Text;
using System.ComponentModel; // BackgroundWorker


namespace ExampleServer
{


  class CRTBase
  {
  private int[] DigitsArray;
  private IntegerMath IntMath;
  internal const int DigitsArraySize = ChineseRemainder.DigitsArraySize;



  private CRTBase()
    {
    }



  internal CRTBase( IntegerMath UseIntMath )
    {
    if( DigitsArraySize > IntegerMath.PrimeArrayLength )
      throw( new Exception( "CRTBase digit size is too big." ));

    IntMath = UseIntMath;

    DigitsArray = new int[DigitsArraySize];
    // SetToZero(); Not necessary for managed code.
    }



  internal int GetDigitAt( int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "CRTBase GetDigitAt Index is too big." ));

    return DigitsArray[Index];
    }



  internal void SetDigitAt( int SetTo, int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "CRTBase SetDigitAt Index is too big." ));

    DigitsArray[Index] = SetTo;
    }



  internal bool ParamIsGreater( CRTBase ToCheck )
    {
    for( int Count = DigitsArraySize - 1; Count >= 0; Count-- )
      {
      // Usually a lot of upper values will both be zero until it
      // gets down to smaller magnitudes.  So a 1024-bit number
      // would go to a Count of about 130 or 131 (depending on what
      // it's congruent to) to find its first non-zero BaseMultiple
      // here.
      if( ToCheck.DigitsArray[Count] == DigitsArray[Count] )
        continue;

      // The first one it finds that's not equal.
      if( ToCheck.DigitsArray[Count] > DigitsArray[Count] )
        return true;

      if( ToCheck.DigitsArray[Count] < DigitsArray[Count] )
        return false;

      }

    return false; // It's equal but not greater.
    }



  internal void SetToZero()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count] = 0;

    }



  internal bool IsZero()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count] != 0 )
        return false;

      }

    return true;
    }



  internal void SetToOne()
    {
    DigitsArray[0] = 1;

    // All the bases would be at zero.
    for( int Count = 1; Count < DigitsArraySize; Count++ )
      DigitsArray[Count] = 0;

    }


  internal bool IsOne()
    {
    if( DigitsArray[0] != 1 )
      return false;

    for( int Count = 1; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count] != 0 )
        return false;

      }

    return true;
    }



  internal void Copy( CRTBase ToCopy )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count] = ToCopy.DigitsArray[Count];

    }



  internal bool IsEqual( CRTBase ToCheck )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count] != ToCheck.DigitsArray[Count] )
        return false;

      }

    return true;
    }



  internal string GetString()
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 20; Count >= 0; Count-- )
      {
      string ShowS = DigitsArray[Count].ToString() + ", ";
      // DigitsArray[Count].Prime

      SBuilder.Append( ShowS );
      }

    return SBuilder.ToString();
    }


  }
}


