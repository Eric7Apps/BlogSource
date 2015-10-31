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
  private Integer CipherToDP;
  private Integer PlainTextMinusCipherToDP;

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
    CipherToDP = new Integer();
    PlainTextMinusCipherToDP = new Integer();
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
    // const int RandomIndex = 1024 / 32;
    // const int RandomIndex = 768 / 32;
    const int RandomIndex = 512 / 32;
    int ShowBits = RandomIndex * 32;
    // int TestLoops = 0;

    Worker.ReportProgress( 0, "Bits size is: " + ShowBits.ToString());

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

      IntMath.ModularPower2( ToEncrypt, PubKeyExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      CipherTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Encrypted number: " + IntMath.ToString10( CipherTextNumber ));
      Worker.ReportProgress( 0, " " );

      ECTime DecryptTime = new ECTime();
      DecryptTime.SetToNow();
      IntMath.ModularPower2( ToEncrypt, PrivKInverseExponent, PubKeyN );
      Worker.ReportProgress( 0, "Decrypted number: " + IntMath.ToString10( ToEncrypt ));

      if( !PlainTextNumber.IsEqual( ToEncrypt ))
        {
        // If it's not really made from two primes can this happen?
        Worker.ReportProgress( 0, "PlainTextNumber not equal to unencrypted value." );
        return;
        }

      // IntMath.Subtract( ToEncrypt, Padding );
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Decrypt time seconds: " + DecryptTime.GetSecondsToNow().ToString( "N0" ));
      Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "Ascii string after decrypt is: " + ToEncrypt.GetAsciiString() );
      // Worker.ReportProgress( 0, " " );

      if( Worker.CancellationPending )
        return;

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
      return; // Comment this out to just leave it while( true ) for testing.
      }
    }




  internal bool FindFactorsFromOnePartOfPrivateExponent( Integer PubKeyExponent,
                             Integer PubKeyN,
                             Integer PrivKInverseExponentDP,
                             BackgroundWorker Worker )
    {
    // If you can find PrivKInverseExponentDP (or DQ) then you can find the factors.

    //                                                1         2         3         4         5         6         7         8
    //                                       12345678901234567890123456789012345678901234567890123456789012345678901234567890
    ToEncryptForInverse.SetFromAsciiString( "This is known Plain Text. This is known Plain Text. This is known Plain" );
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

    IntMath.ModularPower2( ToEncryptForInverse, PubKeyExponent, PubKeyN );
    if( Worker.CancellationPending )
      return false;

    CipherTextNumber.Copy( ToEncryptForInverse );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherTextNumber: " + IntMath.ToString10( CipherTextNumber ));

    // PrivKInverseExponentDP is congruent to PrivKInverseExponent mod PrimePMinus1.
    // PrivKInverseExponentDQ is congruent to PrivKInverseExponent mod PrimeQMinus1.

    CipherToDP.Copy( CipherTextNumber );
    IntMath.ModularPower2( CipherToDP, PrivKInverseExponentDP, PubKeyN );
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



  }
}



