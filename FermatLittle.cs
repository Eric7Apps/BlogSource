// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Collections.Generic;
using System.Text;
// using System.Threading.Tasks;

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


  }
}
