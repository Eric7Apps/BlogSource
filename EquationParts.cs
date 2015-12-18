// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// This is just another variation on combinatorics.


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker
using System.Threading; // For Sleep().


namespace ExampleServer
{
  class EquationParts
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Product;
  private Integer SolutionP;
  private Integer SolutionQ;
  private BackgroundWorker Worker;
  private QuadResWorkerInfo WInfo;
  private uint[] SmallPairsP;
  private uint[] SmallPairsQ;
  private const uint SmallBase = 2 * 3 * 5 * 7 * 11 * 13;
  private const uint EulerPhi  =     2 * 4 * 6 * 10 * 12;
  private const uint Base =          SmallBase * 17 * 19;
  private const uint BiggerEulerPhi = EulerPhi * 16 * 18;
  private Integer ProductSqrRoot;
  private Integer MaxX;
  private FindFactors FindFactors1;
  private XYRec[] XYRecArray;
  private const int XYRecArrayLength = 100;



  // ((P - ac) / B) = y(xB + a) + xc
  internal struct XYRec
    {
    internal uint L;
    internal uint[] XToY;
    // internal bool[] XPlusYBool;
    }



  private EquationParts()
    {
    }



  internal EquationParts( QuadResWorkerInfo UseWInfo, BackgroundWorker UseWorker )
    {
    WInfo = UseWInfo;
    Worker = UseWorker;
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    Product = new Integer();
    SolutionP = new Integer();
    SolutionQ = new Integer();
    ProductSqrRoot = new Integer();
    MaxX = new Integer();
    FindFactors1 = new FindFactors(  Worker, IntMath );


    IntMath.SetFromString( Product, WInfo.PublicKeyModulus );
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



  internal bool FindFactors()
    {
    if( Worker.CancellationPending )
      return false;

    if( IntMath.SquareRoot( Product, ProductSqrRoot ))
      {
      SolutionP.Copy( ProductSqrRoot );
      SolutionQ.Copy( ProductSqrRoot );
      Worker.ReportProgress( 0, "SolutionP: " + IntMath.ToString10( SolutionP ));
      Worker.ReportProgress( 0, "SolutionQ: " + IntMath.ToString10( SolutionQ ));
      return true;
      }

    SetupSmallPairsArray();
    DoBaseLoop();
    if( !SolutionP.IsZero())
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "SolutionP: " + IntMath.ToString10( SolutionP ));
      Worker.ReportProgress( 0, "SolutionQ: " + IntMath.ToString10( SolutionQ ));
      return true;
      }

