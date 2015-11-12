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
  public struct OneDigit
    {
    public int Prime;
    public int Value;
    }



  class ChineseRemainder
  {
  private OneDigit[] DigitsArray;

  // This has to be set in relation to the Integer.DigitArraySize so that
  // it isn't too big for the MultplyUint that's done in
  // GetTraditionalInteger().  Also it has to be checked with the Max
  // Value test.
  internal const int DigitsArraySize = Integer.DigitArraySize * 2; // Index is 1024?


  /*
  private ChineseRemainder()
    {
    }
    */


  internal ChineseRemainder( IntegerMath IntMath )
    {
    if( DigitsArraySize > IntegerMath.PrimeArrayLength )
      throw( new Exception( "ChineseRemainder digit size is too big." ));

    DigitsArray = new OneDigit[DigitsArraySize];
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].Prime = (int)IntMath.GetPrimeAt( Count );

    // SetToZero(); Not necessary for managed code.
    }



  internal int GetDigitAt( int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder GetDigitAt Index is too big." ));

    return DigitsArray[Index].Value;
    }



  internal int GetPrimeAt( int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder GetPrimeAt Index is too big." ));

    return DigitsArray[Index].Prime;
    }


  internal void SetDigitAt( int SetTo, int Index )
    {
    if( Index >= DigitsArraySize )
      throw( new Exception( "ChineseRemainder SetDigitAt Index is too big." ));

    DigitsArray[Index].Value = SetTo;
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
      DigitsArray[Count].Value = ToCopy.DigitsArray[Count].Value;

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
      if( DigitsArray[Count].Value >= DigitsArray[Count].Prime )
        DigitsArray[Count].Value -= DigitsArray[Count].Prime;
        // DigitsArray[Count].Value = DigitsArray[Count].Value % DigitsArray[Count].Prime;

      }
    }



  internal void Subtract( ChineseRemainder ToSub )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value -= ToSub.DigitsArray[Count].Value;
      if( DigitsArray[Count].Value < 0 )
        DigitsArray[Count].Value += DigitsArray[Count].Prime;

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
      DigitsArray[Count].Value %= DigitsArray[Count].Prime;
      }
    }



  // This is the closest this has to Divide().
  internal void ModularInverse( ChineseRemainder ToDivide )
    {
    // ToDivide times what equals this number?
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      for( int CountPrime = 0; CountPrime < DigitsArray[Count].Prime; CountPrime++ )
        {
        int Test = CountPrime * ToDivide.DigitsArray[Count].Value;
        Test = Test % DigitsArray[Count].Prime;
        if( Test == DigitsArray[Count].Value )
          {
          DigitsArray[Count].Value = CountPrime;
          break;
          }
        }
      }
    }



  internal void SetFromTraditionalInteger( Integer SetFrom, IntegerMath IntMath )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = (int)IntMath.GetMod32( SetFrom, (uint)DigitsArray[Count].Prime );
      }
    }



  internal void SetFromUInt( uint SetFrom )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = (int)(SetFrom % (uint)DigitsArray[Count].Prime );
      }
    }


  }
}
