// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// These ideas mainly come from a set of books written by Donald Knuth
// in the 1960s called "The Art of Computer Programming", especially
// Volume 2 – Seminumerical Algorithms.
// But it is also a whole lot like the ordinary arithmetic you'd do on
// paper, and there are some comments in the code below about that.

// For more on division see also:
// Brinch Hansen, Multiple-Length Division Revisited, 1994
// http://brinch-hansen.net/papers/1994b.pdf



using System;
using System.Text;



namespace ExampleServer
{
  class IntegerMath
  {
  private long[] SignedD; // Signed digits for use in subtraction.
  private ulong[,] M; // Scratch pad, just like you would do on paper.
  private Integer Quotient;
  private Integer Remainder;
  private Integer Test1;
  private Integer Test2;
  private Integer ToDivideKeep;
  private Integer DivideByKeep;
  private Integer DivideBy;
  private Integer Temp1;
  private Integer Temp2;
  private Integer Temp3;
  private Integer ToDivide;
  private Integer XForModPower;
  private Integer ExponentCopy;
  private int PrimeArrayLast = 0;
  private uint[] PrimeArray;




  internal IntegerMath()
    {
    SignedD = new long[Integer.DigitArraySize];
    M = new ulong[Integer.DigitArraySize, Integer.DigitArraySize];
    Quotient = new Integer();
    Remainder = new Integer();
    Test1 = new Integer();
    Test2 = new Integer();
    ToDivideKeep = new Integer();
    DivideByKeep = new Integer();
    DivideBy = new Integer();
    Temp1 = new Integer();
    Temp2 = new Integer();
    Temp3 = new Integer();
    ToDivide = new Integer();
    XForModPower = new Integer();
    ExponentCopy = new Integer();

    MakePrimeArray();
    }




  internal uint GetFirstPrimeFactor( uint ToTest )
    {
    if( ToTest <= 3 )
      return 0;
      
    uint Max = (uint)FindULSqrRoot( ToTest ); 

    for( int Count = 0; Count < PrimeArrayLast; Count++ )
      {
      uint TestN = PrimeArray[Count];
      if( (ToTest % TestN) == 0 )
        return TestN;

      if( TestN > Max )
        return 0;

      }

    return 0;
    }



  private void MakePrimeArray()
    {
    // try
    PrimeArray = new uint[1024 * 32];
    // catch 

    PrimeArray[0] = 2;
    PrimeArray[1] = 3;
    PrimeArray[2] = 5;
    PrimeArray[3] = 7;
    PrimeArray[4] = 11;
    PrimeArray[5] = 13;
    PrimeArray[6] = 17;
    PrimeArray[7] = 19;
    PrimeArray[8] = 23;

    PrimeArrayLast = 9;
    for( uint TestN = 29; ; TestN += 2 )
      {
      if( (TestN % 3) == 0 )
        continue;

      // If it has no prime factors then add it.
      if( 0 == GetFirstPrimeFactor( TestN ))
        {
        PrimeArray[PrimeArrayLast] = TestN;
        PrimeArrayLast++;

        if( PrimeArrayLast >= PrimeArray.Length )
          return;

        }
      }
    }



  internal uint IsDivisibleBySmallPrime( Integer ToTest )
    {
    if( (ToTest.GetD( 0 ) & 1) == 0 )
      return 2; // It's divisible by 2.

    if( 0 == GetMod3( ToTest ))
      return 3;

    for( int Count = 2; Count < PrimeArrayLast; Count++ )
      {
      if( 0 == GetMod32( ToTest, PrimeArray[Count] ))
        return PrimeArray[Count];

      }

    // No small primes divide it.
    return 0;
    }



  internal void SubtractULong( Integer Result, ulong ToSub )
    {
    if( Result.IsULong())
      {
      ulong ResultU = Result.GetAsULong();
      if( ToSub > ResultU )
        throw( new Exception( "SubULong() (IsULong() and (ToSub > Result)." ));

      ResultU = ResultU - ToSub;
      Result.SetD( 0, ResultU & 0xFFFFFFFF );
      Result.SetD( 1, ResultU >> 32 );
      if( Result.GetD( 1 ) == 0 )
        Result.SetIndex( 0 );
      else
        Result.SetIndex( 1 );

      return;
      }

    // If it got this far then Index is at least 2.
    SignedD[0] = (long)Result.GetD( 0 ) - (long)(ToSub & 0xFFFFFFFF);
    SignedD[1] = (long)Result.GetD( 1 ) - (long)(ToSub >> 32);

    if( (SignedD[0] >= 0) && (SignedD[1] >= 0) )
      {
      // No need to reorganize it.
      Result.SetD( 0, (ulong)SignedD[0] );
      Result.SetD( 1, (ulong)SignedD[1] );
      return;
      }


    for( int Count = 2; Count <= Result.GetIndex(); Count++ )
      SignedD[Count] = (long)Result.GetD( Count );

    for( int Count = 0; Count < Result.GetIndex(); Count++ )
      {
      if( SignedD[Count] < 0 )
        {
        SignedD[Count] += (long)0xFFFFFFFF + 1;
        SignedD[Count + 1]--;
        }
      }
     
    if( SignedD[Result.GetIndex()] < 0 )
      throw( new Exception( "SubULong() SignedD[Index] < 0." ));

    for( int Count = 0; Count <= Result.GetIndex(); Count++ )
      Result.SetD( Count, (ulong)SignedD[Count] );

    for( int Count = Result.GetIndex(); Count >= 0; Count-- )
      {
      if( Result.GetD( Count ) != 0 )
        {
        Result.SetIndex( Count );
        return;
        }
      }

    // If this was zero it wouldn't find a nonzero
    // digit to set the Index to and it would end up down here.
    Result.SetIndex( 0 );
    }




  internal void Subtract( Integer Result, Integer ToSub )
    {
    if( ToSub.IsULong() )
      {
      SubtractULong( Result, ToSub.GetAsULong());
      return;
      }

    if( ToSub.GetIndex() > Result.GetIndex() )
      throw( new Exception( "In Subtract() ToSub.Index > Index." ));

    for( int Count = 0; Count <= ToSub.GetIndex(); Count++ )
      SignedD[Count] = (long)Result.GetD( Count ) - (long)ToSub.GetD( Count );

    for( int Count = ToSub.GetIndex() + 1; Count <= Result.GetIndex(); Count++ )
      SignedD[Count] = (long)Result.GetD( Count );
 
    for( int Count = 0; Count < Result.GetIndex(); Count++ )
      {
      if( SignedD[Count] < 0 )
        {
        SignedD[Count] += (long)0xFFFFFFFF + 1;
        SignedD[Count + 1]--;
        }
      }

    if( SignedD[Result.GetIndex()] < 0 )
      throw( new Exception( "Subtract() SignedD[Index] < 0." ));

    for( int Count = 0; Count <= Result.GetIndex(); Count++ )
      Result.SetD( Count, (ulong)SignedD[Count] );

    for( int Count = Result.GetIndex(); Count >= 0; Count-- )
      {
      if( Result.GetD( Count ) != 0 )
        {
        Result.SetIndex( Count );
        return;
        }
      }

    // If it never found a non-zero digit it would get down to here.
    Result.SetIndex( 0 );
    }




