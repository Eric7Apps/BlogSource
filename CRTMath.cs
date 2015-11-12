// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/



using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



namespace ExampleServer
{
  class CRTMath
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer ExponentCopy;
  private Integer TempForModPower;
  private ChineseRemainder CRTAccumulate;
  private ChineseRemainder CRTAccumulateExact;
  private ChineseRemainder CRTWorkingTemp;
  private Integer XForModPower;
  private ChineseRemainder CRTXForModPower;
  private Integer[] BaseArray;
  private ChineseRemainder[] CRTBaseArray;
  private Integer[] BaseModArray;
  private ChineseRemainder[] CRTBaseModArray;
  private ChineseRemainder[] NumbersArray;
  private ChineseRemainder CRTCopyForSquare;
  private Integer AccumulateBase;
  private ChineseRemainder CRTAccumulateBase;
  private ChineseRemainder CRTOneDigit;
  private Integer OneDigit;
  private Integer ToTestForTraditionalInteger;
  private ChineseRemainder CRTToTestForTraditionalInteger;
  private ChineseRemainder CRTTempForIsEqual;
  private Integer[] GeneralBaseArray; // For testing.
  internal ulong QuotientForTest = 0;


  /*
  private CRTMath()
    {

    }
    */



  internal CRTMath()
    {
    // Most of these are created ahead of time so that they don't have
    // to be created inside a loop.
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    ExponentCopy = new Integer();
    XForModPower = new Integer();
    TempForModPower = new Integer();
    CRTXForModPower = new ChineseRemainder( IntMath );
    AccumulateBase = new Integer();
    CRTCopyForSquare = new ChineseRemainder( IntMath );
    CRTAccumulateBase = new ChineseRemainder( IntMath );
    CRTOneDigit = new ChineseRemainder( IntMath );
    OneDigit = new Integer();
    CRTAccumulate = new ChineseRemainder( IntMath );
    CRTAccumulateExact = new ChineseRemainder( IntMath );
    CRTWorkingTemp = new ChineseRemainder( IntMath );
    ToTestForTraditionalInteger = new Integer();
    CRTToTestForTraditionalInteger = new ChineseRemainder( IntMath );
    CRTTempForIsEqual = new ChineseRemainder( IntMath );

    SetupNumbersArray();
    SetupBaseArray();
    }



  internal void ModularPower( Integer Result,
                              ChineseRemainder CRTResult,
                              Integer Exponent,
                              Integer Modulus,
                              ChineseRemainder CRTModulus )
    {
    // The square and multiply method is in Wikipedia:
    // https://en.wikipedia.org/wiki/Exponentiation_by_squaring
    // x^n = (x^2)^((n - 1)/2) if n is odd.
    // x^n = (x^2)^(n/2)       if n is even.

    if( CRTBaseModArray == null )
      throw( new Exception( "SetupBaseModArray() should have already been done here." ));

    if( Result.IsZero())
      return; // With Result still zero.

    if( CRTResult.IsZero())
      return; // With CRTResult still zero.

    if( Result.IsEqual( Modulus ))
      {
      // It is congruent to zero % ModN.
      Result.SetToZero();
      return;
      }

    if( CRTResult.IsEqual( CRTModulus ))
      {
      // It is congruent to zero % ModN.
      CRTResult.SetToZero();
      return;
      }

    // Result is not zero at this point.
    if( Exponent.IsZero() )
      {
      Result.SetFromULong( 1 );
      CRTResult.SetToOne();
      return;
      }

    if( Modulus.ParamIsGreater( Result ))
      {
      // throw( new Exception( "This is not supposed to be input for RSA plain text." ));
      IntMath.Divide( Result, Modulus, Quotient, Remainder );
      Result.Copy( Remainder );
      CRTResult.SetFromTraditionalInteger( Remainder, IntMath );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    XForModPower.Copy( Result );
    CRTXForModPower.Copy( CRTResult );

    Integer AccumulateForTest = new Integer();

    ExponentCopy.Copy( Exponent );
    int TestIndex = 0;
    Result.SetFromULong( 1 );
    CRTResult.SetToOne();
    while( true )
      {
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 ) // If the bottom bit is 1.
        {
        IntMath.Multiply( Result, XForModPower );
        CRTResult.Multiply( CRTXForModPower );
        ModularReduction( CRTResult,
                          Modulus, // For testing.
                          AccumulateForTest,
                          CRTAccumulate );

        AddByGeneralBaseArrays( TempForModPower, Result );
        Result.Copy( TempForModPower );
        CRTResult.Copy( CRTAccumulate );
        }

      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( ExponentCopy.IsZero())
        break;

      // Square it.
      IntMath.Multiply( XForModPower, XForModPower );
      CRTCopyForSquare.Copy( CRTXForModPower );
      CRTXForModPower.Multiply( CRTCopyForSquare );

      AddByGeneralBaseArrays( TempForModPower, XForModPower );
      XForModPower.Copy( TempForModPower );

      ModularReduction( CRTXForModPower,
                        Modulus,
                        AccumulateForTest,
                        CRTAccumulate );

      CRTXForModPower.Copy( CRTAccumulate );

      // These are not equal: CRTXForModPower and XForModPower.
      }

    int HowBig = Result.GetIndex() - Modulus.GetIndex();
    if( HowBig > 2 ) // Testing how big this is.
      throw( new Exception( "The difference in index size was more than 2. Diff: " + HowBig.ToString() ));

    // So this Quotient has only one or two 32-bit digits in it.
    GetTraditionalInteger( Result, CRTResult );
    IntMath.Divide( Result, Modulus, Quotient, Remainder );
    
    // The point of having this Modular Reduction algorithm is that it keeps
    // this Quotient very small, and that this Divide() doesn't have to be
    // done at all during the big loop above.  It's only done once at the end.

    // Testing how big it is.
    if( Quotient.GetIndex() > 0 )
      throw( new Exception( "Quotient.GetIndex() > 0.  It is: " + Quotient.GetIndex().ToString() ));

    if( Quotient.GetIndex() > 1 )
      throw( new Exception( "Quotient.GetIndex() > 1.  It is: " + Quotient.GetIndex().ToString() ));

    QuotientForTest = Quotient.GetAsULong();
    // QuotientForTest: 41,334
    // QuotientForTest: 43,681
    // QuotientForTest: 44,396

    Result.Copy( Remainder );
    CRTResult.SetFromTraditionalInteger( Remainder, IntMath );
    }




