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

    internal struct CRTCombinSetupRec
      {
      internal int Start;
      internal int End;
      }


  class CRTCombinatorics
  {
  private BackgroundWorker Worker;
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private CRTArrayRec[] CRTArray;
  private XYMatchesRec[] ModArray;
  private const int ModArrayLength = 50;
  private const uint ModBitsModulus = 0x10000;
  private const uint ModBitsModulusBitMask = 0xFFFF;
  private bool[,] ModBitsTestArray;
  private uint[] XArrayForSum;
  private uint[] MediumXArrayForSum;
  internal const uint BaseTo13 = 2 * 3 * 5 * 7 * 11 * 13;
  private int LastIncrementIndex = 0;
  private Integer GetValueBasePart;
  private Integer LastAccumulateValue;
  // private Integer TestAccumulate;
  private CRTCombinSetupRec[] SetupArray;
  private uint ModMask = 0;


  internal struct CRTArrayRec
    {
    internal uint Base;
    internal uint[] XPlusYDigits;
    internal int DigitsIndex;
    internal Integer BigBase;
    internal uint BigBaseBottomDigit;
    internal uint BigBaseModCurrentBase;
    internal uint[,] MatchingInverseArray;
    }




  internal struct XYMatchesRec
    {
    internal uint B;
    internal uint L;
    internal uint[] XtoY;
    internal uint[] XPlusY;
    internal uint[] XTimesY;
    internal uint[] XPlusYNoDup;
    internal bool[] XPlusYBool;
    }


  private CRTCombinatorics()
    {

    }


  internal CRTCombinatorics( uint UseModMask, CRTCombinSetupRec[] UseSetupArray, BackgroundWorker UseWorker, IntegerMath UseIntMath )
    {
    SetupArray = UseSetupArray;
    Worker = UseWorker;
    ModMask = UseModMask;

    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    // GetValBigBase = new Integer();
    GetValueBasePart = new Integer();
    LastAccumulateValue = new Integer();
    // TestAccumulate = new Integer();
    CRTArray = new CRTArrayRec[SetupArray.Length];
    }



  internal void SetUpDigitArrays()
    {
    for( int Count = 0; Count < SetupArray.Length; Count++ )
      {
      MakeCRTArrayRec( Count, SetupArray[Count].Start, SetupArray[Count].End );
      }

    SetupCRTBaseValues();
    MakeMatchingInverseArrays();
    LastIncrementIndex = 0;
    }



  private void MakeCRTArrayRec( int Index,
                                int Start,
                                int End )
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeCRTArrayRec()." );

    CRTArrayRec Rec = new CRTArrayRec();
    Rec.Base = 1;
    for( int Count = Start; Count <= End; Count++ )
      Rec.Base = Rec.Base * IntMath.GetPrimeAt( Count );

    uint[] NoDupArray = new uint[Rec.Base];
    int Last = 0;
    for( uint Count = 0; Count < Rec.Base; Count++ )
      {
      // if( Worker.CancellationPending )
        // return;

      bool IsInArray = true;
      for( int PCount = Start; PCount <= End; PCount++ )
        {
        uint Prime = IntMath.GetPrimeAt( PCount );
        uint Test = Count % Prime;
        if( !IsInXPlusYNoDupArrayBool( PCount, Test ))
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
        NoDupArray[Last] = Count;
        Last++;
        }
      }

    Array.Resize( ref NoDupArray, Last );


    if( Index == 0 )
      {
      if( ModMask != 0xFFFFFFFF )
        {
        Worker.ReportProgress( 0, "Before 3 out of 4 length: " + NoDupArray.Length.ToString());
        NoDupArray = Utility.RemoveThreeOutOfFourFromUIntArray( NoDupArray, ModMask );
        Worker.ReportProgress( 0, "After 3 out of 4 length: " + NoDupArray.Length.ToString());
        }
      }


    Utility.SortUintArray( ref NoDupArray );
    Rec.XPlusYDigits = NoDupArray;
    Rec.DigitsIndex = 0;
    CRTArray[Index] = Rec;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CRTArrayRec array size is: " + Last.ToString());
    Worker.ReportProgress( 0, "Base is: " + Rec.Base.ToString());
    Worker.ReportProgress( 0, " " );
    // 510,510
    // Base17 = 2 * 3 * 5 * 7 * 11 * 13 * 17 = 510,510
    //          1 * 2 * 3 * 4  * 6  * 7 *  9 = 

    // Example Base 17 array sizes: 2835, 3888, 4536, 2916
    // That's out of 510,510.

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeCRTArrayRec()." ));
      }
    }



  internal void SetDigitIndexesToZero()
    {
    int CRTLength = SetupArray.Length;
    for( int Count = 0; Count < CRTLength; Count++ )
      CRTArray[Count].DigitsIndex = 0;

    }



  internal bool IncrementCRTDigitsWithBitMask()
    {
    try
    {
    int Index = CRTArray.Length - 1;
    if( LastIncrementIndex != Index )
      return IncrementCRTDigits();

    int DigitsArrayLength = CRTArray[Index].XPlusYDigits.Length;

    int Start = CRTArray[Index].DigitsIndex;
    for( int Count = Start; Count < DigitsArrayLength; Count++ )
      {
      CRTArray[Index].DigitsIndex++;
      if( CRTArray[Index].DigitsIndex >= DigitsArrayLength )
        {
        CRTArray[Index].DigitsIndex--;
        return IncrementCRTDigits();
        }

      // uint Test = GetIncrementAccumulateBits( CRTArray[Index] );
      uint Test = GetIncrementAccumulateBits();
      if( IsInBitsTestArray( Test ))
        return true;

      }

    CRTArray[Index].DigitsIndex--;
    return IncrementCRTDigits();

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in IncrementCRTDigitsWithBitMask(): " + Except.Message ));
      }
    }



  /*
  Slower.
  private uint GetIncrementAccumulateBits( CRTArrayRec Rec )
    {
    // int Index = CRTArray.Length - 1;

    uint CurrentBase = Rec.Base;
    uint AccumulateDigit = (uint)IntMath.GetMod32( LastAccumulateValue, CurrentBase );
    int DigitsIndex = Rec.DigitsIndex;
    uint CountB = Rec.MatchingInverseArray[DigitsIndex, AccumulateDigit];
    uint BasePart = Rec.BigBaseBottomDigit;
    // uint BasePart = (uint)CRTArray[Index].BigBase.GetD( 0 );
    ulong AccumBits = checked( (ulong)BasePart * (ulong)CountB );

    // This is not the same thing as AccumulateDigit:
    AccumBits += LastAccumulateValue.GetD( 0 );
    AccumBits = AccumBits & 0xFFFFFFFF;
    return (uint)AccumBits;
    }
    */




  private uint GetIncrementAccumulateBits()
    {
    int Index = CRTArray.Length - 1;
    uint CurrentBase = CRTArray[Index].Base;
    uint AccumulateDigit = (uint)IntMath.GetMod32( LastAccumulateValue, CurrentBase );
    int DigitsIndex = CRTArray[Index].DigitsIndex;
    uint CountB = CRTArray[Index].MatchingInverseArray[DigitsIndex, AccumulateDigit];
    uint BasePart = CRTArray[Index].BigBaseBottomDigit;
    ulong AccumBits = (ulong)BasePart * (ulong)CountB;

    // This is not the same thing as AccumulateDigit:
    AccumBits += LastAccumulateValue.GetD( 0 );
    AccumBits = AccumBits & 0xFFFFFFFF;
    return (uint)AccumBits;
    }





  private bool IncrementCRTDigits()
    {
    // Change this from the top so that lower accumulate
    // values (ValueToIndex) can stay the same.
    int CRTLength = CRTArray.Length;
    for( int Count = CRTLength - 1; Count >= 0; Count-- )
      {
      LastIncrementIndex = Count;
      CRTArray[Count].DigitsIndex++;
      if( CRTArray[Count].DigitsIndex < CRTArray[Count].XPlusYDigits.Length )
        return true; // Nothing more to do.

      CRTArray[Count].DigitsIndex = 0; // It wrapped around.
      // Go around to the next lower digit.
      }

    // If it got here then it got to the bottom digit without
    // returning and the bottom digit wrapped around to zero.
    // So that's as far as it can go.
    return false;
    }



  internal void MakeModArrays( Integer TopB, Integer Left )
    {
    // Worker.ReportProgress( 0, "Top of MakeModArrays()." );
    ModArray = new XYMatchesRec[ModArrayLength];

    for( int Count = 0; Count < ModArrayLength; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      // B would be zero for all of these smaller than TopB.
      ModArray[Count].B = (uint)IntMath.GetMod32( TopB, IntMath.GetPrimeAt( Count ));
      ModArray[Count].L = (uint)IntMath.GetMod32( Left, IntMath.GetPrimeAt( Count ));
      }

    MakeXYArrays();
    }



  private void MakeXYArrays()
    {
    for( int Count = 0; Count < ModArrayLength; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      int HowManyMatched = 0;
      uint Prime = IntMath.GetPrimeAt( Count );
      ModArray[Count].XtoY = new uint[Prime];
      ModArray[Count].XPlusY = new uint[Prime];
      ModArray[Count].XTimesY = new uint[Prime];

      /*
      if( Count < 30 )
        {
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Prime: " + Prime.ToString() );
        Worker.ReportProgress( 0, "X\tY\tB\tL\tXPlusY\tXTimesY" );
        }
        */

      for( uint CountX = 0; CountX < Prime; CountX++ )
        {
        ModArray[Count].XtoY[CountX] = 0xFFFFFFFF;
        ModArray[Count].XPlusY[CountX] = 0xFFFFFFFF;
        ModArray[Count].XTimesY[CountX] = 0xFFFFFFFF;
        }

      // Yes, it can be zero if this prime is more than the
      // ones in TopB.
      for( uint CountX = 0; CountX < Prime; CountX++ )
        {
        for( uint CountY = 0; CountY < Prime; CountY++ )
          {
          // L = B(x + y) + xy
          uint XY = (CountX * CountY) % Prime;
          uint XPlusY = (CountX + CountY) % Prime;
          uint Bxy = (ModArray[Count].B * XPlusY) % Prime;
          uint L = (Bxy + XY) % Prime;
          if( L == ModArray[Count].L )
            {
            if( L != 0 )
              {
              if( (CountX == 0) && (CountY == 0))
                throw( new Exception( "This can't happen. L is not zero and (CountX == 0) && (CountY == 0) at: " + Prime.ToString() ));

              }
            else
              {
              // L is congruent to zero so:

              // if( (CountX == 0) && (CountY == 0))
                // throw( new Exception( "This can happen. They are both zero at: "+ Prime.ToString() ));

              // If one is zero, both are zero.
              if( (CountX == 0) && (CountY != 0))
                throw( new Exception( "This can't happen. L is zero and (CountX == 0) && (CountY != 0) at: " + Prime.ToString() ));

              if( (CountX != 0) && (CountY == 0))
                throw( new Exception( "This can't happen. L is zero and (CountX != 0) && (CountY == 0) at: " + Prime.ToString() ));

              // if( (CountX != 0) && (CountY != 0))
                // throw( new Exception( "This happens." ));
              // This happens.
              // It's a multiplicative-inverse type of relationship.
              // 1	20	47	0	21	20
              // 2	50	47	0	52	47
              // 3	47	47	0	50	35
              }

            // Notice that these loops go to opposite values
            // like 7 + 3 and 3 + 7.  So most of the XPlusY
            // values will have a duplicate.
            // Also, the size of the XPlusY array is the size
            // of the prime.  It's never more than that because
            // of the multiplicative-inverse relationship on 
            // the numbers.  (There is only one matching value
            // for a particular CountX.)
            // There are P - 1 count values and each of those have
            // one duplicate for the sums.  So the number of unique
            // XPlusY values is half of the prime.

            HowManyMatched++;
            ModArray[Count].XtoY[CountX] = CountY;
            ModArray[Count].XPlusY[CountX] = (CountX + CountY) % Prime;
            ModArray[Count].XTimesY[CountX] = (CountX * CountY) % Prime;

            // XTimesY is zero when x or y is zero.  But only when
            // the prime is not in TopB.

            // The sum is zero when the prime is 2.
            // The sum is zero in a lot of places.
            // if( ModArray[Count].XPlusY[CountX] == 0 )
              // Worker.ReportProgress( 0, "Sum is zero mod: " + Prime.ToString() );

            // If sum is zero is a solution then
            // L = B(x + y) + xy
            // L is congruent to xy mod this prime.


            /*
            if( Count < 30 )
              {
              string ShowS = CountX.ToString() + "\t" +
                           CountY.ToString() + "\t" +
                           ModArray[Count].B.ToString() + "\t" +
                           ModArray[Count].L.ToString() + "\t" +
                           ModArray[Count].XPlusY[CountX] + "\t" +
                           ModArray[Count].XTimesY[CountX];

              Worker.ReportProgress( 0, ShowS );
              }
              */
            }
          }
        }
      
      if( HowManyMatched == 0 )
        throw( new Exception( "This never happens. Zero matches for prime: " + Prime.ToString() ));

      }

    RemoveArrayDuplicates();
    }



  private void RemoveArrayDuplicates()
    {
    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "Removing duplicates." );

    for( int Count = 0; Count < ModArrayLength; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      uint Prime = IntMath.GetPrimeAt( Count );
      // Worker.ReportProgress( 0, "\r\nPrime: " + Prime.ToString() );

      ModArray[Count].XPlusYNoDup = Utility.MakeNoDuplicatesUIntArray( ModArray[Count].XPlusY );
      // Worker.ReportProgress( 0, Count.ToString() + ") No dup length: " + ModArray[Count].XPlusYNoDup.Length.ToString());

      ModArray[Count].XPlusYBool = new bool[Prime];
      for( int CountB = 0; CountB < Prime; CountB++ )
        ModArray[Count].XPlusYBool[CountB] = false; // Not necessary in managed code.

      for( int CountB = 0; CountB < ModArray[Count].XPlusYNoDup.Length; CountB++ )
        {
        uint NoDupValue = ModArray[Count].XPlusYNoDup[CountB];
        ModArray[Count].XPlusYBool[NoDupValue] = true;
        }
      }
    }



  private bool IsInXPlusYNoDupArrayBool( int PrimeIndex, uint Test )
    {
    // Would the JIT compiler in-line this and optimize
    // range-checking for it?
    return ModArray[PrimeIndex].XPlusYBool[Test];
    }



  internal void MakeModBitsArray( Integer TopB, Integer Left )
    {
    try
    {
    // Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeModBitsArray." );

    ModBitsTestArray = new bool[256, 256];

    XYMatchesRec Rec = new XYMatchesRec();
    Rec.B = (uint)(TopB.GetD( 0 ) & ModBitsModulusBitMask);
    Rec.L = (uint)(Left.GetD( 0 ) & ModBitsModulusBitMask);

    Rec.XtoY = new uint[ModBitsModulus];
    Rec.XPlusY = new uint[ModBitsModulus];
    Rec.XTimesY = new uint[ModBitsModulus];

    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "X\tY\tB\tL\tXPlusY\tXTimesY" );

    int HowManyMatched = 0;

    // They can only be odd numbers.
    for( uint CountX = 1; CountX < ModBitsModulus; CountX += 2 )
      {
      if( Worker.CancellationPending )
        return;

      // Not necessary in managed code since it was just created above.
      ModBitsTestArray[CountX >> 8, CountX & 0xFF] = false;

      for( uint CountY = 1; CountY < ModBitsModulus; CountY += 2 )
        {
        // L = B(x + y) + xy
        uint XY = (CountX * CountY) & ModBitsModulusBitMask;
        uint XPlusY = (CountX + CountY) & ModBitsModulusBitMask;
        uint Bxy = (Rec.B * XPlusY) & ModBitsModulusBitMask;
        uint L = (Bxy + XY) & ModBitsModulusBitMask;
        if( L == Rec.L )
          {
          HowManyMatched++;
          Rec.XtoY[CountX] = CountY;
          Rec.XPlusY[CountX] = (CountX + CountY) & ModBitsModulusBitMask;
          Rec.XTimesY[CountX] = (CountX * CountY) & ModBitsModulusBitMask;

          /*
          string ShowS = CountX.ToString() + "\t" +
                         CountY.ToString() + "\t" +
                         Rec.B.ToString() + "\t" +
                         Rec.L.ToString() + "\t" +
                         Rec.XPlusY[CountX].ToString() + "\t" +
                         Rec.XTimesY[CountX].ToString();

          Worker.ReportProgress( 0, ShowS );
          */
          }
        }
      }

    if( HowManyMatched == 0 )
      throw( new Exception( "Zero matches for bits mod." ));

    // Worker.ReportProgress( 0, "Finished with MakeModBitsArray." );
    // Worker.ReportProgress( 0, " " );

    Rec.XPlusYNoDup = Utility.MakeNoDuplicatesUIntArray( Rec.XPlusY );

    for( int Count = 0; Count < Rec.XPlusYNoDup.Length; Count++ )
      {
      uint Value = Rec.XPlusYNoDup[Count];
      uint X = Value >> 8;
      uint Y = Value & 0xFF;
      ModBitsTestArray[X, Y] = true;
      }

    // Out of 64K possibilities:
    // ModBitsArray no dup size is: 1369
    // ModBitsArray no dup size is: 8192
    // ModBitsArray no dup size is: 4097

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in IsInModBitsXPlusYNoDupArray()." ));
      }
    }



  internal bool IsInBitsTestArray( uint ToTest )
    {
    // I think the JIT compiler would in-line this.
    uint Value = ToTest;
    Value = Value & 0xFFFF;
    uint X = Value >> 8;
    uint Y = Value & 0xFF;
    return ModBitsTestArray[X, Y];
    }



  internal bool IsInPlusModArrays( Integer ToTest )
    {
    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    uint Test = (uint)IntMath.GetMod32( ToTest, 59 * 61 * 67 );
    uint Prime = 59;
    uint TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 16, TestP ))
      return false;

    Prime = 61;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 17, TestP ))
      return false;

    Prime = 67;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 18, TestP ))
      return false;

    ///////////
    Test = (uint)IntMath.GetMod32( ToTest, 71 * 73 * 79 );
    Prime = 71;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 19, TestP ))
      return false;

    Prime = 73;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 20, TestP ))
      return false;

    Prime = 79;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 21, TestP ))
      return false;


    ///////////
    Test = (uint)IntMath.GetMod32( ToTest, 83 * 89 * 97 );
    Prime = 83;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 22, TestP ))
      return false;

    Prime = 89;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 23, TestP ))
      return false;

    Prime = 97;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArrayBool( 24, TestP ))
      return false;

    for( int Count = 25; Count < ModArrayLength; Count++ )
      {
      Prime = IntMath.GetPrimeAt( Count );
      Test = (uint)IntMath.GetMod32( ToTest, Prime );
      if( !IsInXPlusYNoDupArrayBool( Count, Test ))
        return false;

      }

    return true;
    }



  private bool XMakesSum( uint Sum, uint X, int Index )
    {
    try
    {
    uint Prime = IntMath.GetPrimeAt( Index );
    uint SumModPrime = Sum % Prime;
    uint XModPrime = X % Prime;
    uint Y = ModArray[Index].XtoY[XModPrime];
    if( Y == 0xFFFFFFFF )
      return false;

    if( ((XModPrime + Y) % Prime) == SumModPrime)
      return true;

    return false;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in XMakesSum()." ));
      }
    }



  internal void MakeXArrayForSum( Integer XPlusY )
    {
    try
    {
    //            0   1   2   3    4    5
    // BaseTo13 = 2 * 3 * 5 * 7 * 11 * 13;
    uint Sum = (uint)IntMath.GetMod32( XPlusY, BaseTo13 );
    XArrayForSum = new uint[BaseTo13];

    int Last = 0;
    for( uint X = 1; X < BaseTo13; X += 2 )
      {
      // TopB would never be less than 2,310 = 2 * 3 * 5 * 7 * 11.
      // See MoveTopFactorsDown() for the smallest index.
      // So it can get rid of values congruent to zero for those 
      // small primes to 11.
      if( (X % 3) == 0 )
        continue;

      if( (X % 5) == 0 )
        continue;

      if( (X % 7) == 0 )
        continue;

      if( (X % 11) == 0 )
        continue;

      bool Matched = true;
      for( int Index = 1; Index <= 5; Index++ )
        {
        uint Prime = IntMath.GetPrimeAt( Index );
        if( !XMakesSum( Sum, X, Index ))
          {
          Matched = false;
          break;
          }
        }

      if( Matched )
        {
        XArrayForSum[Last] = X;
        Last++;
        }
      }

    if( Last == 0 )
      throw( new Exception( "XArrayForSum length is zero." ));

    Worker.ReportProgress( 0, "XArrayForSum length is: " + Last.ToString() );
    Worker.ReportProgress( 0, "BaseTo13 is: " + BaseTo13.ToString() );
    Array.Resize( ref XArrayForSum, Last );

    MakeMediumXArrayForSum( XPlusY );

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeXArrayForSum()." ));
      }
    }



  private void MakeMediumXArrayForSum( Integer XPlusY )
    {
    try
    {
    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    uint Sum = (uint)IntMath.GetMod32( XPlusY, 17 * 19 * 23 );
    MediumXArrayForSum = new uint[XArrayForSum.Length  * 17 * 19 * 23];

    int Last = 0;
    int SmallArrayLength = XArrayForSum.Length;

    for( uint BaseCount = 0; BaseCount < (17 * 19 * 23); BaseCount++ )
      {
      uint Base = BaseCount * BaseTo13;
      for( int Count = 0; Count < SmallArrayLength; Count++ )
        {
        uint X = Base + XArrayForSum[Count];

        bool Matched = true;
        for( int Index = 6; Index <= 8; Index++ )
          {
          uint Prime = IntMath.GetPrimeAt( Index );
          uint XMod = X % Prime;

          if( !XMakesSum( Sum, XMod, Index ))
            {
            Matched = false;
            break;
            }
          }

        if( Matched )
          {
          MediumXArrayForSum[Last] = X;
          Last++;
          }
        }
      }

    if( Last == 0 )
      throw( new Exception( "XArrayForSum length is zero." ));

    if( Last == SmallArrayLength )
      throw( new Exception( "MediumXArrayForSum Last == SmallArrayLength." ));

    Worker.ReportProgress( 0, "MediumXArrayForSum length is: " + Last.ToString() );
    Array.Resize( ref MediumXArrayForSum, Last );

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeMediumXArrayForSum()." ));
      }
    }


  internal int GetMediumXArrayForSumLength()
    {
    return MediumXArrayForSum.Length;
    }


  internal uint GetValueForMediumXArrayForSum( int Index )
    {
    return MediumXArrayForSum[Index];
    }




   // See CRTMath.GetTraditionalInteger() for more on how this works.

  internal void GetIntegerValue( Integer Accumulate )
    {
    try
    {
    if( LastIncrementIndex == (CRTArray.Length - 1))
      {
      CalculateLastAccumulatePart( Accumulate );
      return;
      }

    int DigitsIndex = CRTArray[0].DigitsIndex;
    Accumulate.SetFromULong( CRTArray[0].XPlusYDigits[DigitsIndex] );

    // Count starts at 1, so it's the base at 1.
    int CRTLength = CRTArray.Length;
    for( int Count = 1; Count < CRTLength; Count++ )
      {
      uint CurrentBase = CRTArray[Count].Base;
      uint AccumulateDigit = (uint)IntMath.GetMod32( Accumulate, CurrentBase );
      DigitsIndex = CRTArray[Count].DigitsIndex;
      uint CountB = CRTArray[Count].MatchingInverseArray[DigitsIndex, AccumulateDigit];
      GetValueBasePart.Copy( CRTArray[Count].BigBase );
      IntMath.MultiplyUInt( GetValueBasePart, CountB );
      Accumulate.Add( GetValueBasePart );

      if( Count == (CRTArray.Length - 2))
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
    int Index = CRTArray.Length - 1;
    uint CurrentBase = CRTArray[Index].Base;
    uint AccumulateDigit = (uint)IntMath.GetMod32( Accumulate, CurrentBase );
    int DigitsIndex = CRTArray[Index].DigitsIndex;
    uint CountB = CRTArray[Index].MatchingInverseArray[DigitsIndex, AccumulateDigit];
    GetValueBasePart.Copy( CRTArray[Index].BigBase );
    IntMath.MultiplyUInt( GetValueBasePart, CountB );
    Accumulate.Add( GetValueBasePart );
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in CalculateLastAccumulatePart(): " + Except.Message ));
      }
    }



      /*
      for( uint CountB = 0; CountB < CurrentBase; CountB++ )
        {
        ulong ToTest = checked( (ulong)CRTArray[Count].BigBaseModCurrentBase * (ulong)CountB );
        // ToTest = ToTest % CurrentBase;
        ToTest = checked( ToTest + AccumulateDigit );
        ToTest = ToTest % CurrentBase;
        // ToTest can be zero when AccumulateDigit is not.

        if( Digit == ToTest )
          {
          if( CountB != CRTArray[Count].MatchingInverseArray[DigitsIndex, AccumulateDigit] )
            throw( new Exception( "CountB didn't match in the accumulate part." ));

          // CRTArray[Count].BaseMultiple = CountB;
          // if( MatchingValue != CountB )
            // throw( new Exception( "Bug: MatchingValue is not right." ));
 
          GetValueBasePart.Copy( CRTArray[Count].BigBase ); // GetValBigBase );
          IntMath.MultiplyUInt( GetValueBasePart, CountB );
          Accumulate.Add( GetValueBasePart );
          break;
          }
        }
        */




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
    int CRTLength = CRTArray.Length;
    for( int Count = 1; Count < CRTLength; Count++ )
      {
      Worker.ReportProgress( 0, "Setting up at Count: " + Count.ToString() );

      CRTArray[Count].MatchingInverseArray = new uint[CRTArray[Count].XPlusYDigits.Length, CRTArray[Count].Base];
      uint CurrentBase = CRTArray[Count].Base;

      for( uint DigitsIndex = 0; DigitsIndex < CRTArray[Count].XPlusYDigits.Length; DigitsIndex++ )
        {
        if( Worker.CancellationPending )
          return;

        uint Digit = CRTArray[Count].XPlusYDigits[DigitsIndex];
        for( uint CountAccum = 0; CountAccum < CurrentBase; CountAccum++ )
          {
          if( Worker.CancellationPending )
            return;

          for( uint CountB = 0; CountB < CurrentBase; CountB++ )
            {
            ulong ToTest = checked( (ulong)CRTArray[Count].BigBaseModCurrentBase * (ulong)CountB );
            ToTest = checked( ToTest + CountAccum );
            ToTest = ToTest % CurrentBase;
            if( Digit == ToTest )
              {
              CRTArray[Count].MatchingInverseArray[DigitsIndex, CountAccum] = CountB;
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



  private void SetupCRTBaseValues()
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of SetupCRTBaseValues()." );

    if( CRTArray[0].Base == 0 )
      throw( new Exception( "Base was zero in SetupCRTBaseValues() at: 0" ));

    Integer BigBase = new Integer();
    BigBase.SetFromULong( CRTArray[0].Base );
    CRTArray[0].BigBase = new Integer();
    CRTArray[0].BigBase.Copy( BigBase );
    // Zero and one have the same base set here.

    // Count starts at 1, so it's the base at 1.
    int CRTLength = CRTArray.Length;
    for( int Count = 1; Count < CRTLength; Count++ )
      {
      if( CRTArray[Count].Base == 0 )
        throw( new Exception( "Base was zero in SetupCRTBaseValues() at: " + Count.ToString() ));

      CRTArray[Count].BigBase = new Integer();
      CRTArray[Count].BigBase.Copy( BigBase );
      CRTArray[Count].BigBaseBottomDigit = (uint)BigBase.GetD( 0 );
      CRTArray[Count].BigBaseModCurrentBase = (uint)IntMath.GetMod32( CRTArray[Count].BigBase, CRTArray[Count].Base );

      // Multiply it by the current base for the next loop.
      IntMath.MultiplyUInt( BigBase, CRTArray[Count].Base );
      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in SetupCRTBaseValues(): " + Except.Message ));
      }
    }



  }
}

