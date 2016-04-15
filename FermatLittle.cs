// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com



using System;
using System.Collections.Generic;
using System.Text;


namespace ExampleServer
{
  class FermatLittle
  {
  private MainForm MForm;
  private IntegerMath IntMath;


  private FermatLittle()
    {
    }



  internal FermatLittle( MainForm UseForm )
    {
    MForm = UseForm;

    IntMath = new IntegerMath();
    }


  private void ShowStatus( string ToShow )
    {
    if( MForm == null )
      return;

    MForm.ShowStatus( ToShow );
    }


  internal void TestDigits()
    {
    try
    {
    // With base 10:
    // a*10^3 + b*10^2 + c*10^1 + d*10^0
    // 10^3 = 1000

    // Fermat says 3 is a prime so:
    // 10^(3 - 1) - 1 = x * 3
    // 10^2 = 100
    // 100 - 1 = 99
    // 99 is a multiple of 3.

    // 7 is a prime so:
    // 10^(7 - 1) - 1 = x * 7
    // 10^6 = 1000000
    // 1000000 - 1 = 999999
    int TestNines = 999999;
    int Test7 = TestNines % 7;
    ShowStatus( "TestNines % 7 is: " + Test7.ToString());

    // uint Base = 10;
    // ulong BigBase = Base;
    // uint Base = 9;
    // ulong BigBase = Base;
    uint Base = 2 * 3;
    ulong BigBase = Base;
    for( uint Count = 2; Count < 20; Count++ )
      {
      ShowStatus( " " );
      // At Count = 2 BigBase will be 100, or 10^2.
      BigBase = checked( BigBase * Base );
      uint Exponent = Count + 1;
      if( (Base % Exponent) == 0 )
        {
        ShowStatus( Exponent.ToString() + " divides the base." );
        continue;
        }

      // At Count = 2 Exponent is 3.
      ulong Minus1 = BigBase - 1;
      ulong ModExponent = Minus1 % Exponent;
      if( ModExponent != 0 )
        ShowStatus( Exponent.ToString() + " is not a prime." );
      else
        ShowStatus( Exponent.ToString() + " might or might not be a prime." );

      uint FirstFactor = IntMath.GetFirstPrimeFactor( Exponent );
      if( (FirstFactor == 0) ||
          (FirstFactor == Exponent))
        {
        ShowStatus( Exponent.ToString() + " is a prime." );
        }
      else
        {
        ShowStatus( Exponent.ToString() + " is composite with a factor of " + FirstFactor.ToString() );
        }
      }

    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in TestDigits()." );
      ShowStatus( Except.Message );
      }
    }



  internal void TestBigDigits()
    {
    try
    {
    uint Base = 2 * 3 * 5;
    Integer BigBase = new Integer();
    Integer Minus1 = new Integer();
    Integer IntExponent = new Integer();
    Integer IntBase = new Integer();
    Integer Gcd = new Integer();

    BigBase.SetFromULong( Base );
    IntBase.SetFromULong( Base );

    for( uint Count = 2; Count < 200; Count++ )
      {
      // At Count = 2 BigBase will be 100, or 10^2.
      IntMath.MultiplyULong( BigBase, Base );
      uint Exponent = Count + 1;
      IntExponent.SetFromULong( Exponent );
      IntMath.GreatestCommonDivisor( IntBase, IntExponent, Gcd );
      if( !Gcd.IsOne() )
        {
        // ShowStatus( Exponent.ToString() + " has a factor in common with base." );
        continue;
        }

      Minus1.Copy( BigBase );
      IntMath.SubtractULong( Minus1, 1 );

      ShowStatus( " " );
      ulong ModExponent = IntMath.GetMod32( Minus1, Exponent );
      if( ModExponent != 0 )
        ShowStatus( Exponent.ToString() + " is not a prime." );
      else
        ShowStatus( Exponent.ToString() + " might or might not be a prime." );

      uint FirstFactor = IntMath.GetFirstPrimeFactor( Exponent );
      if( (FirstFactor == 0) ||
          (FirstFactor == Exponent))
        {
        ShowStatus( Exponent.ToString() + " is a prime." );
        }
      else
        {
        ShowStatus( Exponent.ToString() + " is composite with a factor of " + FirstFactor.ToString() );
        }
      }

    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in TestDigits()." );
      ShowStatus( Except.Message );
      }
    }


