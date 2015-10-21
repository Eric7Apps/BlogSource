// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker
using System.Security.Cryptography;

// See RSA Cryptosystem:
// https://en.wikipedia.org/wiki/RSA_%28cryptosystem%29


namespace ExampleServer
{
  class MakeKeysBackground
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Temp1;
  private Integer Temp2;
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
    Integer Gcd = new Integer();
    Integer Prime1 = new Integer();
    Integer Prime2 = new Integer();
    Integer PubKeyN = new Integer();
    Integer PhiN = new Integer();
    Integer PubKExponent = new Integer();
    Integer PrivKInverseExponent = new Integer();
    Integer Quotient = new Integer();
    Integer Remainder = new Integer();
    Integer Test1 = new Integer();
    Integer ToEncrypt = new Integer();
    Integer PlainTextNumber = new Integer();
    Integer CipherTextNumber = new Integer();

    int TestLoops = 0;

    // 65537 is a prime.
    // Common exponent for RSA.  It is 2^16 + 1.
    const uint PubKeyExponentUint = 65537;
    PubKExponent.SetFromULong( PubKeyExponentUint );
    const int RandomIndex = 1024 / 32;
    // const int RandomIndex = 768 / 32;
    // const int RandomIndex = 512 / 32;
    int ShowBits = RandomIndex * 32;
    Worker.ReportProgress( 0, "Bits size is: " + ShowBits.ToString());

    // ulong Loops = 0;
    while( true )
    // for( int Count = 0; Count < 1000; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      Thread.Sleep( 1 ); // Give up the time slice.  Let other things run.

      // Make two prime factors.
      if( !MakeAPrime( Prime1, RandomIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      if( !MakeAPrime( Prime2, RandomIndex, 20 ))
        return;

      if( Worker.CancellationPending )
        return;

      // This is extremely unlikely.
      IntMath.GreatestCommonDivisor( Prime1, Prime2, Gcd );
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
      IntMath.GreatestCommonDivisor( Prime1, PubKExponent, Gcd );
      if( !Gcd.IsOne())
        {
        Worker.ReportProgress( 0, "They had a GCD with PubKExponent: " + IntMath.ToString10( Gcd ));
        continue;
        }

      if( Worker.CancellationPending )
        return;

      IntMath.GreatestCommonDivisor( Prime2, PubKExponent, Gcd );
      if( !Gcd.IsOne())
        {
        Worker.ReportProgress( 0, "2) They had a GCD with PubKExponent: " + IntMath.ToString10( Gcd ));
        continue;
        }

      // These checks should be more thorough.

      if( Worker.CancellationPending )
        return;

      Worker.ReportProgress( 0, "Prime 1:" );
      Worker.ReportProgress( 0, IntMath.ToString10( Prime1 ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Prime 2:" );
      Worker.ReportProgress( 0, IntMath.ToString10( Prime2 ));
      Worker.ReportProgress( 0, " " );

      PubKeyN.Copy( Prime1 );
      IntMath.Multiply( PubKeyN, Prime2 );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PubKeyN:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PubKeyN ));
      Worker.ReportProgress( 0, " " );
    
      // What about if these aren't really prime?
      // Then this isn't really Phi of N.
      PhiN.Copy( Prime1 );
      IntMath.SubtractULong( PhiN, 1 );
      Temp1.Copy( Prime2 );
      IntMath.SubtractULong( Temp1, 1 );
      IntMath.Multiply( PhiN, Temp1 );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "PhiN:" );
      Worker.ReportProgress( 0, IntMath.ToString10( PhiN ));
      Worker.ReportProgress( 0, " " );

      // PhiN should not be made from only small factors, so verify that.

      // PrivKInverseExponent is the multiplicative inverse of PubKExponent
      // mod PhiN.

      // Solve this problem for the multiplicative inverse:
      // PubKExponent * X = 1 mod PhiN.
      // This means that 
      // (PubKExponent * X) = (Y * PhiN) + 1
      // X is less than PhiN.
      // So Y is less than PubKExponent.
      // Y can't be zero.

      // If this equation can be solved then it can be solved modulo
      // any number.  So it has to be solvable mod PubKExponent.
      // See: Hasse Principle.

      // (Y * PhiN) + 1 mod PubKExponent has to be zero if Y is a solution.
      ulong PhiNModPubKey = IntMath.GetMod32( PhiN, 65537 );
      Worker.ReportProgress( 0, "PhiNModPubKey: " + PhiNModPubKey.ToString( "N0" ));

      if( Worker.CancellationPending )
        return;

      PrivKInverseExponent.SetToZero();
      for( uint Y = 1; Y < PubKeyExponentUint; Y++ )
        {
        ulong X = (ulong)Y * (ulong)PhiNModPubKey;
        X++; // Add 1 to it for (Y * PhiN) + 1.
        // X = X % PubKExponent;
        X = X % PubKeyExponentUint;
        
        if( X == 0 )
          {
          if( Worker.CancellationPending )
            return;

          Worker.ReportProgress( 0, "Found Y at: " + Y.ToString( "N0" ));

          PrivKInverseExponent.Copy( PhiN );
          IntMath.MultiplyULong( PrivKInverseExponent, Y );
          PrivKInverseExponent.AddULong( 1 );

          IntMath.Divide( PrivKInverseExponent, PubKExponent, Quotient, Remainder );
          if( !Remainder.IsZero())
            {
            Worker.ReportProgress( 0, "This can't happen. !Remainder.IsZero()" );
            return;
            }

          PrivKInverseExponent.Copy( Quotient );
          Worker.ReportProgress( 0, "PrivKInverseExponent: " + IntMath.ToString10( PrivKInverseExponent ));
          break;
          }
        }

      if( PrivKInverseExponent.IsZero())
        continue;

      if( Worker.CancellationPending )
        return;

      Test1.Copy( PrivKInverseExponent );
      IntMath.MultiplyULong( Test1, PubKeyExponentUint );
      IntMath.Divide( Test1, PhiN, Quotient, Remainder );
      // This Remainder should be 1.
      Worker.ReportProgress( 0, "Remainder should be 1: " + IntMath.ToString10( Remainder ));

      if( Worker.CancellationPending )
        return;

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

      //                                                             1         2         3
      //                                                    12345678901234567890123456789012345
      // ToEncrypt.SetFromAsciiString( TestLoops.ToString() + "Testing this string" + TestLoops.ToString() );
      // Worker.ReportProgress( 0, "Ascii string is: " + ToEncrypt.GetAsciiString() );

      TestLoops++;

      // Do padding.

      PlainTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Before encrypting number: " + IntMath.ToString10( ToEncrypt ));
      Worker.ReportProgress( 0, " " );

      IntMath.ModularPower2( ToEncrypt, PubKExponent, PubKeyN );
      if( Worker.CancellationPending )
        return;

      CipherTextNumber.Copy( ToEncrypt );

      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Encrypted number: " + IntMath.ToString10( CipherTextNumber ));
      Worker.ReportProgress( 0, " " );

      // There are ways to optimize this decryption process.
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

      Worker.ReportProgress( 1, "Prime1: " + IntMath.ToString10( Prime1 ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "Prime2: " + IntMath.ToString10( Prime2 ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "PubKeyN: " + IntMath.ToString10( PubKeyN ));
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 1, "PrivKInverseExponent: " + IntMath.ToString10( PrivKInverseExponent ));
      return; // Comment this out to just leave it while( true ) for testing.
      }
    }




  }
}



