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
  class FactorBase
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Product;
  private Integer ProductSqrRoot;
  private Integer EulerExponent;
  private Integer EulerResult;
  private Integer EulerModulus;
  private Integer OneMainFactor;
  private BackgroundWorker Worker;
  private QuadResWorkerInfo WInfo;
  private ECTime StartTime;
  private YBaseToPrimesRec[] YBaseToPrimesArray;
  private int YBaseToPrimesArrayLast = 0;
  private const uint IncrementConst = 1;



  internal struct YBaseToPrimesRec
    {
    internal uint Prime;
    internal uint Digit;
    internal uint BitLength;
    internal uint YBaseMod;
    internal uint ProdMod;
    internal uint[] YToX;
    }



  private FactorBase()
    {
    }



  internal FactorBase( QuadResWorkerInfo UseWInfo, BackgroundWorker UseWorker )
    {
    WInfo = UseWInfo;
    Worker = UseWorker;
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    Product = new Integer();
    ProductSqrRoot = new Integer();
    EulerExponent = new Integer();
    EulerResult = new Integer();
    EulerModulus = new Integer();
    OneMainFactor = new Integer();

    StartTime = new ECTime();
    StartTime.SetToNow();

    IntMath.SetFromString( Product, WInfo.PublicKeyModulus );
    IntMath.SquareRoot( Product, ProductSqrRoot );
    YBaseToPrimesArray = new YBaseToPrimesRec[8];
    }



  internal void MakeBaseNumbers()
    {
    MakeYBaseToPrimesArray();
    if( Worker.CancellationPending )
      return;

    Integer YTop = new Integer();
    Integer Y = new Integer();
    Integer XSquared = new Integer();
    Integer OneMainFactor = new Integer();

    YTop.SetToZero();
    uint XSquaredBitLength = 1;

    ExponentVectorNumber ExpNumber = new ExponentVectorNumber( IntMath );

    uint Loops = 0;
    uint BSmoothCount = 0;
    uint BSmoothTestsCount = 0;
    uint IncrementYBy = 0;
    while( true )
      {
      if( Worker.CancellationPending )
        return;

      Loops++;
      if( (Loops & 0x3FF) == 0 )
        {
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Loops: " + Loops.ToString( "N0" ));
        Worker.ReportProgress( 0, "BSmoothCount: " + BSmoothCount.ToString( "N0" ));
        Worker.ReportProgress( 0, "BSmoothTestsCount: " + BSmoothTestsCount.ToString( "N0" ));
        if( BSmoothTestsCount != 0 )
          {
          double TestsRatio = (double)BSmoothCount / (double)BSmoothTestsCount; 
          Worker.ReportProgress( 0, "TestsRatio: " + TestsRatio.ToString( "N3" ));
          }
        }

      /*
      if( (Loops & 0xFFFFF) == 0 )
        {
        // Use Task Manager to tweak the CPU Utilization if you want
        // it be below 100 percent.
        Thread.Sleep( 1 );
        }
        */

      // About 98 percent of the time it is running IncrementBy().
      IncrementYBy += IncrementConst;
      uint BitLength = IncrementBy();
      const uint SomeOptimumBitLength = 10;
      if( BitLength < SomeOptimumBitLength )
        continue;

      double Ratio = (double)BitLength / (double)XSquaredBitLength;

      // If this Ratio was about 1.0 it would mean all of the exponents 
      // are one.  It would exclude numbers with exponents higher
      // than one.  But if a large prime had an exponent more than one
      // then it could bring this ratio down pretty far.
      // The most extreme ratio would be if all of the factors were 2
      // so that BitLength is 1 because that's the only factor
      // that got added in IncrementBy(), where it calculates
      // TotalBitLength.

      const double SomeOptimumRatio = 0.5;
      if( Ratio > SomeOptimumRatio )
        {
        BSmoothTestsCount++;
        YTop.AddULong( IncrementYBy );
        IncrementYBy = 0;
        Y.Copy( ProductSqrRoot );
        Y.Add( YTop );
        XSquared.Copy( Y );
        IntMath.DoSquare( XSquared );
        if( XSquared.ParamIsGreater( Product ))
          throw( new Exception( "Bug. XSquared.ParamIsGreater( Product )." ));

        IntMath.Subtract( XSquared, Product );

        XSquaredBitLength = (uint)(XSquared.GetIndex() * 32);
        uint TopDigit = (uint)XSquared.GetD( XSquared.GetIndex());
        uint TopLength = GetBitLength( TopDigit );
        XSquaredBitLength += TopLength;
        if( XSquaredBitLength == 0 )
          XSquaredBitLength = 1;

        // if( ItIsTheAnswerAlready( XSquared ))  It's too unlikely.
        // QuadResCombinatorics could run in parallel to check for that,
        // and it would be way ahead of this.

        GetOneMainFactor();
        if( OneMainFactor.IsEqual( XSquared ))
          {
          MakeFastExpNumber( ExpNumber );
          }
        else
          {
          // IntMath.Divide( XSquared, OneMainFactor, Quotient, Remainder );
          ExpNumber.SetFromTraditionalInteger( XSquared );
          }

        if( ExpNumber.IsBSmooth())
          {
          BSmoothCount++;
          string DelimS = IntMath.ToString10( Y ) + "\t" +
                         ExpNumber.ToDelimString();

          Worker.ReportProgress( 1, DelimS );

          if( (BSmoothCount & 0x3F) == 0 )
            {
            Worker.ReportProgress( 0, " " );
            Worker.ReportProgress( 0, "BitLength: " + BitLength.ToString());
            Worker.ReportProgress( 0, "XSquaredBitLength: " + XSquaredBitLength.ToString());
            Worker.ReportProgress( 0, "Ratio: " + Ratio.ToString( "N2" ));
            Worker.ReportProgress( 0, ExpNumber.ToString() );

            if( BSmoothCount > (YBaseToPrimesArrayLast + 1))
              {
              Worker.ReportProgress( 0, "Found enough to make the matrix." );
              Worker.ReportProgress( 0, "BSmoothCount: " + BSmoothCount.ToString( "N0" ));
              Worker.ReportProgress( 0, "YBaseToPrimesArrayLast: " + YBaseToPrimesArrayLast.ToString( "N0" ));
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

              return;
              }
            }
          }
        }
      }
    }



  private void AddYBaseToPrimesRec( YBaseToPrimesRec Rec )
    {
    YBaseToPrimesArray[YBaseToPrimesArrayLast] = Rec;
    YBaseToPrimesArrayLast++;
    if( YBaseToPrimesArrayLast >= YBaseToPrimesArray.Length )
      Array.Resize( ref YBaseToPrimesArray, YBaseToPrimesArray.Length + (1024 * 16));

    }



  // 98 percent of the time it's running this one function.
  private uint IncrementBy()
    {
    uint TotalBitLength = 0;
    int Last = YBaseToPrimesArrayLast;
    for( int Count = 0; Count < Last; Count++ )
      {
      // It wouldn't take many logic gates per array record to make
      // this work in parallel in hardware.
      // How would a GPU be used to do this?
      YBaseToPrimesArray[Count].Digit += IncrementConst;
      YBaseToPrimesArray[Count].Digit = YBaseToPrimesArray[Count].Digit % YBaseToPrimesArray[Count].Prime;
      uint XValue = YBaseToPrimesArray[Count].YToX[YBaseToPrimesArray[Count].Digit];
      if( XValue == 0 )
        TotalBitLength += YBaseToPrimesArray[Count].BitLength;

      }

    return TotalBitLength;
    }



  private void GetOneMainFactor()
    {
    OneMainFactor.SetToOne();

    for( int Count = 0; Count < YBaseToPrimesArrayLast; Count++ )
      {
      uint XValue = YBaseToPrimesArray[Count].YToX[YBaseToPrimesArray[Count].Digit];
      if( XValue == 0 )
        IntMath.MultiplyUInt( OneMainFactor, YBaseToPrimesArray[Count].Prime );

      }
    }



  private void MakeFastExpNumber( ExponentVectorNumber ExpNumber )
    {
    ExpNumber.SetToZero();

    for( int Count = 0; Count < YBaseToPrimesArrayLast; Count++ )
      {
      uint XValue = YBaseToPrimesArray[Count].YToX[YBaseToPrimesArray[Count].Digit];
      if( XValue == 0 )
        ExpNumber.AddOneFastVectorElement( YBaseToPrimesArray[Count].Prime );

      }
    }



  private void MakeYBaseToPrimesArray()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeYBaseToPrimesArray()." );

    for( int Count = 0; Count < IntegerMath.PrimeArrayLength; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      if( (Count & 0xFF) == 0 )
        Worker.ReportProgress( 0, "MakeY count: " + Count.ToString());

      // Rec.Digit;

      YBaseToPrimesRec Rec = new YBaseToPrimesRec();

      Rec.Prime = IntMath.GetPrimeAt( Count );
      if( !IsQuadResModProduct( Rec.Prime ))
        continue;

      Rec.BitLength = GetBitLength( Rec.Prime );
      // Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "BitLength: " + Rec.BitLength + "  Prime: " + Rec.Prime.ToString());

      Rec.YBaseMod = (uint)IntMath.GetMod32( ProductSqrRoot, Rec.Prime );
      Rec.ProdMod = (uint)IntMath.GetMod32( Product, Rec.Prime );

      // Rec.GoodY = new bool[Rec.Prime];
      Rec.YToX = new uint[Rec.Prime];

      for( uint Y = 0; Y < Rec.Prime; Y++ )
        {
        ulong YPlusBase = Y + Rec.YBaseMod;
        ulong Test = checked( YPlusBase * YPlusBase );
        Test = Test % Rec.Prime;
        if( Test < Rec.ProdMod )
          Test += Rec.Prime;

        // y^2 - P = x^2
        Test -= Rec.ProdMod;

        // Worker.ReportProgress( 0, "Y: " + Y.ToString() + "  Test: " + Test.ToString());

        // This is making quadratic residues (plus ProdMod and using
        // YBaseMod) so there are usually two of each residue, and
        // two zeros.
        // Rec.GoodY[Y] = true;
        Rec.YToX[Y] = (uint)Test; // Test is x^2.
        }

      AddYBaseToPrimesRec( Rec );
      }

    Worker.ReportProgress( 0, "Finished MakeYBaseToPrimesArray()." );
    Worker.ReportProgress( 0, "YBaseToPrimesArrayLast: " + YBaseToPrimesArrayLast.ToString() );
    }



  private uint GetBitLength( uint Test )
    {
    // This can be done with a binary search.
    // if( 0xFFFF0000 == 0 )
    //   Then all the bits are in the right half.  And so on...

    for( uint Count = 0; Count < 32; Count++ )
      {
      if( Test == 0 )
        return Count;

      Test >>= 1;
      }

    return 32;
    }



  private bool IsQuadResModProduct( uint Prime )
    {
    // Euler's Criterion:

    uint Exp = Prime;
    Exp--; // Subtract 1.
    Exp >>= 1; // Divide by 2.
    EulerExponent.SetFromULong( Exp );
    EulerResult.Copy( Product );
    EulerModulus.SetFromULong( Prime );

    IntMath.ModularPower( EulerResult, EulerExponent, EulerModulus );
    if( EulerResult.IsOne())
      return true;
    else
      return false; // Result should be Prime - 1.

    }



  }
}

