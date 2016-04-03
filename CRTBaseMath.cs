// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Text;
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{
  class CRTBaseMath
  {
  private IntegerMath IntMath;
  private CRTMath CRTMath1;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private bool Cancelled = false;
  private string[] BaseStringsArray;
  private ChineseRemainder[] CRTBaseArray;
  private ChineseRemainder[] CRTBaseModArray;
  private ChineseRemainder[] NumbersArray;
  private int[,] MultInverseArray;
  private ChineseRemainder CRTAccumulateBase;
  private ChineseRemainder CRTAccumulateForBaseMultiples;
  private ChineseRemainder CRTAccumulateBasePart;
  private ChineseRemainder CRTAccumulatePart;
  private Integer BaseModArrayModulus;
  private Integer[] BaseArray;
  private ChineseRemainder CRTTempForIsEqual;
  private ChineseRemainder CRTWorkingTemp;
  private ChineseRemainder CRTXForModPower;
  private Integer ExponentCopy;
  private ChineseRemainder CRTAccumulate;
  private ChineseRemainder CRTCopyForSquare;
  private ulong QuotientForTest = 0;
  private Integer FermatExponent;
  private ChineseRemainder CRTFermatModulus;
  private Integer FermatModulus;
  private ChineseRemainder CRTTestFermat;


  private CRTBaseMath()
    {

    }



  internal CRTBaseMath( BackgroundWorker UseWorker, CRTMath UseCRTMath )
    {
    // Most of these are created ahead of time so that
    // they don't have to be created inside a loop.
    Worker = UseWorker;
    IntMath = new IntegerMath();
    CRTMath1 = UseCRTMath;
    Quotient = new Integer();
    Remainder = new Integer();
    CRTAccumulateBase = new ChineseRemainder( IntMath );
    CRTAccumulateBasePart = new ChineseRemainder( IntMath );
    CRTAccumulateForBaseMultiples = new ChineseRemainder( IntMath );
    CRTAccumulatePart = new ChineseRemainder( IntMath );
    BaseModArrayModulus = new Integer();
    CRTTempForIsEqual = new ChineseRemainder( IntMath );
    CRTWorkingTemp = new ChineseRemainder( IntMath );
    ExponentCopy = new Integer();
    CRTXForModPower = new ChineseRemainder( IntMath );
    CRTAccumulate = new ChineseRemainder( IntMath );
    CRTCopyForSquare = new ChineseRemainder( IntMath );
    FermatExponent = new Integer();
    CRTFermatModulus = new ChineseRemainder( IntMath );
    FermatModulus = new Integer();
    CRTTestFermat = new ChineseRemainder( IntMath );

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
    int BiggestPrime = (int)IntMath.GetPrimeAt( CRTBase.DigitsArraySize - 1 );

    MultInverseArray = new int[CRTBase.DigitsArraySize, BiggestPrime];
    for( int Count = 0; Count < CRTBase.DigitsArraySize; Count++ )
      {
      int Prime = (int)IntMath.GetPrimeAt( Count );
      if( (Count & 0xF) == 1 )
        Worker.ReportProgress( 0, Count.ToString() + ") Setting mult inverses for prime: " + Prime.ToString() );

      for( int Digit = 1; Digit < Prime; Digit++ )
        {
        if( Worker.CancellationPending )
          return;

        // Do the Euclidean algorithm instead of this.
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



  internal void ModularPower( ChineseRemainder CRTResult,
                              Integer Exponent,
                              ChineseRemainder CRTModulus,
                              bool UsePresetBaseArray )
    {
    // The square and multiply method is in Wikipedia:
    // https://en.wikipedia.org/wiki/Exponentiation_by_squaring

    if( Worker.CancellationPending )
      return;

    if( CRTResult.IsZero())
      return; // With CRTResult still zero.

    if( CRTResult.IsEqual( CRTModulus ))
      {
      // It is congruent to zero % ModN.
      CRTResult.SetToZero();
      return;
      }

    // Result is not zero at this point.
    if( Exponent.IsZero() )
      {
      CRTResult.SetToOne();
      return;
      }

    Integer Result = new Integer();
    CRTMath1.GetTraditionalInteger( Result, CRTResult );

    Integer Modulus = new Integer();
    CRTMath1.GetTraditionalInteger( Modulus, CRTModulus );

    if( Modulus.ParamIsGreater( Result ))
      {
      // throw( new Exception( "This is not supposed to be input for RSA plain text." ));
      IntMath.Divide( Result, Modulus, Quotient, Remainder );
      Result.Copy( Remainder );
      CRTResult.SetFromTraditionalInteger( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    if( !UsePresetBaseArray )
      SetupBaseModArray( Modulus );

    if( CRTBaseModArray == null )
      throw( new Exception( "SetupBaseModArray() should have already been done here." ));

    CRTXForModPower.Copy( CRTResult );
    ExponentCopy.Copy( Exponent );
    int TestIndex = 0;
    CRTResult.SetToOne();

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

    ModularReduction( CRTResult, CRTAccumulate );
    CRTResult.Copy( CRTAccumulate );

    // Division is never used in the loop above.

    // This is a very small Quotient.
    // See SetupBaseMultiples() for a description of how to calculate
    // the maximum size of this quotient.
    CRTMath1.GetTraditionalInteger( Result, CRTResult );
    IntMath.Divide( Result, Modulus, Quotient, Remainder );

    // Is the Quotient bigger than a 32 bit integer?
    if( Quotient.GetIndex() > 0 )
      throw( new Exception( "I haven't ever seen this happen. Quotient.GetIndex() > 0.  It is: " + Quotient.GetIndex().ToString() ));

    QuotientForTest = Quotient.GetAsULong();
    if( QuotientForTest > 2097867 )
      throw( new Exception( "This can never happen unless I increase ChineseRemainder.DigitsArraySize." ));

    Result.Copy( Remainder );
    CRTResult.SetFromTraditionalInteger( Remainder );
    }



  // Copyright Eric Chauvin.
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

    CRTBase CRTBaseInput = new CRTBase( IntMath );
    int HowManyToAdd = SetFromCRTNumber( CRTBaseInput, CRTInput );

    // Integer Test = new Integer();
    // ChineseRemainder CRTTest = new ChineseRemainder( IntMath );
    // GetTraditionalInteger( CRTBaseInput, Test );
    // CRTTest.SetFromTraditionalInteger( Test );
    // if( !CRTTest.IsEqual( CRTInput ))
      // throw( new Exception( "CRTTest for CRTInput isn't right." ));

    // Count starts at 1, so it's the prime 3.
    for( int Count = 1; Count <= HowManyToAdd; Count++ )
      {
      // BaseMultiple is a number that is not bigger
      // than the prime at this point.  (The prime at:
      // IntMath.GetPrimeAt( Count ).)
      uint BaseMultiple = (uint)CRTBaseInput.GetDigitAt( Count );

      // It uses the CRTBaseModArray here:
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



  internal int SetFromCRTNumber( CRTBase ToSet, ChineseRemainder SetFrom )
    {
    try
    {
    if( NumbersArray == null )
      throw( new Exception( "Bug: The NumbersArray should have been set up already." ));

    // ToSet.SetToZero();

    // CRTBaseArray[0] is 1.
    if( SetFrom.GetDigitAt( 0 ) == 1 )
      {
      ToSet.SetToOne(); // 1 times 1 for this base.
      CRTAccumulateForBaseMultiples.SetToOne();
      }
    else
      {
      ToSet.SetToZero();
      CRTAccumulateForBaseMultiples.SetToZero();
      }

    int HighestNonZeroDigit = 1;
    // Count starts at 1, so it's at the prime 3.
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int Prime = (int)IntMath.GetPrimeAt( Count );
      int AccumulateDigit = CRTAccumulateForBaseMultiples.GetDigitAt( Count );
      int CRTInputTestDigit = SetFrom.GetDigitAt( Count );
      int BaseDigit = CRTBaseArray[Count].GetDigitAt( Count );
      if( BaseDigit == 0 )
        throw( new Exception( "This never happens. BaseDigit == 0." ));

      int BaseMult = CRTInputTestDigit;
      if( BaseMult < AccumulateDigit )
        BaseMult += Prime;

      BaseMult -= AccumulateDigit;
      int Inverse = MultInverseArray[Count, BaseDigit];
      BaseMult = (BaseMult * Inverse) % Prime;

      ToSet.SetDigitAt( BaseMult, Count );
      if( BaseMult != 0 )
        HighestNonZeroDigit = Count;

      // Notice that this is using CRTBaseArray and not
      // CRTBaseModArray.
      // This would be very fast in parallel hardware,
      // but not in software that has to do each digit
      // one at a time.
      CRTAccumulatePart.Copy( CRTBaseArray[Count] );
      CRTAccumulatePart.Multiply( NumbersArray[BaseMult] );
      CRTAccumulateForBaseMultiples.Add( CRTAccumulatePart );
      }

    return HighestNonZeroDigit;
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetFromCRTNumber(): " + Except.Message ));
      }
    }



  internal void GetTraditionalInteger( CRTBase ToGetFrom, Integer ToSet )
    {
    try
    {
    if( CRTBaseArray == null )
      throw( new Exception( "Bug: The BaseArray should have been set up already." ));

    // This first one has the prime 2 as its base so
    // it's going to be set to either zero or one.
    if( ToGetFrom.GetDigitAt( 0 ) == 1 )
      ToSet.SetToOne();
    else
      ToSet.SetToZero();

    Integer WorkingBase = new Integer();
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int BaseMult = ToGetFrom.GetDigitAt( Count );
      WorkingBase.Copy( BaseArray[Count] );
      IntMath.MultiplyUInt( WorkingBase, (uint)BaseMult );
      ToSet.Add( WorkingBase );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetTraditionalInteger(): " + Except.Message ));
      }
    }



// These bottom digits are 0 for each prime that gets
// multiplied by the base.  So they keep getting one
// more zero at the bottom of each one.
// But the digits in BaseModArray only have the zeros
// at the bottom on the ones that are smaller than the
// modulus.
// At BaseArray[0] it's 1, 1, 1, 1, 1, .... for all of them.
// 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0
// 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 1, 0, 0
// 30, 30, 30, 30, 1, 7, 11, 13, 4, 8, 2, 0, 0, 0

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

    BaseStringsArray = new string[ChineseRemainder.DigitsArraySize];
    BaseArray = new Integer[ChineseRemainder.DigitsArraySize];
    CRTBaseArray = new ChineseRemainder[ChineseRemainder.DigitsArraySize];

    Integer SetBase = new Integer();
    ChineseRemainder CRTSetBase = new ChineseRemainder( IntMath );

    Integer BigBase = new Integer();
    ChineseRemainder CRTBigBase = new ChineseRemainder( IntMath );

    BigBase.SetFromULong( 2 );
    CRTBigBase.SetFromUInt( 2 );
    string BaseS = "2";

    SetBase.SetToOne();
    CRTSetBase.SetToOne();

    // The base at zero is 1.
    BaseArray[0] = SetBase;
    CRTBaseArray[0] = CRTSetBase;
    BaseStringsArray[0] = "1";

    ChineseRemainder CRTTemp = new ChineseRemainder( IntMath );

    // The first time through the loop the base
    // is set to 2.
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

      BaseStringsArray[Count] = BaseS;
      BaseArray[Count] = SetBase;
      CRTBaseArray[Count] = CRTSetBase;
      // if( Count < 50 )
        // Worker.ReportProgress( 0, CRTBaseArray[Count].GetString() );

      if( !IsEqualToInteger( CRTBaseArray[Count],
                             BaseArray[Count] ))
        throw( new Exception( "Bug.  The bases aren't equal." ));

      // Multiply it for the next BigBase.
      uint Prime = IntMath.GetPrimeAt( Count );
      BaseS = BaseS + "*" + Prime.ToString();
      IntMath.MultiplyUInt( BigBase, Prime );
      CRTBigBase.Multiply( NumbersArray[IntMath.GetPrimeAt( Count )] );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupBaseArray(): " + Except.Message ));
      }
    }



  // CRTBaseModArray doesn't have the pattern of zeros
  // down to the end like in CRTBaseArray.
  internal void SetupBaseModArray( Integer Modulus )
    {
    try
    {
    BaseModArrayModulus = Modulus;

    if( NumbersArray == null )
      throw( new Exception( "NumbersArray should have already been setup in SetupBaseModArray()." ));

    CRTBaseModArray = new ChineseRemainder[ChineseRemainder.DigitsArraySize];

    ChineseRemainder CRTSetBase = new ChineseRemainder( IntMath );

    Integer BigBase = new Integer();
    ChineseRemainder CRTBigBase = new ChineseRemainder( IntMath );

    BigBase.SetFromULong( 2 );
    CRTBigBase.SetFromUInt( 2 );

    CRTSetBase.SetToOne();
    CRTBaseModArray[0] = CRTSetBase;

    ChineseRemainder CRTTemp = new ChineseRemainder( IntMath );

    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      CRTSetBase = new ChineseRemainder( IntMath );
      CRTSetBase.Copy( CRTBigBase );
      CRTBaseModArray[Count] = CRTSetBase;

      // Multiply it for the next BigBase.
      IntMath.MultiplyUInt( BigBase, IntMath.GetPrimeAt( Count ));
      IntMath.Divide( BigBase, Modulus, Quotient, Remainder );
      BigBase.Copy( Remainder );
      CRTBigBase.SetFromTraditionalInteger( BigBase );
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
    uint BiggestPrime = IntMath.GetPrimeAt( CRTBase.DigitsArraySize + 1 );
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



  internal bool IsFermatPrime( ChineseRemainder CRTToTest, int HowMany )
    {
    // Also see Rabin-Miller test.
    // Also see Solovay-Strassen test.

    // This Fermat primality test is usually described
    // as using random primes to test with, and you
    // could do it that way too.
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
    CRTMath1.GetTraditionalInteger( FermatExponent, CRTToTest );
    IntMath.SubtractULong( FermatExponent, 1 );
    // This is a very small Modulus since it's being set from a uint.
    CRTTestFermat.SetFromUInt( Base ); 
    CRTFermatModulus.Copy( CRTToTest );
    CRTMath1.GetTraditionalInteger( FermatModulus, CRTFermatModulus );

    ModularPower( CRTTestFermat, FermatExponent, CRTFermatModulus, false );
    if( CRTTestFermat.IsOne())
      return true; // It passed the test. It _might_ be a prime.
    else
      return false; // It is _definitely_ a composite number.

    }




  internal void GetExponentForm( CRTBase ToGetFrom, uint BaseVal )
    {
    try
    {
    if( CRTBaseArray == null )
      throw( new Exception( "Bug: The BaseArray should have been set up already." ));

    StringBuilder SBuilder = new StringBuilder();

    string BaseS = BaseVal.ToString();
    // This first one has the prime 2 as its base so
    // it's going to be set to either zero or one.
    if( ToGetFrom.GetDigitAt( 0 ) == 1 )
      SBuilder.Append( "(" + BaseS + "^1) " );
    else
      SBuilder.Append( "(" + BaseS + "^0) " );

    Integer WorkingBase = new Integer();
    for( int Count = 1; Count < ChineseRemainder.DigitsArraySize; Count++ )
      {
      int BaseMult = ToGetFrom.GetDigitAt( Count );
      if( BaseMult == 0 )
        continue;

      // WorkingBase.Copy( BaseArray[Count] );
      SBuilder.Append( "(" + BaseS + "^(" + BaseMult.ToString() + "*(" + BaseStringsArray[Count] + "))) " );

      // IntMath.MultiplyUInt( WorkingBase, (uint)BaseMult );
      // ToSet.Add( WorkingBase );
      }

    Worker.ReportProgress( 0, SBuilder.ToString() );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetExponentForm(): " + Except.Message ));
      }
    }



  }
}


