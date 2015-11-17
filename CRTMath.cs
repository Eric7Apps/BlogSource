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
  class CRTMath
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer ExponentCopy;
  private ChineseRemainder CRTAccumulate;
  private ChineseRemainder CRTAccumulateExact;
  private ChineseRemainder CRTAccumulatePart;
  private ChineseRemainder CRTWorkingTemp;
  private ChineseRemainder CRTXForModPower;
  private Integer[] BaseArray;
  private ChineseRemainder[] CRTBaseArray;
  private ChineseRemainder[] CRTBaseModArray;
  private ChineseRemainder[] NumbersArray;
  private ChineseRemainder CRTCopyForSquare;
  private Integer AccumulateBase;
  private ChineseRemainder CRTAccumulateBase;
  private Integer ToTestForTraditionalInteger;
  private ChineseRemainder CRTToTestForTraditionalInteger;
  private ChineseRemainder CRTTempForIsEqual;
  private Integer FermatExponent;
  private ChineseRemainder CRTFermatModulus;
  private Integer FermatModulus;
  private ChineseRemainder CRTTestFermat;
  internal ulong QuotientForTest = 0;
  private int[,] MultInverseArray;
  private BackgroundWorker Worker;
  private Integer BaseModArrayModulus;
  private bool Cancelled = false;
  private ChineseRemainder CRTAccumulateForBaseMultiples;



  private CRTMath()
    {

    }



  internal CRTMath( BackgroundWorker UseWorker )
    {
    // Most of these are created ahead of time so that they don't have
    // to be created inside a loop.
    Worker = UseWorker;
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    ExponentCopy = new Integer();
    CRTXForModPower = new ChineseRemainder( IntMath );
    AccumulateBase = new Integer();
    CRTCopyForSquare = new ChineseRemainder( IntMath );
    CRTAccumulateBase = new ChineseRemainder( IntMath );
    CRTAccumulate = new ChineseRemainder( IntMath );
    CRTAccumulateExact = new ChineseRemainder( IntMath );
    CRTAccumulatePart = new ChineseRemainder( IntMath );
    CRTWorkingTemp = new ChineseRemainder( IntMath );
    ToTestForTraditionalInteger = new Integer();
    CRTToTestForTraditionalInteger = new ChineseRemainder( IntMath );
    CRTTempForIsEqual = new ChineseRemainder( IntMath );
    FermatExponent = new Integer();
    CRTFermatModulus = new ChineseRemainder( IntMath );
    FermatModulus = new Integer();
    CRTTestFermat = new ChineseRemainder( IntMath );
    CRTAccumulateForBaseMultiples = new ChineseRemainder( IntMath );


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

        for( int MultCount = 1; MultCount < Prime; MultCount++ )
          {
          if( ((MultCount * Digit) % Prime) == 1 )
            {
            MultInverseArray[Count, Digit] = MultCount;
            // if( Count < 40 )
              // Create either a tab-delimited file, or create source
              // code to copy it into a hard-coded form.
              // Or just let it generate it each time, like it does here.
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



  internal void ModularPower( ChineseRemainder CRTResult,
                              Integer Exponent,
                              ChineseRemainder CRTModulus )
    {
    // The square and multiply method is in Wikipedia:
    // https://en.wikipedia.org/wiki/Exponentiation_by_squaring

    if( Worker.CancellationPending )
      return;

    if( CRTBaseModArray == null )
      throw( new Exception( "SetupBaseModArray() should have already been done here." ));

    if( CRTResult.IsZero())
      return; // With CRTResult still zero.

    if( CRTResult.IsEqual( CRTModulus ))
      {
      // It is congruent to zero % ModN.
      // Result.SetToZero();
      CRTResult.SetToZero();
      return;
      }

    // Result is not zero at this point.
    if( Exponent.IsZero() )
      {
      // Result.SetToOne();
      CRTResult.SetToOne();
      return;
      }

    Integer Result = new Integer();
    GetTraditionalInteger( Result, CRTResult );

    Integer Modulus = new Integer();
    GetTraditionalInteger( Modulus, CRTModulus );

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

    CRTXForModPower.Copy( CRTResult );
    ExponentCopy.Copy( Exponent );
    int TestIndex = 0;
    CRTResult.SetToOne();

    Worker.ReportProgress( 0, "Before the loop in mod power." );

    int LoopsTest = 0;
    while( true )
      {
      LoopsTest++;
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 ) // If the bottom bit is 1.
        {
        CRTResult.Multiply( CRTXForModPower );
        ModularReduction( CRTResult, CRTAccumulate );
        CRTResult.Copy( CRTAccumulate );
        }

      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( ExponentCopy.IsZero())
        break;

      // Square it.
      CRTCopyForSquare.Copy( CRTXForModPower );
      CRTXForModPower.Multiply( CRTCopyForSquare );
      ModularReduction( CRTXForModPower, CRTAccumulate );
      CRTXForModPower.Copy( CRTAccumulate );
      }

    Worker.ReportProgress( 0, "LoopsTest: " + LoopsTest.ToString() );

    // Division is never used in the loop above.

    // This is a very small Quotient.
    // See SetupBaseMultiples() for a description of how to calculate
    // the maximum size of this quotient.
    GetTraditionalInteger( Result, CRTResult );
    IntMath.Divide( Result, Modulus, Quotient, Remainder );

    // The point of having this Modular Reduction algorithm is that it keeps
    // this Quotient very small, and that this Divide() doesn't have to be
    // done at all during the big loop above.  It's only done once at the end.

    // Is the Quotient bigger than a 32 bit integer?
    if( Quotient.GetIndex() > 0 )
      throw( new Exception( "I haven't ever seen this happen. Quotient.GetIndex() > 0.  It is: " + Quotient.GetIndex().ToString() ));

    if( Quotient.GetIndex() > 1 )
      throw( new Exception( "Quotient.GetIndex() > 1.  Haven't ever seen it.  Quotient is: " + Quotient.GetIndex().ToString() ));

    QuotientForTest = Quotient.GetAsULong();
    if( QuotientForTest > 2097867 )
      throw( new Exception( "This can never happen unless I increase ChineseRemainder.DigitsArraySize." ));

    // For a 2048 bit modulus the max it gets to is more like 564,319.

    // For a 4096 bit modulus the max is more like 2,039,559.
    // So DigitsArraySize would have to be increased for working 
    // with a larger modulus.

    // Typical example Quotient values for a 2048 bit modulus:
    // QuotientForTest: 41,334
    // QuotientForTest: 43,681
    // QuotientForTest: 37,967

    // Typical for a 4096 bit modulus:
    // QuotientForTest: 154,832

    // Assume I don't know what the quotient is but I want to find
    // out what it is.  In other words I want to find the complete 
    // output (before that Divide() happens at the end) of
    // ModularPower() when I know the CipherText.
    // It takes less than one second to find it.

    // Let's just say I got the public key modulus and exponent from the
    // TLS ServerCertificate message (see my blog), and I got the
    // CipherText from the TLS ClientKeyExchange message.
    /*
    ECTime FindQuotientTime = new ECTime();
    FindQuotientTime.SetToNow();
    ChineseRemainder CRTModulusMultiple = new ChineseRemainder( IntMath );
    ChineseRemainder CountTest = new ChineseRemainder( IntMath );
    ChineseRemainder CipherText = new ChineseRemainder( IntMath );

    // So assume I have the CipherText value, but not the quotient.
    CipherText.SetFromTraditionalInteger( Remainder, IntMath );

    uint HighestItCanPossiblyBe = (uint)IntMath.GetSumOfPrimesUpToAndIncluding( ChineseRemainder.DigitsArraySize );
    for( uint Count = 0; Count < HighestItCanPossiblyBe; Count++ )
      {
      if( Cancelled )
        break;

      CRTModulusMultiple.Copy( CRTModulus );
      CountTest.SetFromUInt( Count );
      CRTModulusMultiple.Multiply( CountTest );
      CRTModulusMultiple.Add( CipherText );
      if( CRTResult.IsEqual( CRTModulusMultiple ))
        {
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Found the quotient matching this CipherText at: " + Count.ToString( "N0" ));
        Worker.ReportProgress( 0, "It took " + FindQuotientTime.GetSecondsToNow().ToString( "N1" ) + " seconds to find it." );
        // It took 0.8 seconds to find it.
        // It took 0.7 seconds to find it.
        Worker.ReportProgress( 0, " " );
        break;
        }
      }
      */

    Result.Copy( Remainder );
    CRTResult.SetFromTraditionalInteger( Remainder, IntMath );
    }



  internal void ModularReduction( ChineseRemainder CRTInput,
                                 ChineseRemainder CRTAccumulate )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "Bug: The NumbersArray should have been set up already." ));

    if( CRTBaseModArray == null )
      throw( new Exception( "Bug: The BaseModArray should have been set up already." ));

    // This first one has the prime 2 as its base so it's going to
    // be set to either zero or one.
    if( CRTInput.GetDigitAt( 0 ) == 1 )
      {
      CRTAccumulate.SetToOne();
      }
    else
      {
      CRTAccumulate.SetToZero();
      }

    // SetupBaseMultiples() is what makes this very slow.
    int HowManyToAdd = SetupBaseMultiples( CRTInput );

    // Count starts at 1, so it's the prime 3.
    for( int Count = 1; Count <= HowManyToAdd; Count++ )
      {
      // BaseMultiple is a number that is not bigger than the
      // prime at this point.  (The prime at: IntMath.GetPrimeAt( Count ).)
      uint BaseMultiple = (uint)CRTInput.GetBaseMultiple( Count );

      // Unlike in SetupBaseMultiples() it uses the
      // CRTBaseModArray here:
      CRTWorkingTemp.Copy( CRTBaseModArray[Count] );

      CRTWorkingTemp.Multiply( NumbersArray[BaseMultiple] );
      CRTAccumulate.Add( CRTWorkingTemp );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ModularReduction(): " + Except.Message ));
      }
    }



  internal int SetupBaseMultiples( ChineseRemainder ToSetup )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "Bug: The NumbersArray should have been set up already." ));

    // CRTBaseArray[0] is 1.
    if( ToSetup.GetDigitAt( 0 ) == 1 )
      {
      ToSetup.SetBaseMultiple( 1, 0 ); // 1 times 1 for this base.
      CRTAccumulateForBaseMultiples.SetToOne();
      }
    else
      {
      ToSetup.SetBaseMultiple( 0, 0 );
      CRTAccumulateForBaseMultiples.SetToZero();
      }

    int HighestNonZero = 0;

    // HighestNonZero relates to the maximum size the quotient can
    // be at the end of ModularPower().
    // If the highest non-zero match was 420, that goes with the prime
    // number at IntMath.GetPrimeAt( 420 ). And it goes with
    // BaseModArray[420].
    // When 1024-bit primes are used the modulus is a 2048-bit number,
    // and so its Index is 63. (That's 64 32-bit digits.)  But the
    // size of the numbers that have to be reduced (CRTInput) are
    // about the size of the modulus squared.  So the Index for
    // those numbers is about 127 or 128 or so.  And so those are
    // the numbers that can go up to about the 420th prime or so for
    // the BaseArray.  That means that as many as 420 numbers
    // get added up from the BaseArray in to CRTAccumulate.
    // When MatchingValue gets multiplied by the base, MatchingValue
    // is not bigger than the size of the prime at that point.  And
    // so the size of the prime at each point, and the number of
    // BaseArray parts that get added, contributes to the size of
    // QuotientForTest in ModularPower().  See below for how it
    // calculates the maximum size those numbers can possibly ever
    // be, with HighestItCanBe and MaxQuotientForTest.

    // Count starts at 1, so it's at the prime 3.
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      // This system works as long as the primes associated with these
      // digits are in a consistent order.  They could be in any arbitrary
      // order throughout this system, as long as they are consistent
      // throughout the system.
      int Prime = (int)IntMath.GetPrimeAt( Count );
      int AccumulateDigit = CRTAccumulateForBaseMultiples.GetDigitAt( Count );
      int CRTInputTestDigit = ToSetup.GetDigitAt( Count );

      // These base digits are always the same every time this method is 
      // called, with any input.  BaseDigit is never zero.
      // They are always these same series of numbers:
      // BaseDigit at Count 1 is 2.  (2 mod 3 is 2.)
      // BaseDigit at Count 2 is 1.  (6 mod 5 is 1.)
      // BaseDigit at Count 3 is 2.  (30 mod 7 is 2.)
      // At BaseArray[0] it's 1, 1, 1, 1, 1, .... all ones for all of them.
      // 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0,
      // 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 1, 0, 0,
      // 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 1, 7, 11, 13, 4, 8, 2, 0, 0, 0,
      int BaseDigit = CRTBaseArray[Count].GetDigitAt( Count );
      if( BaseDigit == 0 )
        throw( new Exception( "This never happens. BaseDigit == 0." ));

      // See GetTraditionalInteger() to see more about how
      // BaseMult is calculated.

      int BaseMult = CRTInputTestDigit;
      if( BaseMult < AccumulateDigit )
        BaseMult += Prime;

      BaseMult -= AccumulateDigit;
      int Inverse = MultInverseArray[Count, BaseDigit];
      BaseMult = (BaseMult * Inverse) % Prime;

      ToSetup.SetBaseMultiple( BaseMult, Count );
      if( BaseMult != 0 )
        HighestNonZero = Count;

      // Accumulate it for the next loop.
      // Notice that this is using CRTBaseArray and not
      // CRTBaseModArray.

      // This would be very fast in parallel hardware, but not in software
      // that has to do each digit, one at a time.
      CRTAccumulatePart.Copy( CRTBaseArray[Count] );
      CRTAccumulatePart.Multiply( NumbersArray[BaseMult] );
      CRTAccumulateForBaseMultiples.Add( CRTAccumulatePart );
      }

    // ulong MaxQuotientForTest = IntMath.GetSumOfPrimesUpToAndIncluding( HighestNonZero );
    // Worker.ReportProgress( 0, "MaxQuotientForTest: " + MaxQuotientForTest.ToString( "N0" ));

    // ulong HighestItCanBe = IntMath.GetSumOfPrimesUpToAndIncluding( ChineseRemainder.DigitsArraySize );
    // Worker.ReportProgress( 0, "HighestItCanBe: " + HighestItCanBe.ToString( "N0" ));

    // This is he highest it could possibly be for the DigitsArraySize being
    // used in ChineseRemainder.cs.
    // HighestItCanBe: 2,097,867

    // For a 2048 bit modulus:
    // MaxQuotientForTest: 41,741
    // MaxQuotientForTest: 147,388
    // MaxQuotientForTest: 527,248
    // MaxQuotientForTest: 564,319
    // MaxQuotientForTest: 564,319
    // MaxQuotientForTest: 564,319
    // And staying that way at its max of 564,319 for a while...
    // Then the last one
    // MaxQuotientForTest: 325,019
    // The final QuotientForTest: 40,233

    // See how the digits array size is set:
    // Integer.DigitArraySize = ((1024 * 12) / 32) + 1;
    // ChineseRemainder.DigitsArraySize = Integer.DigitArraySize * 2;

    // The DigitsArraySize being used in this example is barely big enough
    // for a 4096-bit modulus.
    // MaxQuotientForTest: 2,039,559  Staying there a while...
    // MaxQuotientForTest: 1,179,453  Then dropping to this at the end.
    // QuotientForTest: 154,832 The last Quotient.

    return HighestNonZero;
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupBaseMultiples(): " + Except.Message ));
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
      // This loop shows how it tries to find a matching CountPrime.
      // It is for bug testing, but it shows clearly how it is
      // trying to find a number that matches up with the CRTInput
      // digit.
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
614,889,782,588,491,410 * 6 +  // BigBase times CountPrime at Prime: 53
32,589,158,477,190,044,730 * 10 +  // BigBase times CountPrime at Prime: 59
1,922,760,350,154,212,639,070 * 20 +  // BigBase times CountPrime at Prime: 61
117,288,381,359,406,970,983,270 * 16 +  // BigBase times CountPrime at Prime: 67
7,858,321,551,080,267,055,879,090 * 10 +  // BigBase times CountPrime at Prime: 71
557,940,830,126,698,960,967,415,390 * 7 +  // BigBase times CountPrime at Prime: 73
*/



  internal void GetIntegerFromBaseMultiples( ChineseRemainder ToGetFrom, Integer ToGet )
    {
    try
    {
    if( BaseArray == null )
      throw( new Exception( "Bug: The BaseArray should have been set up already." ));

    // This first one has the prime 2 as its base so it's going to
    // be set to either zero or one.
    if( ToGetFrom.GetBaseMultiple( 0 ) == 1 )
      ToGet.SetToOne();
    else
      ToGet.SetToZero();

    Integer WorkingBase = new Integer();
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int BaseMult = ToGetFrom.GetBaseMultiple( Count );
      WorkingBase.Copy( BaseArray[Count] );
      IntMath.MultiplyUInt( WorkingBase, (uint)BaseMult );
      ToGet.Add( WorkingBase );
      if( ToGetFrom.GetDigitAt( Count ) != (int)IntMath.GetMod32( ToGet, IntMath.GetPrimeAt( Count )))
        throw( new Exception( "Bug in GetIntegerFromBaseMultiples()." ));

      // int AccumulateDigit = CRTAccumulate.GetDigitAt( Count );
      }

    // Returns with ToGet for the value.
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetIntegerFromBaseMultiples(): " + Except.Message ));
      }
    }




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



  // The primes that are used in the ChineseRemainder numbers could be in
  // any arbitrary order, but these base values would have to be
  // consistent with that order.
  // The order of primes might be arbitrarily set to:
  // 7, 5, 13, 3, ....
  // And so the first few base values would be:
  // 7, 7*5, 7*5*13, 7*5*13*3, ...
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



  /*
CRTBaseModArray doesn't have the pattern of zeros down to the end like in CRTBaseArray.
3, 45, 42, 19, 26, 4, 41, 16, 24, 25, 27, 17, 7, 1, 0, 4, 7, 5, 0, 0, 1, 
29, 55, 21, 0, 45, 22, 8, 22, 28, 31, 16, 22, 4, 1, 0, 12, 6, 6, 0, 0, 1, 
35, 10, 23, 1, 57, 34, 19, 29, 35, 0, 18, 28, 20, 18, 0, 8, 4, 4, 0, 1, 1, 
45, 21, 56, 18, 16, 0, 12, 6, 20, 20, 16, 17, 0, 10, 0, 0, 0, 4, 0, 1, 1, 
20, 8, 34, 34, 50, 19, 10, 14, 29, 9, 28, 22, 8, 0, 0, 4, 4, 5, 0, 2, 0, 
  */

  internal void SetupBaseModArray( Integer Modulus )
    {
    try
    {
    BaseModArrayModulus = Modulus;

    if( NumbersArray == null )
      throw( new Exception( "NumbersArray should have already been setup in SetupBaseModArray()." ));

    // BaseModArray = new Integer[ChineseRemainder.DigitsArraySize];
    CRTBaseModArray = new ChineseRemainder[ChineseRemainder.DigitsArraySize];

    // Integer SetBase = new Integer();
    ChineseRemainder CRTSetBase = new ChineseRemainder( IntMath );

    Integer BigBase = new Integer();
    ChineseRemainder CRTBigBase = new ChineseRemainder( IntMath );

    BigBase.SetFromULong( 2 );
    CRTBigBase.SetFromUInt( 2 );

    // SetBase.SetFromULong( 1 );
    CRTSetBase.SetToOne();

    // BaseModArray[0] = SetBase;
    CRTBaseModArray[0] = CRTSetBase;

    ChineseRemainder CRTTemp = new ChineseRemainder( IntMath );

    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      // SetBase = new Integer();
      CRTSetBase = new ChineseRemainder( IntMath );

      // SetBase.Copy( BigBase );
      CRTSetBase.Copy( CRTBigBase );

      // BaseModArray[Count] = SetBase;

      CRTBaseModArray[Count] = CRTSetBase;
      // if( Count < 50 )
        // Worker.ReportProgress( 0, CRTBaseModArray[Count].GetString() );

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
      CRTSetNumber.SetFromTraditionalInteger( SetNumber, IntMath );
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
    CRTTempForIsEqual.SetFromTraditionalInteger( Test, IntMath );
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


/*
1 +
2 * 1 +  // BigBase times CountPrime at Prime: 3
6 * 2 +  // BigBase times CountPrime at Prime: 5
30 * 3 +  // BigBase times CountPrime at Prime: 7
210 * 8 +  // BigBase times CountPrime at Prime: 11
2,310 * 11 +  // BigBase times CountPrime at Prime: 13
30,030 * 11 +  // BigBase times CountPrime at Prime: 17
510,510 * 17 +  // BigBase times CountPrime at Prime: 19


1 +
2 * 1 +  // BigBase times CountPrime at Prime: 3
2*3 * 2 +  // BigBase times CountPrime at Prime: 5
2*3*5 * 3 +  // BigBase times CountPrime at Prime: 7
2*3*5*7 * 8 +  // BigBase times CountPrime at Prime: 11
...
*/


  // This is a different way of setting the base multiples.
  // It is going backwards through the sequence of accumulated
  // values.
  // It is orders of magnitude slower than SetupBaseMultiples()
  // but it's just to show how it can be done from the top (backwards).
  internal void SetBaseMultiplesFromInteger( Integer FindFrom,
                                             ChineseRemainder CRTToSet )
    {
    CRTToSet.SetAllBaseMultiplesToZero();

    int TopIndex = 0;
    for( int Count = ChineseRemainder.DigitsArraySize - 1; Count >= 0; Count-- )
      {
      if( Cancelled )
        return;

      Integer BaseVal = GetBaseIntegerAt( Count );
      if( BaseVal.ParamIsGreater( FindFrom ))
        {
        TopIndex = Count;
        Worker.ReportProgress( 0, Count.ToString() + ") BaseVal is small enough." );
        break;
        }
      }

    Integer WorkingTotal = new Integer();
    uint Prime = IntMath.GetPrimeAt( TopIndex );
    uint Biggest = Prime - 1;
    int NextIndex = 0;
    for( uint TopCount = Biggest; TopCount >= 1; TopCount-- )
      {
      if( Cancelled )
        return;

      WorkingTotal.Copy( GetBaseIntegerAt( TopIndex ));
      IntMath.MultiplyUInt( WorkingTotal, TopCount );
      if( FindFrom.ParamIsGreater( WorkingTotal ))
        continue;

      // Worker.ReportProgress( 0, "BiggestTop value is: " + TopCount.ToString() );
      // Worker.ReportProgress( 0, "Prime is: " + Prime.ToString() );
      CRTToSet.SetBaseMultiple( (int)TopCount, TopIndex );
      NextIndex = (int)TopIndex - 1;
      break;
      }

    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "NextIndex after first value set: " + NextIndex.ToString() );

    // while( not forever )
    for( int Count = 0; Count < 10000; Count++ )
      {
      if( Cancelled )
        return;

      // Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "Found the next index at: " + NextIndex.ToString() );
      Prime = IntMath.GetPrimeAt( NextIndex );
      // Worker.ReportProgress( 0, "Prime is: " + Prime.ToString() );

      NextIndex = FindBaseMultipleAt( FindFrom,
                                      CRTToSet,
                                      TopIndex,
                                      NextIndex );

      if( NextIndex < 0 )
        break;

      }

    Integer Accumulate = new Integer();
    AccumulateTotal( CRTToSet,
                     TopIndex,
                     Accumulate );

    if( FindFrom.ParamIsGreater( Accumulate ))
      throw( new Exception( "Bug. Accumulate is bigger." ));

    if( !Accumulate.IsEqual( FindFrom ))
      throw( new Exception( "Accumulate isn't right in SetBaseMultiplesFromInteger()." ));

    // This just sets the digits in the array to match the magnitudes.
    CRTToSet.SetFromTraditionalInteger( FindFrom, IntMath );

    Worker.ReportProgress( 0, "Finished with SetBaseMultiplesFromInteger()." );
    Worker.ReportProgress( 0, " " );
    }



  internal void AccumulateTotal( ChineseRemainder CRTToSet,
                                int TopIndex,
                                Integer Accumulate )
    {
    Integer OnePart = new Integer();

    Accumulate.SetToZero();
    for( int Count = TopIndex; Count >= 0; Count-- )
      {
      int BaseMult = CRTToSet.GetBaseMultiple( Count );
      if( BaseMult == 0 )
        continue;

      OnePart.Copy( GetBaseIntegerAt( Count ));
      IntMath.MultiplyUInt( OnePart, (uint)BaseMult );
      Accumulate.Add( OnePart );
      }
    }


  internal int FindBaseMultipleAt( Integer FindFrom,
                                 ChineseRemainder CRTToSet,
                                 int TopIndex,
                                 int FindAtIndex )
    {
    Integer Accumulate = new Integer();

    int Prime = (int)IntMath.GetPrimeAt( FindAtIndex );
    int Biggest = Prime - 1;
    for( int Count = Biggest; Count >= 0; Count-- )
      {
      if( Cancelled )
        return -1;

      CRTToSet.SetBaseMultiple( Count, FindAtIndex );
      AccumulateTotal( CRTToSet,
                       TopIndex,
                       Accumulate );

      if( Accumulate.ParamIsGreaterOrEq( FindFrom ))
        {
        // Worker.ReportProgress( 0, "Found the BaseMultiple: " + Count.ToString() );
        return FindAtIndex - 1;
        }
      }

    throw( new Exception( "Bug in FindBaseMultipleAt(). Got to the bottom." ));
    }



  internal bool IsFermatPrime( ChineseRemainder CRTToTest, int HowMany )
    {
    // Also see Rabin-Miller test.
    // Also see Solovay-Strassen test.

    // Use bigger primes for Fermat test because mod 3, mod 5 etc are too likely
    // to be congruent to 1.  In other words it's a lot more likely to appear
    // to be a prime when it isn't.  This Fermat primality test is usually
    // described as using random primes to test with, and you could do
    // it that way too.
    // IntegerMath.PrimeArrayLength = 1024 * 32;

    int StartAt = 1024 * 16; // Or much bigger.
    for( int Count = StartAt; Count < (HowMany + StartAt); Count++ )
      {
      if( !IsFermatPrimeForOneValue( CRTToTest, IntMath.GetPrimeAt( Count )))
        return false;

      }

    // It _might_ be a prime if it passed this test.
    // Increasing HowMany increases the probability that it's a prime.
    return true;
    }



  // http://en.wikipedia.org/wiki/Primality_test
  // http://en.wikipedia.org/wiki/Fermat_primality_test

  internal bool IsFermatPrimeForOneValue( ChineseRemainder CRTToTest, uint Base  )
    {
    // This won't catch Carmichael numbers.
    // http://en.wikipedia.org/wiki/Carmichael_number

    // Assume ToTest is not a small number.  (Not the size of a small prime.)
    // Normally it would be something like a 1024 bit number or bigger, 
    // but I assume it's at least bigger than a 32 bit number.
    // Assume this has already been checked to see if it's divisible
    // by a small prime.

    // A has to be coprime to P and it is here because ToTest is not 
    // divisible by a small prime.

    // Fermat's little theorem:
    // A ^ (P - 1) is congruent to 1 mod P if P is a prime.
    // Or: A^P - A is congrunt to A mod P.
    // If you multiply A by itself P times then divide it by P, 
    // the remainder is A.  (A^P / P)
    // 5^3 = 125.  125 - 5 = 120.  A multiple of 5.
    // 2^7 = 128.  128 - 2 = 7 * 18 (a multiple of 7.)

    GetTraditionalInteger( FermatExponent, CRTToTest );
    IntMath.SubtractULong( FermatExponent, 1 );
    // This is a very small Modulus since it's being set from a uint.
    CRTTestFermat.SetFromUInt( Base ); 
    CRTFermatModulus.Copy( CRTToTest );
    GetTraditionalInteger( FermatModulus, CRTFermatModulus );

    // ModularReduction() will be very short with this small value.
    SetupBaseModArray( FermatModulus );

    ModularPower( CRTTestFermat, FermatExponent, CRTFermatModulus );
    if( CRTTestFermat.IsOne())
      return true; // It passed the test. It _might_ be a prime.
    else
      return false; // It is _definitely_ a composite number.

    }



  }
}

