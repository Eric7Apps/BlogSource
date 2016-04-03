// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Text;
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{
  class CRTMath
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer[] BaseArray;
  private ChineseRemainder[] CRTBaseArray;
  private ChineseRemainder[] NumbersArray;
  private Integer AccumulateBase;
  private ChineseRemainder CRTAccumulateBase;
  private Integer ToTestForTraditionalInteger;
  private ChineseRemainder CRTToTestForTraditionalInteger;
  private ChineseRemainder CRTTempForIsEqual;
  private int[,] MultInverseArray;
  private BackgroundWorker Worker;
  private bool Cancelled = false;



  private CRTMath()
    {

    }



  internal CRTMath( BackgroundWorker UseWorker )
    {
    // Most of these are created ahead of time so that
    // they don't have to be created inside a loop.
    Worker = UseWorker;
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    AccumulateBase = new Integer();
    CRTAccumulateBase = new ChineseRemainder( IntMath );
    ToTestForTraditionalInteger = new Integer();
    CRTToTestForTraditionalInteger = new ChineseRemainder( IntMath );
    CRTTempForIsEqual = new ChineseRemainder( IntMath );

    Worker.ReportProgress( 0, "Setting up numbers array." );
    SetupNumbersArray();

    Worker.ReportProgress( 0, "Setting up base array." );
    SetupBaseArray();

    Worker.ReportProgress( 0, "Setting up multiplicative inverses." );
    SetMultiplicativeInverses();
    }



  internal void SetCancelled( bool SetTo )
    {
    Cancelled = SetTo;
    }



  private void SetMultiplicativeInverses()
    {
    try
    {
    int BiggestPrime = (int)IntMath.GetPrimeAt( ChineseRemainder.DigitsArraySize - 1 );

    MultInverseArray = new int[ChineseRemainder.DigitsArraySize, BiggestPrime];
    for( int Count = 0; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int Prime = (int)IntMath.GetPrimeAt( Count );
      if( (Count & 0xF) == 1 )
        Worker.ReportProgress( 0, Count.ToString() + ") Setting mult inverses for prime: " + Prime.ToString() );

      for( int Digit = 1; Digit < Prime; Digit++ )
        {
        if( Worker.CancellationPending )
          return;

        // ==========
        // Use the Euclidean algorithm.
        for( int MultCount = 1; MultCount < Prime; MultCount++ )
          {
          if( ((MultCount * Digit) % Prime) == 1 )
            {
            MultInverseArray[Count, Digit] = MultCount;
            // if( Count < 40 )
              // Create either a tab-delimited file, or create source
              // code to copy it into a hard-coded form.
              // Or just let it generate it each time, like it does here.
              // Or in multiple threads.
              // Worker.ReportProgress( 0, Prime.ToString() + "\t" + Digit.ToString() + "\t" + MultCount.ToString() + "\r\n" );
              // Worker.ReportProgress( 0, "    FixedInverseArray[" + Prime.ToString() + ", " + Digit.ToString() + "] = " + MultCount.ToString() + ";" );

            }
          }
        }
      }

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetMultiplicativeInverses(): " + Except.Message ));
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
      uint Prime = IntMath.GetPrimeAt( Count );
      uint AccumulateDigit = (uint)CRTAccumulate.GetDigitAt( Count );
      uint CRTInputTestDigit = (uint)CRTInput.GetDigitAt( Count );
      uint BaseDigit = (uint)CRTBaseArray[Count].GetDigitAt( Count );
      if( BaseDigit == 0 )
        throw( new Exception( "This never happens. BaseDigit == 0." ));

      uint MatchingValue = CRTInputTestDigit;
      if( MatchingValue < AccumulateDigit )
        MatchingValue += Prime;

      MatchingValue -= AccumulateDigit;
      uint Inverse = (uint)MultInverseArray[Count, BaseDigit];
      MatchingValue = (MatchingValue * Inverse) % Prime;
      // This loop shows how it tries to find a matching
      // CountPrime.  It is for bug testing.  It could
      // just use the MatchingValue without using the
      // loop to verify that it's valid.  But it shows
      // clearly how it is trying to find a number that
      // matches up with the CRTInput digit.
      for( uint CountPrime = 0; CountPrime < Prime; CountPrime++ )
        {
        uint ToTestInt = BaseDigit;
        ToTestInt *= CountPrime;
        ToTestInt += AccumulateDigit;
        ToTestInt %= Prime;
        if( CRTInputTestDigit == ToTestInt )
          {
          if( MatchingValue != CountPrime )
            {
            throw( new Exception( "Bug: MatchingValue is not right." ));
            }
 
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

      // Integer.DigitArraySize has to be big enough to multiply this base.
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
*/


/*
These bottom digits are 0 for each prime that gets multiplied by
the base.  So they keep getting one more zero at the bottom of each one.
Then each digit above that is just the whole entire base number mod
the current prime.  (As you can see in SetFromTraditionalInteger()
in the ChineseRemainder.cs file.)
But the ones in BaseModArray only have the zeros at the bottom
on the ones that are smaller than the modulus.

At BaseArray[0] it's 1, 1, 1, 1, 1, .... for all of them.
2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 
6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 1, 0, 0, 
30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 1, 7, 11, 13, 4, 8, 2, 0, 0, 0, 
64, 68, 9, 27, 33, 51, 22, 38, 5, 25, 24, 7, 3, 1, 6, 2, 1, 0, 0, 0, 0, 
47, 38, 32, 53, 9, 31, 7, 31, 14, 16, 16, 19, 10, 11, 15, 9, 0, 0, 0, 0, 0, 
27, 68, 14, 18, 58, 32, 44, 16, 18, 23, 22, 15, 15, 10, 8, 0, 0, 0, 0, 0, 0, 
*/



  private void SetupBaseArray()
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
    // So BaseArray[0] = 1;
    // So BaseArray[1] = 2;
    // So BaseArray[2] = 6;
    // So BaseArray[3] = 30;
    // And so on...
    // In BaseArray[3] digits at 2, 3 and 5 are set to zero.
    // In BaseArray[4] digits at 2, 3, 5 and 7 are set to zero.
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      SetBase = new Integer();
      CRTSetBase = new ChineseRemainder( IntMath );

      SetBase.Copy( BigBase );
      CRTSetBase.Copy( CRTBigBase );

      BaseArray[Count] = SetBase;
      CRTBaseArray[Count] = CRTSetBase;
      // if( Count < 50 )
        // Worker.ReportProgress( 0, CRTBaseArray[Count].GetString() );

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



  private Integer GetBaseIntegerAt( int Index )
    {
    if( BaseArray == null )
      throw( new Exception( "The BaseArray should have already been set up here." ));

    if( Index >= BaseArray.Length )
      throw( new Exception( "Index is past BaseArray in GetBaseIntegerAt()." ));

    return BaseArray[Index];
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
      CRTSetNumber.SetFromTraditionalInteger( SetNumber );
      NumbersArray[Count] = CRTSetNumber;
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupNumbersArray(): " + Except.Message ));
      }
    }



  internal bool IsEqualToInteger( ChineseRemainder CRTTest, Integer Test )
    {
    CRTTempForIsEqual.SetFromTraditionalInteger( Test );
    if( CRTTest.IsEqual( CRTTempForIsEqual ))
      return true;
    else
      return false;

    }



  internal void MultiplicativeInverse( ChineseRemainder Dividend, ChineseRemainder DivideBy, ChineseRemainder Quotient )
    {
    // DivideBy times (what) equals Dividend?
    for( int Count = 0; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int Prime = (int)IntMath.GetPrimeAt( Count );
      int DivideByDigit = DivideBy.GetDigitAt( Count );
      int DividendDigit = Dividend.GetDigitAt( Count );
      int Inverse = MultInverseArray[Count, DivideByDigit];
      // So DivideByDigit * Inverse = 1.
      Inverse = Inverse * DividendDigit;
      // So DivideByDigit * Inverse * DividendDigit = 1 * DividendDigit.

      Inverse = Inverse % Prime;
      Quotient.SetDigitAt( Inverse, Count );

      /*
      int Test = Quotient.GetDigitAt( Count );
      Test = Test * DivideBy.GetDigitAt( Count );
      Test = Test % Prime;
      if( Test != Dividend.GetDigitAt( Count ))
        throw( new Exception( "Test != Dividend.GetDigitAt( Count ) in MultiplicativeInverse()." ));
      */
      }
    }


  }
}

