// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


// See RSA Cryptosystem:
// https://en.wikipedia.org/wiki/RSA_%28cryptosystem%29

// Euler's Theorem, the basis of the RSA Cryptosystem:
// https://en.wikipedia.org/wiki/Euler's_theorem

// Public-Key Cryptography Standards (PKCS) #1: RSA Cryptography
// Specifications Version 2.1
// http://tools.ietf.org/html/rfc2437
// http://tools.ietf.org/html/rfc3447


using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker
using System.Security.Cryptography;




namespace ExampleServer
{

  class RSACryptoSystem
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private ECTime StartTime;
  private BackgroundWorker Worker;
  private RSACryptoWorkerInfo WorkerInfo;
  private RNGCryptoServiceProvider RngCsp;
  private Integer PrimeP;
  private Integer PrimeQ;
  private Integer PrimePMinus1;
  private Integer PrimeQMinus1;
  private Integer PubKeyN;
  private Integer PubKeyExponent;
  private Integer PrivKInverseExponent;
  private Integer PrivKInverseExponentDP;
  private Integer PrivKInverseExponentDQ;
  private Integer QInv;
  private Integer PhiN;
  private Integer TestForDecrypt;
  private Integer M1ForInverse;
  private Integer M2ForInverse;
  private Integer HForQInv;
  private Integer M1MinusM2;
  private Integer M1M2SizeDiff;


  // 65537 is a prime.
  // It is a commonly used exponent for RSA.  It is 2^16 + 1.
  private const uint PubKeyExponentUint = 65537;


  // private const int PrimeIndex = 1; // Approximmately 64-bit primes.
  // private const int PrimeIndex = 2; // Approximmately 96-bit primes.
  // private const int PrimeIndex = 3; // Approximmately 128-bit primes.
  // private const int PrimeIndex = 7; // Approximmately 256-bit primes.
  // private const int PrimeIndex = 15; // Approximmately 512-bit primes.
  private const int PrimeIndex = 31; // Approximmately 1024-bit primes.
  // private const int PrimeIndex = 63; // Approximmately 2048-bit primes.
  // private const int PrimeIndex = 127; // Approximmately 4096-bit primes.


  private RSACryptoSystem()
    {
    }



