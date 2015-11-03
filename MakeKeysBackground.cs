// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker
using System.Security.Cryptography;

// See RSA Cryptosystem:
// https://en.wikipedia.org/wiki/RSA_%28cryptosystem%29

// Euler's Theorem, the basis of the RSA Cryptosystem:
// https://en.wikipedia.org/wiki/Euler's_theorem


namespace ExampleServer
{
  class MakeKeysBackground
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Temp1;
  private Integer Temp2;
  private Integer Test1;
  private Integer ToEncryptForInverse;
  private Integer PlainTextNumber;
  private Integer CipherTextNumber;
  private Integer M1ForInverse;
  private Integer M2ForInverse;
  private Integer HForQInv;
  private Integer M1MinusM2;
  private Integer QInv;
  private Integer PrimeToFind;
  private Integer PlainTextMinusCipherToDP;
  private Integer PlainTextMinusCipherToDQ;
  private Integer CipherToDP;
  private Integer CipherToDQ;

  private BackgroundWorker Worker;
  private MakeKeysWorkerInfo WInfo;
  private RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();
  private ECTime StartTime;


  private MakeKeysBackground()
    {
    }



  internal MakeKeysBackground( BackgroundWorker UseWorker, MakeKeysWorkerInfo UseWInfo )
    {
    Worker = UseWorker;
    WInfo = UseWInfo;

    StartTime = new ECTime();
    StartTime.SetToNow();
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
    Temp1 = new Integer();
    Temp2 = new Integer();
    Test1 = new Integer();
    ToEncryptForInverse = new Integer();
    PlainTextNumber = new Integer();
    CipherTextNumber = new Integer();
    M1ForInverse = new Integer();
    M2ForInverse = new Integer();
    HForQInv = new Integer();
    M1MinusM2 = new Integer();
    QInv = new Integer();
    PrimeToFind = new Integer();
    PlainTextMinusCipherToDP = new Integer();
    PlainTextMinusCipherToDP = new Integer();
    PlainTextMinusCipherToDQ = new Integer();
    CipherToDP = new Integer();
    CipherToDQ = new Integer();
    }



  internal void FreeEverything()
    {
    if( RngCsp != null )
      {
      RngCsp.Dispose();
      RngCsp = null;
      }
    }



