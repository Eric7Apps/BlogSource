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
  private ExponentVectorNumber ExpOneMainFactor;
  private BackgroundWorker Worker;
  private QuadResWorkerInfo WInfo;
  private ECTime StartTime;
  private YBaseToPrimesRec[] YBaseToPrimesArray;
  private int YBaseToPrimesArrayLast = 0;
  private const uint IncrementConst = 1;
  private const int BSmoothLimit = 1024 * 8; // is less than IntegerMath.PrimeArrayLength;


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
    ExpOneMainFactor = new ExponentVectorNumber( IntMath );
    StartTime = new ECTime();
    StartTime.SetToNow();
    IntMath.SetFromString( Product, WInfo.PublicKeyModulus );
    IntMath.SquareRoot( Product, ProductSqrRoot );
    }



  internal void MakeBaseNumbers()
    {
    try
    {
    MakeYBaseToPrimesArray();
    if( Worker.CancellationPending )
      return;

    Integer YTop = new Integer();
    Integer Y = new Integer();
    Integer XSquared = new Integer();
    Integer Test = new Integer();
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
      if( (Loops & 0xF) == 0 )
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

      const uint SomeOptimumBitLength = 2;
      if( BitLength < SomeOptimumBitLength )
        continue;

      // This BitLength has to do with how many small factors you want
      // in the number.  But it doesn't limit your factor base at all.
      // You can still have any size prime in your factor base (up to
      // IntegerMath.PrimeArrayLength).  Compare the size of
      // YBaseToPrimesArrayLast to IntegerMath.PrimeArrayLength.
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
        if( OneMainFactor.IsZero())
          throw( new Exception( "OneMainFactor.IsZero()." ));

        IntMath.Divide( XSquared, OneMainFactor, Quotient, Remainder );
        ExpNumber.SetFromTraditionalInteger( Quotient );
        ExpNumber.Multiply( ExpOneMainFactor );
        ExpNumber.GetTraditionalInteger( Test );
        if( !Test.IsEqual( XSquared ))
          throw( new Exception( "!Test.IsEqual( XSquared )." ));

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
          Worker.ReportProgress( 0, ExpNumber.ToString() );

          // What should BSmoothLimit be?
          // (Since FactorDictionary.cs will reduce the final factor base.)
          if( BSmoothCount > BSmoothLimit )
            {
            Worker.ReportProgress( 0, "Found enough to make the matrix." );
            Worker.ReportProgress( 0, "BSmoothCount: " + BSmoothCount.ToString( "N0" ));
            Worker.ReportProgress( 0, "BSmoothLimit: " + BSmoothLimit.ToString( "N0" ));
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
    catch( Exception Except )
      {
      throw( new Exception( "Exception in MakeBaseNumbers():\r\n" + Except.Message ));
      }
    }



  // 98 percent of the time it's running this one function.
  private uint IncrementBy()
    {
    uint TotalBitLength = 0;
    for( int Count = 0; Count < YBaseToPrimesArrayLast; Count++ )
      {
      // It wouldn't take many logic gates per array record to make
      // this work in parallel in hardware.
      // How would a GPU be used to do this?
      // This is only a practical algorithm if it can be done in parallel
      // of if the criteria is that the number includes several small 
      // factors, where "small" is defined as how big you want to
      // make YBaseToPrimesArrayLast.
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
    ExpOneMainFactor.SetToZero();

    for( int Count = 0; Count < YBaseToPrimesArrayLast; Count++ )
      {
      uint XValue = YBaseToPrimesArray[Count].YToX[YBaseToPrimesArray[Count].Digit];
      if( XValue == 0 )
        {
        ExpOneMainFactor.AddOneFastVectorElement( YBaseToPrimesArray[Count].Prime );
        IntMath.MultiplyUInt( OneMainFactor, YBaseToPrimesArray[Count].Prime );
        }
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
    try
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of MakeYBaseToPrimesArray()." );

    int ArrayLength = 1024 * 8;
    YBaseToPrimesArray = new YBaseToPrimesRec[ArrayLength];

    for( int Count = 0; Count < ArrayLength; Count++ )
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
        // two zeros.  (Two solutions to a quadratic equation.)
        // Rec.GoodY[Y] = true;
        Rec.YToX[Y] = (uint)Test; // Test is x^2.
        }

      YBaseToPrimesArray[YBaseToPrimesArrayLast] = Rec;
      YBaseToPrimesArrayLast++;
      }

    Array.Resize( ref YBaseToPrimesArray, YBaseToPrimesArrayLast );

    Worker.ReportProgress( 0, "Finished MakeYBaseToPrimesArray()." );
    Worker.ReportProgress( 0, "YBaseToPrimesArrayLast: " + YBaseToPrimesArrayLast.ToString() );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in MakeYBaseToPrimesArray():\r\n" + Except.Message ));
      }
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

    IntMath.IntMathNew.ModularPower( EulerResult, EulerExponent, EulerModulus, false );
    if( EulerResult.IsOne())
      return true;
    else
      return false; // Result should be Prime - 1.

    }



  }
}

