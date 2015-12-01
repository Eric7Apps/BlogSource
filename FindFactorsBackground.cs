// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker
using System.Security.Cryptography;



namespace ExampleServer
{
  class FindFactorsBackground
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private MakeKeysWorkerInfo WInfo;
  private RNGCryptoServiceProvider RngCsp;
  private ECTime StartTime;
  private FindFactors FindFactors1;
  // private FindFactorsFromTop FindFromTop;
  private CombinatoricsFromTop CombinatoricsTop;
  private CRTCombinSetupRec[] SetupArray;

  // private const int PrimeIndex = 0; // Approximmately 32-bit primes.
  private const int PrimeIndex = 1; // Approximmately 64-bit primes.
  // private const int PrimeIndex = 2; // Approximmately 96-bit primes.
  // private const int PrimeIndex = 3; // Approximmately 128-bit primes.
  // private const int PrimeIndex = 7; // Approximmately 256-bit primes.
  // private const int PrimeIndex = 15; // Approximmately 512-bit primes.
  // private const int PrimeIndex = 31; // Approximmately 1024-bit primes.
  // private const int PrimeIndex = 63; // Approximmately 2048-bit primes.
  // private const int PrimeIndex = 127; // Approximmately 4096-bit primes.



  private FindFactorsBackground()
    {
    }



  internal FindFactorsBackground( BackgroundWorker UseWorker, MakeKeysWorkerInfo UseWInfo )
    {
    Worker = UseWorker;
    WInfo = UseWInfo;
    SetupArray = WInfo.SetupArray;

    StartTime = new ECTime();
    StartTime.SetToNow();

    RngCsp = new RNGCryptoServiceProvider();
    IntMath = new IntegerMath();
    Worker.ReportProgress( 0, IntMath.GetStatusString() );
    Quotient = new Integer();
    Remainder = new Integer();
    FindFactors1 = new FindFactors( Worker, IntMath );
    // FindFromTop = new FindFactorsFromTop( Worker, IntMath );
    CombinatoricsTop = new CombinatoricsFromTop( SetupArray, Worker, IntMath );
    }



  internal void FreeEverything()
    {
    if( RngCsp != null )
      {
      RngCsp.Dispose();
      RngCsp = null;
      }
    }



