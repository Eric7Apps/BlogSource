// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker


namespace ExampleServer
{
  internal struct OneDigit
    {
    internal int Value; // Digit value.
    // The BaseMultiple gives these numbers magnitude.
    internal int BaseMultiple;
    }



  // The numbers used here (like each Digit Value) could be in any
  // arbitrary order, but they have to be consistent with the primes
  // associated with each digit.  So for example doing
  // IntMath.GetPrimeAt( Count ) works with these digits because
  // they are in the same order as those primes, but if the primes
  // were put in to an array with some other arbitrary order, like
  // 7, 5, 13, 3, .... then it would still work, as long as
  // it was done consistently throughout this number system.
  // So for example SetFromTraditionalInteger() would still work
  // with some other arbitrary order of primes.
  // See also CRTMath.SetupBaseArray() for more info on that.


  class ChineseRemainder
  {
  private OneDigit[] DigitsArray;
  private IntegerMath IntMath;
  // This has to be set in relation to the Integer.DigitArraySize so that
  // it isn't too big for the MultplyUint that's done in
  // GetTraditionalInteger().  Also it has to be checked with the Max
  // Value test.
  internal const int DigitsArraySize = Integer.DigitArraySize * 2;


  /*
  private ChineseRemainder()
    {
    }
    */


  internal ChineseRemainder( IntegerMath UseIntMath )
    {
    if( DigitsArraySize > IntegerMath.PrimeArrayLength )
      throw( new Exception( "ChineseRemainder digit size is too big." ));

    IntMath = UseIntMath;

    DigitsArray = new OneDigit[DigitsArraySize];
    // SetToZero(); Not necessary for managed code.
    }



  internal int GetDigitAt( int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder GetDigitAt Index is too big." ));

    return DigitsArray[Index].Value;
    }



  internal void SetDigitAt( int SetTo, int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder SetDigitAt Index is too big." ));

    DigitsArray[Index].Value = SetTo;
    }



  internal void SetBaseMultiple( int SetTo, int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder SetBaseMultiple Index is too big." ));

    DigitsArray[Index].BaseMultiple = SetTo;
    }



  internal int GetBaseMultiple( int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder GetBaseMultiple Index is too big." ));

    return DigitsArray[Index].BaseMultiple;
    }



  internal void SetAllBaseMultiplesToZero()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].BaseMultiple = 0;

    }




  internal bool ParamIsGreater( ChineseRemainder ToCheck )
    {
    for( int Count = DigitsArraySize - 1; Count >= 0; Count-- )
      {
      // Usually a lot of upper values will both be zero until it
      // gets down to smaller magnitudes.  So a 1024-bit number
      // would go to a Count of about 130 or 131 (depending on what
      // it's congruent to) to find its first non-zero BaseMultiple
      // here.
      if( ToCheck.DigitsArray[Count].BaseMultiple == DigitsArray[Count].BaseMultiple )
        continue;

      // The first one it finds that's not equal.
      if( ToCheck.DigitsArray[Count].BaseMultiple > DigitsArray[Count].BaseMultiple )
        return true;

      if( ToCheck.DigitsArray[Count].BaseMultiple < DigitsArray[Count].BaseMultiple )
        return false;

      }

    return false; // It's equal but not greater.
    }



  internal void SetToZero()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].Value = 0;

    }



  internal bool IsZero()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count].Value != 0 )
        return false;

      }

    return true;
    }



  internal void SetToOne()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].Value = 1;

    }


  internal bool IsOne()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count].Value != 1 )
        return false;

      }

    return true;
    }



  internal void Copy( ChineseRemainder ToCopy )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = ToCopy.DigitsArray[Count].Value;
      DigitsArray[Count].BaseMultiple = ToCopy.DigitsArray[Count].BaseMultiple;
      }
    }



  internal bool IsEqual( ChineseRemainder ToCheck )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      if( DigitsArray[Count].Value != ToCheck.DigitsArray[Count].Value )
        return false;

      }

    return true;
    }



  // Operations like these will make the BaseMultiples invalid if
  // those values have been set previously.  So the BaseMultiples
  // would have to be recalculated before they get used.
  internal void Add( ChineseRemainder ToAdd )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      // Operations like this could be very fast if they were done in
      // hardware, and the small mod operations could be done in very
      // small hardware lookup tables.  They could also be done in parallel,
      // which would make it a lot faster than the way this is done, one
      // digit at a time.  Notice that there is no carry operation here.
      // As Claud Shannon would say, there is no diffusion here.
      DigitsArray[Count].Value += ToAdd.DigitsArray[Count].Value;
      int Prime = (int)IntMath.GetPrimeAt( Count );
      if( DigitsArray[Count].Value >= Prime )
        DigitsArray[Count].Value -= Prime;
        // DigitsArray[Count].Value = DigitsArray[Count].Value % DigitsArray[Count].Prime;

      }
    }



  internal void Subtract( ChineseRemainder ToSub )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value -= ToSub.DigitsArray[Count].Value;
      if( DigitsArray[Count].Value < 0 )
        DigitsArray[Count].Value += (int)IntMath.GetPrimeAt( Count );

      }
    }



  internal void Decrement1()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value -= 1;
      if( DigitsArray[Count].Value < 0 )
        DigitsArray[Count].Value += (int)IntMath.GetPrimeAt( Count );

      }
    }


  internal void SubtractUInt( uint ToSub )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value -= (int)(ToSub % (int)IntMath.GetPrimeAt( Count ));
      if( DigitsArray[Count].Value < 0 )
        DigitsArray[Count].Value += (int)IntMath.GetPrimeAt( Count );

      }
    }


  internal void Multiply( ChineseRemainder ToMul )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      // There is no Diffusion here either, like the kind that
      // Claude Shannon wrote about in A Mathematical Theory of
      // Cryptography.
      DigitsArray[Count].Value *= ToMul.DigitsArray[Count].Value;
      DigitsArray[Count].Value %= (int)IntMath.GetPrimeAt( Count );
      }
    }




  internal void SetFromTraditionalInteger( Integer SetFrom, IntegerMath IntMath )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = (int)IntMath.GetMod32( SetFrom, IntMath.GetPrimeAt( Count ));
      }
    }



  internal void SetFromUInt( uint SetFrom )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = (int)(SetFrom % (int)IntMath.GetPrimeAt( Count ));
      }
    }



  internal string GetString()
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 20; Count >= 0; Count-- )
      {
      string ShowS = DigitsArray[Count].Value.ToString() + ", ";
      // DigitsArray[Count].Prime

      SBuilder.Append( ShowS );
      }

    return SBuilder.ToString();
    }


  }
}

