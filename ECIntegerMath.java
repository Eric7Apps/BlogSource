// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

package com.yourpackage.name;


  // These ideas mainly come from a set of books written by Donald Knuth
  // in the 1960s called "The Art of Computer Programming", especially
  // Volume 2 - Seminumerical Algorithms.

  // For more on division see also:
  // Brinch Hansen, Multiple-Length Division Revisited, 1994
  // http://brinch-hansen.net/papers/1994b.pdf



import java.util.Locale;



public class ECIntegerMath
  {
  private long[] SignedD; // Signed digits for use in subtraction.
  private long[][] M; // Scratch pad, just like you would do on paper.
  private ECInteger Quotient;
  private ECInteger Remainder;
  private ECInteger Test1;
  private ECInteger Test2;
  private ECInteger ToDivideKeep;
  private ECInteger DivideByKeep;
  private ECInteger DivideBy;
  private ECInteger Temp1;
  private ECInteger Temp2;
  private ECInteger Temp3;
  private ECInteger ToDivide;
  private ECInteger XForModPower;
  private ECInteger ExponentCopy;
  // private Random Rand;
  protected int PrimeArrayLast = 0;
  protected int[] PrimeArray;




  protected ECIntegerMath() throws Exception
    {
    SignedD = new long[ECInteger.DigitArraySize];
    M = new long[ECInteger.DigitArraySize][ECInteger.DigitArraySize];
    Quotient = new ECInteger();
    Remainder = new ECInteger();
    Test1 = new ECInteger();
    Test2 = new ECInteger();
    ToDivideKeep = new ECInteger();
    DivideByKeep = new ECInteger();
    DivideBy = new ECInteger();
    Temp1 = new ECInteger();
    Temp2 = new ECInteger();
    Temp3 = new ECInteger();
    ToDivide = new ECInteger();
    XForModPower = new ECInteger();
    ExponentCopy = new ECInteger();

    // Rand = new Random();
    MakePrimeArray();
    }




  protected int GetFirstPrimeFactor( int ToTest ) throws Exception
    {
    if( ToTest <= 3 )
      return 0;
      
    int Max = (int)FindLSqrRoot( ToTest ); 

    for( int Count = 0; Count < PrimeArrayLast; Count++ )
      {
      int TestN = PrimeArray[Count];
      if( (ToTest % TestN) == 0 )
        return TestN;

      if( TestN > Max )
        return 0;

      }

    return 0;
    }




  private void MakePrimeArray() throws Exception
    {
    // try
    PrimeArray = new int[1024 * 2]; // I am not making big random primes from the client side here.
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
    for( int TestN = 29; ; TestN += 2 )
      {
      if( (TestN % 3) == 0 )
        continue;
          
      // If it has no prime factors then add it.
      if( 0 == GetFirstPrimeFactor( TestN ))
        {
        PrimeArray[PrimeArrayLast] = TestN;
        PrimeArrayLast++;

        if( PrimeArrayLast >= PrimeArray.length )
          return;

        }
      }
    }



  protected int IsDivisibleBySmallPrime( ECInteger ToTest ) throws Exception
    {
    if( (ToTest.GetD( 0 ) & 1) == 0 )
      return 2; // It's divisible by 2.

    if( 0 == GetMod3( ToTest ))
      return 3;

    for( int Count = 2; Count < PrimeArrayLast; Count++ )
      {
      if( 0 == GetMod24( ToTest, PrimeArray[Count] ))
        return PrimeArray[Count];

      }

    // No small primes divide it.
    return 0;
    }




  protected void SubtractLong( ECInteger Result, long ToSub ) throws Exception
    {
    if( Result.IsLong())
      {
      long ResultL = Result.GetAsLong();
      if( ToSub > ResultL )
        throw( new Exception( "SubLong() (IsLong() and (ToSub > Result)." ));

      ResultL = ResultL - ToSub;
      Result.SetD( 0, ResultL & 0xFFFFFF );
      Result.SetD( 1, ResultL >> 24 );
      if( Result.GetD( 1 ) == 0 )
        Result.SetIndex( 0 );
      else
        Result.SetIndex( 1 );

      return;
      }

    // If it got this far then Index is at least 2.
    SignedD[0] = Result.GetD( 0 ) - (ToSub & 0xFFFFFF);
    SignedD[1] = Result.GetD( 1 ) - (ToSub >> 24);

    if( (SignedD[0] >= 0) && (SignedD[1] >= 0) )
      {
      // No need to reorganize it.
      Result.SetD( 0, SignedD[0] );
      Result.SetD( 1, SignedD[1] );
      return;
      }


    for( int Count = 2; Count <= Result.GetIndex(); Count++ )
      SignedD[Count] = Result.GetD( Count );

    for( int Count = 0; Count < Result.GetIndex(); Count++ )
      {
      if( SignedD[Count] < 0 )
        {
        SignedD[Count] += (long)0xFFFFFF + 1;
        SignedD[Count + 1]--;
        }
      }
     
    if( SignedD[Result.GetIndex()] < 0 )
      throw( new Exception( "SubLong() SignedD[Index] < 0." ));

    for( int Count = 0; Count <= Result.GetIndex(); Count++ )
      Result.SetD( Count, SignedD[Count] );

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





  protected void Subtract( ECInteger Result, ECInteger ToSub ) throws Exception
    {
    if( ToSub.IsLong() )
      {
      SubtractLong( Result, ToSub.GetAsLong());
      return;
      }

    if( ToSub.GetIndex() > Result.GetIndex() )
      throw( new Exception( "In Subtract() ToSub.Index > Index." ));

    for( int Count = 0; Count <= ToSub.GetIndex(); Count++ )
      SignedD[Count] = Result.GetD( Count ) - ToSub.GetD( Count );

    for( int Count = ToSub.GetIndex() + 1; Count <= Result.GetIndex(); Count++ )
      SignedD[Count] = Result.GetD( Count );
 
    for( int Count = 0; Count < Result.GetIndex(); Count++ )
      {
      if( SignedD[Count] < 0 )
        {
        SignedD[Count] += (long)0xFFFFFF + 1;
        SignedD[Count + 1]--;
        }
      }

    if( SignedD[Result.GetIndex()] < 0 )
      throw( new Exception( "Subtract() SignedD[Index] < 0." ));

    for( int Count = 0; Count <= Result.GetIndex(); Count++ )
      Result.SetD( Count, SignedD[Count] );

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




  private void MultiplyInt( ECInteger Result, int ToMul ) throws Exception
    {
    for( int Column = 0; Column <= Result.GetIndex(); Column++ )
      M[Column][0] = ToMul * Result.GetD( Column );
     
    // Add these up with a carry.
    Result.SetD( 0, M[0][0] & 0xFFFFFF );
    long Carry = M[0][0] >> 24;
    for( int Column = 1; Column <= Result.GetIndex(); Column++ )
      {
      long Total = M[Column][0] + Carry;
      Result.SetD( Column, Total & 0xFFFFFF );
      Carry = Total >> 24;
      }

    if( Carry != 0 )
      {
      Result.IncrementIndex();
        // throw( new Exception( "MultiplyUInt() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  protected void MultiplyLong( ECInteger Result, long ToMul ) throws Exception
    {
    if( Result.IsZero())
      return; // Then the answer is zero, which it already is.

    if( ToMul == 0 )
      {
      Result.SetToZero();
      return;
      }

    long B0 = ToMul & 0xFFFFFF;
    long B1 = ToMul >> 24;
    if( (B1 >> 24) != 0 )
      throw( new Exception( "(B1 >> 24) != 0 in MultiplyLong." ));

    if( B1 == 0 )
      {
      MultiplyInt( Result, (int)B0 );
      return;
      }

    // Since B1 is not zero:
    if( (Result.GetIndex() + 1) >= ECInteger.DigitArraySize ) 
      throw( new Exception( "Overflow in MultiplyLong." ));
     
    for( int Column = 0; Column <= Result.GetIndex(); Column++ )
      {
      M[Column][0] = B0 * Result.GetD( Column );
      // Column + 1 and Row is 1, so it's just like pen and paper.
      M[Column + 1][1] = B1 * Result.GetD( Column );
      }

    // Since B1 is not zero, the index is set one higher.
    Result.IncrementIndex(); // Might throw an exception if it goes out of range.

    M[Result.GetIndex()][0] = 0; // Otherwise it would be undefined
                                 // when it's added up below.

    // Add these up with a carry.
    Result.SetD( 0, M[0][0] & 0xFFFFFF );
    long Carry = M[0][0] >> 24;
    for( int Column = 1; Column <= Result.GetIndex(); Column++ )
      {
      // Split the longs into right and left sides
      // so that they don't overflow.
      long TotalLeft = 0;
      long TotalRight = 0;
      // There's only the two rows for this.
      for( int Row = 0; Row <= 1; Row++ )
        {
        TotalRight += M[Column][Row] & 0xFFFFFF;
        TotalLeft += M[Column][Row] >> 24;
        }

      TotalRight += Carry;
      Result.SetD( Column, TotalRight & 0xFFFFFF );
      Carry = TotalRight >> 24;
      Carry += TotalLeft;
      }

    if( Carry != 0 )
      {
      Result.IncrementIndex();
      //  throw( new Exception( "MulLong() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  // See also: http://en.wikipedia.org/wiki/Karatsuba_algorithm

  protected void Multiply( ECInteger Result, ECInteger ToMul ) throws Exception
    {
    if( ToMul.IsLong())
      {
      MultiplyLong( Result, ToMul.GetAsLong());
      return;
      }

    if( Result.IsZero())
      return;

    // It could never get here if ToMul is zero because GetIsLong()
    // would be true for zero.
    // if( ToMul.IsZero())

    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= ECInteger.DigitArraySize )
      throw( new Exception( "Multiply() overflow." ));

    for( int Row = 0; Row <= ToMul.GetIndex(); Row++ )
      {
      if( ToMul.GetD( Row ) == 0 )
        {
        for( int Column = 0; Column <= Result.GetIndex(); Column++ )
          M[Column + Row][Row] = 0;

        }
      else
        {
        for( int Column = 0; Column <= Result.GetIndex(); Column++ )
          M[Column + Row][Row] = ToMul.GetD( Row ) * Result.GetD( Column );

        }
      }

    // Add the columns up with a carry.
    Result.SetD( 0, M[0][0] & 0xFFFFFF );
    long Carry = M[0][0] >> 24;
    for( int Column = 1; Column <= TotalIndex; Column++ )
      {
      long TotalLeft = 0;
      long TotalRight = 0;
      for( int Row = 0; Row <= ToMul.GetIndex(); Row++ )
        {
        if( Row > Column )
          break;

        if( Column > (Result.GetIndex() + Row) )
          continue;

        // Split the longs into right and left sides
        // so that they don't overflow.
        TotalRight += M[Column][Row] & 0xFFFFFF;
        TotalLeft += M[Column][Row] >> 24;
        }

      TotalRight += Carry;
      Result.SetD( Column, TotalRight & 0xFFFFFF );
      Carry = TotalRight >> 24;
      Carry += TotalLeft;
      }

    Result.SetIndex( TotalIndex );
    if( Carry != 0 )
      {
      Result.IncrementIndex();
      //  throw( new Exception( "Multiply() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  // The ShortDivide() algorithm works like dividing a polynomial which
  // looks like: 
  // (ax3 + bx2 + cx + d) / N = (ax3 + bx2 + cx + d) * (1/N)
  // The 1/N distributes over the polynomial: 
  // (ax3 * (1/N)) + (bx2 * (1/N)) + (cx * (1/N)) + (d * (1/N))

  // The algorithm goes from left to right and reduces that polynomial
  // expression.  So it starts with Quotient being a copy of ToDivide
  // and then it reduces Quotient from left to right.
  private boolean ShortDivide( ECInteger ToDivide,
                            ECInteger DivideBy,
                            ECInteger Quotient,
                            ECInteger Remainder )
    {
    Quotient.Copy( ToDivide );
    // DivideBy has an Index of zero:
    long DivideByU = DivideBy.GetD( 0 );
    long RemainderU = 0;

    // Get the first one set up.
    if( DivideByU > Quotient.GetD( Quotient.GetIndex()) )
      {
      Quotient.SetD( Quotient.GetIndex(), 0 );
      }
    else
      {
      long OneDigit = Quotient.GetD( Quotient.GetIndex() );
      Quotient.SetD( Quotient.GetIndex(), OneDigit / DivideByU );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }

    // Now do the rest.
    for( int Count = Quotient.GetIndex(); Count >= 1; Count-- )
      {
      long TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 24;
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
  // Also, DivideBy is a long.
  protected long ShortDivideRem( ECInteger ToDivideOriginal,
                               long DivideByU,
                               ECInteger Quotient ) throws Exception
    {
    if( ToDivideOriginal.IsLong())
      {
      long ToDiv = ToDivideOriginal.GetAsLong();
      long Q = ToDiv / DivideByU;
      Quotient.SetFromLong( Q );
      return ToDiv % DivideByU;
      }

    ECInteger ToDivide = new ECInteger();

    ToDivide.Copy( ToDivideOriginal );
    Quotient.Copy( ToDivide );

    long RemainderU = 0;
    if( DivideByU > Quotient.GetD( Quotient.GetIndex() ))
      {
      Quotient.SetD( Quotient.GetIndex(), 0 );
      }
    else
      {
      long OneDigit = Quotient.GetD( Quotient.GetIndex() );
      Quotient.SetD( Quotient.GetIndex(), OneDigit / DivideByU );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }

    for( int Count = Quotient.GetIndex(); Count >= 1; Count-- )
      {
      long TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 24;
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
  // 2^24 is 16,777,216.
  protected long GetMod24( ECInteger ToDivideOriginal, long DivideByU ) throws Exception
    {
    // For testing:
    if( (DivideByU >> 24) != 0 )
      throw( new Exception( "GetMod24: (DivideByU >> 24) != 0." ));

    // If this is _equal_ to a small prime it would return zero.
    if( ToDivideOriginal.IsLong())
      {
      long Result = ToDivideOriginal.GetAsLong();
      return Result % DivideByU;
      }

    ToDivide.Copy( ToDivideOriginal );
    long RemainderU = 0;

    if( DivideByU <= ToDivide.GetD( ToDivide.GetIndex() ))
      {
      long OneDigit = ToDivide.GetD( ToDivide.GetIndex() );
      RemainderU = OneDigit % DivideByU;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }
 
    for( int Count = ToDivide.GetIndex(); Count >= 1; Count-- )
      {
      long TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 24;
      TwoDigits |= ToDivide.GetD( Count - 1 );
      RemainderU = TwoDigits % DivideByU;
      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU );
      }

    return RemainderU;
    }
    


  // Optimize this for Mod 3.
  protected int GetMod3( ECInteger ToDivideOriginal )
    {
    if( ToDivideOriginal.IsLong())
      {
      long Result = ToDivideOriginal.GetAsLong();
      return (int)(Result % 3);
      }

    ToDivide.Copy( ToDivideOriginal );
    long RemainderU = 0;

    if( 3 <= ToDivide.GetD( ToDivide.GetIndex() ))
      {
      long OneDigit = ToDivide.GetD( ToDivide.GetIndex() );
      RemainderU = OneDigit % 3;
      ToDivide.SetD( ToDivide.GetIndex(), RemainderU );
      }
 
    for( int Count = ToDivide.GetIndex(); Count >= 1; Count-- )
      {
      long TwoDigits = ToDivide.GetD( Count );
      TwoDigits <<= 24;
      TwoDigits |= ToDivide.GetD( Count - 1 );
      RemainderU = TwoDigits % 3;
      ToDivide.SetD( Count, 0 );
      ToDivide.SetD( Count - 1, RemainderU );
      }

    return (int)RemainderU;
    }




  private long GetMod48FromTwoLongs( long P1, long P0, long Divisor48 ) throws Exception
    {
    // For testing:
    if( Divisor48 <= 0xFFFFFF )
      throw( new Exception( "GetMod48FromTwoLongs Divisor48 <= 0xFFFFFF" ));

    // This is never shifted more than 12 bits, so check to make sure there's
    // room to shift it.
    if( (Divisor48 >> 48) != 0 )
      throw( new Exception( "Divisor48 is too big in GetMod48FromTwoULongs." ));
    
    if( P1 == 0 )
      return P0 % Divisor48;

    // See Gauss Disquisitions, first chapter:
    // R ~ (a*b) mod m
    // R ~ ((a mod m) * (b mod m)) mod m
    // (P1 * 2^64) + P0 is what the number is.
 
    long Part1 = P1 % Divisor48;
    if( (Divisor48 >> 40) == 0 )
      {
      // Then this can be done 24 bits at a time.
      Part1 <<= 24;  // Times 2^24
      Part1 = Part1 % Divisor48;
      Part1 <<= 24;  //  48
      Part1 = Part1 % Divisor48;

      // Part1 <<= 16;  // Brings it to 64
      // Part1 = Part1 % Divisor48;
      }
    else
      {
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor48;
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor48;
      Part1 <<= 12;  // Times 2^12
      Part1 = Part1 % Divisor48;
      Part1 <<= 12;  // Times 2^12 Brings it to 48.
      Part1 = Part1 % Divisor48;

      // Part1 <<= 8;  // Times 2^8
      // Part1 = Part1 % Divisor48;
      // Part1 <<= 8;  // Times 2^8 Brings it to 64.
      // Part1 = Part1 % Divisor48;
      }

    // All of the above was just to get the P1 part of it, so now add P0:
    return (Part1 + P0) % Divisor48;
    }




  protected long GetMod48( ECInteger ToDivideOriginal, long DivideBy ) throws Exception
    {
    if( ToDivideOriginal.IsLong())
      return ToDivideOriginal.GetAsLong() % DivideBy;
    
    // Don't create this here.
    ECInteger ToDivide = new ECInteger();
    ToDivide.Copy( ToDivideOriginal );

    long Digit1;
    long Digit0;
    long Remainder;

    if( ToDivide.GetIndex() == 2 )
      {
      Digit1 = ToDivide.GetD( 2 );
      Digit0 = ToDivide.GetD( 1 ) << 24;
      Digit0 |= ToDivide.GetD( 0 );
      return GetMod48FromTwoLongs( Digit1, Digit0, DivideBy );
      }

    if( ToDivide.GetIndex() == 3 )
      {
      Digit1 = ToDivide.GetD( 3 ) << 24;
      Digit1 |= ToDivide.GetD( 2 );
      Digit0 = ToDivide.GetD( 1 ) << 24;
      Digit0 |= ToDivide.GetD( 0 );
      return GetMod48FromTwoLongs( Digit1, Digit0, DivideBy );
      }

    int Where = ToDivide.GetIndex();
    while( true )
      {
      if( Where <= 3 )
        {
        if( Where < 2 ) // This can't happen.
          throw( new Exception( "Bug: GetMod48(): Where < 2." ));

        if( Where == 2 )
          {
          Digit1 = ToDivide.GetD( 2 );
          Digit0 = ToDivide.GetD( 1 ) << 24;
          Digit0 |= ToDivide.GetD( 0 );
          return GetMod48FromTwoLongs( Digit1, Digit0, DivideBy );
          }

        if( Where == 3 )
          {
          Digit1 = ToDivide.GetD( 3 ) << 24;
          Digit1 |= ToDivide.GetD( 2 );
          Digit0 = ToDivide.GetD( 1 ) << 24;
          Digit0 |= ToDivide.GetD( 0 );
          return GetMod48FromTwoLongs( Digit1, Digit0, DivideBy );
          }
        }
      else
        {
        // The index is bigger than 3.
        // This part would get called at least once.
        Digit1 = ToDivide.GetD( Where ) << 24;
        Digit1 |= ToDivide.GetD( Where - 1 );
        Digit0 = ToDivide.GetD( Where - 2 ) << 24;
        Digit0 |= ToDivide.GetD( Where - 3 );
        Remainder = GetMod48FromTwoLongs( Digit1, Digit0, DivideBy );

        ToDivide.SetD( Where, 0 );
        ToDivide.SetD( Where - 1, 0 );
        ToDivide.SetD( Where - 2, Remainder >> 24 );
        ToDivide.SetD( Where - 3, Remainder & 0xFFFFFF );
        }

      Where -= 2;
      }
    }
    



  protected void SetFromString( ECInteger Result, String InString ) throws Exception
    {
    Base10Number Base10N = new Base10Number();
    ECInteger Tens = new ECInteger();
    ECInteger OnePart = new ECInteger();

    // This might throw an exception if the string is bad.
    Base10N.SetFromString( InString );

    Result.SetFromLong( Base10N.GetD( 0 ));
    Tens.SetFromLong( 10 );

    for( int Count = 1; Count <= Base10N.GetIndex(); Count++ )
      {
      OnePart.SetFromLong( Base10N.GetD( Count ));
      Multiply( OnePart, Tens );
      Result.Add( OnePart );
      MultiplyLong( Tens, 10 );
      }
    }




  protected String ToString10( ECInteger From ) throws Exception
    {
    if( From.IsLong())
      {
      long N = From.GetAsLong();
      return String.format( Locale.US, "%,d", N );
      }

    ECInteger ToDivide = new ECInteger();
    ECInteger Quotient = new ECInteger();

    String Result = "";
    ToDivide.Copy( From );
    int CommaCount = 0;
    while( !ToDivide.IsZero())
      {
      int Digit = (int)ShortDivideRem( ToDivide, 10, Quotient );
      ToDivide.Copy( Quotient );
      if( ((CommaCount % 3) == 0) && (CommaCount != 0) )
        Result = Integer.toString( Digit ) + "," + Result;
      else
        Result = Integer.toString( Digit ) + Result;

      CommaCount++;
      }

    return Result;
    }
  



  protected static boolean IsSmallQuadResidue( int Number )
    {
    int Test = Number % 3; // 0, 1, 1, 0
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




  protected static boolean FirstBytesAreQuadRes( int Test )
    {
    // Is this number a square mod 2^12?
    // (Quadratic residue mod 2^12)

    int FirstByte = Test;
    int SecondByte = (FirstByte & 0x0F00) >> 8;

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





  protected void DoSquare( ECInteger ToSquare ) throws Exception
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
    if( DoubleIndex >= ECInteger.DigitArraySize )
      {    
      throw( new Exception( "Square() overflowed." ));
      }        
       
    for( int Row = 0; Row <= ToSquare.GetIndex(); Row++ )
      {
      if( ToSquare.GetD( Row ) == 0 )
        {
        for( int Column = 0; Column <= ToSquare.GetIndex(); Column++ )
          M[Column + Row][Row] = 0;

        }
      else
        {
        for( int Column = 0; Column <= ToSquare.GetIndex(); Column++ )
          M[Column + Row][Row] = ToSquare.GetD( Row ) * ToSquare.GetD( Column );

        }
      }

    // Add the columns up with a carry.
    ToSquare.SetD( 0, M[0][0] & 0xFFFFFF );
    long Carry = M[0][0] >> 24;
    for( int Column = 1; Column <= DoubleIndex; Column++ )
      {
      long TotalLeft = 0;
      long TotalRight = 0;
      for( int Row = 0; Row <= Column; Row++ )
        {
        if( Row > ToSquare.GetIndex() )
          break;

        if( Column > (ToSquare.GetIndex() + Row) )
          continue;

        TotalRight += M[Column][Row] & 0xFFFFFF;
        TotalLeft += M[Column][Row] >> 24;
        }

      TotalRight += Carry;
      ToSquare.SetD( Column, TotalRight & 0xFFFFFF );
      Carry = TotalRight >> 24;
      Carry += TotalLeft;
      }

    ToSquare.SetIndex( DoubleIndex );
    if( Carry != 0 )
      {
      ToSquare.SetIndex( ToSquare.GetIndex() + 1 );
      if( ToSquare.GetIndex() >= ECInteger.DigitArraySize ) 
        throw( new Exception( "Square() overflow." ));
      
      ToSquare.SetD( ToSquare.GetIndex(), Carry );
      }
    }




  protected long FindLSqrRoot( long ToMatch ) throws Exception
    {
    // Start OneBit with the highest possible bit.
    long OneBit = 0x800000; // 0x80 0000
    long Result = 0;
    for( int Count = 0; Count < 24; Count++ )
      {
      long ToTry = Result | OneBit;
      if( (ToTry * ToTry) <= ToMatch )
        Result |= OneBit; // Then I want the bit.

      OneBit >>= 1;
      }

    ////////////////////////////////////////////
    // Test:
    if( (Result * Result) > ToMatch )
      throw( new Exception( "FindLSqrRoot() Result is too high." ));

    if( Result != 0  )
      {
      if( ((Result + 1) * (Result + 1)) <= ToMatch )
        throw( new Exception( "FindLSqrRoot() Result is too low." ));

      }
    /////////////////////////////////////////
    
    return Result;
    }




  // This is an optimization for multiplying when only the top digit
  // of a number has been set and all of the other digits are zero.
  protected void MultiplyTop( ECInteger Result, ECInteger ToMul ) throws Exception
    {
    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= ECInteger.DigitArraySize )
      throw( new Exception( "MultiplyTop() overflow." ));

    // Just like Multiply() except that all the other rows are zero:

    // In some of these places I should be calling GetIndex() once, outside of the loop.
    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      M[Column + Result.GetIndex()][Result.GetIndex()] = Result.GetD( Result.GetIndex() ) * ToMul.GetD( Column );

    for( int Column = 0; Column < Result.GetIndex(); Column++ )
      Result.SetD( Column, 0 );

    long Carry = 0;
    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      {
      long Total = M[Column + Result.GetIndex()][Result.GetIndex()] + Carry;
      Result.SetD( Column + Result.GetIndex(), Total & 0xFFFFFF );
      Carry = Total >> 24;
      }

    Result.SetIndex( TotalIndex );
    if( Carry != 0 )
      {
      Result.SetIndex( Result.GetIndex() + 1 );
      if( Result.GetIndex() >= ECInteger.DigitArraySize ) 
        throw( new Exception( "MultiplyTop() overflow." ));
      
      Result.SetD( Result.GetIndex(), Carry );
      }
    }




  // This is another optimization.  This is used when the top digit
  // is 1 and all of the other digits are zero.
  // This is effectively just a shift-left operation.
  protected void MultiplyTopOne( ECInteger Result, ECInteger ToMul ) throws Exception
    {
    int TotalIndex = Result.GetIndex() + ToMul.GetIndex();
    if( TotalIndex >= ECInteger.DigitArraySize )
      throw( new Exception( "MultiplyTopOne() overflow." ));

    for( int Column = 0; Column <= ToMul.GetIndex(); Column++ )
      Result.SetD( Column + Result.GetIndex(), ToMul.GetD( Column ));

    for( int Column = 0; Column < Result.GetIndex(); Column++ )
      Result.SetD( Column, 0 );

    // No Carrys need to be done.
    Result.SetIndex( TotalIndex );
    }
  



    protected void Divide( ECInteger ToDivideOriginal,
                        ECInteger DivideByOriginal,
                        ECInteger Quotient,
                        ECInteger Remainder ) throws Exception
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
      Quotient.SetFromLong( 1 );
      Remainder.SetToZero();
      return; //  true;
      }

    if( ToDivide.IsLong())
      {
      long ToDivideU = ToDivide.GetAsLong();
      long DivideByU = DivideBy.GetAsLong();
      long QuotientU = ToDivideU / DivideByU;
      long RemainderU = ToDivideU % DivideByU;
      Quotient.SetFromLong( QuotientU );
      Remainder.SetFromLong( RemainderU );
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




  private void TestDivideBits( long MaxValue,
                               boolean IsTop,
                               int TestIndex,
                               ECInteger ToDivide,
                               ECInteger DivideBy,
                               ECInteger Quotient,
                               ECInteger Remainder ) throws Exception
    {
    // For a particular value of TestIndex, this does the 
    // for-loop to test each bit.
    ECInteger Test1 = new ECInteger();
    ECInteger Test2 = new ECInteger();

    int BitTest = 0x800000;
    for( int BitCount = 23; BitCount >= 0; BitCount-- )
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



    // If you multiply the numerator and the denominator by the same amount
    // then the quotient is still the same.  By shifting left (multiplying by twos)
    // the MaxValue upper limit is more accurate.
    // This is called normalization.
  private int FindShiftBy( long ToTest )
    {
    int ShiftBy = 0;
    // If it's not already shifted all the way over to the left,
    // shift it all the way over.
    for( int Count = 0; Count < 24; Count++ )
      {
      if( (ToTest & 0x800000) != 0 )
        break;

      ShiftBy++;
      ToTest <<= 1;
      }

    return ShiftBy;
    }


  // See more division examples in the C# version of this.

  private void LongDivide3( ECInteger ToDivide,
                            ECInteger DivideBy,
                            ECInteger Quotient,
                            ECInteger Remainder ) throws Exception
    {
    // int ErrorPoint = 0;

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

    long TestBits = DivideBy.GetD( DivideBy.GetIndex());
    int ShiftBy = FindShiftBy( TestBits );
    ToDivide.ShiftLeft( ShiftBy ); // Multiply the numerator and the denominator
    DivideBy.ShiftLeft( ShiftBy ); // by the same amount.

    // ErrorPoint = 1;

    long MaxValue;
    if( (ToDivide.GetIndex() - 1) > (DivideBy.GetIndex() + TestIndex) )
      {
      MaxValue = ToDivide.GetD( ToDivide.GetIndex());
      }
    else
      {
      MaxValue = ToDivide.GetD( ToDivide.GetIndex()) << 24;
      MaxValue |= ToDivide.GetD( ToDivide.GetIndex() - 1 );
      }

    // ErrorPoint = 2;

    // Notice how this ToMatch is different from the theory from the sixties.
    // which assumes some other Base.
    long Denom = DivideBy.GetD( DivideBy.GetIndex());
    if( Denom != 0 )
      MaxValue = MaxValue / Denom;
    else
      MaxValue = 0xFFFFFF;

    if( MaxValue > 0xFFFFFF )
      MaxValue = 0xFFFFFF;

    ////////////////////////
    if( MaxValue == 0 )
      {
      throw( new Exception( "MaxValue is zero at the top in LongDivide3()." ));
      }
    ////////////////////

    // long TestGap;
    
    Quotient.SetDigitAndClear( TestIndex, 1 );
    Quotient.SetD( TestIndex, 0 );

    Test1.Copy( Quotient );
    Test1.SetD( TestIndex, MaxValue );
    MultiplyTop( Test1, DivideBy );

    ////////////////
    Test2.Copy( Quotient );
    Test2.SetD( TestIndex, MaxValue );
    Multiply( Test2, DivideBy );
    if( !Test2.IsEqual( Test1 ))
      throw( new Exception( "In Divide3() !IsEqual( Test2, Test1 )" ));
    ////////////

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

      ///////////
      Test2.Copy( Quotient );
      Test2.SetD( TestIndex, MaxValue );
      Multiply( Test2, DivideBy );
      if( !Test2.IsEqual( Test1 ))
        throw( new Exception( "Top one. !Test2.IsEqual( Test1 ) in LongDivide3()" ));
      ////////////
    
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
      MaxValue = Remainder.GetD( Remainder.GetIndex()) << 24;

      // ErrorPoint = 6;

      int CheckIndex = Remainder.GetIndex() - 1;
      if( CheckIndex > 0 )
        MaxValue |= Remainder.GetD( CheckIndex );

      // ErrorPoint = 7;

      Denom = DivideBy.GetD( DivideBy.GetIndex());
      if( Denom != 0 )
        MaxValue = MaxValue / Denom;
      else
        MaxValue = 0xFFFFFF;

      if( MaxValue > 0xFFFFFF )
        MaxValue = 0xFFFFFF;

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

    }




  // Finding the square root of a number is similar to division since
  // it is a search algorithm.  The TestSqrtBits method shown next is
  // very much like TestDivideBits().  It works the same as
  // FindULSqrRoot(), but on a bigger scale.
  /*
  private void TestSqrtBits( int TestIndex, ECInteger Square, ECInteger SqrRoot )
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
  
  protected boolean SquareRoot( ECInteger Square, ECInteger SqrRoot ) throws Exception
    {
    long ToMatch;
    if( Square.IsLong() )
      {
      ToMatch = Square.GetAsLong();
      SqrRoot.SetD( 0, FindLSqrRoot( ToMatch ));
      SqrRoot.SetIndex( 0 );
      if( (SqrRoot.GetD(0 ) * SqrRoot.GetD( 0 )) == ToMatch )
        return true;
      else
        return false;

      }

    ECInteger Test1 = new ECInteger();

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
      ToMatch = Square.GetD( Square.GetIndex()) << 24;
      ToMatch |= Square.GetD( Square.GetIndex() - 1 );
      }

    SqrRoot.SetD( TestIndex, FindLSqrRoot( ToMatch ));

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
    if( ((SqrRoot.GetD( 0 ) * SqrRoot.GetD( 0 )) & 0xFFFFFF) != Square.GetD( 0 ))
      return false;
    
    Test1.Copy( SqrRoot );
    DoSquare( Test1 );
    if( Square.IsEqual( Test1 ))
      return true;
    else
      return false;
  
    }



  // Test all this.

  private void SearchSqrtXPart( int TestIndex, ECInteger Square, ECInteger SqrRoot ) throws Exception
    {
    // B is the Big part of the number that has already been found.
    // S = (B + x)^2
    // S = B^2 + 2Bx + x^2
    // S - B^2 = 2Bx + x^2
    // R = S - B^2
    // R = 2Bx + x^2
    // R = x(2B + x)
    ECInteger Test1 = new ECInteger();
    ECInteger Test2 = new ECInteger();
    ECInteger Remainder = new ECInteger();
    ECInteger R2 = new ECInteger();
    ECInteger TwoB = new ECInteger();

    Test1.Copy( SqrRoot ); // B
    DoSquare( Test1 ); // B^2
    Remainder.Copy( Square );
    Subtract( Remainder, Test1 ); // S - B^2
    TwoB.Copy( SqrRoot ); // B
    TwoB.ShiftLeft( 1 ); // Times 2 for 2B.
    Test1.Copy( TwoB ); 
    long TestBits = Test1.GetD( Test1.GetIndex());
    int ShiftBy = FindShiftBy( TestBits );
    R2.Copy( Remainder );
    R2.ShiftLeft( ShiftBy );     // Multiply the numerator and the denominator
    Test1.ShiftLeft( ShiftBy ); // by the same amount.

    long Highest; 
    if( R2.GetIndex() == 0 )
      {
      Highest = R2.GetD( R2.GetIndex());
      }
    else
      {
      Highest = R2.GetD( R2.GetIndex()) << 24;
      Highest |= R2.GetD( R2.GetIndex() - 1 );
      }

    Highest = Highest / Test1.GetD( Test1.GetIndex());
    if( Highest == 0 )
      {
      SqrRoot.SetD( TestIndex, 0 );
      return; 
      }

    if( Highest > 0xFFFFFF )
      Highest = 0xFFFFFF;

    int BitTest = 0x800000;
    long XDigit = 0;
    long TempXDigit = 0;
    for( int BitCount = 0; BitCount < 24; BitCount++ )
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
  protected void ModularPower1( ECInteger Result, ECInteger Exponent, ECInteger ModN ) throws Exception
    {
    // This gets called recursively for as many bits as there are in the exponent.
    // How would this recursive stack get handled in the Android virtual machine?
    /////////////
    From Wikipedia:
    Function exp-by-squaring(x,n)
     if n=0 then return 1;
     else if n=1 then return x;
     else if n is even then return exp-by-squaring(x*x, n/2);
     else if n is odd then return x * exp-by-squaring(x*x, (n-1)/2).
    ////////////


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



  protected void ModularPower2( ECInteger Result, ECInteger Exponent, ECInteger ModN ) throws Exception
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
      Result.SetFromLong( 1 );
      return;
      }

    if( ModN.ParamIsGreater( Result ))
      {
      Divide( Result, ModN, Quotient, Remainder );
      Result.Copy( Remainder );
      }

    if( Exponent.IsEqualToLong( 1 ))
      {
      // Result stays the same.
      return;
      }


    ECInteger XForModPower = new ECInteger();
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

    Result.SetFromLong( 1 );
    while( !ExponentCopy.IsZero())
      {
      // If it's odd.
      if( (ExponentCopy.GetD( 0 ) & 1) == 1 )
        {
        Multiply( Result, XForModPower );
        SubtractLong( ExponentCopy, 1 );
        }

      // Square it.
      Multiply( XForModPower, XForModPower );
      ExponentCopy.ShiftRight( 1 ); // Divide by 2.

      if( ModN.ParamIsGreater( Result ))
        {
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



  
  protected void GreatestCommonDivisor( ECInteger A, ECInteger B, ECInteger Gcd ) throws Exception
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



  protected boolean IsFermatPrime( ECInteger ToTest, int HowMany ) throws Exception
    {
    // Also see Rabin-Miller test.
    // Also see Solovay-Strassen test.

    // Start at the Prime 3.
    for( int Count = 1; Count < (HowMany + 1); Count++ )
      {
      if( !IsFermatPrimeForOneValue( ToTest, PrimeArray[Count] ))
        return false;

      }

    return true; // It _might_ be a prime if it passed this test.
    }



  // http://en.wikipedia.org/wiki/Primality_test
  // http://en.wikipedia.org/wiki/Fermat_primality_test

  protected boolean IsFermatPrimeForOneValue( ECInteger ToTest, long Base  ) throws Exception
    {
    // This won't catch Carmichael numbers.
    // http://en.wikipedia.org/wiki/Carmichael_number

    // Assume this is not a small number.  (Not the size of a small prime.)
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
    SubtractLong( Temp1, 1 );
    Temp2.SetFromLong( Base );

    ModularPower2( Temp2, Temp1, ToTest );
    if( Temp2.IsOne())
      return true; // It passed the test.
    else
      return false; // It is composite.

    }





  }



