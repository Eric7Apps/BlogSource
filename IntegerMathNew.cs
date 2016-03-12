// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{
  class IntegerMathNew
  {
  private IntegerMath IntMath;
  private Integer[] BaseArrayP;
  private Integer[] BaseArrayQ;
  private Integer[] GeneralBaseArray;
  private Integer[] BaseWorkArray1;
  private Integer Quotient;
  private Integer Remainder;
  private Integer TempForModPower;
  private Integer TestForModPower;
  private Integer XForModPower;
  private Integer ExponentCopy;
  private Integer TestForModInverse1;
  private Integer AccumulateBase;
  private int MaxModPowerIndex = 0;



  internal IntegerMathNew( IntegerMath UseIntMath )
    {
    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    XForModPower = new Integer();
    ExponentCopy = new Integer();
    TempForModPower = new Integer();
    TestForModPower = new Integer();
    AccumulateBase = new Integer();
    TestForModInverse1 = new Integer();
    }



  // This is the standard modular power algorithm that
  // you could find in any reference, but its use of
  // the new modular reduction algorithm is new.
  // The square and multiply method is in Wikipedia:
  // https://en.wikipedia.org/wiki/Exponentiation_by_squaring
  // x^n = (x^2)^((n - 1)/2) if n is odd.
  // x^n = (x^2)^(n/2)       if n is even.
  internal void ModularPower( Integer Result, Integer Exponent, Integer Modulus ) // , bool DoQuotientTest )
    {
    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( Modulus ))
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

    if( Modulus.ParamIsGreater( Result ))
      {
      // throw( new Exception( "This is not supposed to be input for RSA plain text." ));
      IntMath.Divide( Result, Modulus, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    SetupGeneralBaseArray( Modulus );

    XForModPower.Copy( Result );
    ExponentCopy.Copy( Exponent );
    int TestIndex = 0;
    Result.SetFromULong( 1 );
    while( true )
      {
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 ) // If the bottom bit is 1.
        {
        IntMath.Multiply( Result, XForModPower );
        ModularReduction( TempForModPower, Result );
        Result.Copy( TempForModPower );
        }

      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( ExponentCopy.IsZero())
        break;

      // Square it.
      IntMath.Multiply( XForModPower, XForModPower );
      ModularReduction( TempForModPower, XForModPower );
      XForModPower.Copy( TempForModPower );
      }

    // When ModularReduction() gets called it multiplies a base number
    // by a uint sized digit.  So that can make the result one digit bigger
    // than GeneralBase.  Then when they are added up you can get carry
    // bits that can make it a little bigger.
    int HowBig = Result.GetIndex() - Modulus.GetIndex();
    // if( HowBig > 1 )
      // throw( new Exception( "This does happen. Diff: " + HowBig.ToString() ));

    if( HowBig > 2 )
      throw( new Exception( "The never happens. Diff: " + HowBig.ToString() ));

    ModularReduction( TempForModPower, Result );
    Result.Copy( TempForModPower );

    /*
    if( DoQuotientTest )
      {
      // Assume I don't know what the quotient is but I want to find
      // out what it is.  In other words I want to find the complete 
      // output (before that Divide() happens at the end) of
      // ModularPower() when I know the CipherText.

      ulong MostQuotientCanPossiblyBe = (ulong)Modulus.GetIndex() * 2;
      MostQuotientCanPossiblyBe *= 0xFFFFFFFFUL; // Times the most any one digit can be.
      StatusString += "MostQuotientCanPossiblyBe: " + MostQuotientCanPossiblyBe.ToString( "N0" ) + "\r\n";

      // The idea that a mathematical system is closed under addition and
      // multiplication is true here, and the point where it gets closed is
      // at the maximum value of this quotient.

      // The most the quotient could possibly ever be is proportional
      // to the digit size.  Compare this to the size of the quotient in 
      // CRTMath.ModularPower().  Also, if you look in the ECInteger.java
      // file you'll see that it uses 24 bit digits.  Those numbers would
      // have a smaller quotient here.  And using 16 bit digits would
      // make this quotient even smaller.  But it requires more
      // additions and multiplications to be done in ModularReduction().
      // Using digits of size 0xFFFF means Modulus.GetIndex() is twice
      // as big.  Four times GetIndex() times 0xFFFF is a much smaller
      // quotient.

      // Let's just say I got the public key modulus and exponent from the
      // TLS ServerCertificate message, and I got the
      // CipherText from the TLS ClientKeyExchange message.

      ECTime FindQuotientTime = new ECTime();
      FindQuotientTime.SetToNow();
      Integer ModulusMultiple = new Integer();
      Integer CipherText = new Integer();
      Integer ResultBeforeDivide = new Integer();

      ResultBeforeDivide.Copy( Result );
      Divide( Result, Modulus, Quotient, Remainder );

      // So assume I have the CipherText value, but not the quotient.
      CipherText.Copy( Remainder );

      // It takes less than one second to find this quotient
      // in CRTMath.ModularPower().  But by cryptographic standards
      // for "brute force" this would not take long either.
      for( ulong Count = 0; Count < MostQuotientCanPossiblyBe; Count++ )
        {
        if( Cancelled )
          break;

        ModulusMultiple.Copy( Modulus );
        MultiplyULong( ModulusMultiple, Count );
        ModulusMultiple.Add( CipherText );
        if( Result.IsEqual( ModulusMultiple ))
         {
          StatusString += "\r\n\r\n\r\nFound the quotient matching this CipherText at: " + Count.ToString( "N0" ) + "\r\n";
          StatusString += "It took " + FindQuotientTime.GetSecondsToNow().ToString( "N1" ) + " seconds to find it.\r\n\r\n\r\n";
          break;
          }
        }
      }
    else
      { */
      // Get the Quotient and Remainder this way, since I 
      // know the Result after the modular reductions, and
      // the Modulus.
      IntMath.Divide( Result, Modulus, Quotient, Remainder );
      
      // }

    Result.Copy( Remainder );
    }



  // Reverse this?
  // internal void ReverseModularPower( Integer Result, Integer Exponent, Integer Modulus )
  // What does it mean to have a discrete logarithm in
  // this finite system?



  // To use when the base array is pre-set for PrimeP.
  internal void ModularPowerModPrimeP( Integer Result, Integer Exponent, Integer PrimeP )
    {
    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( PrimeP ))
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

    if( PrimeP.ParamIsGreater( Result ))
      {
      IntMath.Divide( Result, PrimeP, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    XForModPower.Copy( Result );
    ExponentCopy.Copy( Exponent );

    int TestIndex = 0;
    Result.SetFromULong( 1 );
    while( !ExponentCopy.IsZero())
      {
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 )
        {
        IntMath.Multiply( Result, XForModPower );
        IntMath.SubtractULong( ExponentCopy, 1 );
        if( PrimeP.ParamIsGreater( Result ))
          {
          TestForModPower.Copy( Result );
          TestIndex = AddByBaseArraysP( TestForModPower, Result );
          if( TestIndex > MaxModPowerIndex )
            MaxModPowerIndex = TestIndex;

          Result.Copy( TestForModPower );
          // Divide( Result, PrimeP, Quotient, Remainder );
          // Result.Copy( Remainder );
          }
        }

      // Square it.
      IntMath.Multiply( XForModPower, XForModPower );
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( PrimeP.ParamIsGreater( XForModPower ))
        {
        TestForModPower.Copy( XForModPower );
        TestIndex = AddByBaseArraysP( TestForModPower, XForModPower );
        if( TestIndex > MaxModPowerIndex )
          MaxModPowerIndex = TestIndex;

        XForModPower.Copy( TestForModPower );
        // Divide( XForModPower, PrimeP, Quotient, Remainder );
        // XForModPower.Copy( Remainder );
        }
      }

    IntMath.Divide( Result, PrimeP, Quotient, Remainder );
    Result.Copy( Remainder );
    }



  // To use when the base array is pre-set for PrimeQ.
  internal void ModularPowerModPrimeQ( Integer Result, Integer Exponent, Integer PrimeQ )
    {
    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( PrimeQ ))
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

    if( PrimeQ.ParamIsGreater( Result ))
      {
      IntMath.Divide( Result, PrimeQ, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    XForModPower.Copy( Result );
    ExponentCopy.Copy( Exponent );

    int TestIndex = 0;
    Result.SetFromULong( 1 );
    while( !ExponentCopy.IsZero())
      {
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 )
        {
        IntMath.Multiply( Result, XForModPower );
        IntMath.SubtractULong( ExponentCopy, 1 );
        if( PrimeQ.ParamIsGreater( Result ))
          {
          TestForModPower.Copy( Result );
          TestIndex = AddByBaseArraysQ( TestForModPower, Result );
          if( TestIndex > MaxModPowerIndex )
            MaxModPowerIndex = TestIndex;

          Result.Copy( TestForModPower );
          // Divide( Result, PrimeQ, Quotient, Remainder );
          // Result.Copy( Remainder );
          }
        }

      // Square it.
      IntMath.Multiply( XForModPower, XForModPower );
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( PrimeQ.ParamIsGreater( XForModPower ))
        {
        TestForModPower.Copy( XForModPower );
        TestIndex = AddByBaseArraysQ( TestForModPower, XForModPower );
        if( TestIndex > MaxModPowerIndex )
          MaxModPowerIndex = TestIndex;

        XForModPower.Copy( TestForModPower );
        // Divide( XForModPower, PrimeQ, Quotient, Remainder );
        // XForModPower.Copy( Remainder );
        }
      }

    IntMath.Divide( Result, PrimeQ, Quotient, Remainder );
    Result.Copy( Remainder );
    }




  internal int GetMaxModPowerIndex()
    {
    return MaxModPowerIndex;
    }



  internal bool FindMultiplicativeInverseSmall( Integer ToFind, Integer KnownNumber, Integer Modulus, BackgroundWorker Worker )
    {
    // This method is for: KnownNumber * ToFind = 1 mod Modulus
    // An example:
    // PublicKeyExponent * X = 1 mod PhiN.
    // PublicKeyExponent * X = 1 mod (P - 1)(Q - 1).
    // This means that 
    // (PublicKeyExponent * X) = (Y * PhiN) + 1
    // X is less than PhiN.
    // So Y is less than PublicKExponent.
    // Y can't be zero.
    // If this equation can be solved then it can be solved modulo
    // any number.  So it has to be solvable mod PublicKExponent.
    // See: Hasse Principle.
    // This also depends on the idea that the KnownNumber is prime and
    // that there is one unique modular inverse.

    // if( !KnownNumber-is-a-prime )
    //    then it won't work.

    if( !KnownNumber.IsULong())
      throw( new Exception( "FindMultiplicativeInverseSmall() was called with too big of a KnownNumber." ));

    ulong KnownNumberULong  = KnownNumber.GetAsULong();
    //                       65537
    if( KnownNumberULong > 1000000 )
      throw( new Exception( "KnownNumberULong > 1000000. FindMultiplicativeInverseSmall() was called with too big of an exponent." ));

    // (Y * PhiN) + 1 mod PubKExponent has to be zero if Y is a solution.
    ulong ModulusModKnown = IntMath.GetMod32( Modulus, KnownNumberULong );
    Worker.ReportProgress( 0, "ModulusModExponent: " + ModulusModKnown.ToString( "N0" ));
    if( Worker.CancellationPending )
      return false;

    // Y can't be zero.
    // The exponent is a small number like 65537.
    for( uint Y = 1; Y < (uint)KnownNumberULong; Y++ )
      {
      ulong X = (ulong)Y * ModulusModKnown;
      X++; // Add 1 to it for (Y * PhiN) + 1.
      X = X % KnownNumberULong;
      if( X == 0 )
        {
        if( Worker.CancellationPending )
          return false;

        // What is PhiN mod 65537?
        // That gives me Y.
        // The private key exponent is X*65537 + ModPart
        // The CipherText raised to that is the PlainText.

        // P + zN = C^(X*65537 + ModPart)
        // P + zN = C^(X*65537)(C^ModPart)
        // P + zN = ((C^65537)^X)(C^ModPart)

        Worker.ReportProgress( 0, "Found Y at: " + Y.ToString( "N0" ));
        ToFind.Copy( Modulus );
        IntMath.MultiplyULong( ToFind, Y );
        ToFind.AddULong( 1 );
        IntMath.Divide( ToFind, KnownNumber, Quotient, Remainder );
        if( !Remainder.IsZero())
          throw( new Exception( "This can't happen. !Remainder.IsZero()" ));

        ToFind.Copy( Quotient );
        // Worker.ReportProgress( 0, "ToFind: " + ToString10( ToFind ));
        break;
        }
      }

    if( Worker.CancellationPending )
      return false;

    TestForModInverse1.Copy( ToFind );
    IntMath.MultiplyULong( TestForModInverse1, KnownNumberULong );
    IntMath.Divide( TestForModInverse1, Modulus, Quotient, Remainder );
    if( !Remainder.IsOne())
      {
      // The definition is that it's congruent to 1 mod the modulus,
      // so this has to be 1.

      // I've only seen this happen once.  Were the primes P and Q not
      // really primes?
      throw( new Exception( "This is a bug. Remainder has to be 1: " + IntMath.ToString10( Remainder ) ));
      }

    return true;
    }



  internal void SetupBaseArrays( Integer PrimeP, Integer PrimeQ, BackgroundWorker Worker )
    {
    // Normally this would only get called when you start up your server since
    // PrimeP and PrimeQ almost never change.

    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "Top of SetupBaseArrays." );

    // If you multiply two 32-digit numbers together that makes a number
    // that's 64 digits.
    int HowMany = (PrimeP.GetIndex() * 2) + 10; // PrimeQ is the same length.
    BaseArrayP = new Integer[HowMany];
    BaseArrayQ = new Integer[HowMany];
    BaseWorkArray1 = new Integer[HowMany];

    Integer Base = new Integer();
    Integer BaseValue = new Integer();
    Base.SetFromULong( 256 );
    IntMath.MultiplyUInt( Base, 256 );
    IntMath.MultiplyUInt( Base, 256 );
    IntMath.MultiplyUInt( Base, 256 );
    // Worker.ReportProgress( 0, "Base hex: " + Base.GetAsHexString());
    // It is 0x100000000. 0x1 00 00 00 00
    // Which is 4,294,967,296.

    BaseValue.SetFromULong( 1 );
    for( int Count = 0; Count < HowMany; Count++ )
      {
      // Worker.ReportProgress( 0, " " );
      // Worker.ReportProgress( 0, "Count: " + Count.ToString() );
      // Worker.ReportProgress( 0, "BaseValue: " + ToString10( BaseValue ));

      BaseArrayP[Count] = new Integer();
      BaseArrayQ[Count] = new Integer();
      BaseWorkArray1[Count] = new Integer();

      IntMath.Divide( BaseValue, PrimeP, Quotient, Remainder );
      BaseArrayP[Count].Copy( Remainder ); // The base value mod PrimeP
      // Worker.ReportProgress( 0, "BaseArrayP: " + ToString10( BaseArrayP[Count] ));

      IntMath.Divide( BaseValue, PrimeQ, Quotient, Remainder );
      BaseArrayQ[Count].Copy( Remainder ); // The base value mod PrimeQ

      IntMath.Multiply( BaseValue, Base );
      }
    }



  internal void SetupGeneralBaseArray( Integer GeneralBase )
    {
    // The input to the accumulator can be twice the bit length of GeneralBase.
    int HowMany = ((GeneralBase.GetIndex() + 1) * 2) + 10; // Plus some extra for carries...
    if( GeneralBaseArray == null )
      {
      GeneralBaseArray = new Integer[HowMany];
      }

    if( GeneralBaseArray.Length < HowMany )
      {
      GeneralBaseArray = new Integer[HowMany];
      }

    Integer Base = new Integer();
    Integer BaseValue = new Integer();
    Base.SetFromULong( 256 ); // 0x100
    IntMath.MultiplyUInt( Base, 256 ); // 0x10000
    IntMath.MultiplyUInt( Base, 256 ); // 0x1000000
    IntMath.MultiplyUInt( Base, 256 ); // 0x100000000 is the base of this number system.

    BaseValue.SetFromULong( 1 );
    for( int Count = 0; Count < HowMany; Count++ )
      {
      if( GeneralBaseArray[Count] == null )
        GeneralBaseArray[Count] = new Integer();

      IntMath.Divide( BaseValue, GeneralBase, Quotient, Remainder );
      GeneralBaseArray[Count].Copy( Remainder );

      // If this ever happened it would be a bug because
      // the point of copying the Remainder in to BaseValue
      // is to keep it down to a reasonable size.
      // And Base here is one bit bigger than a uint.
      if( Base.ParamIsGreater( Quotient ))
        throw( new Exception( "Bug. This never happens: Base.ParamIsGreater( Quotient )" ));

      // Keep it to mod GeneralBase so Divide() doesn't
      // have to do so much work.
      BaseValue.Copy( Remainder );

      IntMath.Multiply( BaseValue, Base );
      }
    }



  private int AddByBaseArraysP( Integer Result, Integer ToAdd )
    {
    if( BaseArrayP == null )
      throw( new Exception( "SetupBaseArrays() should have already been called once when the server started." ));

    Result.SetToZero();
    for( int Count = 0; Count <= ToAdd.GetIndex(); Count++ )
      {
      BaseWorkArray1[Count].Copy( BaseArrayP[Count] );
      IntMath.MultiplyUInt( BaseWorkArray1[Count], ToAdd.GetD( Count ));
      Result.Add( BaseWorkArray1[Count] );
      }

    return Result.GetIndex();
    }




  private int AddByBaseArraysQ( Integer Result, Integer ToAdd )
    {
    if( BaseArrayQ == null )
      throw( new Exception( "SetupBaseArrays() should have already been called once when the server started." ));

    Result.SetToZero();
    for( int Count = 0; Count <= ToAdd.GetIndex(); Count++ )
      {
      BaseWorkArray1[Count].Copy( BaseArrayQ[Count] );
      IntMath.MultiplyUInt( BaseWorkArray1[Count], ToAdd.GetD( Count ));
      Result.Add( BaseWorkArray1[Count] );
      }

    return Result.GetIndex();
    }



  // Copyright Eric Chauvin 2015.
  internal int ModularReduction( Integer Result, Integer ToReduce )
    {
    try
    {
    if( GeneralBaseArray == null )
      throw( new Exception( "SetupGeneralBaseArray() should have already been called." ));

    Result.SetToZero();

    int HowManyToAdd = ToReduce.GetIndex() + 1;
    int BiggestIndex = 0;
    for( int Count = 0; Count < HowManyToAdd; Count++ )
      {
      // The size of the numbers in GeneralBaseArray are all less than
      // the size of GeneralBase.
      // This multiplication by a uint is with a number that is not bigger
      // than GeneralBase.  Compare this with the two full Muliply()
      // calls done on each digit of the quotient in LongDivide3().

      // AccumulateBase is set to a new value here.
      int CheckIndex = IntMath.MultiplyUIntFromCopy( AccumulateBase, GeneralBaseArray[Count], ToReduce.GetD( Count ));

      if( CheckIndex > BiggestIndex )
        BiggestIndex = CheckIndex;

      Result.Add( AccumulateBase );
      }

    return Result.GetIndex();
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ModularReduction(): " + Except.Message ));
      }
    }


  }
}