  private bool MakeAPrime( Integer Result, int SetToIndex, int HowMany )
    {
    try
    {
    Worker.ReportProgress( 0, "Making a prime..." );

    int Attempts = 0;
    while( true )
      {
      if( Worker.CancellationPending )
        return false;

      // Don't hog the server's resources too much.
      Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.

      int HowManyBytes = (SetToIndex * 4) + 4;
      byte[] RandBytes = MakeRandomBytes( HowManyBytes );
      if( RandBytes == null )
        {
        Worker.ReportProgress( 0, "Error making random bytes in MakeKeysBackGround.MakeAPrime()." );
        return false;
        }

      if( !Result.MakeRandomOdd( SetToIndex, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeKeysBackGround.MakeAPrime()." );
        return false;
        }



      // When testing with small numbers, make them a little smaller.
      // If the index is 1, then a mask of 0xF makes it 32 + 4 bits.
      // 0xFFF is 32 + 12 bits.
      // 0xFFFF is 32 + 16 bits.  48.  So 96 bit modulus.
      ulong MaskTop = Result.GetD( SetToIndex ) & 0x3F;
      if( MaskTop != 0 )
        Result.SetD( SetToIndex, MaskTop );
      else
        continue;


      /*
      Worker.ReportProgress( 0, " " );
      FindFactors1.FindAllFactors( Result );
      FindFactors1.ShowAllFactors();
      Worker.ReportProgress( 0, " " );
      */

      // Make sure that it's about the size I think it is.
      if( Result.GetIndex() < SetToIndex )
        throw( new Exception( "Does this ever happen with the size of the prime?" ));
        // continue;

      // uint TestPrime = IntMath.NumberIsDivisibleByUInt( Result, Worker );
      uint TestPrime = IntMath.IsDivisibleBySmallPrime( Result );
      if( 0 != TestPrime)
        {
        // Worker.ReportProgress( 0, "Next test: " + TestPrime.ToString() );
        // Test:
        // if( IntMath.IsFermatPrime( Result, HowMany ))
          // throw( new Exception( "Passed IsFermatPrime even though it has a small prime." ));

        Attempts++;
        continue;
        }

      Worker.ReportProgress( 0, "Fermat test." );
      if( !IntMath.IsFermatPrime( Result, HowMany ))
        {
        Attempts++;
        Worker.ReportProgress( 0, "Did not pass Fermat test." );
        continue;
        }

      // IsFermatPrime() could take a long time.
      if( Worker.CancellationPending )
        return false;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Found a probable prime." );
      Worker.ReportProgress( 0, "Attempts: " + Attempts.ToString() );
      Worker.ReportProgress( 0, " " );
      // Presumably this will eventually find one and not loop forever.
      return true; // With Result.
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in FindFactorsBackGround.MakeAPrime()." );
      Worker.ReportProgress( 0, Except.Message );
      return false;
      }
    }



  protected byte[] MakeRandomBytes( int HowMany )
    {
    // See:
    // https://en.wikipedia.org/wiki/Random_number_generator_attack

    try
    {
    byte[] Result = new byte[HowMany];
    RngCsp.GetNonZeroBytes( Result );
    return Result;
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in FindFactorsBackGround.MakeRandomBytes()." );
      Worker.ReportProgress( 0, Except.Message );
      return null;
      }
    }




  internal void FindFactors()
    {
    Integer PrimeP = new Integer();
    Integer PrimeQ = new Integer();
    Integer PubKeyN = new Integer();
    int ShowBits = (PrimeIndex + 1) * 32;
    // int TestLoops = 0;


    Worker.ReportProgress( 0, "Prime bits size is: " + ShowBits.ToString());

    // Worker.ReportProgress( 0, IntMath.GetStatusString());

    // ulong Loops = 0;
    while( true )
    // for( int Count = 0; Count < 1000; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      Thread.Sleep( 1 ); // Give up the time slice.  Let other things on the server run.

      if( WInfo.PublicKeyModulus == null )
        {
        // Make two prime factors.
        // Normally you'd only make new primes when you pay the Certificate
        // Authority for a new certificate.  So it happens once a year or once
        // every three years.
        if( !MakeAPrime( PrimeP, PrimeIndex, 20 ))
          return;

        if( Worker.CancellationPending )
          return;

        if( !MakeAPrime( PrimeQ, PrimeIndex, 20 ))
          return;

        if( Worker.CancellationPending )
          return;

        Worker.ReportProgress( 0, "The Index of Prime P is: " + PrimeP.GetIndex().ToString() );
        Worker.ReportProgress( 0, "Prime P:" );
        Worker.ReportProgress( 0, IntMath.ToString10( PrimeP ));
        Worker.ReportProgress( 0, " " );
        Worker.ReportProgress( 0, "Prime Q:" );
        Worker.ReportProgress( 0, IntMath.ToString10( PrimeQ ));
        Worker.ReportProgress( 0, " " );

        PubKeyN.Copy( PrimeP );
        IntMath.Multiply( PubKeyN, PrimeQ );
        }
      else
        {
        IntMath.SetFromString( PubKeyN, WInfo.PublicKeyModulus );
        }

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PubKeyN:" + IntMath.ToString10( PubKeyN ));
      Integer P = new Integer();
      Integer Q = new Integer();
      
      // FindFromTop.FindTwoFactors( PubKeyN, P, Q );
      CombinatoricsTop.FindTwoFactors( PubKeyN, P, Q );

      // FindFactors1.FindTwoFactorsWithFermat( PubKeyN, P, Q, 0 );

      // FindFactors1.FindAllFactors( PubKeyN );
      // FindFactors1.ShowAllFactors();
      Worker.ReportProgress( 0, " " );

      Thread.Sleep( 2000 );

      return; // Comment this out to just leave it while( true ) for testing.
      }
    }



  }
}




