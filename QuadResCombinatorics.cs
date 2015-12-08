// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker
using System.Threading; // For Sleep().



namespace ExampleServer
{
  class QuadResCombinatorics
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Product;
  private Integer SolutionP;
  private Integer SolutionQ;
  private Integer LastAccumulateValue;
  private Integer GetValueBasePart;
  private BackgroundWorker Worker;
  private QuadResWorkerInfo WInfo;
  private QuadResDigitsRec[] QuadResDigitsArray;
  private const int DigitsArrayLength = 4;
  private QuadResToPrimesRec[] QuadResToPrimesArray;
  private const int QuadResToPrimesArrayLength = 200;
  private int LastIncrementIndex = 0;
  private ECTime StartTime;



  internal struct QuadResToPrimesRec
    {
    internal bool[] GoodX;
    internal bool[] QuadRes;
    }


  internal struct QuadResDigitsRec
    {
    internal uint ProdModBase;
    internal uint Base;  // 2 * 3 * 5...
    internal int DigitIndex;
    internal uint[] DigitsArray;
    internal Integer BigBase;
    internal uint BigBaseBottomDigit;
    internal uint BigBaseModCurrentBase;
    internal uint[,] MatchingInverseArray;
    }


  private QuadResCombinatorics()
    {
    }



  internal QuadResCombinatorics( QuadResWorkerInfo UseWInfo, BackgroundWorker UseWorker )
    {
    WInfo = UseWInfo;
    Worker = UseWorker;
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    Product = new Integer();
    SolutionP = new Integer();
    SolutionQ = new Integer();
    LastAccumulateValue = new Integer();
    GetValueBasePart = new Integer();
    StartTime = new ECTime();
    StartTime.SetToNow();

    IntMath.SetFromString( Product, WInfo.PublicKeyModulus );
    QuadResDigitsArray = new QuadResDigitsRec[DigitsArrayLength];
    }



  internal void FreeEverything()
    {

    }



  internal string GetSolutionPString()
    {
    return IntMath.ToString10( SolutionP );
    }


  internal string GetSolutionQString()
    {
    return IntMath.ToString10( SolutionQ );
    }



  private void MakeQuadResToPrimesArray()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeQuadResToPrimesArray()." );

    QuadResToPrimesArray = new QuadResToPrimesRec[QuadResToPrimesArrayLength];

    for( int Count = 0; Count < QuadResToPrimesArrayLength; Count++ )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      // Worker.ReportProgress( 0, "Prime: " + Prime.ToString());

      uint ProdMod = (uint)IntMath.GetMod32( Product, Prime );
      QuadResToPrimesRec Rec = new QuadResToPrimesRec();
      Rec.GoodX = new bool[Prime];
      Rec.QuadRes = new bool[Prime];
      for( uint X = 0; X < Prime; X++ )
        {
        if( Rec.QuadRes[Count] )
          throw( new Exception( "This can't happen with managed code since it just got created." ));

        }

      // Make the Quadratic residue values true.
      for( uint X = 0; X < Prime; X++ )
        {
        uint Test = checked( X * X );
        Test = Test % Prime;
        Rec.QuadRes[Test] = true;
        }

      for( uint X = 0; X < Prime; X++ )
        {
        uint Test = checked( X * X );
        Test = checked(Test + ProdMod);
        Test = Test % Prime;
        if( Rec.QuadRes[Test] )
          {
          // Worker.ReportProgress( 0, "Good X: " + X.ToString());
          Rec.GoodX[X] = true;
          }
        }

