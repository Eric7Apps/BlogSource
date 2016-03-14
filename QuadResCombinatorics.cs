// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


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
  private uint LastAccumulateDigit = 0;
  private uint LastAccumulateBottomDigit = 0;
  private Integer GetValueBasePart;
  private Integer ToDivideMod32;
  private BackgroundWorker Worker;
  private QuadResWorkerInfo WInfo;
  private QuadResDigitsRec[] QuadResDigitsArray;
  private const int DigitsArrayLength = 4;
  private QuadResToPrimesRec[] QuadResToPrimesArray;
  private const int QuadResToPrimesArrayLength = 200;
  private int LastIncrementIndex = 0;
  private ECTime StartTime;
  private const uint GoodXBitsMask = 0xFF;
  private const uint GoodXBitsModulus = 0x100;
  private bool[] GoodXBitsArray;



  internal struct QuadResToPrimesRec
    {
    internal bool[] GoodX;
    internal bool[] QuadRes;
    internal bool[] QuadResForY;
    internal bool IsQResModProd;
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
    ToDivideMod32 = new Integer();
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
      Rec.QuadResForY = new bool[Prime];

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

      Rec.IsQResModProd = IsQuadResModProduct( Prime );

      for( uint Y = 0; Y < Prime; Y++ )
        {
        uint Test = checked( Y * Y );
        Test = Test % Prime;
        if( Test < ProdMod )
          Test += Prime;

        // y^2 - P = x^2
        Test -= ProdMod;
        if( Test == 0 )
          {
          if( !Rec.IsQResModProd )
            continue;

          }

        Rec.QuadResForY[Test] = true;
        }

      // P + x^2 = y^2
      // x^2 = y^2 - P
      // y^2 - P has to be a quad residue mod P.
      // And that is x^2.

      uint StartAt = 0;
      if( !Rec.IsQResModProd )
        {
        // x^2 can't be divisible by this prime.
        // Worker.ReportProgress( 0, Prime.ToString() + " is not a quad residue mod Product." );
        StartAt = 1;
        }

      for( uint X = StartAt; X < Prime; X++ )
        {
        uint Test = checked( X * X );
        Test = Test % Prime;
        bool YCanMake = true;
        if( !Rec.QuadResForY[Test] )
          YCanMake = false;

        Test = checked(Test + ProdMod);
        Test = Test % Prime;

        // y^2 - P = x^2
        /*
        if( Test == 0 )
          {
          // Then y^2 has this prime as a factor.
          // This factor has to have an even exponent.
          // P + x^2 = y^2
          // It's zero twice, or not at all.
          Worker.ReportProgress( 0, Prime.ToString() + " is a factor of y^2." );
          }
          */

        if( Rec.QuadRes[Test] )
          {
          if( !YCanMake )
            throw( new Exception( "This never happens. !YCanMake." ));

          for( uint TestY = 0; TestY < Prime; TestY++ )
            {
            uint TestForZ = TestY * TestY;
            TestForZ = TestForZ % Prime;
            if( TestForZ == Test )
              {
              // P + x^2 = (x + z)(x + z)
              // P + x^2 = x^2 + 2xz + z^2
              // P = 2xz + z^2
              // P = z(2x + z) = (y - x)(y + x)
              // z is a prime and 2x + z is a prime.

              // If this exception happened then the Product would be
              // zero mod a small prime.
              int Z = (int)X + (int)TestY; // 2x + z
              Z = Z % (int)Prime;
              if( Z == 0 )
                throw( new Exception( "Z == 0 for plus at: " + X.ToString() ));

              Z = (int)X - (int)TestY;
              if( Z < 0 )
                Z += (int)Prime;

              Z = Z % (int)Prime;
              if( Z == 0 )
                throw( new Exception( "Z == 0 for minus at: " + X.ToString() ));

              }
            }

          // Worker.ReportProgress( 0, "Good X: " + X.ToString());
          Rec.GoodX[X] = true;
          }
        else
          {
          if( YCanMake )
            throw( new Exception( "This never happens. YCanMake is true." ));

          }
        }

      QuadResToPrimesArray[Count] = Rec;
      }
    }



  private bool IsQuadResModProduct( uint Prime )
    {
    // Euler's Criterion:
    Integer Exponent = new Integer();
    Integer Result = new Integer();
    Integer Modulus = new Integer();

    Exponent.SetFromULong( Prime );
    IntMath.SubtractULong( Exponent, 1 );
    Exponent.ShiftRight( 1 ); // Divide by 2.

    Result.Copy( Product );
    Modulus.SetFromULong( Prime );

    IntMath.IntMathNew.ModularPower( Result, Exponent, Modulus );
    if( Result.IsOne() )
      return true;
    else
      return false; // Result should be Prime - 1.

    }



  /*
  private bool IsQuadResidue( uint Test, int Index )
    {
    uint TestMod = Test % IntMath.GetPrimeAt( Index );
    return QuadResToPrimesArray[Index].QuadRes[TestMod];
    }
    */



  private uint GetMod32( Integer ToDivideOriginal, uint DivideByU )
    {
    if( ToDivideOriginal.IsULong())
      {
      ulong Result = ToDivideOriginal.GetAsULong();
      return (uint)(Result % DivideByU);
      }

    ToDivideMod32.Copy( ToDivideOriginal );
    ulong RemainderU = 0;

    if( DivideByU <= ToDivideMod32.GetD( ToDivideMod32.GetIndex() ))
      {
      ulong OneDigit = ToDivideMod32.GetD( ToDivideMod32.GetIndex() );
      RemainderU = OneDigit % DivideByU;
      ToDivideMod32.SetD( ToDivideMod32.GetIndex(), RemainderU );
      }
 
    for( int Count = ToDivideMod32.GetIndex(); Count >= 1; Count-- )
      {
      ulong TwoDigits = ToDivideMod32.GetD( Count );
      TwoDigits <<= 32;
      TwoDigits |= ToDivideMod32.GetD( Count - 1 );
      RemainderU = TwoDigits % DivideByU;
      ToDivideMod32.SetD( Count, 0 );
      ToDivideMod32.SetD( Count - 1, RemainderU );
      }

    return (uint)RemainderU;
    }



  private bool IsGoodXForAllPrimes( Integer X )
    {
    //  66   67   68   69   70   71   72   73   74   75   76   77
    // 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397,
    uint TestQuick = 389 * 397;
    uint Test = GetMod32( X, TestQuick );
    if( !QuadResToPrimesArray[76].GoodX[Test % 389] )
        return false;

    if( !QuadResToPrimesArray[77].GoodX[Test % 397] )
        return false;

    TestQuick = 379 * 383;
    Test = GetMod32( X, TestQuick );
    if( !QuadResToPrimesArray[74].GoodX[Test % 379] )
        return false;

    if( !QuadResToPrimesArray[75].GoodX[Test % 383] )
        return false;

    int Start = QuadResToPrimesArrayLength - 1;
    for( int Count = Start; Count >= 0; Count-- )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      Test = GetMod32( X, Prime );
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
        }

      if( IsInArray )
        {
        // It was true for all of the primes tested.
        Rec.DigitsArray[Last] = X;
        Last++;
        }
      }

    if( Last == 0 )
      throw( new Exception( "No digits in the array at Index: " + Index.ToString() ));

    Array.Resize( ref Rec.DigitsArray, Last );

    if( Index == 0 )
      {
      if( WInfo.ModMask != 0xFFFFFFFF )
        {
        // This removes them by their index in the array.
        // So you remove 2 out of 3 for 3 threads, or
        // 3 out of 4 for 4 threads, or how ever many
        // threads you want to run.
        // Rec.DigitsArray = Utility.RemoveThreeOutOfFourFromUIntArray( Rec.DigitsArray, WInfo.ModMask );
        Rec.DigitsArray = Utility.RemoveTwoOutOfThreeFromUIntArray( Rec.DigitsArray, WInfo.ModMask );
        Worker.ReportProgress( 0, "After removing with ModMask length: " + Rec.DigitsArray.Length.ToString());
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



  internal bool FindTheFactors()
    {
    MakeQuadResToPrimesArray();
    if( Worker.CancellationPending )
      return false;

    // ===========
    MakeQuadResDigitsArrayRec( 0, 0, 7 );
    MakeQuadResDigitsArrayRec( 1, 8, 9 );
    MakeQuadResDigitsArrayRec( 2, 10, 10 );
    // The last one should have an array that is
    // as large as possible because of
    // IncrementDigitsWithBitTest().
    MakeQuadResDigitsArrayRec( 3, 11, 13 );
    if( Worker.CancellationPending )
      return false;

    SetupBaseValues();
    if( Worker.CancellationPending )
      return false;

    MakeMatchingInverseArrays();
    if( Worker.CancellationPending )
      return false;

    Worker.ReportProgress( 0, "After MakeMatchingInverseArrays()." );

    Integer X = new Integer();
    Integer XSquared = new Integer();
    Integer SqrRoot = new Integer();
    Integer YSquared = new Integer();

    MakeGoodXBitsArray();
    Worker.ReportProgress( 0, "After MakeGoodXBitsArray();." );

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

      GetIntegerValue( X );
      if( !IsInGoodXBitsArray( (uint)X.GetD( 0 )))
        {
        // This happens with the increment bit test
        // because sometimes it just increments to the
        // next full accumulated value.

        if( !IncrementDigitsWithBitTest())
          {
          Worker.ReportProgress( 0, "Incremented to the end." );
          return false;
          }

        continue;
        }

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



  internal bool IncrementDigitsWithBitTest()
    {
    try
    {
    if( LastIncrementIndex != DigitsArrayLength - 1 )
      return IncrementDigits(); // This takes much more time.

    int DigitsLength = QuadResDigitsArray[DigitsArrayLength - 1].DigitsArray.Length;

    uint BasePart = QuadResDigitsArray[DigitsArrayLength - 1].BigBaseBottomDigit;
    uint ProductBottomPart = (uint)(Product.GetD( 0 ));
    int Start = QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex;
    for( int Count = Start; Count < DigitsLength; Count++ )
      {
      QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex++;
      if( QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex >= DigitsLength )
        {
        QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex--;
        return IncrementDigits();
        }

      ulong Test = GetIncrementAccumulateBits( BasePart );
      if( !IsInGoodXBitsArray( (uint)Test ))
        continue;

      Test = Test * Test;
      Test += ProductBottomPart;

      if( IntegerMath.FirstBytesAreQuadRes( (uint)Test ))
        return true;

      }

    QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex--;
    return IncrementDigits();

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in IncrementCRTDigitsWithBitTest(): " + Except.Message ));
      }
    }



  private bool IsInGoodXBitsArray( uint Test )
    {
    return GoodXBitsArray[Test & GoodXBitsMask];
    }



  private void MakeGoodXBitsArray()
    {
    GoodXBitsArray = new bool[GoodXBitsModulus];
    uint ProdModBits = (uint)Product.GetD( 0 ) & GoodXBitsMask;

    int HowMany = 0;
    for( uint Count = 0; Count < GoodXBitsModulus; Count++ )
      {
      uint Test = Count * Count;
      Test += ProdModBits;
      Test = Test & GoodXBitsMask;
      if( IntegerMath.FirstBytesAreQuadRes( Test ))
        {
        HowMany++;
        GoodXBitsArray[Count] = true;
        }
      }

    Worker.ReportProgress( 0, "GoodXBits HowMany: " + HowMany.ToString() );
    }



  private bool IsSolution( Integer X, Integer Y )
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of IsSolution()." );
    FindFactors FindF = new FindFactors( Worker, IntMath );
    FindF.FindSmallPrimeFactorsOnly( Y );
    FindF.ShowAllFactors();

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
      throw( new Exception( "Went all the way to 1 without finding a factor." ));
      // return false;
      }

    Worker.ReportProgress( 0, "Found P: " + IntMath.ToString10( SolutionP ) );
    Worker.ReportProgress( 0, "Found Q: " + IntMath.ToString10( SolutionQ ) );
    Worker.ReportProgress( 0, "Seconds: " + StartTime.GetSecondsToNow().ToString( "N1" ));
    double Seconds = StartTime.GetSecondsToNow();
    int Minutes = (int)Seconds / 60;
    int Hours = Minutes / 60;
    Minutes = Minutes % 60;
    Seconds = Seconds % 60;
    string ShowS = "Hours: " + Hours.ToString( "N0" ) + 
                   "  Minutes: " + Minutes.ToString( "N0" ) +
                   "  Seconds: " + Seconds.ToString( "N0" );

    Worker.ReportProgress( 0, ShowS );

    Worker.ReportProgress( 0, " " );
    return true; // With P and Q.
    }




  private uint GetIncrementAccumulateBits( uint BasePart )
    {
    int DigitsIndex = QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex;
    uint CountB = QuadResDigitsArray[DigitsArrayLength - 1].MatchingInverseArray[DigitsIndex, LastAccumulateDigit];
    // uint BasePart = QuadResDigitsArray[DigitsArrayLength - 1].BigBaseBottomDigit;

    ulong AccumBits = (ulong)BasePart * (ulong)CountB;
    // This is not the same thing as AccumulateDigit:
    AccumBits += LastAccumulateBottomDigit;
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
    for( int Count = 1; Count < DigitsArrayLength; Count++ )
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

        int Index = QuadResDigitsArray.Length - 1;
        CurrentBase = QuadResDigitsArray[Index].Base;
        LastAccumulateDigit = GetMod32( LastAccumulateValue, CurrentBase );
        LastAccumulateBottomDigit = (uint)LastAccumulateValue.GetD( 0 );
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
    uint CurrentBase = QuadResDigitsArray[DigitsArrayLength - 1].Base;
    // uint AccumulateDigit = GetMod32( Accumulate, CurrentBase );
    int DigitsIndex = QuadResDigitsArray[DigitsArrayLength - 1].DigitIndex;
    uint CountB = QuadResDigitsArray[DigitsArrayLength - 1].MatchingInverseArray[DigitsIndex, LastAccumulateDigit];
    GetValueBasePart.Copy( QuadResDigitsArray[DigitsArrayLength - 1].BigBase );
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

    // This is a multiplicative-inverse type of relationship.

    try
    {
    // Count starts at 1, so it's the base at 1.
    for( int Count = 1; Count < DigitsArrayLength; Count++ )
      {
      Worker.ReportProgress( 0, "Making Matching Digits at Count: " + Count.ToString() );

      uint CurrentBase = QuadResDigitsArray[Count].Base;
      // Don't go as high as the sign bit:
      if( CurrentBase > 0x7FFFFFFF )
        throw( new Exception( "CurrentBase > 0x7FFFFFFF in MakeMatchingInverseArrays()." ));

      uint BaseMod = QuadResDigitsArray[Count].BigBaseModCurrentBase;
      QuadResDigitsArray[Count].MatchingInverseArray = new uint[QuadResDigitsArray[Count].DigitsArray.Length, CurrentBase];

      for( uint DigitsIndex = 0; DigitsIndex < QuadResDigitsArray[Count].DigitsArray.Length; DigitsIndex++ )
        {
        // Thread.Sleep( 1 );
        if( Worker.CancellationPending )
          return;

        uint Digit = QuadResDigitsArray[Count].DigitsArray[DigitsIndex];
        for( uint CountB = 0; CountB < CurrentBase; CountB++ )
          {
          // if( Worker.CancellationPending )
            // return;

          ulong ToTest = (ulong)BaseMod * (ulong)CountB;
          ToTest = ToTest % CurrentBase;
          int MatchAccum = (int)Digit - (int)ToTest;
          if( MatchAccum < 0 )
            MatchAccum += (int)CurrentBase;

          QuadResDigitsArray[Count].MatchingInverseArray[DigitsIndex, MatchAccum] = CountB;
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