  internal void ModularReduction( ChineseRemainder CRTInput,
                                  Integer ModulusForTest,
                                  Integer Accumulate,
                                  ChineseRemainder CRTAccumulate )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "Bug: The NumbersArray should have been set up already." ));

    // This first one has the prime 2 as its base so it's going to
    // be set to either zero or one.
    Accumulate.SetFromULong( (uint)CRTInput.GetDigitAt( 0 ));

    if( CRTInput.GetDigitAt( 0 ) == 1 )
      {
      CRTAccumulate.SetToOne();
      CRTAccumulateExact.SetToOne();
      }
    else
      {
      CRTAccumulate.SetToZero();
      CRTAccumulateExact.SetToZero();
      }

    Integer BigBaseForTest = new Integer();
    BigBaseForTest.SetFromULong( 2 );
    Integer ToTest = new Integer();

    // ========= After it's tested.
    // This count doesn't have to go higher than when BigBase is bigger than
    // the modulus (because only zeros are added after that) so reduce the
    // size of this array to match.


    // Count starts at 1, so it's the prime 3.
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      // These calculations are exactly the same as the ones in 
      // GetTraditionalInteger(), so each of these two functions maps
      // an input to an output, but the output of this one is mod the
      // Modulus (plus a small quotient).  The output of the other one
      // is exactly the same number, but in the traditional form.

      uint Prime = (uint)CRTInput.GetPrimeAt( Count );
      // Notice that this uses the regular CRTBaseArray here:
      // This is exactly the same as in GetTraditionalInteger().
      uint ToTestIntKeep = (uint)CRTBaseArray[Count].GetDigitAt( Count );

      // This is exactly the same as in GetTraditionalInteger().
      uint AccumulateDigit = (uint)CRTAccumulateExact.GetDigitAt( Count );
      // Not this:
      // uint AccumulateDigit = (uint)CRTAccumulate.GetDigitAt( Count );

      uint CRTInputTestDigit = (uint)CRTInput.GetDigitAt( Count );
      for( uint CountPrime = 0; CountPrime < Prime; CountPrime++ )
        {
        ////////////////////////////////////////
        // This is what makes RSA breakable.  These digits can be calculated
        // separately and in parallel:
        // The digit of the base in BaseMod is different from the
        // corresponding digit in Base.  But I know the digits
        // in each base.  C is the same for both.
        uint ToTestInt = ToTestIntKeep; // The digit of the base.  Like 5.
        ToTestInt *= CountPrime;        // B * C + A = InputDigit. mod 7.
        ToTestInt += AccumulateDigit;
        ToTestInt %= Prime;
        if( CRTInputTestDigit == ToTestInt )  /////////////////////
          {
          // This is exactly the same as in GetTraditionalInteger().
          // The same CountPrime gets used here.

          ToTest.Copy( BigBaseForTest );
          IntMath.MultiplyUInt( ToTest, CountPrime );
          Accumulate.Add( ToTest );

          // It uses the CRTBaseArray here.
          CRTWorkingTemp.Copy( CRTBaseArray[Count] );
          CRTWorkingTemp.Multiply( NumbersArray[CountPrime] );
          CRTAccumulateExact.Add( CRTWorkingTemp );

          // But it uses the CRTBaseModArray here.
          CRTWorkingTemp.Copy( CRTBaseModArray[Count] );
          CRTWorkingTemp.Multiply( NumbersArray[CountPrime] );
          CRTAccumulate.Add( CRTWorkingTemp );

          break;
          }
        }

      // The Integers have to be big enough to multiply this base.
      IntMath.MultiplyUInt( BigBaseForTest, Prime );
      IntMath.Divide( BigBaseForTest, ModulusForTest, Quotient, Remainder );
      BigBaseForTest.Copy( Remainder );
      }

    if( !IsEqualToInteger( CRTAccumulate, Accumulate ))
      throw( new Exception( "CRTAccumulate not equal to Accumulate at the bottom of ModularReduction()." ));

    // Returns with CRTAccumulate for the value.
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ModularReduction(): " + Except.Message ));
      }
    }



  internal void GetTraditionalInteger( Integer Accumulate, ChineseRemainder CRTInput )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "Bug: The NumbersArray should have been set up already." ));

    // This first one has the prime 2 as its base so it's going to
    // be set to either zero or one.
    Accumulate.SetFromULong( (uint)CRTInput.GetDigitAt( 0 ));

    ChineseRemainder CRTAccumulate = new ChineseRemainder( IntMath );
    if( CRTInput.GetDigitAt( 0 ) == 1 )
      CRTAccumulate.SetToOne();
    else
      CRTAccumulate.SetToZero();

    Integer BigBase = new Integer();
    BigBase.SetFromULong( 2 );
    // CRTBigBase.SetFromUInt( 2 );

    // Count starts at 1, so it's the prime 3.
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      uint Prime = (uint)CRTInput.GetPrimeAt( Count );
      uint ToTestIntKeep = (uint)CRTBaseArray[Count].GetDigitAt( Count );
      uint AccumulateDigit = (uint)CRTAccumulate.GetDigitAt( Count );
      uint CRTInputTestDigit = (uint)CRTInput.GetDigitAt( Count );
      for( uint CountPrime = 0; CountPrime < Prime; CountPrime++ )
        {
        uint ToTestInt = ToTestIntKeep;
        ToTestInt *= CountPrime;
        ToTestInt += AccumulateDigit;
        ToTestInt %= Prime;
        if( CRTInputTestDigit == ToTestInt )
          {
          // Notice that the first time through this loop it's zero, so the
          // base part isn't added if it's already congruent to the Value.
          // So even though it goes all the way up through the DigitsArray,
          // this whole thing could add up to a small number like 7.

          ToTestForTraditionalInteger.Copy( BigBase );
          CRTToTestForTraditionalInteger.Copy( CRTBaseArray[Count] );
          IntMath.MultiplyUInt( ToTestForTraditionalInteger, CountPrime );
          CRTToTestForTraditionalInteger.Multiply( NumbersArray[CountPrime] );
          Accumulate.Add( ToTestForTraditionalInteger );
          CRTAccumulate.Add( CRTToTestForTraditionalInteger );
          break;
          }
        }

      // The Integers have to be big enough to multiply this base.
      IntMath.MultiplyUInt( BigBase, Prime );
      // CRTBigBase.Multiply( NumbersArray[Prime] );
      }

    if( !IsEqualToInteger( CRTAccumulate, Accumulate ))
      throw( new Exception( "CRTAccumulate not equal to Accumulate in GetTraditionalInteger()." ));

    // Returns with CRTAccumulate for the value.
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetTraditionalInteger(): " + Except.Message ));
      }
    }



