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
  private IntegerMath IntMath;
  private OneDigit[] DigitsArray;
  // This has to be set in relation to the Integer.DigitArraySize so that
  // it isn't too big for the MultplyUint that's done in
  // GetTraditionalInteger().  Also it has to be checked with the Max
  // Value test.
  internal const int DigitsArraySize = Integer.DigitArraySize * 2; // Index is 1024?


  private ChineseRemainder()
    {
    }


  internal ChineseRemainder( IntegerMath UseIntegerMath )
    {
    IntMath = UseIntegerMath;

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



  internal void SetToOne()
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].Value = 1;

    }



  internal void Copy( ChineseRemainder ToCopy )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      DigitsArray[Count].Value = ToCopy.DigitsArray[Count].Value;

    }


  internal void Add( ChineseRemainder ToAdd )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      // Operations like this could be very fast if they were done in
      // hardware, and the small mod operations could be done in very
      // small hardware lookup tables.
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


  internal void SetFromTraditionalInteger( Integer SetFrom )
    {
    for( int Count = 0; Count < DigitsArraySize; Count++ )
      {
      DigitsArray[Count].Value = (int)IntMath.GetMod32( SetFrom, (uint)DigitsArray[Count].Prime );
      }
    }



  internal void GetTraditionalInteger( Integer BigBase,
                                       Integer BasePart,
                                       Integer ToTest,
                                       Integer Accumulate )
    {
    // This takes several seconds for a large number.
    try
    {
    // The first few numbers for the base:
    // 2             2
    // 3             6
    // 5            30
    // 7           210
    // 11        2,310
    // 13       30,030
    // 17      510,510
    // 19    9,699,690
    // 23  223,092,870

    // This first one has the prime 2 as its base so it's going to
    // be set to either zero or one.
    Accumulate.SetFromULong( (uint)DigitsArray[0].Value );
    BigBase.SetFromULong( 2 );

    // Count starts at 1, so it's the prime 3.
    for( int Count = 1; Count < DigitsArraySize; Count++ )
      {
      for( uint CountPrime = 0; CountPrime < DigitsArray[Count].Prime; CountPrime++ )
        {
        ToTest.Copy( BigBase );
        IntMath.MultiplyUInt( ToTest, CountPrime );
        // Notice that the first time through this loop it's zero, so the
        // base part isn't added if it's already congruent to the Value.
        // So even though it goes all the way up through the DigitsArray,
        // this whole thing could add up to a small number like 7.
        // Compare this part with how GetMod32() is used in
        // SetFromTraditionalInteger().  And also, compare this with how
        // IntegerMath.NumberIsDivisibleByUInt() works.
        BasePart.Copy( ToTest );
        ToTest.Add( Accumulate );
        // If it's congruent to the Value mod Prime then it's the right number.
        if( (uint)DigitsArray[Count].Value == IntMath.GetMod32( ToTest, (uint)DigitsArray[Count].Prime ))
          {
          Accumulate.Add( BasePart );
          break;
          }
        }

      // The Integers have to be big enough to multiply this base.
      IntMath.MultiplyUInt( BigBase, (uint)DigitsArray[Count].Prime );
      }

    // Returns with Accumulate for the value.

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetTraditionalInteger(): " + Except.Message ));
      }
    }


  /*
  internal bool ParamIsGreater( ChineseRemainder Param )
    {
    The only way this could be done is like this:
    if( Param.GetTraditionalInteger() is more than GetTraditionalInteger() )
      return true;
    else
      return false;

    }
    */


  }
}
