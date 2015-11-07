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



  internal ModularReduction()
    {
    IntMath = new IntegerMath();
    Quotient = new Integer();
    Remainder = new Integer();
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
    // to P times N.  (Like Big O of P times N.)

    int HowManyToAdd = ToAdd.GetIndex() + 1;
    int BiggestIndex = 0;
    for( int Count = 0; Count < HowManyToAdd; Count++ )
      {
      // The size of the numbers in GeneralBaseArray are all less than
      // the size of GeneralBase.
      // This multiplication by a uint is with a number that is not bigger
      // than GeneralBase.  Compare this with the two full Muliply()
      // calls done on each digit of the quotient in LongDivide3().

      // BaseAccumulate is set to a new value here.
      int CheckIndex = IntMath.MultiplyUIntFromCopy( AccumulateArray[Count], GeneralBaseArray[Count], ToAdd.GetD( Count ));
      if( CheckIndex > BiggestIndex )
        BiggestIndex = CheckIndex;

      // Result.Add( BaseAccumulate ); // Add up each Accumulate value.
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

    Result.OrganizeDigits();
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in AddUpAccumulateArray(): " + Except.Message ));
      }
    }



  internal void SetupGeneralBaseArray( Integer GeneralBase )
    {
    // The input to the accumulator can be twice the bit length of GeneralBase.
    int HowMany = ((GeneralBase.GetIndex() + 1) * 2) + 10; // Plus some extra for carries...
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