/*
GetTraditionalInteger() works like this.
It accumulates values like this:

1 +
2 * 1 +  // BigBase times CountPrime at Prime: 3
6 * 2 +  // BigBase times CountPrime at Prime: 5
30 * 3 +  // BigBase times CountPrime at Prime: 7
210 * 8 +  // BigBase times CountPrime at Prime: 11
2,310 * 11 +  // BigBase times CountPrime at Prime: 13
30,030 * 11 +  // BigBase times CountPrime at Prime: 17
510,510 * 17 +  // BigBase times CountPrime at Prime: 19
9,699,690 * 22 +  // BigBase times CountPrime at Prime: 23
223,092,870 * 23 +  // BigBase times CountPrime at Prime: 29
6,469,693,230 * 2 +  // BigBase times CountPrime at Prime: 31
200,560,490,130 * 15 +  // BigBase times CountPrime at Prime: 37
7,420,738,134,810 * 27 +  // BigBase times CountPrime at Prime: 41
304,250,263,527,210 * 28 +  // BigBase times CountPrime at Prime: 43
13,082,761,331,670,030 * 23 +  // BigBase times CountPrime at Prime: 47
614,889,782,588,491,410 * 6 +  // BigBase times CountPrime at Prime: 53
32,589,158,477,190,044,730 * 10 +  // BigBase times CountPrime at Prime: 59
1,922,760,350,154,212,639,070 * 20 +  // BigBase times CountPrime at Prime: 61
117,288,381,359,406,970,983,270 * 16 +  // BigBase times CountPrime at Prime: 67
7,858,321,551,080,267,055,879,090 * 10 +  // BigBase times CountPrime at Prime: 71
557,940,830,126,698,960,967,415,390 * 7 +  // BigBase times CountPrime at Prime: 73
*/




  internal void SetupBaseArray()
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

    try
    {
    if( NumbersArray == null )
      throw( new Exception( "NumbersArray should have already been setup in SetupBaseArray()." ));

    BaseArray = new Integer[ChineseRemainder.DigitsArraySize];
    CRTBaseArray = new ChineseRemainder[ChineseRemainder.DigitsArraySize];

    Integer SetBase = new Integer();
    ChineseRemainder CRTSetBase = new ChineseRemainder( IntMath );

    Integer BigBase = new Integer();
    ChineseRemainder CRTBigBase = new ChineseRemainder( IntMath );

    BigBase.SetFromULong( 2 );
    CRTBigBase.SetFromUInt( 2 );

    SetBase.SetFromULong( 1 );
    CRTSetBase.SetToOne();

    BaseArray[0] = SetBase;
    CRTBaseArray[0] = CRTSetBase;

    ChineseRemainder CRTTemp = new ChineseRemainder( IntMath );

    // Count starts at 1, so it's the prime 3.
    // The first time through the loop the base is set to 2.
    // So BaseArray[1] = 2;
    // So BaseArray[2] = 6;
    // So BaseArray[3] = 30;
    // And so on...
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      SetBase = new Integer();
      CRTSetBase = new ChineseRemainder( IntMath );

      SetBase.Copy( BigBase );
      CRTSetBase.Copy( CRTBigBase );

      BaseArray[Count] = SetBase;
      CRTBaseArray[Count] = CRTSetBase;

      if( !IsEqualToInteger( CRTBaseArray[Count],
                             BaseArray[Count] ))
        throw( new Exception( "Bug.  The bases aren't equal." ));

      // Multiply it for the next BigBase.
      IntMath.MultiplyUInt( BigBase, IntMath.GetPrimeAt( Count ));
      CRTBigBase.Multiply( NumbersArray[IntMath.GetPrimeAt( Count )] );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupBaseArray(): " + Except.Message ));
      }
    }



  internal void SetupBaseModArray( Integer Modulus )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "NumbersArray should have already been setup in SetupBaseModArray()." ));

    BaseModArray = new Integer[ChineseRemainder.DigitsArraySize];
    CRTBaseModArray = new ChineseRemainder[ChineseRemainder.DigitsArraySize];

    Integer SetBase = new Integer();
    ChineseRemainder CRTSetBase = new ChineseRemainder( IntMath );

    Integer BigBase = new Integer();
    ChineseRemainder CRTBigBase = new ChineseRemainder( IntMath );

    BigBase.SetFromULong( 2 );
    CRTBigBase.SetFromUInt( 2 );

    SetBase.SetFromULong( 1 );
    CRTSetBase.SetToOne();

    BaseModArray[0] = SetBase;
    CRTBaseModArray[0] = CRTSetBase;

    ChineseRemainder CRTTemp = new ChineseRemainder( IntMath );

    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      SetBase = new Integer();
      CRTSetBase = new ChineseRemainder( IntMath );

      SetBase.Copy( BigBase );
      CRTSetBase.Copy( CRTBigBase );

      BaseModArray[Count] = SetBase;
      CRTBaseModArray[Count] = CRTSetBase;

      // Multiply it for the next BigBase.
      IntMath.MultiplyUInt( BigBase, IntMath.GetPrimeAt( Count ));

      IntMath.Divide( BigBase, Modulus, Quotient, Remainder );
      BigBase.Copy( Remainder );
      CRTBigBase.SetFromTraditionalInteger( BigBase, IntMath );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupBaseModArray(): " + Except.Message ));
      }
    }



  private void SetupNumbersArray()
    {
    try
    {
    uint BiggestPrime = IntMath.GetPrimeAt( ChineseRemainder.DigitsArraySize + 1 );
    NumbersArray = new ChineseRemainder[BiggestPrime];
    Integer SetNumber = new Integer();
    for( uint Count = 0; Count < BiggestPrime; Count++ )
      {
      SetNumber.SetFromULong( Count );
      ChineseRemainder CRTSetNumber = new ChineseRemainder( IntMath );
      CRTSetNumber.SetFromTraditionalInteger( SetNumber, IntMath );
      NumbersArray[Count] = CRTSetNumber;
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupNumbersArray(): " + Except.Message ));
      }
    }



  internal bool IsEqualToInteger( ChineseRemainder CRTTest,
                                  Integer Test )
    {
    CRTTempForIsEqual.SetFromTraditionalInteger( Test, IntMath );
    if( CRTTest.IsEqual( CRTTempForIsEqual ))
      return true;
    else
      return false;

    }




  internal void SetupGeneralBaseArray( Integer GeneralBase )
    {
    // The input to the accumulator can be twice the bit length of GeneralBase.
    int HowMany = ((GeneralBase.GetIndex() + 1) * 2) + 10; // Plus some extra for carries...
    if( GeneralBaseArray == null )
      {
      GeneralBaseArray = new Integer[HowMany];
      }

    if( GeneralBaseArray.Length < HowMany )
      {
      GeneralBaseArray = new Integer[HowMany];
      }

    Integer Base = new Integer();
    Integer BaseValue = new Integer();
    Base.SetFromULong( 256 ); // 0x100
    IntMath.MultiplyUInt( Base, 256 ); // 0x10000
    IntMath.MultiplyUInt( Base, 256 ); // 0x1000000
    IntMath.MultiplyUInt( Base, 256 ); // 0x100000000 is the base of this number system.

    BaseValue.SetFromULong( 1 );
    for( int Count = 0; Count < HowMany; Count++ )
      {
      if( GeneralBaseArray[Count] == null )
        GeneralBaseArray[Count] = new Integer();

      IntMath.Divide( BaseValue, GeneralBase, Quotient, Remainder );
      GeneralBaseArray[Count].Copy( Remainder );

      // If this ever happened it would be a bug because
      // the point of copying the Remainder in to BaseValue
      // is to keep it down to a reasonable size.
      // And Base here is one bit bigger than a uint.
      if( Base.ParamIsGreater( Quotient ))
        throw( new Exception( "Bug. This never happens: Base.ParamIsGreater( Quotient )" ));

      // Keep it to mod GeneralBase so Divide() doesn't
      // have to do so much work.
      BaseValue.Copy( Remainder );

      IntMath.Multiply( BaseValue, Base );
      }
    }



  // This is the Modular Reduction algorithm.  It reduces
  // ToAdd to Result.
  internal int AddByGeneralBaseArrays( Integer Result, Integer ToAdd )
    {
    try
    {
    if( GeneralBaseArray == null )
      throw( new Exception( "SetupGeneralBaseArray() should have already been called." ));

    Result.SetToZero();

    // The Index size of ToAdd is usually double the length of the modulus
    // this is reducing it to.  Like if you multiply P and Q to get N, then
    // the ToAdd that comes in here is about the size of N and the GeneralBase
    // is about the size of P.  So the amount of work done here is proportional
    // to P times N.

    int HowManyToAdd = ToAdd.GetIndex() + 1;
    int BiggestIndex = 0;
    for( int Count = 0; Count < HowManyToAdd; Count++ )
      {
      // The size of the numbers in GeneralBaseArray are all less than
      // the size of GeneralBase.
      // This multiplication by a uint is with a number that is not bigger
      // than GeneralBase.  Compare this with the two full Muliply()
      // calls done on each digit of the quotient in LongDivide3().

      // Accumulate is set to a new value here.
      int CheckIndex = IntMath.MultiplyUIntFromCopy( AccumulateBase, GeneralBaseArray[Count], ToAdd.GetD( Count ));

      if( CheckIndex > BiggestIndex )
        BiggestIndex = CheckIndex;

      Result.Add( AccumulateBase );
      }

    // Add all of them up at once.  This could cause an overflow, so 
    // Result.Add is done above.
    // AddUpAccumulateArray( Result, HowManyToAdd, BiggestIndex );

    return Result.GetIndex();
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in AddByGeneralBaseArrays(): " + Except.Message ));
      }
    }


  }
}