  private bool MakeAPrime( Integer Result, int RandomIndex, int HowMany )
    {
    try
    {
    while( true )
      {
      if( Worker.CancellationPending )
        return false;

      // Don't hog the server's resources too much.
      Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.

      int HowManyBytes = (RandomIndex * 4) + 4;
      byte[] RandBytes = MakeRandomBytes( HowManyBytes );
      if( RandBytes == null )
        {
        Worker.ReportProgress( 0, "Error making random bytes in MakeKeysBackGround.MakeAPrime()." );
        return false;
        }

      if( !Result.MakeRandomOdd( RandomIndex, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeKeysBackGround.MakeAPrime()." );
        return false;
        }

      // Make sure that it's about the size I think it is.
      if( Result.GetIndex() < RandomIndex )
        continue;

      uint TestPrime = IntMath.IsDivisibleBySmallPrime( Result );
      if( 0 != TestPrime)
        {
        Worker.ReportProgress( 0, "Next test: " + TestPrime.ToString() );
        // Test:
        // if( IntMath.IsFermatPrime( Result, HowMany ))
          // throw( new Exception( "Passed IsFermatPrime even though it has a small prime." ));

        continue;
        }

      Worker.ReportProgress( 0, "Fermat test." );

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
      Worker.ReportProgress( 0, " " );
      // Presumably this will eventually find one and not loop forever.
      return true; // With Result.
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in MakeKeysBackGround.MakeAPrime()." );
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
      Worker.ReportProgress( 0, "Error in MakeKeysBackGround.MakeRandomBytes()." );
      Worker.ReportProgress( 0, Except.Message );
      return null;
      }
    }



  internal void MakeRSAKeys()
    {
    // These numbers are in RFC 2437:
    Integer PrimeP = new Integer();
    Integer PrimeQ = new Integer();
    Integer PrimePMinus1 = new Integer();
    Integer PrimeQMinus1 = new Integer();
    Integer PubKeyN = new Integer();
    Integer PhiN = new Integer();
    Integer PubKeyExponent = new Integer();
    Integer PrivKInverseExponent = new Integer();
    Integer PrivKInverseExponentDP = new Integer();
    Integer PrivKInverseExponentDQ = new Integer();
    Integer QInv = new Integer();
    Integer M1 = new Integer();
    Integer M2 = new Integer();
    // Integer HForQInv = new Integer();

    Integer ToEncrypt = new Integer();
    Integer PlainTextNumber = new Integer();
    Integer CipherTextNumber = new Integer();
    Integer Gcd = new Integer();
    Integer Quotient = new Integer();
    Integer Remainder = new Integer();
    // 65537 is a prime.
    // Commonly used exponent for RSA.  It is 2^16 + 1.
    const uint PubKeyExponentUint = 65537;
    PubKeyExponent.SetFromULong( PubKeyExponentUint );
    const int RandomIndex = 1024 / 32;
    // const int RandomIndex = 768 / 32;
    // const int RandomIndex = 512 / 32;
    int ShowBits = RandomIndex * 32;
    // int TestLoops = 0;

    Worker.ReportProgress( 0, "Bits size is: " + ShowBits.ToString());

    // Worker.ReportProgress( 0, IntMath.GetStatusString());
    // TestAddAndSubtract();

    // ulong Loops = 0;
    while( true )
    // for( int Count = 0; Count < 1000; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      Thread.Sleep( 1 ); // Give up the time slice.  Let other things on the server run.

      // Make two prime factors.

      if( !MakeAPrime( PrimeP, RandomIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      byte[] TestBytes;
      try
      {
      TestBytes = PrimeP.GetBigEndianByteArray();
      }
      catch( Exception Except )
        {
        Worker.ReportProgress( 0, "Exception with GetBigEndianByteArray()." );
        Worker.ReportProgress( 0, Except.Message );
        return;
        }

      try
      {
      Test1.SetFromBigEndianByteArray( TestBytes );
      }
      catch( Exception Except )
        {
        Worker.ReportProgress( 0, "Exception with SetFromBigEndianByteArray()." );
        Worker.ReportProgress( 0, Except.Message );
        return;
        }

      if( !Test1.IsEqual( PrimeP ))
        {
        Worker.ReportProgress( 0, "The big endian bytes weren't set right." );
        continue;
        }


      if( !MakeAPrime( PrimeQ, RandomIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      // This is extremely unlikely since there's a high probability
      // that they are primes.
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

      PrimePMinus1.Copy( PrimeP );
      IntMath.SubtractULong( PrimePMinus1, 1 );
      PrimeQMinus1.Copy( PrimeQ );
      IntMath.SubtractULong( PrimeQMinus1, 1 );

      // These checks should be more thorough.

      if( Worker.CancellationPending )
        return;

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
      // next, but the Least Common Multiple of Prime1Minus1 and Prime2Minus1
      // can also be used here.  The mathematical basis for this has to do
      // with Euler's Theorem (which has a history from Fermat's Little Theorem).
      // PrimePMinus1 and PrimeQMinus1 can't have large common factors
      // because that would make it easier to factor the public key PubKeyN.

      // PhiN is: (P - 1)(Q - 1) = PQ - P - Q + 1
      // If I add (P - 1) to PhiN I get:
      // PQ - P - Q + 1 + (P - 1) = PQ - Q.
      // If I add (Q - 1) to that I get:
      // PQ - Q + (Q - 1) = PQ - 1.
      // So PQ - 1 has the same common factors as (P - 1) and (Q - 1).
      // How difficult is it to find the factors of PQ - 1?
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
          Worker.ReportProgress( 0, "This GCD number is bigger than 1234567: " + IntMath.ToString10( Gcd ));
          continue;
          }
        }

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

      if( !IntMath.FindMultiplicativeInverseSmall( PrivKInverseExponent, PubKeyExponent, PhiN, Worker ))
        return;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponent: " + IntMath.ToString10( PrivKInverseExponent ));
      if( PrivKInverseExponent.IsZero())
        continue;

      if( Worker.CancellationPending )
        return;


      // In RFC 2437 it defines a number dP which is the multiplicative
      // inverse, mod (P - 1) of e.  That dP is named PrivKInverseExponentDP here.
      Worker.ReportProgress( 0, " " );
      if( !IntMath.FindMultiplicativeInverseSmall( PrivKInverseExponentDP, PubKeyExponent, PrimePMinus1, Worker ))
        return;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponentDP: " + IntMath.ToString10( PrivKInverseExponentDP ));
      if( PrivKInverseExponentDP.IsZero())
        continue;

      if( Worker.CancellationPending )
        return;


      // In RFC 2437 it defines a number dQ which is the multiplicative
      // inverse, mod (Q - 1) of e.  That dQ is named PrivKInverseExponentDQ here.
      Worker.ReportProgress( 0, " " );
      if( !IntMath.FindMultiplicativeInverseSmall( PrivKInverseExponentDQ, PubKeyExponent, PrimeQMinus1, Worker ))
        return;

      if( PrivKInverseExponentDQ.IsZero())
        continue;

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PrivKInverseExponentDQ: " + IntMath.ToString10( PrivKInverseExponentDQ ));
      if( Worker.CancellationPending )
        return;

      // Show how the prime factors can be found if one of the parts dP or dQ
      // can be found.
      FindFactorsFromOnePartOfPrivateExponent( PubKeyExponent,
                   PubKeyN,
                   PrivKInverseExponentDP,
                   Worker );

      ExploreRSAOptimization( PubKeyExponent,
                              PubKeyN,
                              PrivKInverseExponentDP,
                              PrivKInverseExponentDQ,
                              PrimeP,
                              PrimeQ,
                              Worker );

      // IntMath.TestMultiplicativeInverse( Worker );

      // Make a random number to test encryption/decryption.
      int HowManyBytes = (RandomIndex * 4) + 4;
      byte[] RandBytes = MakeRandomBytes( HowManyBytes );
      if( RandBytes == null )
        {
        Worker.ReportProgress( 0, "Error making random bytes in MakeKeysBackGround.MakeAPrime()." );
        return;
        }

      if( !ToEncrypt.MakeRandomOdd( RandomIndex, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeKeysBackGround.MakeAPrime()." );
        return;
        }

      // TestLoops++;

      PlainTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Before encrypting number: " + IntMath.ToString10( ToEncrypt ));
      Worker.ReportProgress( 0, " " );

      IntMath.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      CipherTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Encrypted number: " + IntMath.ToString10( CipherTextNumber ));
      Worker.ReportProgress( 0, " " );

      ECTime DecryptTime = new ECTime();
      DecryptTime.SetToNow();
      IntMath.ModularPower( ToEncrypt, PrivKInverseExponent, PubKeyN );
      Worker.ReportProgress( 0, "Decrypted number: " + IntMath.ToString10( ToEncrypt ));

      if( !PlainTextNumber.IsEqual( ToEncrypt ))
        {
        // If it's not really made from two primes can this happen?
        Worker.ReportProgress( 0, "PlainTextNumber not equal to unencrypted value." );
        return;
        }

      // IntMath.Subtract( ToEncrypt, Padding );
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Decrypt time seconds: " + DecryptTime.GetSecondsToNow().ToString( "N2" ));
      Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "Ascii string after decrypt is: " + ToEncrypt.GetAsciiString() );
      // Worker.ReportProgress( 0, " " );

      if( Worker.CancellationPending )
        return;

      //////////
      // Test the optimized way of decrypting:
      if( !ToEncrypt.MakeRandomOdd( RandomIndex, RandBytes ))
        {
        Worker.ReportProgress( 0, "Error making random number in MakeKeysBackGround.MakeAPrime()." );
        return;
        }

      PlainTextNumber.Copy( ToEncrypt );
      IntMath.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      CipherTextNumber.Copy( ToEncrypt );

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

      // return; // Comment this out to just leave it while( true ) for testing.
      }
    }




  internal bool FindFactorsFromOnePartOfPrivateExponent( Integer PubKeyExponent,
                             Integer PubKeyN,
                             Integer PrivKInverseExponentDP,
                             BackgroundWorker Worker )
    {
    // If you can find PrivKInverseExponentDP (or DQ) then you can find the factors.

    // Make sure this number is big enough.  Bigger then PrimeP and
    // PrimeQ is big enough. And the approximate bit-length of those is
    // known from the public key length.
    ToEncryptForInverse.SetFromAsciiString( "Any arbitrary number. This is known Plain Text. This is known Plain Text. This is known Plain Text. This is known Plain Text. " );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "ASCII string is: " + ToEncryptForInverse.GetAsciiString() );

    PlainTextNumber.Copy( ToEncryptForInverse );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PlainTextNumber: " + IntMath.ToString10( PlainTextNumber ));

    if( PubKeyN.ParamIsGreaterOrEq( PlainTextNumber ))
      {
      Worker.ReportProgress( 0, "PlainTextNumber is too big." );
      return false;
      }

    // Make sure it's not even close to PubKeyN minus 1.
    if( PubKeyN.GetIndex() == PlainTextNumber.GetIndex() )
      {
      Worker.ReportProgress( 0, "PlainTextNumber is too big. (Index)" );
      return false;
      }

    IntMath.ModularPower( ToEncryptForInverse, PubKeyExponent, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    CipherTextNumber.Copy( ToEncryptForInverse );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherTextNumber: " + IntMath.ToString10( CipherTextNumber ));

    // PrivKInverseExponentDP is congruent to PrivKInverseExponent mod PrimePMinus1.
    // PrivKInverseExponentDQ is congruent to PrivKInverseExponent mod PrimeQMinus1.

    CipherToDP.Copy( CipherTextNumber );
    IntMath.ModularPower( CipherToDP, PrivKInverseExponentDP, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    PlainTextMinusCipherToDP.Copy( PlainTextNumber );
    if( PlainTextMinusCipherToDP.ParamIsGreaterOrEq( CipherToDP ))
      PlainTextMinusCipherToDP.Add( PubKeyN );

    // Notice the relationship between the plain text number and the
    // partial-plain-text number here.  This is the key to understanding
    // how the Chinese Remainder Theorem is getting used in another step
    // of the RSA optimization process.
    IntMath.Subtract( PlainTextMinusCipherToDP, CipherToDP );

    // PlainTextMinusCipherToDP and PubKeyN are both congruent to zero mod PrimeP.
    IntMath.GreatestCommonDivisor( PlainTextMinusCipherToDP, PubKeyN, PrimeToFind );
    IntMath.Divide( PubKeyN, PrimeToFind, Quotient, Remainder );
    if( Remainder.IsZero())
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "The two factors are:" );
      Worker.ReportProgress( 0, "1) " + IntMath.ToString10( PrimeToFind ));
      Worker.ReportProgress( 0, "2) " + IntMath.ToString10( Quotient ));
      return true;
      }
    else
      {
      Worker.ReportProgress( 0, "There was an error finding the factors." );
      return false;
      }
    }



  private void TestAddAndSubtract()
    {
    // This is just to test that the sign comes out right, so it's just testing
    // the outer wrapper parts.
    Integer TestSub1 = new Integer();
    Integer TestSub2 = new Integer();

    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));
    // Worker.ReportProgress( 0, "Result: " + TestSub1.GetAsHexString());

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub1.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub1.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub1.IsNegative = true;
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub1.IsNegative = true;
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Subtract( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Addition:" );

    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));
    // Worker.ReportProgress( 0, "Result: " + TestSub1.GetAsHexString());

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub1.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub1.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));


    TestSub1.SetFromULong( 23456 );
    TestSub2.SetFromULong( 12345 );
    TestSub1.IsNegative = true;
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));

    TestSub1.SetFromULong( 12345 );
    TestSub2.SetFromULong( 23456 );
    TestSub1.IsNegative = true;
    TestSub2.IsNegative = true;
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "TestSub1: " + IntMath.ToString10( TestSub1 ));
    Worker.ReportProgress( 0, "TestSub2: " + IntMath.ToString10( TestSub2 ));
    IntMath.Add( TestSub1, TestSub2 );
    Worker.ReportProgress( 0, "Result: " + IntMath.ToString10( TestSub1 ));
    }



    // What is the size of a number when it's the result of two numbers
    // being multiplied?
    // 2 times 2 is 4.
    //       1  0
    //       1  0
    //       0  0
    //    1  0
    //    1  0  0
    // Biggest bit is at index 2. (Zero based index.)

    // 7 * 7 = 49
    //                 1  1  1
    //                 1  1  1
    //                --------
    //                 1  1  1
    //              1  1  1
    //           1  1  1
    //          --------------
    //        1  1  0  0  0  1
    //       32 16           1 = 49
    // Biggest bit is at 5 (2 + 2) + 1 because of the carry on the highest bit.
    // The highest bit is at either index + index or it's
    // at index + index + 1.



  internal bool ExploreRSAOptimization( Integer PubKeyExponent,
                                        Integer PubKeyN,
                                        Integer PrivKInverseExponentDP,
                                        Integer PrivKInverseExponentDQ,
                                        Integer PrimeP,
                                        Integer PrimeQ,
                                        BackgroundWorker Worker )
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Start of ExploreRSAOptimization()." );
    Worker.ReportProgress( 0, " " );

    // CRT Coefficient means Chinese Remainder Theorem Coefficient.

    // See section 5.1.2 of RFC 2437 for these steps:
    // http://tools.ietf.org/html/rfc2437
    //      2.2 Let m_1 = c^dP mod p.
    //      2.3 Let m_2 = c^dQ mod q.
    //      2.4 Let h = qInv ( m_1 - m_2 ) mod p.
    //      2.5 Let m = m_2 + hq.

    // This is a totally arbitrary number that is roughly the size of 
    // PrimeP and PrimeQ (about the same bit length), but bigger than
    // either of them.
    ToEncryptForInverse.SetFromAsciiString( "Any arbitrary number." );
    ToEncryptForInverse.Add( PrimeP );
    ToEncryptForInverse.Add( PrimeQ );

    PlainTextNumber.Copy( ToEncryptForInverse );
    Worker.ReportProgress( 0, "PlainTextNumber: " + IntMath.ToString10( PlainTextNumber ));

    IntMath.ModularPower( ToEncryptForInverse, PubKeyExponent, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    CipherTextNumber.Copy( ToEncryptForInverse );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherTextNumber: " + IntMath.ToString10( CipherTextNumber ));

    // If the plain text number is too small then M1ForInverse and
    // M2ForInverse come out to be equal to the plain text number.
    // With the RSA crypto system, padding is used to prevent this
    // from happening.
    //      2.2 Let m_1 = c^dP mod p.
    M1ForInverse.Copy( CipherTextNumber );
    IntMath.ModularPower( M1ForInverse, PrivKInverseExponentDP, PrimeP );
    if( Worker.CancellationPending )
      return false;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "M1ForInverse: " + IntMath.ToString10( M1ForInverse ));

    // In RFC 2437 it defines a number dQ which is the multiplicative
    // inverse, mod (Q - 1) of e.  That dQ is named PrivKInverseExponentDQ here.

    //      2.3 Let m_2 = c^dQ mod q.
    M2ForInverse.Copy( CipherTextNumber );
    IntMath.ModularPower( M2ForInverse, PrivKInverseExponentDQ, PrimeQ );
    if( Worker.CancellationPending )
      return false;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "M2ForInverse: " + IntMath.ToString10( M2ForInverse ));

    /////////////
    CipherToDP.Copy( CipherTextNumber );
    IntMath.ModularPower( CipherToDP, PrivKInverseExponentDP, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherToDP: " + IntMath.ToString10( CipherToDP ));

    PlainTextMinusCipherToDP.Copy( PlainTextNumber );
    if( PlainTextMinusCipherToDP.ParamIsGreaterOrEq( CipherToDP ))
      PlainTextMinusCipherToDP.Add( PubKeyN );

    // Notice the relationship between the plain text number and the
    // partial-plain-text number here.  This is the key to understanding
    // how the Chinese Remainder Theorem is getting used.
    IntMath.Subtract( PlainTextMinusCipherToDP, CipherToDP );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PlainTextMinusCipherToDP: " + IntMath.ToString10( PlainTextMinusCipherToDP ));

    // PlainTextMinusCipherToDP and PubKeyN are both congruent to zero mod PrimeP.
    IntMath.GreatestCommonDivisor( PlainTextMinusCipherToDP, PubKeyN, PrimeToFind );
    IntMath.Divide( PubKeyN, PrimeToFind, Quotient, Remainder );
    if( Remainder.IsZero())
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "The number PrimeP is:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PrimeToFind ));
      }
    else
      {
      Worker.ReportProgress( 0, "This is a bug. This should not happen." );
      return false;
      }

    ////////
    CipherToDQ.Copy( CipherTextNumber );
    IntMath.ModularPower( CipherToDQ, PrivKInverseExponentDQ, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherToDQ: " + IntMath.ToString10( CipherToDQ ));

    PlainTextMinusCipherToDQ.Copy( PlainTextNumber );
    if( PlainTextMinusCipherToDQ.ParamIsGreaterOrEq( CipherToDQ ))
      PlainTextMinusCipherToDQ.Add( PubKeyN );

    IntMath.Subtract( PlainTextMinusCipherToDQ, CipherToDQ );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PlainTextMinusCipherToDQ: " + IntMath.ToString10( PlainTextMinusCipherToDQ ));

    // PlainTextMinusCipherToDQ and PubKeyN are both congruent to zero mod PrimeQ.
    IntMath.GreatestCommonDivisor( PlainTextMinusCipherToDQ, PubKeyN, PrimeToFind );
    IntMath.Divide( PubKeyN, PrimeToFind, Quotient, Remainder );
    if( Remainder.IsZero())
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "The number PrimeQ is:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PrimeToFind ));
      }
    else
      {
      Worker.ReportProgress( 0, "This is a bug. This should not happen." );
      return false;
      }


    ///////////
    //      2.4 Let h = qInv * ( m_1 - m_2 ) mod p.
    //      2.5 Let m = m_2 + hq.
    // m is the plain text message.
    // From the way it's defined above, the plain text message is larger than
    // either PrimeP or PrimeQ and so it's necessarily larger than
    // M2ForInverse, which isn't larger than PrimeQ.
    if( PlainTextNumber.ParamIsGreater( M2ForInverse ))
      {
      Worker.ReportProgress( 0, "This is a bug. The plain text was too small." );
      return false;
      }

    HForQInv.Copy( PlainTextNumber );
    //      2.5 Let m = m_2 + hq.
    IntMath.Subtract( HForQInv, M2ForInverse );
    IntMath.Divide( HForQInv, PrimeQ, Quotient, Remainder );
    if( !Remainder.IsZero())
      {
      Worker.ReportProgress( 0, "This is a bug. The Remainder for HForQInv has to be zero." );
      return false;
      }

    HForQInv.Copy( Quotient );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "HForQInv is: " + IntMath.ToString10( HForQInv ));
    //      2.5 Let m = m_2 + hq.
    // HForQInv is: 1, 2, 3, or 17, or some small number like that because
    // of the way the plain text number was defined to be about the
    // same bit length as PrimeQ and PrimeP.  So the bit length of
    // HForQInv should be very small.  But that's not true of any
    // arbitrary plain text number that can be any size.
    // m = m_2 + hq

    if( Worker.CancellationPending )
      return false;


    // HForQInv is now equal to this h:
    //      2.4 Let h = qInv * ( m_1 - m_2 ) mod p.
    // h  + (Y * PrimeP) = qInv * ( m_1 - m_2 )

    if( M1ForInverse.ParamIsGreater( M2ForInverse ))
      M1ForInverse.Add( PrimeP );

    M1MinusM2.Copy( M1ForInverse );
    IntMath.Subtract( M1MinusM2, M2ForInverse );

    // M1MinusM2 is about the same bit length as PrimeP or PrimeQ here.
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "M1MinusM2 is: " + IntMath.ToString10( M1MinusM2 ));
    // M1MinusM2 is:
    // 952,979,419,398,732,400,062,376,950,243,360,175,031,829,176,382,341,223,531,337,704,165,570,449,595,634,433,911,712,498,266,542,709,814,619,022,231,534,764,141,184,715,551,949,575,393,740,319,223,328,486,967,452,408
    // The number PrimeP is:
    // 4,309,187,269,259,906,350,710,607,763,515,484,238,396,386,998,650,671,014,243,877,765,782,493,146,329,875,226,603,402,585,088,707,322,678,166,907,546,268,966,354,062,137,357,415,937,440,385,719,393,661,841,140,387,859


    // The RFC describes qInv as:
    // coefficient INTEGER -- (inverse of q) mod p }

    // QInv is the multiplicative inverse of PrimeQ mod PrimeP.
    // That means that:
    // QInv * PrimeQ = (Y * PrimeP) + 1
    // PrimeQ and PrimeP are about the same size.  Meaning that they
    // have about the same number of bits (because they're defined that
    // way), and that implies that QInv and Y have about the same bit
    // length.

    Worker.ReportProgress( 0, " " );
    // try
    // {
    if( !IntMath.MultiplicativeInverse( PrimeQ, PrimeP, QInv, Worker ))
      {
      Worker.ReportProgress( 0, "MultiplicativeInverse() returned false." );
      return false;
      }
    /*
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Exception for MultiplicativeInverse()." );
      Worker.ReportProgress( 0, Except.Message );
      return false;
      } */

    Worker.ReportProgress( 0, "QInv is: " + IntMath.ToString10( QInv ));

    // HForQInv is now equal to this h:
    //      2.4 Let h = qInv * ( m_1 - m_2 ) mod p.
    // h  + (Y * PrimeP) = qInv * ( m_1 - m_2 )
    Test1.Copy( QInv );
    IntMath.Multiply( Test1, M1MinusM2 );
    Worker.ReportProgress( 0, "Before QInv Divide." );
    IntMath.Divide( Test1, PrimeP, Quotient, Remainder );
    if( !Remainder.IsEqual( HForQInv ))
      {
      Worker.ReportProgress( 0, "This is a bug. !Remainder.IsEqual( HForQInv )." );
      return false;
      }

    Worker.ReportProgress( 0, "Finished ExploreRSAOptimization()." );
    return true;
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
    M1ForInverse.Copy( EncryptedNumber );
    IntMath.ModularPower( M1ForInverse, PrivKInverseExponentDP, PrimeP );
    if( Worker.CancellationPending )
      return false;

    //      2.3 Let m_2 = c^dQ mod q.
    M2ForInverse.Copy( EncryptedNumber );
    IntMath.ModularPower( M2ForInverse, PrivKInverseExponentDQ, PrimeQ );
    if( Worker.CancellationPending )
      return false;

    //      2.4 Let h = qInv ( m_1 - m_2 ) mod p.
    if( M1ForInverse.ParamIsGreater( M2ForInverse ))
      M1ForInverse.Add( PrimeP );

    M1MinusM2.Copy( M1ForInverse );
    IntMath.Subtract( M1MinusM2, M2ForInverse );
    HForQInv.Copy( M1MinusM2 );
    IntMath.Multiply( HForQInv, QInv );

    if( PrimeP.ParamIsGreater( HForQInv ))
      {
      IntMath.Divide( HForQInv, PrimeP, Quotient, Remainder );
      HForQInv.Copy( Remainder );
      }

    // throw( new Exception( "Test this." ));

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



