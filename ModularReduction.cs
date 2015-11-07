// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

// This is my new Modular Reduction Algorithm.
// This was put up on GitHub on 11/7/2015.


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExampleServer
{
  class ModularReduction
  {
  private IntegerMath IntMath;
  private Integer[] GeneralBaseArray;
  private Integer[] AccumulateArray;
  private Integer Quotient;
  private Integer Remainder;
  private Integer XForModPower;
  private Integer ExponentCopy;
  private Integer TempForModPower;



  internal ModularReduction()
    {
    IntMath = new IntegerMath();
    XForModPower = new Integer();
    ExponentCopy = new Integer();
    Quotient = new Integer();
    Remainder = new Integer();
    TempForModPower = new Integer();
    }



  internal void ModularPower( Integer Result, Integer Exponent, Integer GeneralBase )
    {
    // The square and multiply method is in Wikipedia:
    // https://en.wikipedia.org/wiki/Exponentiation_by_squaring
    // x^n = (x^2)^((n - 1)/2) if n is odd.
    // x^n = (x^2)^(n/2)       if n is even.

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

    // This could also be called ahead of time if the base (the modulus)
    // doesn't change.  Like when your public key doesn't change.
    SetupGeneralBaseArray( GeneralBase );

    XForModPower.Copy( Result );
    ExponentCopy.Copy( Exponent );
    int TestIndex = 0;
    Result.SetFromULong( 1 );
    while( true )
      {
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 ) // If the bottom bit is 1.
        {
        IntMath.Multiply( Result, XForModPower );
        
        // Modular Reduction:
        AddByGeneralBaseArrays( TempForModPower, Result );
        Result.Copy( TempForModPower );
        }

      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      if( ExponentCopy.IsZero())
        break;

      // Square it.
      IntMath.Multiply( XForModPower, XForModPower );

      // Modular Reduction:
      AddByGeneralBaseArrays( TempForModPower, XForModPower );
      XForModPower.Copy( TempForModPower );
      }

    // When AddByGeneralBaseArrays() gets called it multiplies a number
    // by a uint sized digit.  So that can make the result one digit bigger
    // than GeneralBase.  Then when they are added up you can get carry
    // bits that can make it a little bigger.
    // If by chance you got a carry bit on _every_ addition that was done
    // in AddByGeneralBaseArrays() then this number could increase in size
    // by 1 bit for each addition that was done.  It would take 32 bits of
    // carrying for HowBig to increase by 1.
    // See HowManyToAdd in AddByGeneralBaseArrays() for why this check is done.
    int HowBig = Result.GetIndex() - GeneralBase.GetIndex();
    if( HowBig > 2 ) // I have not seen this happen yet.
      throw( new Exception( "The difference in index size was more than 2. Diff: " + HowBig.ToString() ));

    // So this Quotient has only one or two 32-bit digits in it.
    // And this Divide() is only called once at the end.  Not in the loop.
    IntMath.Divide( Result, GeneralBase, Quotient, Remainder );
    Result.Copy( Remainder );
    }



  // This is the Modular Reduction algorithm.  It reduces
  // ToAdd to Result.
  private int AddByGeneralBaseArrays( Integer Result, Integer ToAdd )
    {
    try
    {
    if( GeneralBaseArray == null )
      throw( new Exception( "SetupGeneralBaseArray() should have already been called." ));

    Result.SetToZero();

    // The Index size of ToAdd is usually double the length of the modulus
    // this is reducing it to.  Like if you multiply P and Q to get N, then
    // the ToAdd that comes in here is about the size of N and the GeneralBase
    // is about the size of P.  So the amount of work done here is proportional
    // to P times N.

    int HowManyToAdd = ToAdd.GetIndex() + 1;
    int BiggestIndex = 0;
    for( int Count = 0; Count < HowManyToAdd; Count++ )
      {
      // The size of the numbers in GeneralBaseArray are all less than
      // the size of GeneralBase.
      // This multiplication by a uint is with a number that is not bigger
      // than GeneralBase.  Compare this with the two full Muliply()
      // calls done on each digit of the quotient in LongDivide3().

      // AccumulateArray[Count] is set to a new value here.
      int CheckIndex = IntMath.MultiplyUIntFromCopy( AccumulateArray[Count], GeneralBaseArray[Count], ToAdd.GetD( Count ));
      if( CheckIndex > BiggestIndex )
        BiggestIndex = CheckIndex;

      }

    // Add all of them up at once.
    AddUpAccumulateArray( Result, HowManyToAdd, BiggestIndex );

    return Result.GetIndex();
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in AddByGeneralBaseArrays(): " + Except.Message ));
      }
    }



  private void AddUpAccumulateArray( Integer Result, int HowManyToAdd, int BiggestIndex )
    {
    try
    {
    for( int Count = 0; Count <= (BiggestIndex + 1); Count++ )
      Result.SetD( Count, 0 );

    Result.SetIndex( BiggestIndex );

    for( int Count = 0; Count < HowManyToAdd; Count++ )
      {
      int HowManyDigits = AccumulateArray[Count].GetIndex() + 1;
      for( int CountDigits = 0; CountDigits < HowManyDigits; CountDigits++ )
        {
        ulong Sum = AccumulateArray[Count].GetD( CountDigits ) + Result.GetD( CountDigits );
        Result.SetD( CountDigits, Sum );
        }
      }

    // This is like ax + by + cz + ... = Result.
    // You know what a, b, c... are.
    // But you don't know what x, y, and z are.
    // So how do you reverse this and get x, y and z?
    // Is this reversible?

    Result.OrganizeDigits();
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in AddUpAccumulateArray(): " + Except.Message ));
      }
    }


  /* This is in the Integer.cs file.  After all the adds are done
     the carry has to be done to reorganize it.  See HowManyToAdd in
     AddByGeneralBaseArrays() to see how much carrying should be done.
  internal void OrganizeDigits()
    {
    // Tell the compiler these aren't going to change for the for-loop.
    int LocalIndex = Index;

    // After they've been added, reorganize it.
    ulong Carry = D[0] >> 32;
    D[0] = D[0] & 0xFFFFFFFF;
    for( int Count = 1; Count <= LocalIndex; Count++ )
      {
      ulong Total = Carry + D[Count];
      D[Count] = Total & 0xFFFFFFFF;
      Carry = Total >> 32;
      }

    if( Carry != 0 )
      {
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "Integer.Add() overflow." ));

      D[Index] = Carry;
      }
    }
    */


  internal void SetupGeneralBaseArray( Integer GeneralBase )
    {
    // The input to the accumulator can be twice the bit length of GeneralBase.
    int HowMany = ((GeneralBase.GetIndex() + 1) * 2) + 10; // Plus some extra for carries...how many?
    if( GeneralBaseArray == null )
      {
      GeneralBaseArray = new Integer[HowMany];
      AccumulateArray = new Integer[HowMany];
      }

    if( GeneralBaseArray.Length < HowMany )
      {
      GeneralBaseArray = new Integer[HowMany];
      AccumulateArray = new Integer[HowMany];
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

      if( AccumulateArray[Count] == null )
        AccumulateArray[Count] = new Integer();

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


  }
}