    return false;
    }




  private void DoBaseLoop()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of DoBaseLoop." );

    Integer Left = new Integer();
    Integer B = new Integer();
    Integer Temp = new Integer();
    B.SetFromULong( Base );
    uint ProdMod = (uint)IntMath.GetMod32( Product, Base );
    int Loops = 0;
    for( ulong CountP = 0; CountP < (17 * 19); CountP++ )
      {
      if( Worker.CancellationPending )
        return;

      ulong BasePartP = SmallBase * CountP;
      for( int IndexP = 0; IndexP < EulerPhi; IndexP++ )
        {
        if( Worker.CancellationPending )
          return;

        ulong A = BasePartP + SmallPairsP[IndexP];

        if( (A % 17) == 0 )
          continue;

        if( (A % 19) == 0 )
          continue;

        for( ulong CountQ = 0; CountQ < (17 * 19); CountQ++ )
          {
          // Worker.ReportProgress( 0, "CountQ: " + CountQ.ToString());

          if( Worker.CancellationPending )
            return;

          ulong BasePartQ = SmallBase * CountQ;
          for( int IndexQ = 0; IndexQ < EulerPhi; IndexQ++ )
            {
            if( Worker.CancellationPending )
              return;

            ulong C = BasePartQ + SmallPairsQ[IndexQ];

            if( (C % 17) == 0 )
              continue;

            if( (C % 19) == 0 )
              continue;

            ulong Test = A * C;
            Test = Test % Base;
            if( Test != ProdMod )
              continue;

            // The number of loops that get here is BiggerEulerPhi.
            Loops++;
            // if( (Loops & 0x7F) == 0 )
              Worker.ReportProgress( 0, "Loops: " + Loops.ToString( "N0" ) + " out of " + BiggerEulerPhi.ToString( "N0" ));

            if( Loops > 100 )
              return;

            Temp.SetFromULong( A * C );
            // This could happen with small test numbers.
            if( Product.ParamIsGreater( Temp ))
              continue;

            // This could happen with small test numbers.
            if( A != 1 )
              {
              Temp.SetFromULong( A );
              IntMath.Divide( Product, Temp, Quotient, Remainder );
              if( Remainder.IsZero())
                {
                SolutionP.SetFromULong( A );
                SolutionQ.Copy( Quotient );
                return;
                }
              }

            // This could happen with small test numbers.
            if( C != 1 )
              {
              Temp.SetFromULong( C );
              IntMath.Divide( Product, Temp, Quotient, Remainder );
              if( Remainder.IsZero())
                {
                SolutionP.SetFromULong( C );
                SolutionQ.Copy( Quotient );
                return;
                }
              }

            FindFactorsFromLeft( A, C, Left, Temp, B );
            if( !SolutionP.IsZero())
              return;

            }
          }
        }
      }
    }




  private void FindFactorsFromLeft( ulong A,
                                    ulong C,
                                    Integer Left,
                                    Integer Temp,
                                    Integer B )
    {
    if( Worker.CancellationPending )
      return;

/*
// (323 - 2*4 / 5) = xy5 + 2y + 4x
// (315 / 5) = xy5 + 2y + 4x
// 63 = xy5 + 2y + 4x
// 21 * 3 = xy5 + 2y + 4x
// 3*7*3 = xy5 + 2y + 4x
// 3*7*3 = 3y5 + 2y + 4*3
// 3*7*3 = 15y + 2y + 12
// 3*7*3 - 12 = y(15 + 2)
// 3*7*3 - 3*4 = y(15 + 2)
// 51 = 3 * 17



// (323 - 1*3 / 5) = xy5 + 1y + 3x
// (320 / 5) = xy5 + 1y + 3x
// 64 = xy5 + 1y + 3x
// 64 - 3x = xy5 + 1y
// 64 - 3x = y(x5 + 1)
// 64 - 3x = y(x5 + 1)
1 = y(x5 + 1) mod 3
*/

    Left.Copy( Product );
    Temp.SetFromULong( A * C );
    IntMath.Subtract( Left, Temp );
    IntMath.Divide( Left, B, Quotient, Remainder );
    if( !Remainder.IsZero())
      throw( new Exception( "Remainder is not zero for Left." ));

    Left.Copy( Quotient );
    // Worker.ReportProgress( 0, "Left: " + IntMath.ToString10( Left ));
    // Worker.ReportProgress( 0, "A: " + A.ToString() + "  C: " + C.ToString());

    FindFactors1.FindSmallPrimeFactorsOnly( Left );
    FindFactors1.ShowAllFactors();

    MaxX.Copy( ProductSqrRoot );
    Temp.SetFromULong( A );
    if( MaxX.ParamIsGreater( Temp ))
      return; // MaxX would be less than zero.

    IntMath.Subtract( MaxX, Temp );
    IntMath.Divide( MaxX, B, Quotient, Remainder );
    MaxX.Copy( Quotient );
    // Worker.ReportProgress( 0, "MaxX: " + IntMath.ToString10( MaxX ));

    Temp.Copy( MaxX );
    IntMath.MultiplyULong( Temp, C );
    if( Left.ParamIsGreater( Temp ))
      {
      throw( new Exception( "Does this happen?  MaxX can't be that big." ));
      /*
      Worker.ReportProgress( 0, "MaxX can't be that big." );
      MaxX.Copy( Left );
      Temp.SetFromULong( C );
      IntMath.Divide( MaxX, Temp, Quotient, Remainder );
      MaxX.Copy( Quotient );
      Worker.ReportProgress( 0, "MaxX was set to: " + IntMath.ToString10( MaxX ));
      */
      }

    // P = (xB + a)(yB + c)
    // P = (xB + a)(yB + c)
    // P - ac = xyBB + ayB + xBc
    // ((P - ac) / B) = xyB + ay + xc
    // ((P - ac) / B) = y(xB + a) + xc

    // This is congruent to zero mod one really big prime.
    // ((P - ac) / B) - xc = y(xB + a)

    // BottomPart is when x is at max in:
    // ((P - ac) / B) - xc
    Integer BottomPart = new Integer();
    BottomPart.Copy( Left );
    Temp.Copy( MaxX );
    IntMath.MultiplyULong( Temp, C );
    IntMath.Subtract( BottomPart, Temp );
    if( BottomPart.IsNegative )
      throw( new Exception( "Bug.  BottomPart is negative." ));

    // Worker.ReportProgress( 0, "BottomPart: " + IntMath.ToString10( BottomPart ));

    Integer Gcd = new Integer();
    Temp.SetFromULong( C );
    IntMath.GreatestCommonDivisor( BottomPart, Temp, Gcd );
    if( !Gcd.IsOne())
      throw( new Exception( "This can't happen with the GCD." ));

    // FindFactors1.FindSmallPrimeFactorsOnly( BottomPart );
    // Temp.SetFromULong( C );
    // FindFactors1.FindSmallPrimeFactorsOnly( Temp );
    // FindFactors1.ShowAllFactors();

    MakeXYRecArray( Left, B, A, C );

    FindXTheHardWay( B, Temp, A );
    }




  private void FindXTheHardWay( Integer B, Integer Temp, ulong A )
    {
    Integer CountX = new Integer();
    CountX.SetToOne();
    while( true )
      {
      if( Worker.CancellationPending )
        return;

      Temp.Copy( CountX );
      IntMath.Multiply( Temp, B );
      Temp.AddULong( A );
      IntMath.Divide( Product, Temp, Quotient, Remainder );
      if( Remainder.IsZero())
        {
        if( !Quotient.IsOne())
          {
          SolutionP.Copy( Temp );
          SolutionQ.Copy( Quotient );
          return;
          }
        }

      CountX.Increment();
      if( MaxX.ParamIsGreater( CountX ))
        {
        // Worker.ReportProgress( 0, "Tried everything up to MaxX." );
        return;
        }
      }
    }



  private void SetupSmallPairsArray()
    {
    Worker.ReportProgress( 0, "Top of SetupSmallPairsArray()." );

    uint ProdMod = (uint)IntMath.GetMod32( Product, SmallBase );
    SmallPairsP = new uint[EulerPhi];
    SmallPairsQ = new uint[EulerPhi];

    int Index = 0;
    for( uint CountP = 1; CountP < SmallBase; CountP += 2 )
      {
      if( Worker.CancellationPending )
        return;

      if( (CountP % 3) == 0 ) // If its divisible by 3.
        continue;

      if( (CountP % 5) == 0 )
        continue;

      if( (CountP % 7) == 0 )
        continue;

      if( (CountP % 11) == 0 )
        continue;

      if( (CountP % 13) == 0 )
        continue;

      SmallPairsP[Index] = CountP;
      for( uint CountQ = 1; CountQ < SmallBase; CountQ += 2 )
        {
        if( (CountQ % 3) == 0 ) // If its divisible by 3.
          continue;

        if( (CountQ % 5) == 0 )
          continue;

        if( (CountQ % 7) == 0 )
          continue;

        if( (CountQ % 11) == 0 )
          continue;

        if( (CountQ % 13) == 0 )
          continue;

        ulong Test = (ulong)CountP * (ulong)CountQ;
        Test = Test % SmallBase;
        if( Test == ProdMod )
          {
          // Worker.ReportProgress( 0, "Matching AC at: " + CountP.ToString() );

          SmallPairsQ[Index] = CountQ;
          break;
          }
        }

      Index++;
      }
    }




  private void MakeXYRecArray( Integer Left, Integer B, ulong A, ulong C )
    {
    XYRecArray = new XYRec[XYRecArrayLength];

    for( int Count = 0; Count < XYRecArrayLength; Count++ )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      // Worker.ReportProgress( 0, "Prime: " + Prime.ToString());
      XYRec Rec = new XYRec();
      Rec.XToY = new uint[Prime];
      Rec.L = (uint)IntMath.GetMod32( Left, Prime );
      // B would be zero for the small base primes.
      uint BMod = (uint)IntMath.GetMod32( B, Prime );
      // ((P - ac) / B) = y(xB + a) + xc
      // L = y(xB + a) + xc
      int HowManyMatched = 0;
      for( uint X = 0; X < Prime; X++ )
        {
        Rec.XToY[X] = 0xFFFFFFFF;
        int HowManyY = 0;
        for( uint Y = 0; Y < Prime; Y++ )
          {
          ulong Test = checked((X * BMod) + A);
          Test = Test % Prime;

          // xB + a can't be congruent to zero mod any small prime.
          // This doesn't change anything.
          if( Test == 0 )
            continue;

          Test = Test * Y;
          Test = Test % Prime;
          Test += X * C;
          Test = Test % Prime;
          if( Test == Rec.L )
            {
            Rec.XToY[X] = Y;
            HowManyY++;
            HowManyMatched++;
            }
          }

        if( HowManyY > 1 )
          {
          throw( new Exception( "HowManyY > 1." ));
          }
        }

      if( BMod == 0 )
        {
        if( HowManyMatched != Prime )
          {
          string ShowS = "BMod is zero. HowManyMatched != Prime.  Matched: " + HowManyMatched.ToString();
          throw( new Exception( ShowS ));
          }
        }
      else
        {
        if( HowManyMatched != (Prime - 1))
          {
          string ShowS = "HowManyMatched != (Prime - 1).  Matched: " + HowManyMatched.ToString();
          throw( new Exception( ShowS ));
          }
        }

      XYRecArray[Count] = Rec;
      }
    }



  }
}