  internal RSACryptoSystem( BackgroundWorker UseWorker, RSACryptoWorkerInfo UseWInfo )
    {
    Worker = UseWorker;
    WorkerInfo = UseWInfo;
    StartTime = new ECTime();
    StartTime.SetToNow();

    RngCsp = new RNGCryptoServiceProvider();
    IntMath = new IntegerMath();
    Worker.ReportProgress( 0, IntMath.GetStatusString() );
    Quotient = new Integer();
    Remainder = new Integer();
    PrimeP = new Integer();
    PrimeQ = new Integer();
    PrimePMinus1 = new Integer();
    PrimeQMinus1 = new Integer();
    PubKeyN = new Integer();
    PubKeyExponent = new Integer();
    PrivKInverseExponent = new Integer();
    PrivKInverseExponentDP = new Integer();
    PrivKInverseExponentDQ = new Integer();
    QInv = new Integer();
    PhiN = new Integer();
    TestForDecrypt = new Integer();
    M1ForInverse = new Integer();
    M2ForInverse = new Integer();
    HForQInv = new Integer();
    M1MinusM2 = new Integer();
    M1M2SizeDiff = new Integer();

    PubKeyExponent.SetFromULong( PubKeyExponentUint );
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
    int Attempts = 0;
    while( true )
      {
      Attempts++;

      if( Worker.CancellationPending )
        return false;

      // Don't hog the server's resources too much.
      Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.

      int HowManyBytes = (SetToIndex * 4) + 4;
      byte[] RandBytes = MakeRandomBytes( HowManyBytes );
      if( RandBytes == null )
        {
        Worker.ReportProgress( 0, "Error making random bytes in MakeAPrime()." );
        return false;
        }

      if( !Result.MakeRandomOdd( SetToIndex, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeAPrime()." );
        return false;
        }

      // Make sure that it's the size I think it is.
      if( Result.GetIndex() < SetToIndex )
        throw( new Exception( "Bug. The size of the random prime is not right." ));

      uint TestPrime = IntMath.IsDivisibleBySmallPrime( Result );
      if( 0 != TestPrime)
        continue;

      if( !IntMath.IsFermatPrime( Result, HowMany ))
        {
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
      return true; // With Result.
      }
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in MakeAPrime()" );
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
      Worker.ReportProgress( 0, "Error in MakeRandomBytes()." );
      Worker.ReportProgress( 0, Except.Message );
      return null;
      }
    }



  internal void MakeRSAKeys()
    {
    int ShowBits = (PrimeIndex + 1) * 32;
    // int TestLoops = 0;

    Worker.ReportProgress( 0, "Making RSA keys." );
    Worker.ReportProgress( 0, "Bits size is: " + ShowBits.ToString());

    // ulong Loops = 0;
    while( true )
      {
      if( Worker.CancellationPending )
        return;

      Thread.Sleep( 1 ); // Give up the time slice.  Let other things on the server run.

      // Make two prime factors.
      // Normally you'd only make new primes when you pay the Certificate
      // Authority for a new certificate.
      if( !MakeAPrime( PrimeP, PrimeIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      if( !MakeAPrime( PrimeQ, PrimeIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      // This is extremely unlikely.
      Integer Gcd = new Integer();
      IntMath.GreatestCommonDivisor( PrimeP, PrimeQ, Gcd );
      if( !Gcd.IsOne())
        {
        Worker.ReportProgress( 0, "They had a GCD: " + IntMath.ToString10( Gcd ));
        continue;
        }

      if( Worker.CancellationPending )
        return;

      // This would never happen since the public key exponent used here
      // is one of the small primes in the array in IntegerMath that it
      // was checked against.  But it does show here in the code that
      // they have to be co-prime to each other.  And in the future it
      // might be found that the public key exponent has to be much larger
      // than the one used here.
      IntMath.GreatestCommonDivisor( PrimeP, PubKeyExponent, Gcd );
      if( !Gcd.IsOne())
        {
        Worker.ReportProgress( 0, "They had a GCD with PubKeyExponent: " + IntMath.ToString10( Gcd ));
        continue;
        }

      if( Worker.CancellationPending )
        return;

      IntMath.GreatestCommonDivisor( PrimeQ, PubKeyExponent, Gcd );
      if( !Gcd.IsOne())
        {
        Worker.ReportProgress( 0, "2) They had a GCD with PubKeyExponent: " + IntMath.ToString10( Gcd ));
        continue;
        }

      // For Modular Reduction.
      IntMath.IntMathNew.SetupBaseArrays( PrimeP, PrimeQ, Worker );

      PrimePMinus1.Copy( PrimeP );
      IntMath.SubtractULong( PrimePMinus1, 1 );
      PrimeQMinus1.Copy( PrimeQ );
      IntMath.SubtractULong( PrimeQMinus1, 1 );

      // These checks should be more thorough.

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

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PubKeyN:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PubKeyN ));
      Worker.ReportProgress( 0, " " );

      // Euler's Theorem:
      // https://en.wikipedia.org/wiki/Euler's_theorem
      // if x ≡ y (mod φ(n)),
      // then a^x ≡ a^y (mod n).

      // Euler's Phi function (aka Euler's Totient function) is calculated
      // next.

      // PhiN is made from the two factors: (P - 1)(Q - 1)
      // PhiN is: (P - 1)(Q - 1) = PQ - P - Q + 1
      // If I add (P - 1) to PhiN I get:
      // PQ - P - Q + 1 + (P - 1) = PQ - Q.
      // If I add (Q - 1) to that I get:
      // PQ - Q + (Q - 1) = PQ - 1.
      // (P - 1)(Q - 1) + (P - 1) + (Q - 1) = PQ - 1

      // If (P - 1) and (Q - 1) had a larger GCD then PQ - 1 would have 
      // that same factor too.

      IntMath.GreatestCommonDivisor( PrimePMinus1, PrimeQMinus1, Gcd );
      Worker.ReportProgress( 0, "GCD of PrimePMinus1, PrimeQMinus1 is: " + IntMath.ToString10( Gcd ));

      if( !Gcd.IsULong())
        {
        Worker.ReportProgress( 0, "This GCD number is too big: " + IntMath.ToString10( Gcd ));
        continue;
        }
      else
        {
        ulong TooBig = Gcd.GetAsULong();
        // How big of a GCD is too big?
        if( TooBig > 1234567 )
          {
          // (P - 1)(Q - 1) + (P - 1) + (Q - 1) = PQ - 1
          Worker.ReportProgress( 0, "This GCD number is bigger than 1234567: " + IntMath.ToString10( Gcd ));
          continue;
          }
        }

      Integer Temp1 = new Integer();

      PhiN.Copy( PrimePMinus1 );
      Temp1.Copy( PrimeQMinus1 );
      IntMath.Multiply( PhiN, Temp1 );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PhiN:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PhiN ));
      Worker.ReportProgress( 0, " " );
      if( Worker.CancellationPending )
        return;

      // In RFC 2437 there are commonly used letters/symbols to represent 
      // the numbers used.  So the number e is the public exponent.
      // The number e that is used here is called PubKeyExponentUint = 65537.
      // In the RFC the private key d is the multiplicative inverse of 
      // e mod PhiN.  Which is mod (P - 1)(Q - 1).  It's called
      // PrivKInverseExponent here.

      if( !IntMath.IntMathNew.FindMultiplicativeInverseSmall( PrivKInverseExponent, PubKeyExponent, PhiN, Worker ))
        return;

      if( PrivKInverseExponent.IsZero())
        continue;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponent: " + IntMath.ToString10( PrivKInverseExponent ));

      if( Worker.CancellationPending )
        return;

      // In RFC 2437 it defines a number dP which is the multiplicative
      // inverse, mod (P - 1) of e.  That dP is named PrivKInverseExponentDP here.
      Worker.ReportProgress( 0, " " );
      if( !IntMath.IntMathNew.FindMultiplicativeInverseSmall( PrivKInverseExponentDP, PubKeyExponent, PrimePMinus1, Worker ))
        return;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponentDP: " + IntMath.ToString10( PrivKInverseExponentDP ));
      if( PrivKInverseExponentDP.IsZero())
        continue;

      // PrivKInverseExponentDP is PrivKInverseExponent mod PrimePMinus1.
      Integer Test1 = new Integer();
      Test1.Copy( PrivKInverseExponent );
      IntMath.Divide( Test1, PrimePMinus1, Quotient, Remainder );
      Test1.Copy( Remainder );
      if( !Test1.IsEqual( PrivKInverseExponentDP ))
        throw( new Exception( "Bug. This does not match the definition of PrivKInverseExponentDP." ));

      if( Worker.CancellationPending )
        return;

      // In RFC 2437 it defines a number dQ which is the multiplicative
      // inverse, mod (Q - 1) of e.  That dQ is named PrivKInverseExponentDQ here.
      Worker.ReportProgress( 0, " " );
      if( !IntMath.IntMathNew.FindMultiplicativeInverseSmall( PrivKInverseExponentDQ, PubKeyExponent, PrimeQMinus1, Worker ))
        return;

      if( PrivKInverseExponentDQ.IsZero())
        continue;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponentDQ: " + IntMath.ToString10( PrivKInverseExponentDQ ));
      if( Worker.CancellationPending )
        return;

      Test1.Copy( PrivKInverseExponent );
      IntMath.Divide( Test1, PrimeQMinus1, Quotient, Remainder );
      Test1.Copy( Remainder );
      if( !Test1.IsEqual( PrivKInverseExponentDQ ))
        throw( new Exception( "Bug. This does not match the definition of PrivKInverseExponentDQ." ));


      // Make a random number to test encryption/decryption.
      Integer ToEncrypt = new Integer();
      int HowManyBytes = PrimeIndex * 4;
      byte[] RandBytes = MakeRandomBytes( HowManyBytes );
      if( RandBytes == null )
        {
        Worker.ReportProgress( 0, "Error making random bytes in MakeRSAKeys()." );
        return;
        }

      if( !ToEncrypt.MakeRandomOdd( PrimeIndex - 1, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number ToEncrypt." );
        return;
        }

      Integer PlainTextNumber = new Integer();
      PlainTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Before encrypting number: " + IntMath.ToString10( ToEncrypt ));
      Worker.ReportProgress( 0, " " );

      IntMath.IntMathNew.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      Worker.ReportProgress( 0, IntMath.GetStatusString() );

      Integer CipherTextNumber = new Integer();
      CipherTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Encrypted number: " + IntMath.ToString10( CipherTextNumber ));
      Worker.ReportProgress( 0, " " );

      ECTime DecryptTime = new ECTime();
      DecryptTime.SetToNow();
      IntMath.IntMathNew.ModularPower( ToEncrypt, PrivKInverseExponent, PubKeyN );
      Worker.ReportProgress( 0, "Decrypted number: " + IntMath.ToString10( ToEncrypt ));

      if( !PlainTextNumber.IsEqual( ToEncrypt ))
        {
        throw( new Exception( "PlainTextNumber not equal to unencrypted value." ));
        // Because P or Q wasn't really a prime?
        // Worker.ReportProgress( 0, "PlainTextNumber not equal to unencrypted value." );
        // continue;
        }

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Decrypt time seconds: " + DecryptTime.GetSecondsToNow().ToString( "N2" ));
      Worker.ReportProgress( 0, " " );
      if( Worker.CancellationPending )
        return;

      // Test the standard optimized way of decrypting:
      if( !ToEncrypt.MakeRandomOdd( PrimeIndex - 1, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeRSAKeys()." );
        return;
        }

      PlainTextNumber.Copy( ToEncrypt );
      IntMath.IntMathNew.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      CipherTextNumber.Copy( ToEncrypt );

      // QInv is the multiplicative inverse of PrimeQ mod PrimeP.
      if( !IntMath.MultiplicativeInverse( PrimeQ, PrimeP, QInv, Worker ))
        throw( new Exception( "MultiplicativeInverse() returned false." ));

      if( QInv.IsNegative )
        throw( new Exception( "This is a bug. QInv is negative." ));

      Worker.ReportProgress( 0, "QInv is: " + IntMath.ToString10( QInv ));

      DecryptWithQInverse( CipherTextNumber,
                           ToEncrypt, // Decrypt it to this.
                           PlainTextNumber, // Test it against this.
                           PubKeyN,
                           PrivKInverseExponentDP,
                           PrivKInverseExponentDQ,
                           PrimeP,
                           PrimeQ,
                           Worker );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Found the values:" );
      Worker.ReportProgress( 0, "Seconds: " + StartTime.GetSecondsToNow().ToString( "N0" ));
      Worker.ReportProgress( 0, " " );

      Worker.ReportProgress( 1, "Prime1: " + IntMath.ToString10( PrimeP ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "Prime2: " + IntMath.ToString10( PrimeQ ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "PubKeyN: " + IntMath.ToString10( PubKeyN ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "PrivKInverseExponent: " + IntMath.ToString10( PrivKInverseExponent ));

      Worker.ReportProgress( 0, " " );

      // return; // Comment this out to just leave it while( true ) for testing.
      }
    }



  internal bool DecryptWithQInverse( Integer EncryptedNumber,
                                     Integer DecryptedNumber,
                                     Integer TestDecryptedNumber,
                                     Integer PubKeyN,
                                     Integer PrivKInverseExponentDP,
                                     Integer PrivKInverseExponentDQ,
                                     Integer PrimeP,
                                     Integer PrimeQ,
                                     BackgroundWorker Worker )
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of DecryptWithQInverse()." );

    // QInv and the dP and dQ numbers are normally already set up before
    // you start your listening socket.
    ECTime DecryptTime = new ECTime();
    DecryptTime.SetToNow();

    // See section 5.1.2 of RFC 2437 for these steps:
    // http://tools.ietf.org/html/rfc2437
    //      2.2 Let m_1 = c^dP mod p.
    //      2.3 Let m_2 = c^dQ mod q.
    //      2.4 Let h = qInv ( m_1 - m_2 ) mod p.
    //      2.5 Let m = m_2 + hq.

    Worker.ReportProgress( 0, "EncryptedNumber: " + IntMath.ToString10( EncryptedNumber ));

    //      2.2 Let m_1 = c^dP mod p.
    TestForDecrypt.Copy( EncryptedNumber );
    IntMath.IntMathNew.ModularPowerModPrimeP( TestForDecrypt, PrivKInverseExponentDP, PrimeP );
    if( Worker.CancellationPending )
      return false;

    M1ForInverse.Copy( TestForDecrypt );

    //      2.3 Let m_2 = c^dQ mod q.
    TestForDecrypt.Copy( EncryptedNumber );
    IntMath.IntMathNew.ModularPowerModPrimeQ( TestForDecrypt, PrivKInverseExponentDQ, PrimeQ );

    if( Worker.CancellationPending )
      return false;

    M2ForInverse.Copy( TestForDecrypt );

    //      2.4 Let h = qInv ( m_1 - m_2 ) mod p.

    // How many is optimal to avoid the division?
    int HowManyIsOptimal = (PrimeP.GetIndex() * 3);
    for( int Count = 0; Count < HowManyIsOptimal; Count++ )
      {
      if( M1ForInverse.ParamIsGreater( M2ForInverse ))
        M1ForInverse.Add( PrimeP );
      else
        break;

      }

    if( M1ForInverse.ParamIsGreater( M2ForInverse ))
      {
      M1M2SizeDiff.Copy( M2ForInverse );
      IntMath.Subtract( M1M2SizeDiff, M1ForInverse );
      // Unfortunately this long Divide() has to be done.
      IntMath.Divide( M1M2SizeDiff, PrimeP, Quotient, Remainder );
      Quotient.AddULong( 1 );
      Worker.ReportProgress( 0, "The Quotient for M1M2SizeDiff is: " + IntMath.ToString10( Quotient ));
      IntMath.Multiply( Quotient, PrimeP );
      M1ForInverse.Add( Quotient );
      }

    M1MinusM2.Copy( M1ForInverse );
    IntMath.Subtract( M1MinusM2, M2ForInverse );

    if( M1MinusM2.IsNegative )
      throw( new Exception( "This is a bug. M1MinusM2.IsNegative is true." ));

    if( QInv.IsNegative )
      throw( new Exception( "This is a bug. QInv.IsNegative is true." ));

    HForQInv.Copy( M1MinusM2 );
    IntMath.Multiply( HForQInv, QInv );

    if( HForQInv.IsNegative )
      throw( new Exception( "This is a bug. HForQInv.IsNegative is true." ));

    if( PrimeP.ParamIsGreater( HForQInv ))
      {
      IntMath.Divide( HForQInv, PrimeP, Quotient, Remainder );
      HForQInv.Copy( Remainder );
      }

    //      2.5 Let m = m_2 + hq.
    DecryptedNumber.Copy( HForQInv );
    IntMath.Multiply( DecryptedNumber, PrimeQ );
    DecryptedNumber.Add( M2ForInverse );
    if( !TestDecryptedNumber.IsEqual( DecryptedNumber ))
      throw( new Exception( "!TestDecryptedNumber.IsEqual( DecryptedNumber )." ));

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "DecryptedNumber: " + IntMath.ToString10( DecryptedNumber ));
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestDecryptedNumber: " + IntMath.ToString10( TestDecryptedNumber ));
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Decrypt with QInv time seconds: " + DecryptTime.GetSecondsToNow().ToString( "N2" ));
    Worker.ReportProgress( 0, " " );
    return true;
    }


  }
}