  internal void GetMaximumValues()
    {
    string RSA2048 = "2519590847565789349402718324004839857142928212620403202777713783604366202070" +
           "7595556264018525880784406918290641249515082189298559149176184502808489120072" +
           "8449926873928072877767359714183472702618963750149718246911650776133798590957" +
           "0009733045974880842840179742910064245869181719511874612151517265463228221686" +
           "9987549182422433637259085141865462043576798423387184774447920739934236584823" +
           "8242811981638150106748104516603773060562016196762561338441436038339044149526" +
           "3443219011465754445417842402092461651572335077870774981712577246796292638635" +
           "6373289912154831438167899885040445364023527381951378636564391212010397122822" +
           "120720357";

    Integer BigRSA = new Integer();
    IntMath.SetFromString( BigRSA, RSA2048 );
    // Do a basic sanity-check just to make sure the
    // RSA number was copied right.
    if( 0 != IntMath.IsDivisibleBySmallPrime( BigRSA ))
      throw( new Exception( "BigRSA was not copied right." ));

    // One factor is smaller than the square root.
    // So it's the biggest one that's less than the
    // square root.
    Integer BiggestFactor = new Integer();
    IntMath.SquareRoot( BigRSA, BiggestFactor );
    Integer BigBase = MakeBigBase( BiggestFactor );
    if( BigBase == null )
      {
      ShowStatus( "BigBase was null." );
      return;
      }
    }



  private Integer MakeBigBase( Integer Max )
    {
    Integer Base = new Integer();
    Base.SetFromULong( 2 );
    Integer LastBase = new Integer();
    // Start at the prime 3.
    for( int Count = 1; Count < IntegerMath.PrimeArrayLength; Count++ )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      IntMath.MultiplyULong( Base, Prime );
      if( Max.ParamIsGreater( Base ))
        return LastBase;

      LastBase.Copy( Base );
      }

    return null;
    }


  internal void TestPrimes()
    {
    uint BiggestPrime = IntMath.GetPrimeAt( 20 );

    for( int Count = 1; Count < 40; Count++ )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      ShowStatus( " " );
      ShowStatus( "Prime: 0x" + Prime.ToString( "X8" ));
      for( ulong X = 0; X < 1000000000L; X++ )
        {
        if( (X & 0x3FFFFFF) == 0 )
          {
          // ShowStatus( "CheckEvents: " + X.ToString() );
          if( !MForm.CheckEvents())
            return;

          }

        ulong Test = Prime * X;
        if( IsAllOnes( Test ))
          {
          ShowStatus( "X: 0x" + X.ToString( "X8" ));
          ShowStatus( "Test: 0x" + Test.ToString( "X8" ));
          break; // Found one.
          }
        }
      }

    ShowStatus( " " );
    ShowStatus( "Finished testing." );
    }



  internal void GetXForPrimes()
    {
    uint BiggestPrime = IntMath.GetPrimeAt( 20 );

    for( int Count = 1; Count < 20; Count++ )
      {
      uint Prime = IntMath.GetPrimeAt( Count );
      ShowStatus( " " );
      ShowStatus( "Prime: 0x" + Prime.ToString( "X8" ));
      for( uint X = 0; X < (256 * 256); X++ )
        {
        ulong Test = Prime * X;
        // if( IsAllOnes( Test & 0xF ))
        if( (Test & 0xFFFF) == 0xFFFF )
          {
          ShowStatus( "X: 0x" + X.ToString( "X8" ));
          ShowStatus( "Test: 0x" + Test.ToString( "X8" ));
          break; // Found one.
          }
        }
      }

    ShowStatus( " " );
    ShowStatus( "Finished testing." );
    }



  private bool IsAllOnes( ulong Test )
    {
    if( Test == 0 )
      return false;

    for( int Count = 0; Count < 64; Count++ )
      {
      if( (Test & 1) != 1 )
        return false;

      Test >>= 1;
      if( Test == 0 )
        return true;

      }

    return true;
    }



  }
}