  private void MultiplyUInt( Integer Result, uint ToMul )
    {
    for( int Column = 0; Column <= Result.GetIndex(); Column++ )
      M[Column, 0] = ToMul * Result.GetD( Column );
     
    // Add these up with a carry.
    Result.SetD( 0, M[0, 0] & 0xFFFFFFFF );
    ulong Carry = M[0, 0] >> 32;
    for( int Column = 1; Column <= Result.GetIndex(); Column++ )
      {
      // Using a compile-time check on this constant, 
      // this Test value does not overflow:
      // const ulong Test = ((ulong)0xFFFFFFFF * (ulong)(0xFFFFFFFF)) + 0xFFFFFFFF;

      // ulong Total = checked( M[Column, 0] + Carry );
      ulong Total = M[Column, 0] + Carry;
      Result.SetD( Column, Total & 0xFFFFFFFF );
      Carry = Total >> 32;
      }

    if( Carry != 0 )
      {
      Result.IncrementIndex();
        // throw( new Exception( "MultiplyUInt() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  internal void MultiplyULong( Integer Result, ulong ToMul )
    {
    // Using compile-time checks, this one overflows:
    // const ulong Test = ((ulong)0xFFFFFFFF + 1) * ((ulong)0xFFFFFFFF + 1);
    // This one doesn't:
    // const ulong Test = (ulong)0xFFFFFFFF * ((ulong)0xFFFFFFFF + 1);

    if( Result.IsZero())
      return; // Then the answer is zero, which it already is.

    if( ToMul == 0 )
      {
      Result.SetToZero();
      return;
      }

    ulong B0 = ToMul & 0xFFFFFFFF;
    ulong B1 = ToMul >> 32;
    
    if( B1 == 0 )
      {
      MultiplyUInt( Result, (uint)B0 );
      return;
      }

    // Since B1 is not zero:
    if( (Result.GetIndex() + 1) >= Integer.DigitArraySize ) 
      throw( new Exception( "Overflow in MultiplyULong." ));
     
    for( int Column = 0; Column <= Result.GetIndex(); Column++ )
      {
      M[Column, 0] = B0 * Result.GetD( Column );
      // Column + 1 and Row is 1, so it's just like pen and paper.
      M[Column + 1, 1] = B1 * Result.GetD( Column );
      }

    // Since B1 is not zero, the index is set one higher.
    Result.IncrementIndex(); // Might throw an exception if it goes out of range.

    M[Result.GetIndex(), 0] = 0; // Otherwise it would be undefined
                                 // when it's added up below.

    // Add these up with a carry.
    Result.SetD( 0, M[0, 0] & 0xFFFFFFFF );
    ulong Carry = M[0, 0] >> 32;
    for( int Column = 1; Column <= Result.GetIndex(); Column++ )
      {
      // This does overflow:
      // const ulong Test = ((ulong)0xFFFFFFFF * (ulong)(0xFFFFFFFF)) 
      //                  + ((ulong)0xFFFFFFFF * (ulong)(0xFFFFFFFF)); 
      // Split the ulongs into right and left sides
      // so that they don't overflow.
      ulong TotalLeft = 0;
      ulong TotalRight = 0;
      // There's only the two rows for this.
      for( int Row = 0; Row <= 1; Row++ )
        {
        TotalRight += M[Column, Row] & 0xFFFFFFFF;
        TotalLeft += M[Column, Row] >> 32;
        }

      TotalRight += Carry;
      Result.SetD( Column, TotalRight & 0xFFFFFFFF );
      Carry = TotalRight >> 32;
      Carry += TotalLeft;
      }

    if( Carry != 0 )
      {
      Result.IncrementIndex();
      //  throw( new Exception( "MulULong() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  // See also: http://en.wikipedia.org/wiki/Karatsuba_algorithm

  internal void Multiply( Integer Result, Integer ToMul )
    {
    // try
    // {

    if( ToMul.IsULong())
      {
      MultiplyULong( Result, ToMul.GetAsULong());
      return;
      }

    if( Result.IsZero())
      return;

    // It could never get here if ToMul is zero because GetIsULong()
    // would be true for zero.
    // if( ToMul.IsZero())

    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= Integer.DigitArraySize )
      throw( new Exception( "Multiply() overflow." ));

    for( int Row = 0; Row <= ToMul.GetIndex(); Row++ )
      {
      if( ToMul.GetD( Row ) == 0 )
        {
        for( int Column = 0; Column <= Result.GetIndex(); Column++ )
          M[Column + Row, Row] = 0;

        }
      else
        {
        for( int Column = 0; Column <= Result.GetIndex(); Column++ )
          M[Column + Row, Row] = ToMul.GetD( Row ) * Result.GetD( Column );

        }
      }

    // Add the columns up with a carry.
    Result.SetD( 0, M[0, 0] & 0xFFFFFFFF );
    ulong Carry = M[0, 0] >> 32;
    for( int Column = 1; Column <= TotalIndex; Column++ )
      {
      ulong TotalLeft = 0;
      ulong TotalRight = 0;
      for( int Row = 0; Row <= ToMul.GetIndex(); Row++ )
        {
        if( Row > Column )
          break;

        if( Column > (Result.GetIndex() + Row) )
          continue;

        // Split the ulongs into right and left sides
        // so that they don't overflow.
        TotalRight += M[Column, Row] & 0xFFFFFFFF;
        TotalLeft += M[Column, Row] >> 32;
        }

      TotalRight += Carry;
      Result.SetD( Column, TotalRight & 0xFFFFFFFF );
      Carry = TotalRight >> 32;
      Carry += TotalLeft;
      }

    Result.SetIndex( TotalIndex );
    if( Carry != 0 )
      {
      Result.IncrementIndex();
      //  throw( new Exception( "Multiply() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }

    /*
    }
    catch( Exception ) // Except )
      {
      // "Bug in Multiply: " + Except.Message
      }
      */
    }




  // The ShortDivide() algorithm works like dividing a polynomial which
  // looks like: 
  // (ax3 + bx2 + cx + d) / N = (ax3 + bx2 + cx + d) * (1/N)
  // The 1/N distributes over the polynomial: 
  // (ax3 * (1/N)) + (bx2 * (1/N)) + (cx * (1/N)) + (d * (1/N))
  // (ax3/N) + (bx2/N) + (cx/N) + (d/N)

  // The algorithm goes from left to right and reduces that polynomial
  // expression.  So it starts with Quotient being a copy of ToDivide
  // and then it reduces Quotient from left to right.
  private bool ShortDivide( Integer ToDivide,
                            Integer DivideBy,
                            Integer Quotient,
                            Integer Remainder )
    {
    Quotient.Copy( ToDivide );
    // DivideBy has an Index of zero:
    ulong DivideByU = DivideBy.GetD( 0 );
    ulong RemainderU = 0;

    // Get the first one set up.
    if( DivideByU > Quotient.GetD( Quotient.GetIndex()) )
      {
      Quotient.SetD( Quotient.GetIndex(), 0 );
      }
    else
      {
      ulong OneDigit = Quotient.GetD( Quotient.GetIndex() );
      Quotient.SetD( Quotient.GetIndex(), OneDigit / DivideByU );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }

    // Now do the rest.
    for( int Count = Quotient.GetIndex(); Count >= 1; Count-- )
      {
      ulong TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 32;
      TwoDigits |= ToDivide.GetD( Count - 1 );
      Quotient.SetD( Count - 1, TwoDigits / DivideByU );
      RemainderU = TwoDigits % DivideByU;
      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU ); // What's left to divide.
      }

    // Set the index for the quotient.
    // The quotient would have to be at least 1 here,
    // so it will find where to set the index.
    for( int Count = Quotient.GetIndex(); Count >= 0; Count-- )
      {
      if( Quotient.GetD( Count ) != 0 )
        {
        Quotient.SetIndex( Count );
        break;
        }
      }

    Remainder.SetD( 0, RemainderU );
    Remainder.SetIndex( 0 );
    if( RemainderU == 0 )
      return true;
    else
      return false;

    }




  // This is a variation on ShortDivide that returns the remainder. 
  // Also, DivideBy is a ulong.
  internal ulong ShortDivideRem( Integer ToDivideOriginal,
                               ulong DivideByU,
                               Integer Quotient )
    {
    if( ToDivideOriginal.IsULong())
      {
      ulong ToDiv = ToDivideOriginal.GetAsULong();
      ulong Q = ToDiv / DivideByU;
      Quotient.SetFromULong( Q );
      return ToDiv % DivideByU;
      }

    ToDivide.Copy( ToDivideOriginal );
    Quotient.Copy( ToDivide );

    ulong RemainderU = 0;
    if( DivideByU > Quotient.GetD( Quotient.GetIndex() ))
      {
      Quotient.SetD( Quotient.GetIndex(), 0 );
      }
    else
      {
      ulong OneDigit = Quotient.GetD( Quotient.GetIndex() );
      Quotient.SetD( Quotient.GetIndex(), OneDigit / DivideByU );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }

    for( int Count = Quotient.GetIndex(); Count >= 1; Count-- )
      {
      ulong TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 32;
      TwoDigits |= ToDivide.GetD( Count - 1 );

      Quotient.SetD( Count - 1, TwoDigits / DivideByU );
      RemainderU = TwoDigits % DivideByU;

      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU );
      }

    for( int Count = Quotient.GetIndex(); Count >= 0; Count-- )
      {
      if( Quotient.GetD( Count ) != 0 )
        {
        Quotient.SetIndex( Count );
        break;
        }
      }

    return RemainderU;
    }
  


  // This is a variation on ShortDivide() to get the remainder only.
  internal ulong GetMod32( Integer ToDivideOriginal, ulong DivideByU )
    {
    if( (DivideByU >> 32) != 0 )
      throw( new Exception( "GetMod32: (DivideByU >> 32) != 0." ));

    // If this is _equal_ to a small prime it would return zero.
    if( ToDivideOriginal.IsULong())
      {
      ulong Result = ToDivideOriginal.GetAsULong();
      return Result % DivideByU;
      }

    ToDivide.Copy( ToDivideOriginal );
    ulong RemainderU = 0;

    if( DivideByU <= ToDivide.GetD( ToDivide.GetIndex() ))
      {
      ulong OneDigit = ToDivide.GetD( ToDivide.GetIndex() );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }
 
    for( int Count = ToDivide.GetIndex(); Count >= 1; Count-- )
      {
      ulong TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 32;
      TwoDigits |= ToDivide.GetD( Count - 1 );
      RemainderU = TwoDigits % DivideByU;
      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU );
      }

    return RemainderU;
    }
    


  // Is there a better way to optimize this for Mod 3?
  internal ulong GetMod3( Integer ToDivideOriginal )
    {
    if( ToDivideOriginal.IsULong())
      {
      ulong Result = ToDivideOriginal.GetAsULong();
      return Result % 3;
      }

    ToDivide.Copy( ToDivideOriginal );
    ulong RemainderU = 0;

    if( 3 <= ToDivide.GetD( ToDivide.GetIndex() ))
      {
      ulong OneDigit = ToDivide.GetD( ToDivide.GetIndex() );
      RemainderU = OneDigit % 3;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }
 
    for( int Count = ToDivide.GetIndex(); Count >= 1; Count-- )
      {
      ulong TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 32;
      TwoDigits |= ToDivide.GetD( Count - 1 );
      RemainderU = TwoDigits % 3;
      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU );
      }

    return RemainderU;
    }




