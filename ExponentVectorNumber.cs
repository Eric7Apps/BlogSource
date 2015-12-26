// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// A number that has the factors 2^3 * 5^1 * 7^2 can be represented as
// a vector.  The VectorValuesArray holds the elements of that vector.

// The maximum number of factors a number can have is if they were all twos.
// So it's the number of bits in the number.  (16 has 4 twos and it is
// represented by 4 bits.)  But if you have a 32 bit number and you
// divide out a 6 bit number, you'd have about a 26-bit number left.
// If you divide out the smallest primes first, then you know any other
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
// using System.Threading; // For Sleep().



namespace ExampleServer
{
  internal struct VectorValueRec
    {
    internal uint Prime;
    internal uint Exponent;
    }


  class ExponentVectorNumber
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  // private BackgroundWorker Worker;
  private VectorValueRec[] VectorValuesArray;
  private int VectorValuesArrayLast = 0;
  private Integer ExtraValue;
  private const int IncreaseArraySizeBy = 32;
  private Integer PartAForAdd;
  private Integer PartBForAdd;
  internal bool UseThis = true;



  private ExponentVectorNumber()
    {
    }



  // internal ExponentVectorNumber( BackgroundWorker UseWorker, IntegerMath UseIntMath )
  internal ExponentVectorNumber( IntegerMath UseIntMath )
    {
    // Worker = UseWorker;
    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    ExtraValue = new Integer();
    PartAForAdd = new Integer();
    PartBForAdd = new Integer();

    VectorValuesArray = new VectorValueRec[IncreaseArraySizeBy];
    }



  internal void SetToZero()
    {
    ExtraValue.SetToZero();
    VectorValuesArrayLast = 0;
    }



  internal bool IsZero()
    {
    if( (VectorValuesArrayLast == 0) && ExtraValue.IsZero())
      return true;
    else
      return false;

    }



  internal void Copy( ExponentVectorNumber ToCopy )
    {
    try
    {
    VectorValuesArrayLast = ToCopy.VectorValuesArrayLast;
    Array.Resize( ref VectorValuesArray, VectorValuesArrayLast + IncreaseArraySizeBy );

    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      VectorValuesArray[Count] = ToCopy.VectorValuesArray[Count];

    ExtraValue.Copy( ToCopy.ExtraValue );
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.Copy(): " + Except.Message ));
      }
    }



