/*

Obsolete and not used anymore.


// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // For Sleep().
using System.ComponentModel; // BackgroundWorker
using System.Security.Cryptography;


namespace ExampleServer
{
  class LowExponentBackground
  {
  private IntegerMath IntMath;
  private ECTime StartTime;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private RNGCryptoServiceProvider RngCsp;
  private const int PrimeIndex = 1; // Approximmately 64-bit primes, the size of a ulong.
  private int PrimesInBase = 0;
  // private const int PrimeIndex = 15; // Approximmately 512-bit primes.
  // private const int PrimeIndex = 31; // Approximmately 1024-bit primes.
  // private const int PrimeIndex = 63; // Approximmately 2048-bit primes.
  // private const int PrimeIndex = 127; // Approximmately 4096-bit primes.



  private LowExponentBackground()
    {
    }



  internal LowExponentBackground( BackgroundWorker UseWorker, LowExponentWorkerInfo UseWInfo )
    {
    Worker = UseWorker;
    WInfo = UseWInfo;

    StartTime = new ECTime();
    StartTime.SetToNow();

    RngCsp = new RNGCryptoServiceProvider();
    IntMath = new IntegerMath();
    string ShowS = IntMath.GetStatusString();
    Worker.ReportProgress( 0, ShowS );

    Quotient = new Integer();
    Remainder = new Integer();
    }



  internal void FreeEverything()
    {
    if( RngCsp != null )
      {
      RngCsp.Dispose();
      RngCsp = null;
      }
    }



  private void TestSmallExponents()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of TestSmallExponents()." );

    // Integer ToEncrypt2 = new Integer();

    Integer PubKeyExponent = new Integer();
    // const uint PubKeyExponentUint = 65537; // A very commonly used pubic exponent.
    const uint PubKeyExponentUint = 3; // A very small exponent.
    PubKeyExponent.SetFromULong( PubKeyExponentUint );

    Integer PrimeP = new Integer();
    if( !MakeAPrime( PrimeP, PrimeIndex, 20 ))
      return;

    if( Worker.CancellationPending )
      return;

    Integer PrimeQ = new Integer();
    if( !MakeAPrime( PrimeQ, PrimeIndex, 20 ))
      return;

    Integer PubKeyN = new Integer();
    PubKeyN.Copy( PrimeP );
    IntMath.Multiply( PubKeyN, PrimeQ );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PubKeyN:" );
    Worker.ReportProgress( 0, IntMath.ToString10( PubKeyN ));
    Worker.ReportProgress( 0, " " );

    // Make a random number to test encryption/decryption.
    int HowManyBytes = PrimeIndex * 4;
    byte[] RandBytes = MakeRandomBytes( HowManyBytes );
    if( RandBytes == null )
      {
      Worker.ReportProgress( 0, "Error making random bytes." );
      return;
      }

    Integer ToEncrypt = new Integer();
    if( !ToEncrypt.MakeRandomOdd( PrimeIndex - 1, RandBytes ))
      {
      Worker.ReportProgress( 0, "Error making random number in MakeKeysBackGround.MakeAPrime()." );
      return;
      }

    Integer PlainText1 = new Integer();
    PlainText1.Copy( ToEncrypt );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PlainText1: " + IntMath.ToString10( PlainText1 ));
    Worker.ReportProgress( 0, " " );

    IntMath.IntMathNew.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
    if( Worker.CancellationPending )
      return;

    Integer CipherText1 = new Integer();
    CipherText1.Copy( ToEncrypt );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherText1: " + IntMath.ToString10( CipherText1 ));
    Worker.ReportProgress( 0, " " );

    Integer BigBase = new Integer();
    MakeBigBase( BigBase, PubKeyN );
    Integer PlainText2 = new Integer();
    PlainText2.Copy( BigBase );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PlainText2: " + IntMath.ToString10( PlainText2 ));
    Worker.ReportProgress( 0, " " );

    ToEncrypt.Copy( PlainText2 );
    IntMath.IntMathNew.ModularPower( ToEncrypt, PubKeyExponent, PubKeyN );
    if( Worker.CancellationPending )
      return;

    Integer CipherText2 = new Integer();
    CipherText2.Copy( ToEncrypt );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherText2: " + IntMath.ToString10( CipherText2 ));
    Worker.ReportProgress( 0, " " );

    Integer TempCipher = new Integer();
    TempCipher.Copy( CipherText1 );
    IntMath.Multiply( TempCipher, CipherText2 );

    IntMath.IntMathNew.SetupGeneralBaseArray( PubKeyN );
    Integer ModularReduce = new Integer();
    IntMath.IntMathNew.ModularReduction( ModularReduce, TempCipher );
    // ModularReduce is now a small (one or two digits) multiple of PubKeyN.
    IntMath.Divide( ModularReduce, PubKeyN, Quotient, Remainder );
    Integer CipherTextBoth = new Integer();
    CipherTextBoth.Copy( Remainder );
    if( Worker.CancellationPending )
      return;

    // From Wikipedia:
    // "RSA has the property that the product of two ciphertexts is equal to
    // the encryption of the product of the respective plaintexts. That is
    // m1^e*m2^e ≡ (m1m2)^e (mod n)."

    // CipherTextBoth is: m1^e*m2^e

    // If I take the plain text of my choice (BigBase), and encrypt it, then
    // (m1m2)^e = N*X + CipherBoth, which is congruent to zero base 2 * 3 * 5...
    // Cipher is Cipher1 times Cipher2 mod N.
    // m1^e*m2^e ≡ (m1m2)^e (mod n).
    MakeXFromCipherBoth( PubKeyN, CipherTextBoth, BigBase );

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "End of TestSmallExponents()." );
    Worker.ReportProgress( 0, " " );
    }




  private void MakeXFromCipherBoth( Integer PubKeyN, Integer CipherTextBoth, Integer BigBase )
    {
    // N*X + CipherBoth is congruent to zero mod BigBase.

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "BigBase is: " + IntMath.ToString10( BigBase ));

    Integer CipherBothModBase = new Integer();
    IntMath.Divide( CipherTextBoth, BigBase, Quotient, Remainder );
    CipherBothModBase.Copy( Remainder );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "CipherBothModBase is: " + IntMath.ToString10( CipherBothModBase ));

    Integer PubKeyNModBase = new Integer();
    IntMath.Divide( PubKeyN, BigBase, Quotient, Remainder );
    PubKeyNModBase.Copy( Remainder );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "PubKeyNModBase is: " + IntMath.ToString10( PubKeyNModBase ));

    IntMath.IntMathNew.SetupGeneralBaseArray( BigBase );

    Integer ModularReduce = new Integer();
    Integer TestMod = new Integer();
    Integer TestX = new Integer();
    TestX.SetToZero();
    for( int Count = 0; Count < 1000; Count++ )
      {
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Count: " + Count.ToString() );

      if( Worker.CancellationPending )
        return;

      TestMod.Copy( PubKeyNModBase );
      IntMath.Multiply( TestMod, TestX );
      IntMath.IntMathNew.ModularReduction( ModularReduce, TestMod );
      TestMod.Copy( ModularReduce );
      TestMod.Add( CipherBothModBase );
      IntMath.Divide( TestMod, BigBase, Quotient, Remainder );
      if( Remainder.IsZero())
        {
        Worker.ReportProgress( 0, "Found X at: " + Count.ToString());
        return;
        }

      // if( (TestMod.GetD( 0 ) & 1) == 1 )
        // Worker.ReportProgress( 0, "TestMod is odd." );
      // else
        // Worker.ReportProgress( 0, "TestMod is even." );

      for( int CountP = 1; CountP < PrimesInBase; CountP++ )
        {
        uint Prime = IntMath.GetPrimeAt( CountP );
        uint SmallVal = (uint)IntMath.GetMod32( TestMod, Prime );
        if( SmallVal == 0 )
          Worker.ReportProgress( 0, "Zero mod " + Prime.ToString() + " at: " + Count.ToString() );

        }

      TestX.AddULong( 1 );
      }
    }



  /////////
  private void ShowBinomialCoefficients()
    {
    try
    {
    // The expansion of: (X + Y)^N
    // Binomial coefficient is  N! / (K!*(N - K)!).

    // 0 raised to the 0 power is 1.  It is defined that way.
    // So (0 + 2)^3 has all terms set to zero except for 2^3.

    // 3^7     is congruent to Fixed     mod the Modulus.
    // P^7     is congruent to Something mod the Modulus.
    // (P*3)^7 is congruent to 3^7 * P^7 mod the Modulus.

    // (Fixed * Something)^7 is congruent to 3^7 * P^7 mod the modulus
    // 3^7 * P^7 is the CipherText that I have.
    // ====== What's the multiplicative inverse of Fixed mod the Modulus?

    // Does the multplicative inverse of BigBase do something?
    // What are all its small primes congruent to?

    // (x3 + c)^7 = 

    // 1 *  x3^7 * c^0 +
    // 7 *  x3^6 * c^1 +
    // 21 * x3^5 * c^2 +
    // 35 * x3^4 * c^3 +
    // 35 * x3^3 * c^4 +
    // 21 * x3^2 * c^5 +
    // 7 *  x3^1 * c^6 +
    // 1 *  x3^0 * c^7


    // ((Plain / 3 + Plain % 3)^Exponent) mod Modulus is:

    // 1 *  x^7 * 3^7 * c^0 +
    // 7 *  x^6 * 3^6 * c^1 +
    // 21 * x^5 * 3^5 * c^2 +
    // 35 * x^4 * 3^4 * c^3 +
    // 35 * x^3 * 3^3 * c^4 +
    // 21 * x^2 * 3^2 * c^5 +
    // 7 *  x^1 * 3^1 * c^6 +
    // 1 *  x^0 * 3^0 * c^7

    // There is this relationship between 3 and the Modulus.
    // All of these are 3 times something mod Modulus.
    // 1 *  3^7 * c^0 * x^7 +
    // 7 *  3^6 * c^1 * x^6 +
    // 21 * 3^5 * c^2 * x^5 +
    // 35 * 3^4 * c^3 * x^4 +
    // 35 * 3^3 * c^4 * x^3 +
    // 21 * 3^2 * c^5 * x^2 +
    // 7 *  3^1 * c^6 * x^1 +

    // 1 *  3^0 * c^7 * x^0


    // Which is:

    // 3x * (
    // 1 *  3^6 * c^0 * x^6 +
    // 7 *  3^5 * c^1 * x^5 +
    // 21 * 3^4 * c^2 * x^4 +
    // 35 * 3^3 * c^3 * x^3 +
    // 35 * 3^2 * c^4 * x^2 +
    // 21 * 3^1 * c^5 * x^1 +
    // 7 *  3^0 * c^6 * x^0 ) +
    // c^7
    // mod Modulus.


    uint Exponent = 7;
    ulong ExponentFactorial = GetFactorial( Exponent );
    Worker.ReportProgress( 0, "ExponentFactorial: " + ExponentFactorial.ToString() );

    for( uint Count = 0; Count <= Exponent; Count++ )
      {
      uint K = Count;
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "K: " + K.ToString() );

      ulong KFactorial = GetFactorial( K );
      uint ExponentMinusK = (uint)((int)Exponent - (int)K);
      ulong ExponentMinusKFactorial = GetFactorial( ExponentMinusK );
      ulong Denom = KFactorial * ExponentMinusKFactorial;
      ulong Coefficient = ExponentFactorial / Denom;
      Worker.ReportProgress( 0, "Coefficient: " + Coefficient.ToString() );
      }
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Exception in ShowBinomialCoefficients()." );
      Worker.ReportProgress( 0, Except.Message );
      }
    }
    //////////


  //////////
  private ulong GetFactorial( uint Value )
    {
    if( Value == 0 )
      return 1;

    if( Value == 1 )
      return 1;

    uint Factorial = 1;
    for( uint Count = 2; Count <= Value; Count++ )
      Factorial = Factorial * Count;

    return Factorial;
    }
    /////////



  private void MakeBigBase( Integer Result, Integer Maximum )
    {
    Integer TempBase = new Integer();
    TempBase.SetToOne();
    for( int Count = 0; Count < 10000; Count++ )
      {
      Result.Copy( TempBase );
      // Worker.ReportProgress( 0, "BigBase: " + IntMath.ToString10( Result ));
      IntMath.MultiplyULong( TempBase, IntMath.GetPrimeAt( Count ));
      if( Maximum.ParamIsGreater( TempBase ))
        {
        // Since Count is zero based, thie shows the right number after the
        // max is reached.
        PrimesInBase = Count;
        Worker.ReportProgress( 0, "It took " + PrimesInBase.ToString() + " primes to make BigBase." );
        return; // Leave Result smaller than Maximum.
        }
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



  private void ShowModularValues( Integer ShowFor )
    {
    if( (ShowFor.GetD( 0 ) & 1) == 1 )
      Worker.ReportProgress( 0, "It is odd." );
    else
      Worker.ReportProgress( 0, "It is even." );

    ulong ModVal = IntMath.GetMod32( ShowFor, 3 );
    Worker.ReportProgress( 0, "Mod 3: " + ModVal.ToString());

    ModVal = IntMath.GetMod32( ShowFor, 5 );
    Worker.ReportProgress( 0, "Mod 5: " + ModVal.ToString());

    ModVal = IntMath.GetMod32( ShowFor, 7 );
    Worker.ReportProgress( 0, "Mod 7: " + ModVal.ToString());
    }



  internal void StartTest()
    {
    uint Exponent = 10;
    string ShowS = IntMath.ShowBinomialCoefficients( Exponent );
    Worker.ReportProgress( 0, ShowS );

    // TestSmallExponents();
    // SetupTables( 17 );
    }


  ////////
  internal void SetupTables( ulong Exponent )
    {
    StartTime.SetToNow();

    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Top of SetupTables()." );

    // If it starts out as an odd number then it stays odd.
    // If it starts out as an even number then it stays even.
    // So I don't need 2 in the base.
    uint Base = 3 * 5 * 7 * 11; //  * 13 * 17 * 19 * 23;
    // Base *= 29;

    // 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
    // 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157,
    // 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227,
    // 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283,
    // 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367,
    // 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439,
    // 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509,
    // 521, 523, 541,

    Worker.ReportProgress( 0, "Base is: " + Base.ToString( "N0" ));

    uint TestPublicKeyModBase = 7;
    ModTable3To23 = new ModTable( Worker, IntMath, Base, TestPublicKeyModBase, Exponent );
    ModTable3To23.SetupTable();
    // ModTable3To23.ShowSquareSeries();


    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Finished making all mod tables." );
    Worker.ReportProgress( 0, "Seconds: " + StartTime.GetSecondsToNow().ToString( "N0" ));
    Worker.ReportProgress( 0, " " );
    }
    //////////


  /////////
  internal void ModularPowerSmallExponent( Integer Result, Integer Exponent, Integer GeneralBase, BackgroundWorker Worker )
    {
    // Do small exponents provide security?

    if( !Exponent.IsULong())
      throw( new Exception( "This is not supposed to be used for an exponent this big." ));

    ulong ExponentU = Exponent.GetAsULong();
    //               65537 A commonly used public key exponent.
    if( ExponentU > 123456 )
      throw( new Exception( "The exponent is too big for ModularPowerSmallExponent()." ));

    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( GeneralBase ))
      {
      // It is congruent to zero % ModN.
      Result.SetToZero();
      return;
      }

    // Result is not zero at this point.
    if( Exponent.IsZero() )
      {
      Result.SetFromULong( 1 );
      return;
      }

    // With the RSA Cryptosystem this will not start out being bigger
    // than the modulus.  The plain text input is not bigger than N.
    if( GeneralBase.ParamIsGreater( Result ))
      {
      // throw( new Exception( "This is not supposed to be input for RSA plain text." ));
      IntMath.Divide( Result, GeneralBase, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    IntMath.SetupGeneralBaseArray( GeneralBase );

    Integer OriginalValue = new Integer();
    OriginalValue.Copy( Result );
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "OriginalValue:" );
    ShowModularValues( OriginalValue );

    for( uint Count = 0; Count < (ExponentU - 1); Count++ )
      {
      // Doing the exponent as repeated multiplication is not efficient, but it
      // makes something clear.  How can this be made reversible?
      IntMath.Multiply( Result, OriginalValue );
      Worker.ReportProgress( 0, " " );
      Worker.ReportProgress( 0, "Result at: " + Count.ToString());
      ShowModularValues( Result );

      // If the Modular Reduction wasn't done in this loop then the
      // bit-length of Result would be the bit-length of OriginalValue 
      // times the exponent.  If the exponent was the commonly used
      // public exponent 65537, then that's a really big Result.  Like 
      // 65537 times 2048 bits long.  Too big to work with.  If the
      // exponent was 2048 bits long then the result would definitely
      // be too big to calculate with.

      // But if the exponent is 3, that's not too big to work with.
      // You know what the remainder is (it's the cipher text) and 
      // you know what the base is, it's the public modulus, and you
      // can just guess at a few values of the quotient.
      
      // So the question is, given that information, what is OriginalValue?

      // I don't see how this Modular Reduction is reversible.
      // Given the output, can you find the input?
      // AddByGeneralBaseArrays( TempForModPower, Result );
      // Result.Copy( TempForModPower );

      // Is this Divide reversible if you only know the remainder and the base?
      // IntMath.Divide( Result, GeneralBase, Quotient, Remainder );

      }

    // So this Quotient has only one or two 32-bit digits in it after
    // AddByGeneralBaseArrays().
    IntMath.Divide( Result, GeneralBase, Quotient, Remainder );
    Result.Copy( Remainder );
    }
    //////////


  }
}

*/