   /*
  private bool EquivalentMod()
    {
    // The compiler does a compile-time check on these constants
    // and it finds unreachable code because they are not false.
    const uint Test1 = ((2 * 10) + 3) % 15;
    const uint Test2 = (((2 * 10) % 15) + (3 % 15)) % 15;
    const uint Test3 = ((((2 % 15) * (10 % 15)) % 15) + (3 % 15)) % 15;

    if( Test1 == Test2 )
      {
      if( Test2 == Test3 )
        return true;
      else 
        return false; // Unreachable code.
        
      }
    else
      return false; // Unreachable code.

    // The compiler only tells you about the first unreachable code it
    // finds.  Comment out earlier ones to make it show later ones.
    }
    */



  private ulong GetMod64FromTwoULongs( ulong P1, ulong P0, ulong Divisor64 )
    {
    if( Divisor64 <= 0xFFFFFFFF )
      throw( new Exception( "GetMod64FromTwoULongs Divisor64 <= 0xFFFFFFFF" ));

    // This is never shifted more than 12 bits, so check to make sure there's
    // room to shift it.
    if( (Divisor64 >> 52) != 0 )  
      throw( new Exception( "Divisor64 is too big in GetMod64FromTwoULongs." ));
    
    if( P1 == 0 )
      return P0 % Divisor64;

    //////////////////////////////////////////////
    // See Gauss Disquisitions, first chapter:

    // R ~ (a*b) mod m
    // R ~ ((a mod m) * (b mod m)) mod m
   
    // (P1 * 2^64) + P0 is what the number is.
 
    ulong Part1 = P1 % Divisor64;
    if( (Divisor64 >> 40) == 0 )  
      {
      // Then this can be done 24 bits at a time.
      Part1 <<= 24;  // Times 2^24
      Part1 = Part1 % Divisor64;
      Part1 <<= 24;  //  48
      Part1 = Part1 % Divisor64;
      Part1 <<= 16;  // Brings it to 64
      Part1 = Part1 % Divisor64;
      }
    else
      {
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor64;
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor64;
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor64;
      Part1 <<= 12;  // Times 2^12 Brings it to 48.
      Part1 = Part1 % Divisor64;

      Part1 <<= 8;  // Times 2^8
      Part1 = Part1 % Divisor64;
      Part1 <<= 8;  // Times 2^8 Brings it to 64.
      Part1 = Part1 % Divisor64;
      }

    // All of the above was just to get the P1 part of it, so now add P0:
    return (Part1 + P0) % Divisor64;
    }




  internal ulong GetMod64( Integer ToDivideOriginal, ulong DivideBy )
    {
    if( ToDivideOriginal.IsULong())
      return ToDivideOriginal.GetAsULong() % DivideBy;

    ToDivide.Copy( ToDivideOriginal );

    ulong Digit1;
    ulong Digit0;
    ulong Remainder;

    if( ToDivide.GetIndex() == 2 )
      {
      Digit1 = ToDivide.GetD( 2 );
      Digit0 = ToDivide.GetD( 1 ) << 32;
      Digit0 |= ToDivide.GetD( 0 );
      return GetMod64FromTwoULongs( Digit1, Digit0, DivideBy );
      }

    if( ToDivide.GetIndex() == 3 )
      {
      Digit1 = ToDivide.GetD( 3 ) << 32;
      Digit1 |= ToDivide.GetD( 2 );
      Digit0 = ToDivide.GetD( 1 ) << 32;
      Digit0 |= ToDivide.GetD( 0 );
      return GetMod64FromTwoULongs( Digit1, Digit0, DivideBy );
      }

    int Where = ToDivide.GetIndex();
    while( true )
      {
      if( Where <= 3 )
        {
        if( Where < 2 ) // This can't happen.
          throw( new Exception( "Bug: GetMod64(): Where < 2." ));

        if( Where == 2 )
          {
          Digit1 = ToDivide.GetD( 2 );
          Digit0 = ToDivide.GetD( 1 ) << 32;
          Digit0 |= ToDivide.GetD( 0 );
          return GetMod64FromTwoULongs( Digit1, Digit0, DivideBy );
          }

        if( Where == 3 )
          {
          Digit1 = ToDivide.GetD( 3 ) << 32;
          Digit1 |= ToDivide.GetD( 2 );
          Digit0 = ToDivide.GetD( 1 ) << 32;
          Digit0 |= ToDivide.GetD( 0 );
          return GetMod64FromTwoULongs( Digit1, Digit0, DivideBy );
          }
        }
      else
        {
        // The index is bigger than 3.
        // This part would get called at least once.
        Digit1 = ToDivide.GetD( Where ) << 32;
        Digit1 |= ToDivide.GetD( Where - 1 );
        Digit0 = ToDivide.GetD( Where - 2 ) << 32;
        Digit0 |= ToDivide.GetD( Where - 3 );
        Remainder = GetMod64FromTwoULongs( Digit1, Digit0, DivideBy );

        ToDivide.SetD( Where, 0 );
        ToDivide.SetD( Where - 1, 0 );
        ToDivide.SetD( Where - 2, Remainder >> 32 );
        ToDivide.SetD( Where - 3, Remainder & 0xFFFFFFFF );
        }

      Where -= 2;
      }
    }




