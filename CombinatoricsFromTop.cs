// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{
  class CombinatoricsFromTop
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
  private ECTime StartTime;
  private Integer SolutionX;
  private Integer SolutionY;
  private Integer ProductModTopBQuotient;
  private Integer LeftQuotient;
  private CRTCombinatorics CRTCombin;
  private CRTCombinSetupRec[] SetupArray;


  private CombinatoricsFromTop()
    {
    }



  internal CombinatoricsFromTop( uint ModMask, CRTCombinSetupRec[] UseSetupArray, BackgroundWorker UseWorker, IntegerMath UseIntMath )
    {
    SetupArray = UseSetupArray;

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
    StartTime = new ECTime();
    SolutionX = new Integer();
    SolutionY = new Integer();
    ProductModTopBQuotient = new Integer();
    LeftQuotient = new Integer();
    CRTCombin = new CRTCombinatorics( ModMask, SetupArray, Worker, IntMath );

    SetupBaseArray();
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
    StartTime.SetToNow();
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
      {
      P.Copy( SolutionX );
      Q.Copy( SolutionY );
      return true;
      }

    while( true )
      {
      if( Worker.CancellationPending )
        return false;

      // Or they'll throw exceptions to get out of this loop.

      if( !MoveTopFactorsDown())
        return false;

      FindTheFactors();
      if( !SolutionX.IsZero())
        {
        P.Copy( SolutionX );
        Q.Copy( SolutionY );
        return true;
        }
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
    CalculateTopNumbers();
    }



  private bool MoveTopFactorsDown()
    {
    // The number would have already been checked for small primes
    // so it can't get too small for this TopBIndex.
    // 0   1   2   3    4
    // 2 * 3 * 5 * 7 * 11
    // 0) Base: 2
    // 1) Base: 6
    // 2) Base: 30
    // 3) Base: 210
    // 4) Base: 2,310 = 2 * 3 * 5 * 7 * 11

    if( TopBIndex <= 4 ) // If it doesn't have any factors.
      {
      // Worker.ReportProgress( 0, "TopBIndex got too small." );
      throw( new Exception( "TopBIndex got too small." ));
      return false;
      }
    if( MaximumSmallFactor.ParamIsGreater( TopB ))
      throw( new Exception( "Bug. MaximumSmallFactor.ParamIsGreater( TopB )." ));
      // return false;

    // When it sets the first TopB it compares it to
    // ProductSqrRoot, and sets it to the base that's smaller than
    // that.  One bigger than that (plus one on the index) is bigger
    // than the square root.  When it gets here it has already checked
    // TopB + x from TopB up to the product square root.  So that
    // original TopB is the smallest value it could possibly have
    // checked.  So TopB is the new maximum value for the small factor
    // when this index gets decremented.

    // Set that new max small factor value:
    MaximumSmallFactor.Copy( TopB );

    TopBIndex--;
    TopB.Copy( BaseArray[TopBIndex] );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TopBIndex after lowering it: " + TopBIndex.ToString());
    Worker.ReportProgress( 0, " " );

    try
    {
    CalculateTopNumbers();
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "CalculateTopNumbers() threw an exception in MoveTopFactorsDown()." ));
      // return false; // Move it down one more?
      }

    return true;
    }



  private void CalculateTopNumbers()
    {
    // P = (B + x)(B + y)
    // P = BB + Bx + By + xy
    // P - BB = Bx + By + xy
    // P - BB = B(x + y) + xy

    TopBB.Copy( TopB );
    IntMath.DoSquare( TopBB );
    Left.Copy( Product );
    if( Left.ParamIsGreater( TopBB ))
      throw( new Exception( "Bug. TopBB is bigger than Left." ));

    IntMath.Subtract( Left, TopBB );

    Integer Gcd = new Integer();
    IntMath.GreatestCommonDivisor( Product, Left, Gcd );
    if( !Gcd.IsOne())
      {
      // Left = P - BB
      // The GCD of P and P - BB divides P.  So:
      // P = 0 mod G
      // P - BB = 0 mod G.
      // So BB is congruent to zero mod G.  But BB is made up of
      // only small primes and the Product has no small prime factors,
      // so that can't be true.
      throw( new Exception( "This can't happen because the Gcd is not made of small primes." ));
      }

    // x and y can't be congruent to zero mod the small primes in TopB.

    // Left is co-prime to Product.
    // Left = B(x + y) + xy
    // B(x + y) + xy has no factors in common with the Product.
    // L = Bx + By + xy
    // L = Bx + y(B + x)
    // L - Bx = y(B + x)
    // L - Bx has factors in common with the Product.
    // L - Bx doesn't have any small factors unless they are in y.
    // (L - Bx) is congruent to zero mod y.


    uint SmallPrime = IntMath.IsDivisibleBySmallPrime( Left );
    if( SmallPrime != 0 )
      {
      // Worker.ReportProgress( 0, "Left is divisible by: " + SmallPrime.ToString() );
      // Left is divisible by: 59
      // Left is divisible by: 4297
      // Left = P - BB.
      // The SmallPrime is not in P or in BB.
      // P is not divisible by a small prime.
      // BB is only made up of small primes.
      // If BB is congruent to zero mod SmallPrime (if SmallPrime
      // is contained in BB) then Left can't be congruent to zero mod
      // SmallPrime because P isn't.
      if( 0 == IntMath.GetMod32( TopB, SmallPrime ))
        throw( new Exception( "SmallPrime can't be in TopB." ));

      // If x or y was divisible by a small prime:
      // L = B(x + y) + xy
      // L = B(x + 0) + 0  mod that prime.
      // L = Bx
      // P - BB = Bx
      // P = BB + Bx
      // P = B(B + x)
      // Then P would be congruent to B(B + x) for that small prime.
      // Prime: 113
      // X is congruent to zero mod 113.
      // X	Y	B	L	XPlusY	XTimesY
      // 0	101	70	64	101	0
      // L = B(0 + 101) + 0*101  mod 113
      // L = B(0 + 101) + 0*101
      // L = B*101
      // 64 = 70 * 101  mod 113
      // L is co-prime to B so there is only one Y that works.
      // And the corresponding matching sum mod 113:
      // 101	0	70	64	101	0

      // If L was congruent to zero then
      // L = B(x + y) + xy
      // 0 = B(x + y) + xy
      // And if y was congruent to zero then
      // 0 = B(x + 0) + x0
      // 0 = Bx so x has to be zero.
      // If one is zero, both are zero.

      // If L was congruent to zero, but neither x or y are then
      // 0 = B(x + y) + xy
      // xy is not congruent to zero and so neither is B(x + y).
      // 0 = B(x + y) + xy
      // -xy = B(x + y)
      // For every x there is a corresponding y.
      // That have a multiplicative-inverse type of relationship.
      }

    // If x and y were the same number then
    // P = (B + x)(B + y)
    // And I already checked that the square root of P is not a factor.
    // So x and y have to be different numbers.
    // L = B(x + y) + xy


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
    // Both the x + y part and the xy part have some part of the
    // LeftQuotient.  It is partitioned between them.
    // It's like L = B*q1 + B*q2 + xyRemainder
    // L = B(q1 + q2) + xyRemainder

    LeftQuotient.Copy( Quotient );
    // Worker.ReportProgress( 0, "LeftQuotient is: " + LeftQuotient.GetAsHexString());
    // Worker.ReportProgress( 0, "Product is: " + Product.GetAsHexString());
    // Worker.ReportProgress( 0, "Left is: " + Left.GetAsHexString());
    // Worker.ReportProgress( 0, "ProductModTopB is: " + ProductModTopB.GetAsHexString());
    // Worker.ReportProgress( 0, "ProductSqrRoot is: " + ProductSqrRoot.GetAsHexString());
    // Worker.ReportProgress( 0, "TopB: " + TopB.GetAsHexString());
    // Worker.ReportProgress( 0, "TopBB: " + TopBB.GetAsHexString());

    // P = (B + x)(B + y)
    // P = BB + Bx + By + xy
    // P - BB = Bx + By + xy
    // P - BB = B(x + y) + xy
    // L = B(x + y) + xy

    MinimumX.SetToOne();
    MaximumX.Copy( MaximumSmallFactor );
    if( MaximumX.ParamIsGreater( TopB ))
      throw( new Exception( "This is a bug with setting MaximumSmallFactor." ));

    IntMath.Subtract( MaximumX, TopB );

    // Worker.ReportProgress( 0, "MaximumSmallFactor: " + MaximumSmallFactor.GetAsHexString());
    Worker.ReportProgress( 0, "MinimumX: " + MinimumX.GetAsHexString());
    Worker.ReportProgress( 0, "MaximumX: " + MaximumX.GetAsHexString());

    Integer FactorX = new Integer();
    FactorX.Copy( TopB );
    FactorX.Add( MinimumX );
    IntMath.Divide( Product, FactorX, Quotient, Remainder );
    MaximumY.Copy( Quotient );

    // The Quotient is bigger than the square root of Product
    // so it would be a bug if TopB was bigger than that.
    if( MaximumY.ParamIsGreater( TopB ))
      throw( new Exception( "Bug. TopB is bigger than MaximumY." ));

    IntMath.Subtract( MaximumY, TopB );
    // This would be a bug because MinimumX was set to 1.
    // L = B(x + y) + xy
    if( LeftQuotient.ParamIsGreater( MaximumY ))
      throw( new Exception( "Bug. MaximumY is more than LeftQuotient." ));

    // Worker.ReportProgress( 0, "MaximumY first estimate: " + MaximumY.GetAsHexString());

    // L = B(1 + MaxY) + MaxY
    // L = B + B*MaxY + MaxY
    // L = B + MaxY(B + 1)

    Integer TestMax = new Integer();
    TestMax.Copy( Left );
    if( TestMax.ParamIsGreater( TopB ))
      throw( new Exception( "Bug. TopB is more than TestMax." ));

    IntMath.Subtract( TestMax, TopB );
    // L - B = MaxY(B + 1)
    Integer BPlus1 = new Integer();
    BPlus1.Copy( TopB );
    BPlus1.Increment();
    IntMath.Divide( TestMax, BPlus1, Quotient, Remainder );
    TestMax.Copy( Quotient );

    if( TestMax.ParamIsGreater( MaximumY ))
      {
      throw( new Exception( "Does this ever get lowered?" ));
      // MaximumY.Copy( TestMax );
      // Worker.ReportProgress( 0, "MaximumY was lowered: " + MaximumY.GetAsHexString());
      // So MinimumX has to be raised.
      }

    FactorX.Copy( TopB );
    FactorX.Add( MaximumX );
    IntMath.Divide( Product, FactorX, Quotient, Remainder );
    MinimumY.Copy( Quotient );
    if( MinimumY.ParamIsGreater( TopB ))
      throw( new Exception( "Bug. TopB is bigger than MinimumY." ));

    // Worker.ReportProgress( 0, "MaximumY test 4." );

    IntMath.Subtract( MinimumY, TopB );
    Worker.ReportProgress( 0, "MinimumY: " + MinimumY.GetAsHexString());
    Worker.ReportProgress( 0, "MaximumY: " + MaximumY.GetAsHexString());

    Integer XPlusY = new Integer();
    // Set it to its maximum.
    XPlusY.Copy( MinimumX );
    XPlusY.Add( MaximumY );
    if( LeftQuotient.ParamIsGreater( XPlusY ))
      throw( new Exception( "Bug. XPlusY for max is more than LeftQuotient." ));

    CRTCombin.MakeModArrays( TopB, Left );
    CRTCombin.MakeModBitsArray( TopB, Left );

    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    CRTCombin.SetUpDigitArrays();
    }



  private void FindTheFactors()
    {
    SolutionX.SetToZero();

    Integer XPlusY = new Integer();
    Integer XTimesY = new Integer();
    Integer Temp = new Integer();
    Integer MinimumXPlusY = new Integer();
    Integer MaximumXPlusY = new Integer();

    // Set the min.
    MinimumXPlusY.Copy( MaximumX );
    MinimumXPlusY.Add( MinimumY );

    // Set the max.
    MaximumXPlusY.Copy( MinimumX );
    MaximumXPlusY.Add( MaximumY );

    CRTCombin.SetDigitIndexesToZero();
    uint Loops = 0;
    while( true )
      {
      Loops++;
      if( (Loops & 0x3FFFFF) == 0 )
        {
        Worker.ReportProgress( 0, "Loops: " + Loops.ToString( "N0" ) );
        }

      // Use Task Manager to tweak CPU Utilization with this bit mask.
      if( (Loops & 0xFFFF) == 0 )
        {
        // Allow me to use my computer while this is being tested in
        // the background.
        Thread.Sleep( 1 ); // This is on one of the (four) separate threads.
        }

      if( Worker.CancellationPending )
        return;

      // IncrementCRTDigits() doesn't increment it by magnitude,
      // so it can come out too big or too small.  It's not in
      // sorted order by magnitude.

      CRTCombin.GetIntegerValue( XPlusY );

      if( !CRTCombin.IsInBitsTestArray( (uint)XPlusY.GetD( 0 ) ))
        {
        // The increments like this are by far the biggest user of time in this loop.
        // The Visual Studio profiler shows the "hot path" to be
        // IncrementCRTDigitsWithBitMask().
        if( !CRTCombin.IncrementCRTDigitsWithBitMask())
          {
          Worker.ReportProgress( 0, "CRTCombin incremented to the end." );
          return;
          }

        continue;
        }

      if( !CRTCombin.IsInPlusModArrays( XPlusY ))
        {
        if( !CRTCombin.IncrementCRTDigitsWithBitMask())
          {
          Worker.ReportProgress( 0, "CRTCombin incremented to the end." );
          return;
          }

        continue;
        }

      if( XPlusY.ParamIsGreater( MinimumXPlusY ))
        {
        // TooSmallCount++;
        if( !CRTCombin.IncrementCRTDigitsWithBitMask())
          {
          Worker.ReportProgress( 0, "CRTCombin incremented to the end at min." );
          return;
          }

        continue;
        }

      if( MaximumXPlusY.ParamIsGreater( XPlusY ))
        {
        // TooBigCount++;
        if( !CRTCombin.IncrementCRTDigitsWithBitMask())
          {
          Worker.ReportProgress( 0, "CRTCombin incremented to the end at max." );
          return;
          }

        continue;
        }

      // PastMinMaxCount++;

      // P - BB = B(x + y) + xy
      Temp.Copy( TopB );
      IntMath.Multiply( Temp, XPlusY );
      XTimesY.Copy( Left );
      if( Left.ParamIsGreater( Temp ))
        {
        throw( new Exception( "Right before FindXAndY(). Left.ParamIsGreater( Temp )." ));
        // return;
        }

      IntMath.Subtract( XTimesY, Temp );

      if( FindXAndY( XPlusY, XTimesY ))
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
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Factor1 is: " + IntMath.ToString10( Factor1 ));
        Worker.ReportProgress( 0, "Factor2 is: " + IntMath.ToString10( Factor2 ));
        Worker.ReportProgress( 0, "Seconds: " + StartTime.GetSecondsToNow().ToString( "N1" ));
        return;
        }

      if( !CRTCombin.IncrementCRTDigitsWithBitMask())
        {
        Worker.ReportProgress( 0, "CRTCombin incremented to the end." );
        // Worker.ReportProgress( 0, "PastMinMaxCount: " + PastMinMaxCount.ToString() );
        return;
        }
      }
    }



  /*
  private bool FindXAndYTheHardWay( Integer XPlusY, Integer XTimesY )
    {
    try
    {
    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "Top of FindXAndYTheHardWay()." );

    Integer Y = new Integer();
    Integer X = new Integer();
    // P - BB = B(x + y) + xy
    // TopB );
    ulong AGazillion = 1234567890123456UL;
    // ulong QuickCheckCount = 0;
    for( ulong XCount = 1; XCount < AGazillion; XCount += 2 )
      {
      if( (XCount % 3) == 0 )
        continue;

      if( (XCount % 5) == 0 )
        continue;

      if( (XCount % 7) == 0 )
        continue;

      if( (XCount % 11) == 0 )
        continue;

      if( Worker.CancellationPending )
        return false;


      if( XCount > 0 )
        {
        if( (XCount & 0x3FFFFF) == 0 )
          Worker.ReportProgress( 0, "XCount: " + XCount.ToString() );

        }

      X.SetFromULong( XCount );
      if( XPlusY.ParamIsGreaterOrEq( X ))
        {
        // throw( new Exception( "Tested all the X values." ));
        // Worker.ReportProgress( 0, "Tested all the X values." );
        return false;
        }

      Y.Copy( XPlusY );
      IntMath.SubtractULong( Y, XCount ); // Make it Y.

      // if( ((X0 * Y0) & 0xFFFFFFFF) != XTimesY.GetD( 0 ))
        // continue;

      IntMath.MultiplyULong( Y, XCount );  // Y times X.
      if( Y.IsEqual( XTimesY ))
        {
        Worker.ReportProgress( 0, "Found X and Y." );
        // Worker.ReportProgress( 0, "QuickCheckCount: " + QuickCheckCount.ToString( "N0" ));
        SolutionX.Copy( X );
        SolutionY.Copy( XPlusY );
        IntMath.Subtract( SolutionY, X );
        return true;
        }
      }

    return false;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, Except.Message );
      throw( new Exception( "Exception in FindXAndYTheHardWay()." ));
      }
    }
    */


    // P - BB = B(x + y) + xy
    // L = B(Du + S) + Dv + c
    // L = BDu + BS + Dv + c
    // L = BDu + Dv + c + BS
    // L - c - BS = BDu + Dv
    // (L - c - BS) / D = Bu + v


  private bool FindXAndY( Integer XPlusY, Integer XTimesY )
    {
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of FindXAndY()." );

    CRTCombin.MakeXArrayForSum( XPlusY );

    Integer Y = new Integer();
    Integer X = new Integer();
    // P - BB = B(x + y) + xy


    // TopB );
    ulong AGazillion = 1234567890123456UL;
    ulong QuickCheckCount = 0;
    for( ulong BigCount = 0; BigCount < AGazillion; BigCount++ )
      {
      if( Worker.CancellationPending )
        return false;

      if( BigCount > 0 )
        {
        if( (BigCount & 0xFFFFF) == 0 )
          Worker.ReportProgress( 0, "BigCount: " + BigCount.ToString() );

        }

      ulong BigBase = BigCount * (CRTCombinatorics.BaseTo13 * 17 * 19 * 23);
      int ArrayLength = CRTCombin.GetMediumXArrayForSumLength();
      for( int Count = 0; Count < ArrayLength; Count++ )
        {
        ulong UX = BigBase + CRTCombin.GetValueForMediumXArrayForSum( Count );
        X.SetFromULong( UX );
        if( XPlusY.ParamIsGreaterOrEq( X ))
          {
          // throw( new Exception( "Tested all the X values." ));
          Worker.ReportProgress( 0, "Tested all the X values." );
          return false;
          }

        ulong Y0 = XPlusY.GetD( 0 );
        ulong X0 = UX & 0xFFFFFFFF;
        if( X0 > Y0 )
          Y0 += 0x100000000;

        ulong YDigit = (ulong)(Y0 - X0);
        if( ((X0 * YDigit) & 0xFFFFFFFF) != XTimesY.GetD( 0 ))
          {
          QuickCheckCount++;
          continue;
          }

        Y.Copy( XPlusY );
        IntMath.SubtractULong( Y, UX ); // Make it Y.

        // if( ((X0 * Y0) & 0xFFFFFFFF) != XTimesY.GetD( 0 ))
          // continue;

        IntMath.MultiplyULong( Y, UX );  // Y times X.
        if( Y.IsEqual( XTimesY ))
          {
          Worker.ReportProgress( 0, "Found X and Y." );
          Worker.ReportProgress( 0, "QuickCheckCount: " + QuickCheckCount.ToString( "N0" ));
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
      throw( new Exception( "Exception in FindXAndY()." ));
      }
    }



  }
}
