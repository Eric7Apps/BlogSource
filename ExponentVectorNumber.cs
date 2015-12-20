// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// A number that has the factors 2^3 * 5^1 * 7^2 can be represented as
// a vector.  The VectorValuesArray holds the elements of that vector.

// The maximum number of factors a number can have is if they were all twos.
// So it's the number of bits in the number.  (16 has 4 twos and it is
// represented by 4 bits.)  But if you have a 32 bit number and you
// divide out a 6 bit number, you'd have about a 26-bit number left.
//  If you divide out the smallest primes first, then you know any other
// factors can't be smaller than the primes you've divided out.  So
// you know the minimum bit-length of the other factors and the
// maximum number of factors that can be left in the number.

// When they say that a number is B-smooth, it means that B is:
// B = IntMath.GetPrimeAt( YourMaximumLimit ).
// And the value of Prime in VectorValueRec is never more than B.
// See the IsBSmooth() method.
// See: https://en.wikipedia.org/wiki/Smooth_number


// This could also be done as an extremely sparse vector, where
// each element in an array represents one of the primes.  So the
// number 2^3 * 5^1 * 7^2 would be represented as:
// SparseArray[0] = 3; // Prime is 2.
// SparseArray[1] = 0; // No factors at prime 3.
// SparseArray[2] = 1; // Prime is 5.
// SparseArray[3] = 2; // Prime is 7.
// SparseArray[4] = 2; // Prime is 11.
// ...
// SparseArray[AReallyBigArraySizeNumber] = 2; // Prime is a big number.
// But representing it that way would be so inefficient it wouldn't be
// practical.  You could set up a matrix of those really sparse arrays, but
// a matrix like that would be even less practical.  It would quickly become
// infeasible with a larger set of primes.



using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker
using System.Threading; // For Sleep().



namespace ExampleServer
{
  class ExponentVectorNumber
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private VectorValueRec[] VectorValuesArray;
  private int VectorValuesArrayLast = 0;
  private Integer ExtraValue;


  internal struct VectorValueRec
    {
    internal uint Prime;
    internal uint Exponent;
    }



  private ExponentVectorNumber()
    {
    }



  internal ExponentVectorNumber( BackgroundWorker UseWorker, IntegerMath UseIntMath )
    {
    Worker = UseWorker;
    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    ExtraValue = new Integer();
    VectorValuesArray = new VectorValueRec[8];
    }



  internal void Copy( ExponentVectorNumber ToCopy )
    {
    VectorValuesArrayLast = ToCopy.VectorValuesArrayLast;
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      VectorValuesArray[Count] = ToCopy.VectorValuesArray[Count];

    ExtraValue.Copy( ToCopy.ExtraValue );
    }



  private void UpdateVectorElement( VectorValueRec Rec )
    {
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( Rec.Prime == VectorValuesArray[Count].Prime )
        {
        VectorValuesArray[Count] = Rec;
        return;
        }
      }

    VectorValuesArray[VectorValuesArrayLast] = Rec;
    VectorValuesArrayLast++;
    if( VectorValuesArrayLast >= VectorValuesArray.Length )
      {
      try
      {
      Array.Resize( ref VectorValuesArray, VectorValuesArray.Length + 8 );
      }
      catch( Exception Except )
        {
        throw( new Exception( "Exception in ExponentVectorNumber.UpdateVectorElement(): " + Except.Message ));
        }
      }
    }



  private VectorValueRec GetVectorElement( uint Prime )
    {
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( Prime == VectorValuesArray[Count].Prime )
        return VectorValuesArray[Count];

      }

    // Didn't find a match.
    VectorValueRec Rec = new VectorValueRec();
    Rec.Prime = Prime;
    Rec.Exponent = 0;
    return Rec;
    }



  internal void SetToZero()
    {
    ExtraValue.SetToZero();
    VectorValuesArrayLast = 0;
    }



  internal void SetFromTraditionalInteger( Integer SetFrom )
    {
    try
    {
    SetToZero();

    Integer WorkingCopy = new Integer();
    Integer Factor = new Integer();
    WorkingCopy.Copy( SetFrom );

    while( true )
      {
      // If WorkingCopy was 37, this would return 37, which is
      // the smallest prime in 37.  So dividing this number
      // by 37 would make a Quotient of 1.
      uint Prime = IntMath.IsDivisibleBySmallPrime( WorkingCopy );
      if( Prime == 0 )
        break;

      Factor.SetFromULong( Prime );
      IntMath.Divide( WorkingCopy, Factor, Quotient, Remainder );
      if( !Remainder.IsZero())
        throw( new Exception( "Bug. !Remainder.IsZero() in SetFromTraditionalInteger()." ));

      VectorValueRec Rec = GetVectorElement( Prime );
      Rec.Exponent++;
      UpdateVectorElement( Rec );

      if( Quotient.IsOne())
        return; // It has all the factors and it is B-smooth up to
                // IntegerMath.GetBiggestPrime().

      WorkingCopy.Copy( Quotient );
      }

    // This number is not B-smooth up to IntegerMath.GetBiggestPrime().
    ExtraValue.Copy( WorkingCopy );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.SetFromTraditionalInteger(): " + Except.Message ));
      }
    }



  internal bool IsBSmooth()
    {
    if( ExtraValue.IsZero())
      return true;
    else
      return false;

    }


  internal void GetTraditionalInteger( Integer Result )
    {
    try
    {
    Result.SetToOne();
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      uint Exponent = VectorValuesArray[Count].Exponent;
      uint Prime = VectorValuesArray[Count].Prime;
      for( int ExpCount = 0; ExpCount < Exponent; ExpCount++ )
        IntMath.MultiplyULong( Result, Prime );

      }

    if( !ExtraValue.IsZero())
      IntMath.Multiply( Result, ExtraValue );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.GetTraditionalInteger(): " + Except.Message ));
      }
    }


  // Adding the vectors is the same as multiplying the numbers.
  internal void AddVectors( ExponentVectorNumber ToAdd )
    {
    try
    {
    for( int Count = 0; Count < ToAdd.VectorValuesArrayLast; Count++ )
      {
      VectorValueRec ToAddRec = ToAdd.VectorValuesArray[Count];

      VectorValueRec Rec = GetVectorElement( ToAddRec.Prime );
      Rec.Exponent += ToAddRec.Exponent;
      UpdateVectorElement( Rec );
      }

    // This number is not B-smooth up to IntegerMath.GetBiggestPrime().
    if( !ToAdd.ExtraValue.IsZero())
      ExtraValue.Add( ToAdd.ExtraValue );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.AddVectors(): " + Except.Message ));
      }
    }



  internal void SortByPrimes()
    {
    while( true )
      {
      bool Swapped = false;
      for( int Count = 0; Count < (VectorValuesArrayLast - 1); Count++ )
        {
        if( VectorValuesArray[Count].Prime > VectorValuesArray[Count + 1].Prime )
          {
          VectorValueRec Temp = VectorValuesArray[Count];
          VectorValuesArray[Count] = VectorValuesArray[Count + 1];
          VectorValuesArray[Count + 1] = Temp;
          Swapped = true;
          }
        }
      
      if( !Swapped )
        break;
        
      }
    }


  }
}