  internal void SetFromString( Integer Result, string InString )
    {
    Base10Number Base10N = new Base10Number();
    Integer Tens = new Integer();
    Integer OnePart = new Integer();

    // This might throw an exception if the string is bad.
    Base10N.SetFromString( InString );

    Result.SetFromULong( Base10N.GetD( 0 ));
    Tens.SetFromULong( 10 );

    for( int Count = 1; Count <= Base10N.GetIndex(); Count++ )
      {
      OnePart.SetFromULong( Base10N.GetD( Count ));
      Multiply( OnePart, Tens );
      Result.Add( OnePart );
      MultiplyULong( Tens, 10 );
      }
    }




  internal string ToString10( Integer From )
    {
    if( From.IsULong())
      {
      ulong N = From.GetAsULong();
      return N.ToString( "N0" );
      }

    string Result = "";
    ToDivide.Copy( From );
    int CommaCount = 0;
    while( !ToDivide.IsZero())
      {
      uint Digit = (uint)ShortDivideRem( ToDivide, 10, Quotient );
      ToDivide.Copy( Quotient );
      if( ((CommaCount % 3) == 0) && (CommaCount != 0) )
        Result = Digit.ToString() + "," + Result; // Or use a StringBuilder.
      else
        Result = Digit.ToString() + Result;

      CommaCount++;
      }

    return Result;
    }
  