      QuadResToPrimesArray[Count] = Rec;
      }
    }


  /*
  private bool IsQuadResidue( uint Test, int Index )
    {
    uint TestMod = Test % IntMath.GetPrimeAt( Index );
    return QuadResToPrimesArray[Index].QuadRes[TestMod];
    }
    */



  private bool IsGoodXForAllPrimes( Integer X )
    {
    int Start = QuadResToPrimesArrayLength - 1;
    for( int Count = Start; Count >= 0; Count-- )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      uint Test = (uint)IntMath.GetMod32( X, Prime );
      if( !QuadResToPrimesArray[Count].GoodX[Test] )
        return false;

      }

    // It was congruent to a square for all of the primes in the array.
    return true;
    }



  private bool IsGoodX( uint Test, int Index )
    {
    uint TestMod = Test % IntMath.GetPrimeAt( Index );
    return QuadResToPrimesArray[Index].GoodX[TestMod];
    }



  internal void SetDigitIndexesToZero()
    {
    for( int Count = 0; Count < DigitsArrayLength; Count++ )
      QuadResDigitsArray[Count].DigitIndex = 0;

    }


  private void MakeQuadResDigitsArrayRec( int Index,
                                    int Start,
                                    int End )
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeQuadResDigitsArrayRec()." );

    QuadResDigitsRec Rec = new QuadResDigitsRec();
    Rec.Base = 1;
    for( int Count = Start; Count <= End; Count++ )
      Rec.Base = Rec.Base * IntMath.GetPrimeAt( Count );

    Rec.ProdModBase = (uint)IntMath.GetMod32( Product, Rec.Base );
    Rec.DigitIndex = 0;
    Rec.DigitsArray = new uint[Rec.Base];

    int Last = 0;
    for( uint X = 0; X < Rec.Base; X++ )
      {
      if( Worker.CancellationPending )
        return;

      bool IsInArray = true;
      for( int PCount = Start; PCount <= End; PCount++ )
        {
        if( !IsGoodX( X, PCount ))
          {
          IsInArray = false;
          break;
          }

        if( !IsInArray )
          break;

        }

      if( IsInArray )
        {
        // It was true for all of the primes tested.
        Rec.DigitsArray[Last] = X;
        Last++;
        }
      }

    Array.Resize( ref Rec.DigitsArray, Last );

    if( Index == 0 )
      {
      if( WInfo.ModMask != 0xFFFFFFFF )
        {
        Worker.ReportProgress( 0, "Before 3 out of 4 length: " + Rec.DigitsArray.Length.ToString());
        Rec.DigitsArray = Utility.RemoveThreeOutOfFourFromUIntArray( Rec.DigitsArray, WInfo.ModMask );
        Worker.ReportProgress( 0, "After 3 out of 4 length: " + Rec.DigitsArray.Length.ToString());
        }
      }

    QuadResDigitsArray[Index] = Rec;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Rec.DigitsArray size is: " + Rec.DigitsArray.Length.ToString());
    Worker.ReportProgress( 0, "Base is: " + Rec.Base.ToString());
    Worker.ReportProgress( 0, " " );
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeQuadResDigitsArrayRec()." ));
      }
    }



  internal bool FindFactors()
    {
    MakeQuadResToPrimesArray();
    if( Worker.CancellationPending )
      return false;

    MakeQuadResDigitsArrayRec( 0, 0, 7 );
    MakeQuadResDigitsArrayRec( 1, 8, 9 );
    MakeQuadResDigitsArrayRec( 2, 10, 11 );
    MakeQuadResDigitsArrayRec( 3, 12, 12 );
    if( Worker.CancellationPending )
      return false;

    SetupBaseValues();
    if( Worker.CancellationPending )
      return false;

    MakeMatchingInverseArrays();
    if( Worker.CancellationPending )
      return false;

    Integer X = new Integer();
    Integer XSquared = new Integer();
    Integer SqrRoot = new Integer();
    Integer YSquared = new Integer();

    uint Loops = 0;
    while( true )
      {
      if( Worker.CancellationPending )
        return false;

      Loops++;
      if( (Loops & 0x3FFFFF) == 0 )
        {
        Worker.ReportProgress( 0, "Loops: " + Loops.ToString());
        }

      if( (Loops & 0xFFFF) == 0 )
        {
        // Use Task Manager to tweak the CPU Utilization if you want
        // it be below 100 percent.
        Thread.Sleep( 1 );
        }

      GetIntegerValue( X );

      if( !IsGoodXForAllPrimes( X ))
        {
        if( !IncrementDigitsWithBitTest())
          {
          Worker.ReportProgress( 0, "Incremented to the end." );
          return false;
          }

        continue;
        }

      XSquared.Copy( X );
      IntMath.DoSquare( XSquared );
      YSquared.Copy( Product );
      YSquared.Add( XSquared );

      if( IntMath.SquareRoot( YSquared, SqrRoot ))
        {
        return IsSolution( X, SqrRoot );
        }

      if( !IncrementDigitsWithBitTest())
        {
        Worker.ReportProgress( 0, "Incremented to the end." );
        return false;
        }
      }
    }



  private bool IsSolution( Integer X, Integer Y )
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of IsSolution()." );

    SolutionP.Copy( Y );
    IntMath.Subtract( SolutionP, X );

    // Make Q the bigger one and put them in order.
    SolutionQ.Copy( Y );
    SolutionQ.Add( X );

    if( SolutionP.IsOne() )
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Went all the way to 1 without finding a factor." );
      SolutionP.SetToZero(); // It has no factors.
      SolutionQ.SetToZero();
      return false;
      }

    Worker.ReportProgress( 0, "Found P: " + IntMath.ToString10( SolutionP ) );
    Worker.ReportProgress( 0, "Found Q: " + IntMath.ToString10( SolutionQ ) );
    Worker.ReportProgress( 0, "Seconds: " + StartTime.GetSecondsToNow().ToString( "N1" ));
    Worker.ReportProgress( 0, " " );
    return true; // With P and Q.
    }



  internal bool IncrementDigitsWithBitTest()
    {
    try
    {
    int Index = QuadResDigitsArray.Length - 1;
    if( LastIncrementIndex != Index )
      return IncrementDigits();

    int DigitsArrayLength = QuadResDigitsArray[Index].DigitsArray.Length;

    int Start = QuadResDigitsArray[Index].DigitIndex;
    for( int Count = Start; Count < DigitsArrayLength; Count++ )
      {
      QuadResDigitsArray[Index].DigitIndex++;
      if( QuadResDigitsArray[Index].DigitIndex >= DigitsArrayLength )
        {
        QuadResDigitsArray[Index].DigitIndex--;
        return IncrementDigits();
        }

      ulong Test = GetIncrementAccumulateBits();
      Test = checked( Test * Test );
      Test += (uint)(Product.GetD( 0 ));
      if( IntegerMath.FirstBytesAreQuadRes( (uint)Test ))
        return true;

      }

    QuadResDigitsArray[Index].DigitIndex--;
    return IncrementDigits();

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in IncrementCRTDigitsWithBitTest(): " + Except.Message ));
      }
    }



  private uint GetIncrementAccumulateBits()
    {
    int Index = QuadResDigitsArray.Length - 1;
    uint CurrentBase = QuadResDigitsArray[Index].Base;
    uint AccumulateDigit = (uint)IntMath.GetMod32( LastAccumulateValue, CurrentBase );
    int DigitsIndex = QuadResDigitsArray[Index].DigitIndex;
    uint CountB = QuadResDigitsArray[Index].MatchingInverseArray[DigitsIndex, AccumulateDigit];
    uint BasePart = QuadResDigitsArray[Index].BigBaseBottomDigit;
    ulong AccumBits = (ulong)BasePart * (ulong)CountB;

    // This is not the same thing as AccumulateDigit:
    AccumBits += LastAccumulateValue.GetD( 0 );
    AccumBits = AccumBits & 0xFFFFFF;
    return (uint)AccumBits;
    }




  private bool IncrementDigits()
    {
    // Change this from the top so that lower accumulate
    // values (ValueToIndex) can stay the same.
    int DigitsLength = QuadResDigitsArray.Length;
    for( int Count = DigitsLength - 1; Count >= 0; Count-- )
      {
      LastIncrementIndex = Count;
      QuadResDigitsArray[Count].DigitIndex++;
      if( QuadResDigitsArray[Count].DigitIndex < QuadResDigitsArray[Count].DigitsArray.Length )
        return true; // Nothing more to do.

      QuadResDigitsArray[Count].DigitIndex = 0; // It wrapped around.
      // Go around to the next lower digit.
      }

    // If it got here then it got to the bottom digit without
    // returning and the bottom digit wrapped around to zero.
    // So that's as far as it can go.
    return false;
    }




   // See CRTMath.GetTraditionalInteger() for more on how this works.

  internal void GetIntegerValue( Integer Accumulate )
    {
    try
    {
    if( LastIncrementIndex == (QuadResDigitsArray.Length - 1))
      {
      CalculateLastAccumulatePart( Accumulate );
      return;
      }

    int DigitIndex = QuadResDigitsArray[0].DigitIndex;
    Accumulate.SetFromULong( QuadResDigitsArray[0].DigitsArray[DigitIndex] );

    // Count starts at 1, so it's the base at 1.
    int QRLength = QuadResDigitsArray.Length;
    for( int Count = 1; Count < QRLength; Count++ )
      {
      uint CurrentBase = QuadResDigitsArray[Count].Base;
      uint AccumulateDigit = (uint)IntMath.GetMod32( Accumulate, CurrentBase );
      DigitIndex = QuadResDigitsArray[Count].DigitIndex;
      uint CountB = QuadResDigitsArray[Count].MatchingInverseArray[DigitIndex, AccumulateDigit];
      GetValueBasePart.Copy( QuadResDigitsArray[Count].BigBase );
      IntMath.MultiplyUInt( GetValueBasePart, CountB );
      Accumulate.Add( GetValueBasePart );

      if( Count == (QuadResDigitsArray.Length - 2))
        {
        LastAccumulateValue.Copy( Accumulate );
        }
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in GetIntegerValue(): " + Except.Message ));
      }
    }




  private void CalculateLastAccumulatePart( Integer Accumulate )
    {
    try
    {
    Accumulate.Copy( LastAccumulateValue );
    int Index = QuadResDigitsArray.Length - 1;
    uint CurrentBase = QuadResDigitsArray[Index].Base;
    uint AccumulateDigit = (uint)IntMath.GetMod32( Accumulate, CurrentBase );
    int DigitsIndex = QuadResDigitsArray[Index].DigitIndex;
    uint CountB = QuadResDigitsArray[Index].MatchingInverseArray[DigitsIndex, AccumulateDigit];
    GetValueBasePart.Copy( QuadResDigitsArray[Index].BigBase );
    IntMath.MultiplyUInt( GetValueBasePart, CountB );
    Accumulate.Add( GetValueBasePart );
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in CalculateLastAccumulatePart(): " + Except.Message ));
      }
    }



  private void MakeMatchingInverseArrays()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeMatchingInverseArrays()." );

    // This is a multiplicative-inverse type of relationship, but it's on a
    // composite base.  So it could be calculated from the multiplicative
    // inverses on the several primes that make up the base, for each
    // base in the CRTArray.
    // This makes giant arrays.

    try
    {
    // Count starts at 1, so it's the base at 1.
    int QRLength = QuadResDigitsArray.Length;
    for( int Count = 1; Count < QRLength; Count++ )
      {
      Worker.ReportProgress( 0, "Setting up at Count: " + Count.ToString() );

      QuadResDigitsArray[Count].MatchingInverseArray = new uint[QuadResDigitsArray[Count].DigitsArray.Length, QuadResDigitsArray[Count].Base];
      uint CurrentBase = QuadResDigitsArray[Count].Base;

      for( uint DigitsIndex = 0; DigitsIndex < QuadResDigitsArray[Count].DigitsArray.Length; DigitsIndex++ )
        {
        if( Worker.CancellationPending )
          return;

        uint Digit = QuadResDigitsArray[Count].DigitsArray[DigitsIndex];
        for( uint CountAccum = 0; CountAccum < CurrentBase; CountAccum++ )
          {
          if( Worker.CancellationPending )
            return;

          for( uint CountB = 0; CountB < CurrentBase; CountB++ )
            {
            ulong ToTest = checked( (ulong)QuadResDigitsArray[Count].BigBaseModCurrentBase * (ulong)CountB );
            ToTest = checked( ToTest + CountAccum );
            ToTest = ToTest % CurrentBase;
            if( Digit == ToTest )
              {
              QuadResDigitsArray[Count].MatchingInverseArray[DigitsIndex, CountAccum] = CountB;
              break;
              }
            }
          }
        }
      }

    Worker.ReportProgress( 0, "Finished MakeMatchingInverseArrays()." );
    Worker.ReportProgress( 0, " " );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in MakeMatchingInverseArrays(): " + Except.Message ));
      }
    }



  private void SetupBaseValues()
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of SetupBaseValues()." );

    // MakeQuadResDigitsArrayRec() sets up each record and its base,
    // so that base should already be set in this record.
    if( QuadResDigitsArray[0].Base == 0 )
      throw( new Exception( "Base was zero in SetupBaseValues() at: 0" ));

    Integer BigBase = new Integer();
    BigBase.SetFromULong( QuadResDigitsArray[0].Base );
    QuadResDigitsArray[0].BigBase = new Integer();
    QuadResDigitsArray[0].BigBase.Copy( BigBase );
    // Zero and one have the same base set here.

    // Count starts at 1, so it's the base at 1.
    int QRLength = QuadResDigitsArray.Length;
    for( int Count = 1; Count < QRLength; Count++ )
      {
      if( QuadResDigitsArray[Count].Base == 0 )
        throw( new Exception( "Base was zero in SetupBaseValues() at: " + Count.ToString() ));

      QuadResDigitsArray[Count].BigBase = new Integer();
      QuadResDigitsArray[Count].BigBase.Copy( BigBase );
      QuadResDigitsArray[Count].BigBaseBottomDigit = (uint)BigBase.GetD( 0 );
      QuadResDigitsArray[Count].BigBaseModCurrentBase = (uint)IntMath.GetMod32( QuadResDigitsArray[Count].BigBase, QuadResDigitsArray[Count].Base );

      // Multiply it by the current base for the next loop.
      IntMath.MultiplyUInt( BigBase, QuadResDigitsArray[Count].Base );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupCRTBaseValues(): " + Except.Message ));
      }
    }



  }
}
