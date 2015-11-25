// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading; // For Sleep().
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{

  class FindFactorsFromTop
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private Integer Product;
  private Integer ProductSqrRoot;
  private Integer MaximumSmallFactor;
  private Integer[] BaseArray;
  private const int BaseArraySize = 50;
  private Integer TopB;
  private Integer TopBB;
  private int TopBIndex = 0;
  private Integer MinimumX;
  private Integer MaximumX;
  private Integer MinimumY;
  private Integer MaximumY;
  private Integer Left;
  private Integer ProductModTopB;
  private XYMatchesRec[] ModArray;
  private const int ModArrayLength = 50;
  private bool[,] ModBitsTestArray;
  private const uint BaseTo13 = 2 * 3 * 5 * 7 * 11 * 13;
  private uint[] SmallPlusModArray;
  private uint[] SmallPlusModDiffArray;
  private const uint XTestArrayBase =     2 * 3 * 5 * 7 * 11 * 13 * 17;
  private const uint XTestArrayEulerPhi =     2 * 4 * 6 * 10 * 12 * 16;
  private uint[] XTestArray;
  private Integer SolutionX;
  private Integer SolutionY;
  private Integer ProductModTopBQuotient;
  private const uint ModBitsModulus = 0x10000;
  private const uint ModBitsModulusBitMask = 0xFFFF;
  private Integer LeftQuotient;
  // private uint[] Base17PlusModDiffArray;
  private BaseDiffRec[] BaseDiffArrays;
  private const int BaseDiffArraysMaxIndex = 2;
  // private FindFactors FindFactors1;


  internal struct BaseDiffRec
    {
    internal uint[] DiffArray;
    internal ulong[] ModArray;
    internal ulong Base;
    internal uint Prime;
    internal int PrimeIndex;
    }

  internal struct XYMatchesRec
    {
    internal uint B;
    internal uint L;
    internal uint[] XtoY;
    internal uint[] XPlusY;
    internal uint[] XTimesY;
    internal uint[] XPlusYNoDup;
    }


  private FindFactorsFromTop()
    {
    }


  internal FindFactorsFromTop( BackgroundWorker UseWorker, IntegerMath UseIntMath )
    {
    Worker = UseWorker;
    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    Product = new Integer();
    ProductSqrRoot = new Integer();
    MaximumSmallFactor = new Integer();
    TopB = new Integer();
    TopBB = new Integer();
    MinimumX = new Integer();
    MaximumX = new Integer();
    MinimumY = new Integer();
    MaximumY = new Integer();
    Left = new Integer();
    ProductModTopB = new Integer();
    SolutionX = new Integer();
    SolutionY = new Integer();
    ProductModTopBQuotient = new Integer();
    LeftQuotient = new Integer();

    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    BaseDiffArrays = new BaseDiffRec[BaseDiffArraysMaxIndex + 1];
    int Index = 0;
    BaseDiffArrays[Index].Prime = 13;
    BaseDiffArrays[Index].PrimeIndex = 5;
    BaseDiffArrays[Index].Base = BaseTo13;

    Index = 1;
    BaseDiffArrays[Index].Prime = 17;
    BaseDiffArrays[Index].PrimeIndex = 6;
    BaseDiffArrays[Index].Base = BaseDiffArrays[Index - 1].Base * BaseDiffArrays[Index].Prime;

    // BaseDiffArraysMaxIndex = 2;
    Index = 2;
    BaseDiffArrays[Index].Prime = 19;
    BaseDiffArrays[Index].PrimeIndex = 7;
    BaseDiffArrays[Index].Base = BaseDiffArrays[Index - 1].Base * BaseDiffArrays[Index].Prime;


    SetupBaseArray();
    SetupXTestArray();

    // FindFactors1 = new FindFactors( Worker, IntMath );
    }



  private void SetupBaseArray()
    {
    // 0) Base: 2
    // 1) Base: 6
    // 2) Base: 30
    // 3) Base: 210
    // 4) Base: 2,310
    // 5) Base: 30,030    Prime is 13.
    // 6) Base: 510,510
    // 7) Base: 9,699,690
    // 8) Base: 223,092,870         Prime is 23.
    // 9) Base: 6,469,693,230       It is a ulong at Prime 29.
    // 10) Base: 200,560,490,130
    // 11) Base: 7,420,738,134,810      Prime is 37.

    BaseArray = new Integer[BaseArraySize];
    Integer Base = new Integer();
    Base.SetToOne();
    for( int Count = 0; Count < BaseArraySize; Count++ )
      {
      IntMath.MultiplyULong( Base, IntMath.GetPrimeAt( Count ));
      Integer OneBase = new Integer();
      OneBase.Copy( Base );
      BaseArray[Count] = OneBase;
      // if( Count < 20 )
        // Worker.ReportProgress( 0, Count.ToString() + ") Base: " + IntMath.ToString10( OneBase ));

      }
    }




  internal bool FindTwoFactors( Integer UseProduct, Integer P, Integer Q )
    {
    Product.Copy( UseProduct );
    if( IntMath.SquareRoot( Product, ProductSqrRoot ))
      {
      // In the very unlikely event that the Product is a perfect square.
      P.Copy( ProductSqrRoot );
      Q.Copy( ProductSqrRoot );
      return true;
      }

    SetTopFactorForStart();
    FindTheFactors();
    if( !SolutionX.IsZero())
      return true;

    while( true )
      {
      if( Worker.CancellationPending )
        return false;

      // Or they'll throw exceptions to get out of this loop.

      if( !MoveTopFactorsDown())
        return false;

      FindTheFactors();
      if( !SolutionX.IsZero())
        return true;

      }

    return false;
    }



  private void SetTopFactorForStart()
    {
    TopB = null;
    for( int Count = 0; Count < BaseArraySize; Count++ )
      {
      if( ProductSqrRoot.ParamIsGreater( BaseArray[Count] ))
        {
        TopBIndex = Count - 1;
        TopB = new Integer();
        TopB.Copy( BaseArray[TopBIndex] );
        Worker.ReportProgress( 0, "TopBIndex: " + TopBIndex.ToString());
        break;
        }
      }

    if( TopB == null )
      throw( new Exception( "The base array wasn't big enough to find TopB." ));

    // One factor is larger than the square root and one factor (or
    // combination of smaller factors) is smaller.  This is the one
    // that's always smaller than the square root.
    MaximumSmallFactor.Copy( ProductSqrRoot );
    if( !CalculateTopNumbers())
      throw( new Exception( "CalculateTopNumbers() should not be returning false at the top." ));

    }



  private bool MoveTopFactorsDown()
    {
    if( TopBIndex == 0 )
      throw( new Exception( "TopBIndex got to zero." ));

    if( MaximumSmallFactor.ParamIsGreater( TopB ))
      throw( new Exception( "Bug. MaximumSmallFactor.ParamIsGreater( TopB )." ));
      // return false;

    // When it sets the first TopB it compares it to
    // ProductSqrRoot, and sets it to the base that's smaller than
    // that.  One bigger than that (plus one on the index) is bigger
    // than the square root.  When it gets here it has already checked
    // TopB + x from TopB up to the product square root.  So that
    // original TopB is the smallest value it can possibly check.
    // So TopB is the new maximum value for the small factor when
    // this index gets decremented.

    // Set that new max small factor value:
    MaximumSmallFactor.Copy( TopB );

    TopBIndex--;
    TopB.Copy( BaseArray[TopBIndex] );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TopBIndex after lowering it: " + TopBIndex.ToString());
    Worker.ReportProgress( 0, " " );
    return CalculateTopNumbers();
    }



  private bool CalculateTopNumbers()
    {
    TopBB.Copy( TopB );
    IntMath.DoSquare( TopBB );
    Left.Copy( Product );
    if( Left.ParamIsGreater( TopBB ))
      throw( new Exception( "Bug. TopBB is bigger than Left." ));

    IntMath.Subtract( Left, TopBB );

    IntMath.Divide( Product, TopB, Quotient, Remainder );
    ProductModTopB.Copy( Remainder );

    // The only way this Quotient would be zero is if Product
    // is smaller than TopB.
    ProductModTopBQuotient.Copy( Quotient );
    if( ProductModTopBQuotient.IsZero())
      throw( new Exception( "ProductModTopBQuotient.IsZero()." ));

    IntMath.Divide( Left, TopB, Quotient, Remainder );
    if( !Remainder.IsEqual( ProductModTopB ))
      throw( new Exception( "This is not right with ProductModTopB and Left." ));

    // L = B(x + y) + xy
    // Both the x + y part and the xy part have some part of the LeftQuotient.
    // It's like L = BQ + Bq + xy
    // L = B(Q + q) + xy

    LeftQuotient.Copy( Quotient );
    Worker.ReportProgress( 0, "LeftQuotient is: " + LeftQuotient.GetAsHexString());

    Worker.ReportProgress( 0, "Product is: " + Product.GetAsHexString());
    Worker.ReportProgress( 0, "Left is: " + Left.GetAsHexString());
    Worker.ReportProgress( 0, "ProductModTopB is: " + ProductModTopB.GetAsHexString());

    Worker.ReportProgress( 0, "ProductSqrRoot is: " + ProductSqrRoot.GetAsHexString());
    Worker.ReportProgress( 0, "TopB: " + TopB.GetAsHexString());
    Worker.ReportProgress( 0, "TopBB: " + TopBB.GetAsHexString());

    // P = (B + x)(B + y)
    // P = BB + Bx + By + xy
    // P - BB = Bx + By + xy
    // P - BB = B(x + y) + xy
    // (P - BB) / B = (x + y) + xy/B
    // Q = (x + y) + xy/B

    MinimumX.SetToOne();
    MaximumX.Copy( MaximumSmallFactor );
    if( MaximumX.ParamIsGreater( TopB ))
      throw( new Exception( "This is a bug with setting MaximumSmallFactor." ));

    IntMath.Subtract( MaximumX, TopB );

    Worker.ReportProgress( 0, "MaximumSmallFactor: " + MaximumSmallFactor.GetAsHexString());
    Worker.ReportProgress( 0, "MinimumX: " + MinimumX.GetAsHexString());
    Worker.ReportProgress( 0, "MaximumX: " + MaximumX.GetAsHexString());

    Integer FactorX = new Integer();
    FactorX.Copy( TopB );
    FactorX.Add( MinimumX );
    IntMath.Divide( Product, FactorX, Quotient, Remainder );
    MaximumY.Copy( Quotient );
    if( MaximumY.ParamIsGreater( TopB ))
      throw( new Exception( "TopB is bigger than MaximumY." ));

    IntMath.Subtract( MaximumY, TopB );
    if( LeftQuotient.ParamIsGreater( MaximumY ))
      throw( new Exception( "MaximumY is more than LeftQuotient." ));

    FactorX.Copy( TopB );
    FactorX.Add( MaximumX );
    IntMath.Divide( Product, FactorX, Quotient, Remainder );
    MinimumY.Copy( Quotient );
    if( MinimumY.ParamIsGreater( TopB ))
      throw( new Exception( "TopB is bigger than MinimumY." ));

    IntMath.Subtract( MinimumY, TopB );
    Worker.ReportProgress( 0, "MinimumY: " + MinimumY.GetAsHexString());
    Worker.ReportProgress( 0, "MaximumY: " + MaximumY.GetAsHexString());

    Integer XPlusY = new Integer();
    // Set it to its maximum.
    XPlusY.Copy( MinimumX );
    XPlusY.Add( MaximumY );
    if( LeftQuotient.ParamIsGreater( XPlusY ))
      throw( new Exception( "XPlusY for max is more than LeftQuotient." ));

    // This uses TopB and Left.
    MakeModArrays();
    MakeModBitsArray();
    return true;
    }



  private bool FindTheFactors()
    {
    SolutionX.SetToZero();

    Integer XPlusY = new Integer();
    Integer XTimesY = new Integer();
    Integer Temp = new Integer();
    Integer MaximumXPlusY = new Integer();

    // Set the max.
    MaximumXPlusY.Copy( MinimumX );
    MaximumXPlusY.Add( MaximumY );

    // Set XPlusY to the smallest it can be.
    XPlusY.Copy( MaximumX );
    XPlusY.Add( MinimumY );

    // (P - BB) / B = (x + y) + xy/B
    // Q = (x + y) + xy/B

    // Set XPlusY to the smallest it can be.

    // Minimum X + Y is 2,F21B384 + 2,F21B384
    // Maximum X + Y is 1 + 6,EED48FC5

    if( (XPlusY.GetD( 0 ) & 1) == 1 ) // If it's odd.
      XPlusY.Increment(); // It has to be even.

    uint SmallBaseCheck = (uint)IntMath.GetMod32( XPlusY, BaseTo13 );

    uint Loops = 0;
    // ulong IsInSmallPlusCount = 0;
    // ulong IsInModBitsPlusCount = 0;
    while( true )
      {
      Loops++;
      if( (Loops & 0xFFF) == 1 )
        Worker.ReportProgress( 0, "Loops: " + Loops.ToString() );

      if( (Loops & 0xFF) == 1 )
        {
        // Don't hog the server's resources too much.
        Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.
        }

      // if( 0 == IntMath.GetMod32( XPlusY, SmallBase ))
      if( (SmallBaseCheck % BaseTo13) == 0 )
        return FindTheFactorsWithSmallBase( XPlusY, MaximumXPlusY );

      // This doesn't necessarily line up at zero for SmallBase.
      // It might not contain a zero.
      if( !IsInSmallPlusModArray( XPlusY ))
        {
        XPlusY.AddULong( 2 );
        SmallBaseCheck += 2;
        if( MaximumXPlusY.ParamIsGreater( XPlusY ))
          return false;

        continue;
        }

      // IsInSmallPlusCount++;

      if( !IsInBitsTestArray( XPlusY ))
        {
        XPlusY.AddULong( 2 );
        SmallBaseCheck += 2;
        if( MaximumXPlusY.ParamIsGreater( XPlusY ))
          return false;

        continue;
        }

      // IsInModBitsPlusCount++;

      if( Worker.CancellationPending )
        return false;

      // Worker.ReportProgress( 0, "Loops: " + Loops.ToString() );
      // Worker.ReportProgress( 0, "IsInSmallPlusCount: " + IsInSmallPlusCount.ToString() );
      // Worker.ReportProgress( 0, "IsInModBitsPlusCount: " + IsInModBitsPlusCount.ToString() );

      if( !IsInPlusModArrays( XPlusY ))
        {
        XPlusY.AddULong( 2 );
        SmallBaseCheck += 2;
        if( MaximumXPlusY.ParamIsGreater( XPlusY ))
          return false;

        continue;
        }

      // P - BB = B(x + y) + xy
      Temp.Copy( TopB );
      IntMath.Multiply( Temp, XPlusY );
      XTimesY.Copy( Left );
      if( Left.ParamIsGreater( Temp ))
        {
        // Worker.ReportProgress( 0, "XPlusY is too big." );
        return false;
        }

      IntMath.Subtract( XTimesY, Temp );

      if( FindXTheHardWay( XPlusY, XTimesY ))
        {
        Integer Factor1 = new Integer();
        Integer Factor2 = new Integer();
        Factor1.Copy( TopB );
        Factor1.Add( SolutionX );
        IntMath.Divide( Product, Factor1, Quotient, Remainder );
        if( !Remainder.IsZero())
          throw( new Exception( "Bug for SolutionX. Remainder is not zero." ));

        Factor2.Copy( Quotient );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Factor1 is: " + IntMath.ToString10( Factor1 ));
        Worker.ReportProgress( 0, "Factor2 is: " + IntMath.ToString10( Factor2 ));
        return true;
        }

      XPlusY.AddULong( 2 );
      SmallBaseCheck += 2;
      if( MaximumXPlusY.ParamIsGreater( XPlusY ))
        {
        Worker.ReportProgress( 0, "XPlusY is too big after adding 2." );
        return false;
        }
      }
    }



  private bool FindTheFactorsWithSmallBase( Integer XPlusY, Integer MaximumXPlusY )
    {
    Integer XTimesY = new Integer();
    Integer Temp = new Integer();
    Integer StartingXPlusY = new Integer();

    StartingXPlusY.Copy( XPlusY );

    Worker.ReportProgress( 0, "XPlusY at top: " + IntMath.ToString10( XPlusY ) );

    uint Loops = 0;
    // ulong IsInModCount = 0;
    // ulong IsInModBitsPlusCount = 0;
    while( true )
      {
      Loops++;
      if( (Loops & 0x3) == 1 )
        Worker.ReportProgress( 0, "Loops with BaseTo13: " + Loops.ToString() );

      XPlusY.Copy( StartingXPlusY );

      int Last = SmallPlusModArray.Length;
      for( int Count = 0; Count < Last; Count++ )
        {
        if( (Count & 0xFF) == 1 )
          {
          // Don't hog the server's resources too much.
          Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.
          }

        XPlusY.AddULong( SmallPlusModDiffArray[Count] );

        if( MaximumXPlusY.ParamIsGreater( XPlusY ))
          return false;

        if( !IsInBitsTestArray( XPlusY ))
          continue;

        // IsInModBitsPlusCount++;

        if( Worker.CancellationPending )
          return false;

        if( !IsInPlusModArrays( XPlusY ))
          continue;

        // IsInModCount++;
        // Worker.ReportProgress( 0, "Loops: " + Loops.ToString() );
        // Worker.ReportProgress( 0, "IsInModCount: " + IsInModCount.ToString() );
        // Worker.ReportProgress( 0, "IsInModBitsPlusCount: " + IsInModBitsPlusCount.ToString() );

        // P - BB = B(x + y) + xy
        Temp.Copy( TopB );
        IntMath.Multiply( Temp, XPlusY );
        XTimesY.Copy( Left );
        if( Left.ParamIsGreater( Temp ))
          {
          // Worker.ReportProgress( 0, "XPlusY is too big." );
          return false;
          }

        IntMath.Subtract( XTimesY, Temp );

        if( FindXTheHardWay( XPlusY, XTimesY ))
          {
          Integer Factor1 = new Integer();
          Integer Factor2 = new Integer();
          Factor1.Copy( TopB );
          Factor1.Add( SolutionX );
          IntMath.Divide( Product, Factor1, Quotient, Remainder );
          if( !Remainder.IsZero())
            throw( new Exception( "Bug for SolutionX. Remainder is not zero." ));

          Factor2.Copy( Quotient );
          Worker.ReportProgress( 0, " " );
          Worker.ReportProgress( 0, " " );
          Worker.ReportProgress( 0, "Factor1 is: " + IntMath.ToString10( Factor1 ));
          Worker.ReportProgress( 0, "Factor2 is: " + IntMath.ToString10( Factor2 ));
          return true;
          }
        }

      StartingXPlusY.AddULong( BaseTo13 );
      if( 0 == IntMath.GetMod32( StartingXPlusY, BaseDiffArrays[1].Base ))
        return FindTheFactorsWithBaseDiffArray( 1, StartingXPlusY, MaximumXPlusY );

      }
    }



  private bool FindTheFactorsWithBaseDiffArray( int Index,
                                                Integer XPlusY,
                                                Integer MaximumXPlusY )
    {
    Integer XTimesY = new Integer();
    Integer Temp = new Integer();
    Integer StartingXPlusY = new Integer();

    StartingXPlusY.Copy( XPlusY );

    uint Loops = 0;
    // ulong IsInModCount = 0;
    // ulong IsInModBitsPlusCount = 0;
    while( true )
      {
      Loops++;
      // if( (Loops & 0x3) == 1 )
        Worker.ReportProgress( 0, "Loops with Base " + BaseDiffArrays[Index].Prime.ToString() + ": " + Loops.ToString() );

      // TestAdding.Copy( StartingXPlusY );
      XPlusY.Copy( StartingXPlusY );
      // Worker.ReportProgress( 0, "XPlusY test is:   " + IntMath.ToString10( XPlusY ));
      // Worker.ReportProgress( 0, "MaximumXPlusY is: " + IntMath.ToString10( MaximumXPlusY ));

      int Last = BaseDiffArrays[Index].DiffArray.Length;
      for( int Count = 0; Count < Last; Count++ )
        {
        if( (Count & 0xFF) == 1 )
          {
          // Don't hog the server's resources too much.
          Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.
          }

        XPlusY.AddULong( BaseDiffArrays[Index].DiffArray[Count] );
        if( MaximumXPlusY.ParamIsGreater( XPlusY ))
          return false;

        if( !IsInBitsTestArray( XPlusY ))
          continue;

        // IsInModBitsPlusCount++;

        if( Worker.CancellationPending )
          return false;

        if( !IsInPlusModArrays( XPlusY ))
          continue;

        // IsInModCount++;
        // Worker.ReportProgress( 0, "Loops: " + Loops.ToString() );
        // Worker.ReportProgress( 0, "IsInModCount: " + IsInModCount.ToString() );
        // Worker.ReportProgress( 0, "IsInModBitsPlusCount: " + IsInModBitsPlusCount.ToString() );

        // P - BB = B(x + y) + xy
        Temp.Copy( TopB );
        IntMath.Multiply( Temp, XPlusY );
        XTimesY.Copy( Left );
        if( Left.ParamIsGreater( Temp ))
          {
          // Worker.ReportProgress( 0, "XPlusY is too big." );
          return false;
          }

        IntMath.Subtract( XTimesY, Temp );

        if( FindXTheHardWay( XPlusY, XTimesY ))
          {
          Integer Factor1 = new Integer();
          Integer Factor2 = new Integer();
          Factor1.Copy( TopB );
          Factor1.Add( SolutionX );
          IntMath.Divide( Product, Factor1, Quotient, Remainder );
          if( !Remainder.IsZero())
            throw( new Exception( "Bug for SolutionX. Remainder is not zero." ));

          Factor2.Copy( Quotient );
          Worker.ReportProgress( 0, " " );
          Worker.ReportProgress( 0, " " );
          Worker.ReportProgress( 0, "Factor1 is: " + IntMath.ToString10( Factor1 ));
          Worker.ReportProgress( 0, "Factor2 is: " + IntMath.ToString10( Factor2 ));
          return true;
          }
        }

      StartingXPlusY.AddULong( BaseDiffArrays[Index].Base );

      if( Index < BaseDiffArraysMaxIndex )
        {
        if( 0 == IntMath.GetMod32( StartingXPlusY, BaseDiffArrays[Index + 1].Base ))
          {
          // Calling this recursively:
          return FindTheFactorsWithBaseDiffArray( Index + 1, StartingXPlusY, MaximumXPlusY );
          }
        }
      }
    }



  private void MakeModArrays()
    {
    // Worker.ReportProgress( 0, "Top of MakeModArrays()." );

    // ModArrayLength = TopBIndex << 1; // Twice the TopBIndex.
    ModArray = new XYMatchesRec[ModArrayLength];

    for( int Count = 0; Count < ModArrayLength; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      // B would be zero for all of these, by the definition of TopB.
      ModArray[Count].B = (uint)IntMath.GetMod32( TopB, IntMath.GetPrimeAt( Count ));
      ModArray[Count].L = (uint)IntMath.GetMod32( Left, IntMath.GetPrimeAt( Count ));
      }

    MakeXYArrays();
    }



  private void MakeModBitsArray()
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

      // if( (CountX & 0xFFFF) == 1 )
        // Worker.ReportProgress( 0, "CountX: " + CountX.ToString() );

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

    RemoveModBitsArrayDuplicates( ref Rec );

    for( int Count = 0; Count < Rec.XPlusYNoDup.Length; Count++ )
      {
      uint Value = Rec.XPlusYNoDup[Count];
      uint X = Value >> 8;
      uint Y = Value & 0xFF;
      ModBitsTestArray[X, Y] = true;
      }

    // Out of 64K possibilities:
    // ModBitsArray no dup size is: 1369
    // ModBitsArray no dup size is: 1369
    // ModBitsArray no dup size is: 1369
    // ModBitsArray no dup size is: 8192
    // ModBitsArray no dup size is: 8192
    // ModBitsArray no dup size is: 8193
    // ModBitsArray no dup size is: 8192
    // ModBitsArray no dup size is: 4097

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in IsInModBitsXPlusYNoDupArray()." ));
      }
    }



  private bool IsInBitsTestArray( Integer ToTest )
    {
    try
    {
    uint Value = (uint)ToTest.GetD( 0 );
    Value = Value & 0xFFFF;
    uint X = Value >> 8;
    uint Y = Value & 0xFF;
    return ModBitsTestArray[X, Y];
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in IsInBitsTestArray()." ));
      }
    }



  private void RemoveModBitsArrayDuplicates( ref XYMatchesRec Rec )
    {
    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "Removing duplicates." );
    if( Worker.CancellationPending )
      return;

    Rec.XPlusYNoDup = new uint[ModBitsModulus];
    for( uint CountStart = 0; CountStart < ModBitsModulus; CountStart++ )
      Rec.XPlusYNoDup[CountStart] = Rec.XPlusY[CountStart];

    for( uint CountStart = 0; CountStart < ModBitsModulus; CountStart++ )
      {
      if( Rec.XPlusYNoDup[CountStart] == 0xFFFFFFFF )
        continue;

      for( uint CountTest = CountStart + 1; CountTest < ModBitsModulus; CountTest++ )
        {
        if( Rec.XPlusYNoDup[CountStart] == Rec.XPlusYNoDup[CountTest] )
          Rec.XPlusYNoDup[CountTest] = 0xFFFFFFFF;

        }
      }

    int MoveTo = 0;
    for( uint CountTest = 0; CountTest < ModBitsModulus; CountTest++ )
      {
      if( Rec.XPlusYNoDup[CountTest] != 0xFFFFFFFF )
        {
        Rec.XPlusYNoDup[MoveTo] = Rec.XPlusYNoDup[CountTest];
        MoveTo++;
        }
      }

    Array.Resize( ref Rec.XPlusYNoDup, MoveTo );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "ModBitsArray no dup size is: " + MoveTo.ToString() );
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
      // Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "Prime: " + Prime.ToString() );
      // Worker.ReportProgress( 0, "X\tY\tB\tL\tXPlusY\tXTimesY" );

      for( uint CountX = 0; CountX < Prime; CountX++ )
        {
        ModArray[Count].XPlusY[CountX] = 0xFFFFFFFF;
        ModArray[Count].XTimesY[CountX] = 0xFFFFFFFF;
        }

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
            HowManyMatched++;
            ModArray[Count].XtoY[CountX] = CountY;
            ModArray[Count].XPlusY[CountX] = (CountX + CountY) % Prime;
            ModArray[Count].XTimesY[CountX] = (CountX * CountY) % Prime;

            /*
            string ShowS = CountX.ToString() + "\t" +
                           CountY.ToString() + "\t" +
                           ModArray[Count].B.ToString() + "\t" +
                           ModArray[Count].L.ToString() + "\t" +
                           ModArray[Count].XPlusY[CountX] + "\t" +
                           ModArray[Count].XTimesY[CountX];

            Worker.ReportProgress( 0, ShowS );
            */
            }
          }
        }
      
      if( HowManyMatched == 0 )
        throw( new Exception( "This never happens. Zero matches for prime: " + Prime.ToString() ));

      }

    RemoveArrayDuplicates();
    MakeSmallPlusModArray();

    for( int Count = 1; Count <= BaseDiffArraysMaxIndex; Count++ )
      MakeBasePlusModArray( Count );

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
      // Worker.ReportProgress( 0, "\r\nPrime: " + Prime.ToString() + "\r\n" );

      ModArray[Count].XPlusYNoDup = new uint[Prime];
      for( uint CountStart = 0; CountStart < Prime; CountStart++ )
        ModArray[Count].XPlusYNoDup[CountStart] = ModArray[Count].XPlusY[CountStart];

      for( uint CountStart = 0; CountStart < Prime; CountStart++ )
        {
        if( ModArray[Count].XPlusYNoDup[CountStart] == 0xFFFFFFFF )
          continue;

        for( uint CountTest = CountStart + 1; CountTest < Prime; CountTest++ )
          {
          if( ModArray[Count].XPlusYNoDup[CountStart] == ModArray[Count].XPlusYNoDup[CountTest] )
            ModArray[Count].XPlusYNoDup[CountTest] = 0xFFFFFFFF;

          }
        }

      int MoveTo = 0;
      for( uint CountTest = 0; CountTest < Prime; CountTest++ )
        {
        if( ModArray[Count].XPlusYNoDup[CountTest] != 0xFFFFFFFF )
          {
          ModArray[Count].XPlusYNoDup[MoveTo] = ModArray[Count].XPlusYNoDup[CountTest];
          // Worker.ReportProgress( 0, MoveTo.ToString() + ") XPlusYNoDup: " + ModArray[Count].XPlusYNoDup[MoveTo].ToString() );
          MoveTo++;
          }
        }

      Array.Resize( ref ModArray[Count].XPlusYNoDup, MoveTo );
      // Worker.ReportProgress( 0, Count.ToString() + ") No dup array size is: " + MoveTo.ToString() );

      }
    }



  private bool IsInXPlusYNoDupArray( int PrimeIndex, uint Test )
    {
    if( PrimeIndex >= ModArrayLength )
      throw( new Exception( "The ModArray isn't big enough for this number: " + PrimeIndex.ToString() ));

    try
    {
    int Length = ModArray[PrimeIndex].XPlusYNoDup.Length;
    for( int Count = 0; Count < Length; Count++ )
      {
      if( Test == ModArray[PrimeIndex].XPlusYNoDup[Count] )
        return true;

      }

    return false;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "This one. Exception in IsInXPlusYNoDupArray()." ));
      }
    }



  private void MakeSmallPlusModArray()
    {
    try
    {
    // SmallBaseSize = 2 * 3 * 5 * 7 * 11 * 13;
    SmallPlusModArray = new uint[BaseTo13];
    int Last = 0;
    /*
    PrimeArray[0] = 2;
    PrimeArray[1] = 3;
    PrimeArray[2] = 5;
    PrimeArray[3] = 7;
    PrimeArray[4] = 11;
    PrimeArray[5] = 13;
    PrimeArray[6] = 17;
    PrimeArray[7] = 19;
    PrimeArray[8] = 23;
    */

    for( uint Count = 0; Count < BaseTo13; Count += 2 )
      {
      // if( Worker.CancellationPending )
        // return;

      // Check the primes from 3 to 13.
      bool IsInArray = true;
      for( int PCount = 1; PCount <= 5; PCount++ )
        {
        uint Prime = IntMath.GetPrimeAt( PCount );
        uint Test = Count % Prime;
        if( !IsInXPlusYNoDupArray( PCount, Test ))
          {
          IsInArray = false;
          break;
          }

        if( !IsInArray )
          break;

        }

      if( IsInArray )
        {
        // It was true for all of the primes from 3 to 13.
        SmallPlusModArray[Last] = Count;
        Last++;
        }
      }

    // An object is passed by reference.  But the reference is passed
    // by value.  (Like in Java.)  This actually creates a new array,
    // so it creates a new reference, and that new reference has to be
    // passed back.  So that's why the ref is needed here.
    Array.Resize( ref SmallPlusModArray, Last );
    Utility.SortUintArray( ref SmallPlusModArray );

    Worker.ReportProgress( 0, "SmallPlusModArray[0]: " + SmallPlusModArray[0].ToString());

    SmallPlusModDiffArray = new uint[SmallPlusModArray.Length];
    uint Previous = 0;
    Last = SmallPlusModArray.Length;
    for( int Count = 0; Count < Last; Count++ )
      {
      uint Diff = SmallPlusModArray[Count] - Previous;
      Previous = SmallPlusModArray[Count];
      SmallPlusModDiffArray[Count] = Diff;
      }

    BaseDiffArrays[0].DiffArray = SmallPlusModDiffArray;
    BaseDiffArrays[0].ModArray = Utility.MakeULongArrayFromUIntArray( SmallPlusModArray );

    // for( int Count = 0; Count < SmallPlusModArray.Length; Count++ )
      // Worker.ReportProgress( 0, SmallPlusModArray[Count].ToString());

    Worker.ReportProgress( 0, "SmallPlusModArray size is: " + Last.ToString() );
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeSmallPlusModArray()." ));
      }
    }




  private void MakeBasePlusModArray( int Index )
    {
    int SmallerArrayLength = BaseDiffArrays[Index - 1].DiffArray.Length;
    ulong[] BasePlusModArray = new ulong[SmallerArrayLength * BaseDiffArrays[Index].Prime];

    int Last = 0;
    for( uint CountBase = 0; CountBase < BaseDiffArrays[Index].Prime; CountBase++ )
      {
      if( Worker.CancellationPending )
        return;

      ulong Base = CountBase * BaseDiffArrays[Index - 1].Base;
      // Worker.ReportProgress( 0, "Base: " + Base.ToString( "N0" ));

      for( int Count = 0; Count < SmallerArrayLength; Count++ )
        {
        ulong ToTest = Base + BaseDiffArrays[Index - 1].ModArray[Count];
        uint TestMod = (uint)(ToTest % BaseDiffArrays[Index].Prime);
        if( !IsInXPlusYNoDupArray( BaseDiffArrays[Index].PrimeIndex, TestMod ))
          continue;

        BasePlusModArray[Last] = ToTest;
        Last++;
        }
      }

    if( Last == SmallerArrayLength )
      throw( new Exception( "Last == SmallerArrayLength." ));

    Array.Resize( ref BasePlusModArray, Last );
    Worker.ReportProgress( 0, "BasePlusModArray size is: " + Last.ToString( "N0" ));
    Worker.ReportProgress( 0, "Base is: " + BaseDiffArrays[Index].Base.ToString( "N0" ));

    BaseDiffArrays[Index].ModArray = BasePlusModArray;
    BaseDiffArrays[Index].DiffArray = new uint[BasePlusModArray.Length];
    ulong Previous = 0;
    Last = BasePlusModArray.Length;
    for( int Count = 0; Count < Last; Count++ )
      {
      ulong Diff = BasePlusModArray[Count] - Previous;
      if( (Diff >> 32) != 0 )
        throw( new Exception( "(Diff >> 32) != 0 at Index: " + Index.ToString() ));

      Previous = BasePlusModArray[Count];
      BaseDiffArrays[Index].DiffArray[Count] = (uint)Diff;
      }
    }



  private bool IsInSmallPlusModArray( Integer ToTest )
    {
    try
    {
    uint Test = (uint)IntMath.GetMod32( ToTest, BaseTo13 );
    int Length = SmallPlusModArray.Length;
    for( int Count = 0; Count < Length; Count++ )
      {
      if( Test == SmallPlusModArray[Count] )
        return true;

      }

    return false;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in MakeSmallPlusModArray()." ));
      }
    }



  private bool IsInPlusModArrays( Integer ToTest )
    {
    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    uint Test = (uint)IntMath.GetMod32( ToTest, 59 * 61 * 67 );
    uint Prime = 59;
    uint TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 16, TestP ))
      return false;

    Prime = 61;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 17, TestP ))
      return false;

    Prime = 67;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 18, TestP ))
      return false;

    ///////////
    Test = (uint)IntMath.GetMod32( ToTest, 71 * 73 * 79 );
    Prime = 71;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 19, TestP ))
      return false;

    Prime = 73;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 20, TestP ))
      return false;

    Prime = 79;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 21, TestP ))
      return false;


    ///////////
    Test = (uint)IntMath.GetMod32( ToTest, 83 * 89 * 97 );
    Prime = 83;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 22, TestP ))
      return false;

    Prime = 89;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 23, TestP ))
      return false;

    Prime = 97;
    TestP = Test % Prime;
    if( !IsInXPlusYNoDupArray( 24, TestP ))
      return false;

    for( int Count = 25; Count < ModArrayLength; Count++ )
      {
      Prime = IntMath.GetPrimeAt( Count );
      Test = (uint)IntMath.GetMod32( ToTest, Prime );
      if( !IsInXPlusYNoDupArray( Count, Test ))
        return false;

      }

    return true;
    }



  private void SetupXTestArray()
    {
    try
    {
    // XTestArrayBase =     2 * 3 * 5 * 7 * 11 * 13 * 17;
    // XTestArrayEulerPhi =     2 * 4 * 6 * 10 * 12 * 16;
    XTestArray = new uint[XTestArrayEulerPhi];

    int Index = 0;
    for( uint Count = 0; Count < XTestArrayBase; Count++ )
      {
      if( (Count & 1) == 0 ) // If its an even number.
        continue;

      if( (Count % 3) == 0 ) // If its divisible by 3.
        continue;

      if( (Count % 5) == 0 )
        continue;

      if( (Count % 7) == 0 )
        continue;

      if( (Count % 11) == 0 )
        continue;

      if( (Count % 13) == 0 )
        continue;

      if( (Count % 17) == 0 )
        continue;

      XTestArray[Index] = Count;
      Index++;
      }
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in SetupXTestArray()." ));
      }
    }



  private bool FindXTheHardWay( Integer XPlusY, Integer XTimesY )
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of FindXTheHardWay()." );

    Integer XY = new Integer();
    Integer X = new Integer();
    // P - BB = B(x + y) + xy
    // TopB );
    ulong AGazillion = 1234567890123456Ul;
    for( ulong BigCount = 0; BigCount < AGazillion; BigCount++ )
      {
      if( Worker.CancellationPending )
        return false;

      if( BigCount > 0 )
        {
        if( (BigCount & 0xFF) == 0 )
          Worker.ReportProgress( 0, "BigCount: " + BigCount.ToString() );

        }

      ulong BigBase = BigCount * XTestArrayBase;
      for( int Count = 0; Count < XTestArrayEulerPhi; Count++ )
        {
        ulong UX = BigBase + XTestArray[Count];
        X.SetFromULong( UX );
        if( XPlusY.ParamIsGreaterOrEq( X ))
          {
          // Worker.ReportProgress( 0, "Tested all the X values." );
          return false;
          }

        XY.Copy( XPlusY );
        IntMath.SubtractULong( XY, UX ); // Make it Y.

        // Y can only be mod certain things depending on what X is.
        // So I don't need to use the big integers until some
        // tests are done with that first.

        IntMath.MultiplyULong( XY, UX );  // Y times X is XY
        if( XY.IsEqual( XTimesY ))
          {
          Worker.ReportProgress( 0, "Found X and Y." );
          SolutionX.Copy( X );
          SolutionY.Copy( XPlusY );
          IntMath.Subtract( SolutionY, X );
          return true;
          }
        }
      }

    // After trying a gazillion numbers.
    return false;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in FindXTheHardWay()." ));
      }
    }





  }
}