  internal static bool IsSmallQuadResidue( uint Number )
    {
    uint Test = Number % 3; // 0, 1, 1, 0
    if( Test == 2 )
      return false;

    Test = Number % 5;
    if( (Test == 2) || (Test == 3))  // 0, 1, 4, 4, 1, 0
      return false;

    Test = Number % 7;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 2)) )
      return false;

    Test = Number % 11;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 9) ||
          (Test == 5) ||
          (Test == 3)) )
      return false;

    Test = Number % 13;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 9) ||
          (Test == 3) ||
          (Test == 12) ||
          (Test == 10)) )
      return false;

    Test = Number % 17;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 9) ||
          (Test == 16) ||
          (Test == 8) ||
          (Test == 2) ||
          (Test == 15) ||
          (Test == 13)) )
      return false;

    Test = Number % 19;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 9) ||
          (Test == 16) ||
          (Test == 6) ||
          (Test == 17) ||
          (Test == 11) ||
          (Test == 7) ||
          (Test == 5)) )
    return false;

    Test = Number % 23;
    if( !((Test == 0) ||
          (Test == 1) ||
          (Test == 4) ||
          (Test == 9) ||
          (Test == 16) ||
          (Test == 2) ||
          (Test == 13) ||
          (Test == 3) ||
          (Test == 18) ||
          (Test == 12) ||
          (Test == 8) ||
          (Test == 6)) ) 
      return false;

    // If it made it this far...
    return true;
    }




  internal static bool FirstBytesAreQuadRes( uint Test )
    {
    // Is this number a square mod 2^12?
    // (Quadratic residue mod 2^12)

    uint FirstByte = Test;
    uint SecondByte = (FirstByte & 0x0F00) >> 8;

    FirstByte = FirstByte & 0x0FF;
    switch( FirstByte )
      {
      case 0x00: // return true;
      
        if( (SecondByte == 0) ||
            (SecondByte == 1) ||
            (SecondByte == 4) ||
            (SecondByte == 9))
          return true;
        else
          return false;
       
      case 0x01: return true;
      case 0x04: return true;
      case 0x09: return true;
      case 0x10: return true;
      case 0x11: return true;
      case 0x19: return true;
      case 0x21: return true;
      case 0x24: return true;
      case 0x29: return true;
      case 0x31: return true;
      case 0x39: return true;
      case 0x40: // return true;
        // 0x40, 0, 2, 4, 6, 8, 10, 12, 14
        if( (SecondByte & 0x01) == 0x01 )
          return false;
        else
          return true;

      case 0x41: return true;
      case 0x44: return true;
      case 0x49: return true;
      case 0x51: return true;
      case 0x59: return true;
      case 0x61: return true;
      case 0x64: return true;
      case 0x69: return true;
      case 0x71: return true;
      case 0x79: return true;
      case 0x81: return true;
      case 0x84: return true;
      case 0x89: return true;
      case 0x90: return true;
      case 0x91: return true;
      case 0x99: return true;
      case 0xA1: return true;
      case 0xA4: return true;
      case 0xA9: return true;
      case 0xB1: return true;
      case 0xB9: return true;
      case 0xC1: return true;
      case 0xC4: return true;
      case 0xC9: return true;
      case 0xD1: return true;
      case 0xD9: return true;
      case 0xE1: return true;
      case 0xE4: return true;
      case 0xE9: return true;
      case 0xF1: return true;
      case 0xF9: return true;  // 44 out of 256.

      default: return false;
      }
    }





  internal void DoSquare( Integer ToSquare )
    {
    if( ToSquare.GetIndex() == 0 )
      {
      ToSquare.Square0();
      return;
      }

    if( ToSquare.GetIndex() == 1 )
      {
      ToSquare.Square1();
      return;
      }
        
    if( ToSquare.GetIndex() == 2 )
      {
      ToSquare.Square2();
      return;
      }
    
    // Now Index is at least 3:
    int DoubleIndex = ToSquare.GetIndex() << 1;
    if( DoubleIndex >= Integer.DigitArraySize )
      {    
      throw( new Exception( "Square() overflowed." ));
      }        
       
    for( int Row = 0; Row <= ToSquare.GetIndex(); Row++ )
      {
      if( ToSquare.GetD( Row ) == 0 )
        {
        for( int Column = 0; Column <= ToSquare.GetIndex(); Column++ )
          M[Column + Row, Row] = 0;

        }
      else
        {
        for( int Column = 0; Column <= ToSquare.GetIndex(); Column++ )
          M[Column + Row, Row] = ToSquare.GetD( Row ) * ToSquare.GetD( Column );

        }
      }

    // Add the columns up with a carry.
    ToSquare.SetD( 0, M[0, 0] & 0xFFFFFFFF );
    ulong Carry = M[0, 0] >> 32;
    for( int Column = 1; Column <= DoubleIndex; Column++ )
      {
      ulong TotalLeft = 0;
      ulong TotalRight = 0;
      for( int Row = 0; Row <= Column; Row++ )
        {
        if( Row > ToSquare.GetIndex() )
          break;

        if( Column > (ToSquare.GetIndex() + Row) )
          continue;

        TotalRight += M[Column, Row] & 0xFFFFFFFF;
        TotalLeft += M[Column, Row] >> 32;
        }

      TotalRight += Carry;
      ToSquare.SetD( Column, TotalRight & 0xFFFFFFFF );
      Carry = TotalRight >> 32;
      Carry += TotalLeft;
      }

    ToSquare.SetIndex( DoubleIndex );
    if( Carry != 0 )
      {
      ToSquare.SetIndex( ToSquare.GetIndex() + 1 );
      if( ToSquare.GetIndex() >= Integer.DigitArraySize ) 
        throw( new Exception( "Square() overflow." ));
      
      ToSquare.SetD( ToSquare.GetIndex(), Carry );
      }
    }




  internal ulong FindULSqrRoot( ulong ToMatch ) 
    {
    // Start OneBit with the highest possible bit.
    ulong OneBit = 0x80000000; // 0x8000 0000
    ulong Result = 0;
    for( int Count = 0; Count < 32; Count++ )
      {
      ulong ToTry = Result | OneBit;
      if( (ToTry * ToTry) <= ToMatch )
        Result |= OneBit; // Then I want the bit.

      OneBit >>= 1;
      }

    ////////////////////////////////////////////
    // Test:
    if( (Result * Result) > ToMatch )
      throw( new Exception( "FindULSqrRoot() Result is too high." ));

    // This would overflow if Answer was 0xFFFFFFFF.
    // It won't do overflow checking at run time unless you use
    // the checked keyword. 
    if( (Result != 0) && (Result != 0xFFFFFFFF)  )
      {
      if( ((Result + 1) * (Result + 1)) <= ToMatch )
        throw( new Exception( "FindULSqrRoot() Result is too low." ));

      }
    /////////////////////////////////////////
    
    return Result;
    }




  // This is an optimization for multiplying when only the top digit
  // of a number has been set and all of the other digits are zero.
  internal void MultiplyTop( Integer Result, Integer ToMul )
    {
    // try
    // {
    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= Integer.DigitArraySize )
      throw( new Exception( "MultiplyTop() overflow." ));

    // Just like Multiply() except that all the other rows are zero:

    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      M[Column + Result.GetIndex(), Result.GetIndex()] = Result.GetD( Result.GetIndex() ) * ToMul.GetD( Column );

    for( int Column = 0; Column < Result.GetIndex(); Column++ )
      Result.SetD( Column, 0 );

    ulong Carry = 0;
    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      {
      ulong Total = M[Column + Result.GetIndex(), Result.GetIndex()] + Carry;
      Result.SetD( Column + Result.GetIndex(), Total & 0xFFFFFFFF );
      Carry = Total >> 32;
      }

    Result.SetIndex( TotalIndex );
    if( Carry != 0 )
      {
      Result.SetIndex( Result.GetIndex() + 1 );
      if( Result.GetIndex() >= Integer.DigitArraySize ) 
        throw( new Exception( "MultiplyTop() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }

    /*
    }
    catch( Exception ) // Except )
      {
      // "Bug in MultiplyTop: " + Except.Message
      }
    */
    }




  // This is another optimization.  This is used when the top digit
  // is 1 and all of the other digits are zero.
  // This is effectively just a shift-left operation.
  internal void MultiplyTopOne( Integer Result, Integer ToMul )
    {
    // try
    // {
    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= Integer.DigitArraySize )
      throw( new Exception( "MultiplyTopOne() overflow." ));

    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      Result.SetD( Column + Result.GetIndex(), ToMul.GetD( Column ));

    for( int Column = 0; Column < Result.GetIndex(); Column++ )
      Result.SetD( Column, 0 );

    // No Carrys need to be done.
    Result.SetIndex( TotalIndex );

    /*
    }
    catch( Exception ) // Except )
      {
      // "Bug in MultiplyTopOne: " + Except.Message
      }
      */
    }
  



    internal void Divide( Integer ToDivideOriginal,
                        Integer DivideByOriginal,
                        Integer Quotient,
                        Integer Remainder )
    {
    // Returns true if it divides exactly with zero remainder.
    // This first checks for some basics before trying to divide it:

    if( DivideByOriginal.IsZero() )
      throw( new Exception( "Divide() dividing by zero." ));

    ToDivide.Copy( ToDivideOriginal );
    DivideBy.Copy( DivideByOriginal );

    if( ToDivide.ParamIsGreater( DivideBy ))
      {
      Quotient.SetToZero();
      Remainder.Copy( ToDivide );
      return; //  false;
      }

    if( ToDivide.IsEqual( DivideBy ))
      {
      Quotient.SetFromULong( 1 );
      Remainder.SetToZero();
      return; //  true;
      }

    if( ToDivide.IsULong())
      {
      ulong ToDivideU = ToDivide.GetAsULong();
      ulong DivideByU = DivideBy.GetAsULong();
      ulong QuotientU = ToDivideU / DivideByU;
      ulong RemainderU = ToDivideU % DivideByU;
      Quotient.SetFromULong( QuotientU );
      Remainder.SetFromULong( RemainderU );
      // if( RemainderU == 0 )
        return; //  true;
      // else
        // return false;
      }

    if( DivideBy.GetIndex() == 0 )
      {
      ShortDivide( ToDivide, DivideBy, Quotient, Remainder );
      return;
      }

    // return LongDivide1( ToDivide, DivideBy, Quotient, Remainder );
    // return LongDivide2( ToDivide, DivideBy, Quotient, Remainder );
    LongDivide3( ToDivide, DivideBy, Quotient, Remainder );
    }





  /* 
  private bool LongDivide1( Integer ToDivide,
                            Integer DivideBy,
                            Integer Quotient,
                            Integer Remainder )
    {
    Integer Test1 = new Integer();

    int TestIndex = ToDivide.Index - DivideBy.Index;
    if( TestIndex != 0 )
      {
      // Is 1 too high?
      Test1.SetDigitAndClear( TestIndex, 1 );
      Test1.MultiplyTopOne( DivideBy );  
      if( ToDivide.ParamIsGreater( Test1 ))
        TestIndex--;

      }

    Quotient.SetDigitAndClear( TestIndex, 1 );
    Quotient.D[TestIndex] = 0;
    uint BitTest = 0x80000000;
    while( true )
      {
      // For-loop to test each bit:
      for( int BitCount = 31; BitCount >= 0; BitCount-- )
        {
        Test1.Copy( Quotient );
        Test1.D[TestIndex] |= BitTest;
        Test1.Multiply( DivideBy );
        if( Test1.ParamIsGreaterOrEq( ToDivide ))
          Quotient.D[TestIndex] |= BitTest; // Then keep the bit.
        
        BitTest >>= 1;
        } 
      
      if( TestIndex == 0 )
        break;

      TestIndex--;
      BitTest = 0x80000000;
      }

    Test1.Copy( Quotient );
    Test1.Multiply( DivideBy );
    if( Test1.IsEqual( ToDivide ) )
      {
      Remainder.SetToZero();
      return true; // Divides exactly.
      }
    
    Remainder.Copy( ToDivide );
    Remainder.Subtract( Test1 );

    // Does not divide it exactly.
    return false;
    }
    */




  private void TestDivideBits( ulong MaxValue,
                               bool IsTop,
                               int TestIndex,
                               Integer ToDivide,
                               Integer DivideBy,
                               Integer Quotient,
                               Integer Remainder )
    {
    // For a particular value of TestIndex, this does the 
    // for-loop to test each bit.

    // When you're not testing you wouldn't want to be creating these
    // and allocating the RAM for them each time it's called.
    Integer Test1 = new Integer();
    Integer Test2 = new Integer();

    uint BitTest = 0x80000000;
    for( int BitCount = 31; BitCount >= 0; BitCount-- )
      {
      if( (Quotient.GetD( TestIndex ) | BitTest) > MaxValue )
        {
        // If it's more than the MaxValue then the
        // multiplication test can be skipped for
        // this bit.
        // SkippedMultiplies++;
        BitTest >>= 1;
        continue;
        }

      // Is it only doing the multiplication for the top digit?
      if( IsTop )
        {
        Test1.Copy( Quotient );
        Test1.SetD( TestIndex, Test1.GetD( TestIndex ) | BitTest );
        MultiplyTop( Test1, DivideBy );

        
        /////////////////////
        Test2.Copy( Quotient );
        Test2.SetD( TestIndex, Test2.GetD( TestIndex ) | BitTest );
        Multiply( Test2, DivideBy );

        if( !Test1.IsEqual( Test2 ))
          throw( new Exception( "!Test1.IsEqual( Test2 ) in TestDivideBits()." ));
        ///////////////////////
         
        }
      else
        {
        Test1.Copy( Quotient );
        Test1.SetD( TestIndex, Test1.GetD( TestIndex ) | BitTest );
        Multiply( Test1, DivideBy );
        }

      if( Test1.ParamIsGreaterOrEq( ToDivide ))
        Quotient.SetD( TestIndex, Quotient.GetD( TestIndex ) | BitTest ); // Keep the bit.
        
      BitTest >>= 1;
      } 
    }
    


  /*
  // This works like LongDivide1 except that it estimates the maximum
  // value for the digit and the for-loop for bit testing is called
  // as a separate function.
  private bool LongDivide2( Integer ToDivide,
                            Integer DivideBy,
                            Integer Quotient,
                            Integer Remainder )
    {
    Integer Test1 = new Integer();

    int TestIndex = ToDivide.Index - DivideBy.Index;
    // See if TestIndex is too high.
    if( TestIndex != 0 )
      {
      // Is 1 too high?
      Test1.SetDigitAndClear( TestIndex, 1 );
      Test1.MultiplyTopOne( DivideBy );  
      if( ToDivide.ParamIsGreater( Test1 ))
        TestIndex--;

      }

    // If you were multiplying 99 times 97 you'd get 9,603 and the upper
    // two digits [96] are used to find the MaxValue.  But if you were multiply
    // 12 * 13 you'd have 156 and only the upper one digit is used to find
    // the MaxValue.
    // Here it checks if it should use one digit or two:
    ulong MaxValue;
    if( (ToDivide.Index - 1) > (DivideBy.Index + TestIndex) )
      {
      MaxValue = ToDivide.D[ToDivide.Index];
      }
    else
      {
      MaxValue = ToDivide.D[ToDivide.Index] << 32;
      MaxValue |= ToDivide.D[ToDivide.Index - 1];
      }

    MaxValue = MaxValue / DivideBy.D[DivideBy.Index];

    Quotient.SetDigitAndClear( TestIndex, 1 );
    Quotient.D[TestIndex] = 0;
    TestDivideBits( MaxValue,
                    true,
                    TestIndex,
                    ToDivide,
                    DivideBy,
                    Quotient,
                    Remainder );

    if( TestIndex == 0 )
      {
      Test1.Copy( Quotient );
      Test1.Multiply( DivideBy );  
      Remainder.Copy( ToDivide );
      Remainder.Subtract( Test1 );

      ///////////////
      if( DivideBy.ParamIsGreater( Remainder ))
        throw( new Exception( "Remainder > DivideBy in LongDivide2()." ));
      //////////////

      if( Remainder.IsZero() )
        return true;
      else
        return false;

      }

    TestIndex--;

    while( true )
      {
      // This remainder is used the same way you do long division
      // with paper and pen and you keep working with a remainder
      // until the remainder is reduced to something smaller than
      // DivideBy.  You look at the remainder to estimate
      // your next quotient digit.
      Test1.Copy( Quotient );
      Test1.Multiply( DivideBy );
      Remainder.Copy( ToDivide );
      Remainder.Subtract( Test1 );
      MaxValue = Remainder.D[Remainder.Index] << 32;
      MaxValue |= Remainder.D[Remainder.Index - 1];
      MaxValue = MaxValue / DivideBy.D[DivideBy.Index];
      TestDivideBits( MaxValue,
                      false,
                      TestIndex,
                      ToDivide,
                      DivideBy,
                      Quotient,
                      Remainder );

      if( TestIndex == 0 )
        break;

      TestIndex--;
      }


    Test1.Copy( Quotient );
    Test1.Multiply( DivideBy );
    Remainder.Copy( ToDivide );
    Remainder.Subtract( Test1 );

    //////////////////////////////
    if( DivideBy.ParamIsGreater( Remainder ))
      throw( new Exception( "LgRemainder > LgDivideBy in LongDivide2()." ));
    ////////////////////////////////

    if( Remainder.IsZero() )
      return true;
    else
      return false;

    }
  */




    // If you multiply the numerator and the denominator by the same amount
    // then the quotient is still the same.  By shifting left (multiplying by twos)
    // the MaxValue upper limit is more accurate.
    // This is called normalization.
  private int FindShiftBy( ulong ToTest )
    {
    int ShiftBy = 0;
    // If it's not already shifted all the way over to the left,
    // shift it all the way over.
    for( int Count = 0; Count < 32; Count++ )
      {
      if( (ToTest & 0x80000000) != 0 )
        break;

      ShiftBy++;
      ToTest <<= 1;
      }

    return ShiftBy;
    }





  private void LongDivide3( Integer ToDivide,
                            Integer DivideBy,
                            Integer Quotient,
                            Integer Remainder )
    {
    // int ErrorPoint = 0;

    // try
    // {

    int TestIndex = ToDivide.GetIndex() - DivideBy.GetIndex();
    if( TestIndex < 0 )
      {
      // "TestIndex < 0 in Divide3."
      // Result.SetToZero();
      return;
      }

    if( TestIndex != 0 )
      {
      // Is 1 too high?
      Test1.SetDigitAndClear( TestIndex, 1 );
      MultiplyTopOne( Test1, DivideBy );
      if( ToDivide.ParamIsGreater( Test1 ))
        TestIndex--;

      }

    // Keep a copy of the originals.
    ToDivideKeep.Copy( ToDivide );
    DivideByKeep.Copy( DivideBy );

    ulong TestBits = DivideBy.GetD( DivideBy.GetIndex());
    int ShiftBy = FindShiftBy( TestBits );
    ToDivide.ShiftLeft( ShiftBy ); // Multiply the numerator and the denominator
    DivideBy.ShiftLeft( ShiftBy ); // by the same amount.

    // ErrorPoint = 1;

    ulong MaxValue;
    if( (ToDivide.GetIndex() - 1) > (DivideBy.GetIndex() + TestIndex) )
      {
      MaxValue = ToDivide.GetD( ToDivide.GetIndex());
      }
    else
      {
      MaxValue = ToDivide.GetD( ToDivide.GetIndex()) << 32;
      MaxValue |= ToDivide.GetD( ToDivide.GetIndex() - 1 );
      }

    // ErrorPoint = 2;

    // Notice how this ToMatch is different from the theory from the sixties.
    // which assumes some other Base.
    ulong Denom = DivideBy.GetD( DivideBy.GetIndex());
    if( Denom != 0 )
      MaxValue = MaxValue / Denom;
    else
      MaxValue = 0xFFFFFFFF;

    if( MaxValue > 0xFFFFFFFF )
      MaxValue = 0xFFFFFFFF;

    ////////////////////////
    if( MaxValue == 0 )
      {
      throw( new Exception( "MaxValue is zero at the top in LongDivide3()." ));
      }
    ////////////////////

    // ulong TestGap;
    
    Quotient.SetDigitAndClear( TestIndex, 1 );
    Quotient.SetD( TestIndex, 0 );

    Test1.Copy( Quotient );
    Test1.SetD( TestIndex, MaxValue );
    MultiplyTop( Test1, DivideBy );

    /*
    Test2.Copy( Quotient );
    Test2.SetD( TestIndex, MaxValue );
    Multiply( Test2, DivideBy );
    if( !Test2.IsEqual( Test1 ))
      throw( new Exception( "In Divide3() !IsEqual( Test2, Test1 )" ));
    */

    if( Test1.ParamIsGreaterOrEq( ToDivide ))
      {
      // ToMatchExactCount++;
      // Most of the time (roughly 5 out of every 6 times) 
      // this MaxValue estimate is exactly right:
      Quotient.SetD( TestIndex, MaxValue );
      }
    else
      {
      // MaxValue can't be zero here. If it was it would
      // already be low enough before it got here.
      MaxValue--;

      ////////////////
      if( MaxValue == 0 )
        throw( new Exception( "After decrement: MaxValue is zero in LongDivide3()." ));
      ////////////////
      
      // ErrorPoint = 3;

      Test1.Copy( Quotient );
      Test1.SetD( TestIndex, MaxValue );
      MultiplyTop( Test1, DivideBy );

      /*
      Test2.Copy( Quotient );
      Test2.SetD( TestIndex, MaxValue );
      Multiply( Test2, DivideBy );
      if( !Test2.IsEqual( Test1 ))
        throw( new Exception( "Top one. !Test2.IsEqual( Test1 ) in LongDivide3()" ));
      */
    
      if( Test1.ParamIsGreaterOrEq( ToDivide ))
        {
        // ToMatchDecCount++;
        Quotient.SetD( TestIndex, MaxValue );
        }
      else
        {
        // TestDivideBits is done as a last resort, but it's rare.
        // But it does at least limit it to a worst case scenario
        // of trying 32 bits, rather than 4 billion or so decrements.

        TestDivideBits( MaxValue,
                        true,
                        TestIndex,
                        ToDivide,
                        DivideBy,
                        Quotient,
                        Remainder );
        }

      // TestGap = MaxValue - LgQuotient.D[TestIndex];
      // if( TestGap > HighestToMatchGap )
        // HighestToMatchGap = TestGap;

      // HighestToMatchGap: 4,294,967,293
      // uint size:         4,294,967,295 uint
      }

    // ErrorPoint = 4;

    // If it's done.
    if( TestIndex == 0 )
      {
      Test1.Copy( Quotient );
      Multiply( Test1, DivideByKeep );
      Remainder.Copy( ToDivideKeep );
      Subtract( Remainder, Test1 );

      /////////////////////////////
      if( DivideByKeep.ParamIsGreater( Remainder ))
        {
        ///////////
        throw( new Exception( "Remainder > DivideBy in LongDivide3()." ));
        }
      //////////////////////////////

      // if( Remainder.IsZero())
        return; // true;
      // else
        // return false;

      }

    // Now do the rest of the digits.
    TestIndex--;
    while( true )
      {
      // ErrorPoint = 5;
      Test1.Copy( Quotient );
      Multiply( Test1, DivideBy );

      //////////////////////////
      if( ToDivide.ParamIsGreater( Test1 ))
        {
        throw( new Exception( "Bug here in LongDivide3()." ));
        }
      ///////////////////////////

      Remainder.Copy( ToDivide );
      Subtract( Remainder, Test1 );
      MaxValue = Remainder.GetD( Remainder.GetIndex()) << 32;

      // ErrorPoint = 6;

      int CheckIndex = Remainder.GetIndex() - 1;
      if( CheckIndex > 0 )
        MaxValue |= Remainder.GetD( CheckIndex );

      // ErrorPoint = 7;

      Denom = DivideBy.GetD( DivideBy.GetIndex());
      if( Denom != 0 )
        MaxValue = MaxValue / Denom;
      else
        MaxValue = 0xFFFFFFFF;

      if( MaxValue > 0xFFFFFFFF )
        MaxValue = 0xFFFFFFFF;

      Test1.Copy( Quotient );
      Test1.SetD( TestIndex, MaxValue );
      Multiply( Test1, DivideBy );

      if( Test1.ParamIsGreaterOrEq( ToDivide ))
        {
        // Most of the time this MaxValue estimate is exactly right:
        // ToMatchExactCount++;
        Quotient.SetD( TestIndex, MaxValue );
        }
      else
        {
        MaxValue--;
        Test1.Copy( Quotient );
        Test1.SetD( TestIndex, MaxValue );
        Multiply( Test1, DivideBy );
        if( Test1.ParamIsGreaterOrEq( ToDivide ))
          {
          // ToMatchDecCount++;
          Quotient.SetD( TestIndex, MaxValue );
          }
        else
          {
          TestDivideBits( MaxValue,
                          false,
                          TestIndex,
                          ToDivide,
                          DivideBy,
                          Quotient,
                          Remainder );

          // TestGap = MaxValue - LgQuotient.D[TestIndex];
          // if( TestGap > HighestToMatchGap )
            // HighestToMatchGap = TestGap;

          }
        }


      if( TestIndex == 0 )
        break;

      TestIndex--;
      }

    // ErrorPoint = 8;

    //////////
    Test1.Copy( Quotient );
    Multiply( Test1, DivideByKeep );
    Remainder.Copy( ToDivideKeep );
    Subtract( Remainder, Test1 );

    /////////////////////////////////
    if( DivideByKeep.ParamIsGreater( Remainder ))
      {
      throw( new Exception( "Remainder > DivideBy in LongDivide3()." ));
      }
    /////////////////////////////////

    // if( Remainder.IsZero())
      return; //  true;
    // else
      // return false;

    /*
    }
    catch( Exception ) // Except )
      {
      // "Bug in Divide3: " + ErrorPoint.ToString() + "\r\n\r\n" + Except.Message
      return false;
      }
    */
    }




  // Finding the square root of a number is similar to division since
  // it is a search algorithm.  The TestSqrtBits method shown next is
  // very much like TestDivideBits().  It works the same as
  // FindULSqrRoot(), but on a bigger scale.
  /*
  private void TestSqrtBits( int TestIndex, Integer Square, Integer SqrRoot )
    {
    Integer Test1 = new Integer();

    uint BitTest = 0x80000000;
    for( int BitCount = 31; BitCount >= 0; BitCount-- )
      {
      Test1.Copy( SqrRoot );
      Test1.D[TestIndex] |= BitTest;
      Test1.Square();
      if( !Square.ParamIsGreater( Test1 ) )
        SqrRoot.D[TestIndex] |= BitTest; // Use the bit.
        
      BitTest >>= 1;
      } 
    }
    */



  // In the SquareRoot() method SqrRoot.Index is half of Square.Index.
  // Compare this to the Square() method where the Carry might or
  // might not increment the index to an odd number.  (So if the Index
  // was 5 its square root would have an Index of 5 / 2 = 2.)

  // The SquareRoot1() method uses FindULSqrRoot() either to find the
  // whole answer, if it's a small number, or it uses it to find the
  // top part.  Then from there it goes on to a bit by bit search
  // with TestSqrtBits().
  
  public bool SquareRoot( Integer Square, Integer SqrRoot ) 
    {
    ulong ToMatch;
    if( Square.IsULong() )
      {
      ToMatch = Square.GetAsULong();
      SqrRoot.SetD( 0, FindULSqrRoot( ToMatch ));
      SqrRoot.SetIndex( 0 );
      if( (SqrRoot.GetD(0 ) * SqrRoot.GetD( 0 )) == ToMatch )
        return true;
      else
        return false;

      }

    Integer Test1 = new Integer();

    int TestIndex = Square.GetIndex() >> 1; // LgSquare.Index / 2;
    SqrRoot.SetDigitAndClear( TestIndex, 1 );
    // if( (TestIndex * 2) > (LgSquare.Index - 1) )
    if( (TestIndex << 1) > (Square.GetIndex() - 1) )
      {
      ToMatch = Square.GetD( Square.GetIndex());
      }
    else
      {
      // LgSquare.Index is at least 2 here.
      ToMatch = Square.GetD( Square.GetIndex()) << 32;
      ToMatch |= Square.GetD( Square.GetIndex() - 1 );
      }

    SqrRoot.SetD( TestIndex, FindULSqrRoot( ToMatch ));

    TestIndex--;
    while( true )
      {
      // TestSqrtBits( TestIndex, LgSquare, LgSqrRoot );
      SearchSqrtXPart( TestIndex, Square, SqrRoot );
      if( TestIndex == 0 )
        break;

      TestIndex--;
      }

    // Avoid squaring the whole thing to see if it's an exact square root:
    if( ((SqrRoot.GetD( 0 ) * SqrRoot.GetD( 0 )) & 0xFFFFFFFF) != Square.GetD( 0 ))
      return false;
    
    Test1.Copy( SqrRoot );
    DoSquare( Test1 );
    if( Square.IsEqual( Test1 ))
      return true;
    else
      return false;
  
    }



  // Test all this.

  private void SearchSqrtXPart( int TestIndex, Integer Square, Integer SqrRoot )
    {
    // B is the Big part of the number that has already been found.
    // S = (B + x)^2
    // S = B^2 + 2Bx + x^2
    // S - B^2 = 2Bx + x^2
    // R = S - B^2
    // R = 2Bx + x^2
    // R = x(2B + x)
    Integer Test1 = new Integer();
    Integer Test2 = new Integer();
    Integer Remainder = new Integer();
    Integer R2 = new Integer();
    Integer TwoB = new Integer();

    Test1.Copy( SqrRoot ); // B
    DoSquare( Test1 ); // B^2
    Remainder.Copy( Square );
    Subtract( Remainder, Test1 ); // S - B^2
    TwoB.Copy( SqrRoot ); // B
    TwoB.ShiftLeft( 1 ); // Times 2 for 2B.
    Test1.Copy( TwoB ); 
    ulong TestBits = Test1.GetD( Test1.GetIndex());
    int ShiftBy = FindShiftBy( TestBits );
    R2.Copy( Remainder );
    R2.ShiftLeft( ShiftBy );     // Multiply the numerator and the denominator
    Test1.ShiftLeft( ShiftBy ); // by the same amount.

    ulong Highest; 
    if( R2.GetIndex() == 0 )
      {
      Highest = R2.GetD( R2.GetIndex());
      }
    else
      {
      Highest = R2.GetD( R2.GetIndex()) << 32;
      Highest |= R2.GetD( R2.GetIndex() - 1 );
      }

    Highest = Highest / Test1.GetD( Test1.GetIndex());
    if( Highest == 0 )
      {
      SqrRoot.SetD( TestIndex, 0 );
      return; 
      }

    if( Highest > 0xFFFFFFFF )
      Highest = 0xFFFFFFFF;

    uint BitTest = 0x80000000;
    ulong XDigit = 0;
    ulong TempXDigit = 0;
    for( int BitCount = 0; BitCount < 32; BitCount++ )
      {
      TempXDigit = XDigit | BitTest;
      if( TempXDigit > Highest )
        {
        BitTest >>= 1;
        continue;
        }

      Test1.Copy( TwoB );
      Test1.SetD( TestIndex, TempXDigit ); // 2B + x
      Test2.SetDigitAndClear( TestIndex, TempXDigit ); // Set X.
      MultiplyTop( Test2, Test1 ); 
      if( Test2.ParamIsGreaterOrEq( Remainder ))
        XDigit |= BitTest; // Then keep the bit.

      BitTest >>= 1;
      } 

    SqrRoot.SetD( TestIndex, XDigit );
    }




  // This is the recursive one.
  // I wrote these based on the "psuedo code" for it in Wikipedia.
  // http://en.wikipedia.org/wiki/Square-and-multiply_algorithm
  /*
  internal void ModularPower1( Integer Result, Integer Exponent, Integer ModN )
    {
    // This gets called recursively for as many bits as there are in the exponent.
    //////////
    From Wikipedia:
    Function exp-by-squaring(x,n)
     if n=0 then return 1;
     else if n=1 then return x;
     else if n is even then return exp-by-squaring(x*x, n/2);
     else if n is odd then return x * exp-by-squaring(x*x, (n-1)/2).
    //////////


    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( ModN ))
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

    if( ModN.ParamIsGreater( Result ))
      {
      Divide( Result, ModN, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    Integer ResultCopy = new Integer();
    ResultCopy.Copy( Result );
    Integer ExponentCopy = new Integer();
    ExponentCopy.Copy( Exponent );

    if( (Exponent.GetD( 0 ) & 1) == 0 )
      {
      // If the exponent is even.
      // Call this function recursively.
      // Use _local_ copies of Integers.
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.
      Multiply( ResultCopy, Result ); // Square it.
      ModularPower1( ResultCopy, ExponentCopy, ModN );
      Result.Copy( ResultCopy );
      }
    else
      {
      // If it's odd.
      // else if n is odd then return x * exp-by-squaring(x*x, (n-1)/2).

      // The exponent is more than 1 here.
      SubtractULong( ExponentCopy, 1 );
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.

      Multiply( ResultCopy, Result ); // Square it.
      ModularPower1( ResultCopy, ExponentCopy, ModN );
      Multiply( Result, ResultCopy );
      }

    if( ModN.ParamIsGreater( Result ))
      {
      Divide( Result, ModN, Quotient, Remainder );
      Result.Copy( Remainder );
      }
    }
    */



  internal void ModularPower2( Integer Result, Integer Exponent, Integer ModN )
    {
    if( Result.IsZero())
      return; // With Result still zero.

    if( Result.IsEqual( ModN ))
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

    if( ModN.ParamIsGreater( Result ))
      {
      Divide( Result, ModN, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToULong( 1 ))
      {
      // Result stays the same.
      return;
      }

    XForModPower.Copy( Result );

    /*
    From Wikipedia Ruby example:
    def power(x,n)
      result = 1
      while n.nonzero?
        if n[0].nonzero?
          result *= x
          n -= 1
        end
      x *= x
      n /= 2
      end

    return result
    end
    */

    ExponentCopy.Copy( Exponent );

    Result.SetFromULong( 1 );
    while( !ExponentCopy.IsZero())
      {
      // If it's odd.
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 )
        {
        Multiply( Result, XForModPower );
        SubtractULong( ExponentCopy, 1 );
        }

      // Square it.
      Multiply( XForModPower, XForModPower );
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.

      if( ModN.ParamIsGreater( Result ))
        {
        // Here is where that really long division algorithm gets used a
        // lot in a loop.  There are ways to optimize this. (Barrett Reduction?)
        // The best way would be to use a "modular aware" multiplication
        // algorithm so that the multiplication itself (or squaring) gets
        // done in a way so that this division never has to happen.
        // It would be a Karatsuba-like algorithm that uses smaller numbers
        // for its calculations.
        Divide( Result, ModN, Quotient, Remainder );
        Result.Copy( Remainder );
        }

      if( ModN.ParamIsGreater( XForModPower ))
        {
        Divide( XForModPower, ModN, Quotient, Remainder );
        XForModPower.Copy( Remainder );
        }
      }
    }



  internal void GreatestCommonDivisor( Integer A, Integer B, Integer Gcd )
    {
    // Don't do GCD with something that is zero.
    if( A.IsZero())
      throw( new Exception( "Doing GCD with a parameter that is zero." ));

    if( B.IsZero())
      throw( new Exception( "Doing GCD with a parameter that is zero." ));

    if( A.IsEqual( B ))
      {
      Gcd.Copy( A );
      return;
      }

    // Don't change the original numbers that came in as parameters.
    if( A.ParamIsGreater( B ))
      {
      // Switch them around.
      Temp1.Copy( B );
      Temp2.Copy( A );
      }
    else
      {
      Temp1.Copy( A );
      Temp2.Copy( B );
      }

    while( true )
      {
      Divide( Temp1, Temp2, Quotient, Remainder );
      if( Remainder.IsZero())
        {
        Gcd.Copy( Temp2 ); // It's the smaller one.
        // It can't return from this loop until the remainder is zero.
        return;
        }

      Temp1.Copy( Temp2 ); // B is always bigger than the remainder.
      Temp2.Copy( Remainder );
      }
    }



  internal bool IsFermatPrime( Integer ToTest, int HowMany )
    {
    // Also see Rabin-Miller test.
    // Also see Solovay-Strassen test.

    // Use bigger primes for Fermat test because mod 3, mod 5 etc are too likely
    // to be congruent to 1.  In other words it's a lot more likely to appear
    // to be a prime when it isn't.  This Fermat primality test is usually
    // described as using random primes to test with, and you could do
    // it that way too.
    int StartAt = 20; // Or much bigger.
    for( int Count = StartAt; Count < (HowMany + StartAt); Count++ )
      {
      if( !IsFermatPrimeForOneValue( ToTest, PrimeArray[Count] ))
        return false;

      }

    // It _might_ be a prime if it passed this test.
    // Increasing HowMany increases the probability that it's a prime.
    return true;
    }



  // http://en.wikipedia.org/wiki/Primality_test
  // http://en.wikipedia.org/wiki/Fermat_primality_test

  internal bool IsFermatPrimeForOneValue( Integer ToTest, ulong Base  )
    {
    // This won't catch Carmichael numbers.
    // http://en.wikipedia.org/wiki/Carmichael_number

    // Assume ToTest is not a small number.  (Not the size of a small prime.)
    // Normally it would be something like a 1024 bit number or bigger, 
    // but I assume it's at least bigger than a 32 bit number.
    // Assume this has already been checked to see if it's divisible
    // by a small prime.

    // A has to be coprime to P and it is here because ToTest is not 
    // divisible by a small prime.

    // Fermat's little theorem:
    // A ^ (P - 1) is congruent to 1 mod P if P is a prime.
    // Or: A^P - A is congrunt to A mod P.
    // If you multiply A by itself P times then divide it by P, 
    // the remainder is A.  (A^P / P)
    // 5^3 = 125.  125 - 5 = 120.  A multiple of 5.
    // 2^7 = 128.  128 - 2 = 7 * 18 (a multiple of 7.)

    Temp1.Copy( ToTest );
    SubtractULong( Temp1, 1 );
    Temp2.SetFromULong( Base );

    ModularPower2( Temp2, Temp1, ToTest );
    if( Temp2.IsOne())
      return true; // It passed the test. It _might_ be a prime.
    else
      return false; // It is _definitely_ a composite number.

    }




  }
}