  internal bool IsEqual( ExponentVectorNumber ToTest )
    {
    try
    {
    if( ToTest.VectorValuesArrayLast >= VectorValuesArray.Length )
      return false;

    // These have to be in the right sorted order for this to work.
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( VectorValuesArray[Count].Prime != ToTest.VectorValuesArray[Count].Prime )
        return false;

      if( VectorValuesArray[Count].Exponent != ToTest.VectorValuesArray[Count].Exponent )
        return false;

      }

    return true;
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.IsEqual(): " + Except.Message ));
      }
    }



  internal bool IsAllEven()
    {
    try
    {
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( (VectorValuesArray[Count].Exponent & 1) == 1 )
        return false;

      }

    return true;
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.IsEqual(): " + Except.Message ));
      }
    }



  /*
  internal void RemoveRecord( uint Prime )
    {
    int MoveTo = 0;
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( VectorValuesArray[Count].Prime != Prime )
        {
        VectorValuesArray[MoveTo] = VectorValuesArray[Count];
        MoveTo++;
        }
      }

    VectorValuesArrayLast = MoveTo;

    // This is the same as dividing, so it would leave it 
    // at 1 if if divided out everything.
    if( MoveTo == 0 )
      SetToOne();

    }
    */



  internal VectorValueRec GetValueRecordAt( int Index )
    {
    if( Index >= VectorValuesArrayLast )
      {
      VectorValueRec Rec = new VectorValueRec();
      return Rec; // With Prime set to zero.
      }

    return VectorValuesArray[Index];
    }



  internal void UpdateVectorElement( VectorValueRec Rec )
    {
    try
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
      Array.Resize( ref VectorValuesArray, VectorValuesArray.Length + IncreaseArraySizeBy );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.UpdateVectorElement(): " + Except.Message ));
      }
    }



  internal void AddOneFastVectorElement( uint Prime )
    {
    try
    {
    VectorValueRec Rec = new VectorValueRec();
    Rec.Prime = Prime;
    Rec.Exponent = 1;
    VectorValuesArray[VectorValuesArrayLast] = Rec;
    VectorValuesArrayLast++;
    if( VectorValuesArrayLast >= VectorValuesArray.Length )
      Array.Resize( ref VectorValuesArray, VectorValuesArray.Length + IncreaseArraySizeBy );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in AddOneFastVectorElement(): " + Except.Message ));
      }
    }



  /*
  internal void AddOneVectorElement( uint Prime, uint ExpToAdd )
    {
    try
    {
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      if( VectorValuesArray[Count].Prime == Prime )
        {
        VectorValuesArray[Count].Exponent += ExpToAdd;
        return;
        }
      }

    // If it wasn't found, make a new one.
    VectorValueRec Rec = new VectorValueRec();
    Rec.Prime = Prime;
    Rec.Exponent = ExpToAdd;
    VectorValuesArray[VectorValuesArrayLast] = Rec;
    VectorValuesArrayLast++;
    if( VectorValuesArrayLast >= VectorValuesArray.Length )
      Array.Resize( ref VectorValuesArray, VectorValuesArray.Length + IncreaseArraySizeBy );

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.AddOneVectorElement(): " + Except.Message ));
      }
    }
    */



  internal VectorValueRec GetVectorElement( uint Prime )
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



  internal string ToString()
    {
    SortByPrimes();

    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      uint Prime = VectorValuesArray[Count].Prime;
      uint Exponent = VectorValuesArray[Count].Exponent;
      string ShowS = "[" + Prime.ToString() + ", " + Exponent.ToString() + "]  ";
      SBuilder.Append( ShowS );
      }

    if( !ExtraValue.IsZero())
      SBuilder.Append( "  Extra: : " + IntMath.ToString10( ExtraValue ));

    return SBuilder.ToString();
    }



  internal string ToDelimString()
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      uint Prime = VectorValuesArray[Count].Prime;
      uint Exponent = VectorValuesArray[Count].Exponent;
      string ShowS = Prime.ToString() + ";" + Exponent.ToString() + ":";
      SBuilder.Append( ShowS );
      }

    // if( !ExtraValue.IsZero())
      // SBuilder.Append( "  Extra: : " + IntMath.ToString10( ExtraValue ));

    return SBuilder.ToString();
    }



  internal void SetFromDelimString( string InString )
    {
    try
    {
    SetToZero();
    string[] SplitS = InString.Split( new Char[] { ':' } );
    for( int Count = 0; Count < SplitS.Length; Count++ )
      {
      string[] SplitVal = SplitS[Count].Split( new Char[] { ';' } );
      if( SplitVal.Length < 2 )
        break;

      VectorValueRec Rec = new VectorValueRec();
      Rec.Prime = (uint)Int32.Parse( SplitVal[0] );
      Rec.Exponent = (uint)Int32.Parse( SplitVal[1] );
      VectorValuesArray[VectorValuesArrayLast] = Rec;
      VectorValuesArrayLast++;
      if( VectorValuesArrayLast >= VectorValuesArray.Length )
        Array.Resize( ref VectorValuesArray, VectorValuesArray.Length + IncreaseArraySizeBy );

      }
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.SetFromDelimString(): " + Except.Message ));
      }
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



  internal void SetFromULong( ulong SetFrom )
    {
    try
    {
    SetToZero();

    while( true )
      {
      uint Prime = IntMath.GetFirstPrimeFactor( SetFrom );
      // This returns zero when it has tested up to the square root of SetFrom.
      if( Prime == 0 )
        break;

      SetFrom = SetFrom / Prime;

      VectorValueRec Rec = GetVectorElement( Prime );
      Rec.Exponent++;
      UpdateVectorElement( Rec );
      }

    if( SetFrom > IntMath.GetBiggestPrime())
      throw( new Exception( "SetFrom > IntMath.GetBiggestPrime() in ExponentVectorNumber.SetFromULong()." ));

    VectorValueRec SetRec = GetVectorElement( (uint)SetFrom );
    SetRec.Exponent++;
    UpdateVectorElement( SetRec );

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




  internal void Add( ExponentVectorNumber ToAdd )
    {
    try
    {
    PartAForAdd.SetToOne();
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      uint Exponent = VectorValuesArray[Count].Exponent;
      uint Prime = VectorValuesArray[Count].Prime;
      for( int ExpCount = 0; ExpCount < Exponent; ExpCount++ )
        IntMath.MultiplyULong( PartAForAdd, Prime );

      }

    if( !ExtraValue.IsZero())
      IntMath.Multiply( PartAForAdd, ExtraValue );

    PartBForAdd.SetToOne();
    for( int Count = 0; Count < VectorValuesArrayLast; Count++ )
      {
      uint Exponent = ToAdd.VectorValuesArray[Count].Exponent;
      uint Prime = ToAdd.VectorValuesArray[Count].Prime;
      for( int ExpCount = 0; ExpCount < Exponent; ExpCount++ )
        IntMath.MultiplyULong( PartBForAdd, Prime );

      }

    if( !ToAdd.ExtraValue.IsZero())
      IntMath.Multiply( PartBForAdd, ToAdd.ExtraValue );

    PartAForAdd.Add( PartBForAdd );

    // This is the bad part:
    SetFromTraditionalInteger( PartAForAdd );
    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.AddWithNoFactorsInCommon(): " + Except.Message ));
      }
    }



  // Adding the vectors is the same as multiplying the numbers.
  internal void Multiply( ExponentVectorNumber ToAdd )
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

    if( !ToAdd.ExtraValue.IsZero())
      ExtraValue.Add( ToAdd.ExtraValue );

    // Like distributing out the two parts of extra.  So 
    // they are added together.
    // Factors * (Extra1 + Extra2);

    }
    catch( Exception Except )
      {
      throw( new Exception( "Exception in ExponentVectorNumber.Multiply(): " + Except.Message ));
      }
    }



  internal void MultiplyUint( uint ToMul )
    {
    while( true )
      {
      uint Prime = IntMath.GetFirstPrimeFactor( ToMul );
      // This returns zero when it has tested up to the square root of ToMul.
      if( Prime == 0 )
        break;

      ToMul = ToMul / Prime;

      VectorValueRec Rec = GetVectorElement( Prime );
      Rec.Exponent++;
      UpdateVectorElement( Rec );
      }

    if( ToMul > IntMath.GetBiggestPrime())
      throw( new Exception( "SetFrom > IntMath.GetBiggestPrime() in ExponentVectorNumber.MultiplyUint()." ));

    VectorValueRec SetRec = GetVectorElement( ToMul );
    SetRec.Exponent++;
    UpdateVectorElement( SetRec );
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
