// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/



package com.yourpackage.name;


  // These ideas mainly come from a set of books written by Donald Knuth
  // in the 1960s called "The Art of Computer Programming", especially
  // Volume 2 - Seminumerical Algorithms.




public class ECInteger
  {
  // Unlike the C# version, this uses 24 bits per digit because Java doesn't have unsigned
  // long or unsigned int, so it would overflow in to the sign bit if it used 32 bits like
  // in the C# version.
  private long[] D; // The digits.
  private int Index; // Highest digit.
  // The digit array size is fixed to keep it simple, but you might
  // want to make it dynamic so it can grow as needed.
  protected static final int DigitArraySize = ((1024 * 24) / 24) + 1;




  protected ECInteger()
    {
    D = new long[DigitArraySize];
    SetToZero();
    }



  // Java doesn't have unsigned values so the ByteToShort()
  // ShortToByte() functions are needed to make things work
  // right in Java.
  private short ByteToShort( byte In )
    {
    short Result = (short)(In & 0x7F);
    if( (In & 0x80) == 0x80 )
      Result |= 0x80;

    return Result;
    }



  /*
  private byte ShortToByte( short In )
    {
    byte Result = (byte)(In & 0x7F);
    if( (In & 0x80) == 0x80 )
      Result |= 0x80;

    return Result;
    }
    */


  protected void SetToZero()
    {
    Index = 0;
    D[0] = 0;
    }



  protected boolean IsZero()
    {
    if( (Index == 0) && (D[0] == 0) )
      return true;
    else
      return false;

    }



  protected boolean IsOne()
    {
    if( (Index == 0) && (D[0] == 1) )
      return true;
    else
      return false;

    }



  protected long GetD( int Where )
    {
    // It would be a really bad compiler if it couldn't inline and optimize
    // these small functions.  But actually, it can help improve the
    // performance if access to an array is done inside a small function
    // like this because it gives the compiler better information for
    // doing range checks and things.

    // if( Where >= DigitArraySize )
      // etc.

    return D[Where];
    }



  protected void SetD( int Where, long ToWhat )
    {
    D[Where] = ToWhat;
    }


  protected int GetIndex()
    {
    return Index;
    }


  protected void SetIndex( int Where )
    {
    Index = Where;
    }


  protected void IncrementIndex() throws Exception
    {
    Index++;
    if( Index >= DigitArraySize )
      throw( new Exception( "Integer IncrementIndex() overflow." ));
      // Instead of throwing the exception you could allocate a
      // bigger array here and copy it and all that if you want
      // to allow for any arbitrary size integer.

    }



  protected void SetToMaxValue()
    {
    Index = DigitArraySize - 1;
    for( int Count = 0; Count < DigitArraySize; Count++ )
      D[Count] = 0xFFFFFF; // 24 bit digits.

    }




  protected void SetFromLong( long ToSet ) throws Exception
    {
    if( ToSet < 0 )
      throw( new Exception( "ToSet < 0 in SetFromLong." ));

    // If ToSet was zero then D[0] would be zero and
    // Index would be zero.
    D[0] = ToSet & 0xFFFFFF; // 24 bit digits.
    D[1] = (ToSet >> 24) & 0xFFFFFF ;

    if( D[1] == 0 )
      Index = 0;
    else
      Index = 1;

    }




  protected void Copy( ECInteger CopyFrom )
    {
    Index = CopyFrom.Index;
    for( int Count = 0; Count <= Index; Count++ )
      D[Count] = CopyFrom.D[Count];

    }




  protected boolean IsEqualToLong( long ToTest )
    {
    if( Index > 1 )
      return false;

    if( D[0] != (ToTest & 0xFFFFFF))
      return false;

    // The bottom 24 bits are equal.

    if( Index == 0 )
      {
      if( (ToTest >> 24) != 0 )
        return false;
      else
        return true;

      }

    // Index is equal to 1.

    if( (ToTest >> 24) != D[1] )
      return false;

    return true;
    }



  protected boolean IsEqual( ECInteger X )
    {
    // This first one is a likely way to quickly return a false.
    if( D[0] != X.D[0] )
      return false;

    if( Index != X.Index )
      return false;

    // Starting from 1 because when the numbers are close 
    // the upper digits are the same, but the smaller digits are usually
    // different.  So it can find digits that are different sooner this way.
    for( int Count = 1; Count <= Index; Count++ )
      {
      if( D[Count] != X.D[Count] )
        return false;

      }

    return true;
    }




  protected boolean IsLong()
    {
    if( Index > 1 )
      return false;
    else
      return true;
    
    }




  protected long GetAsLong()
    {
    // This is normally used after calling IsLong().
    // It is assumed here that it is a long.
    if( Index == 0 ) // Then D[1] is undefined.
      return D[0];

    long Result = D[1] << 24;
    Result |= D[0];
    return Result;
    }



  /*
  protected String GetAsHexString()
    {
    String Result = "";

    for( int Count = Index; Count >= 0; Count-- )
      Result += D[Count].ToString( "X" ) + ", ";
    
    return Result;
    }
    */



  protected boolean ParamIsGreater( ECInteger X )
    {
    if( Index != X.Index )
      {
      if( X.Index > Index )
        return true;
      else
        return false;

      }

    // Indexes are the same:
    for( int Count = Index; Count >= 0; Count-- )
      {
      if( D[Count] != X.D[Count] )
        {
        if( X.D[Count] > D[Count] )
          return true;
        else
          return false;
        
        }  
      }

    return false; // It was equal, but it wasn't greater.
    }




  protected boolean ParamIsGreaterOrEq( ECInteger X )
    {
    if( Index != X.Index )
      {
      if( X.Index > Index )
        return true;
      else
        return false;

      }

    // Indexes are the same:
    for( int Count = Index; Count >= 0; Count-- )
      {
      if( D[Count] != X.D[Count] )
        {
        if( X.D[Count] > D[Count] )
          return true;
        else
          return false;
        
        }
      }

    return true; // It was equal.
    }




  protected void AddLong( long ToAdd ) throws Exception
    {
    if( (ToAdd >> 24) > 0xFFFFFF )
      throw( new Exception( "AddLong() ToAdd is too big." ));


    D[0] += ToAdd & 0xFFFFFF;

    if( Index == 0 ) // Then D[1] would be an undefined value.
      {
      D[1] = ToAdd >> 24;
      if( D[1] != 0 )
        Index = 1;

      }
    else
      {
      D[1] += ToAdd >> 24;
      }
 
    if( (D[0] >> 24) == 0 ) 
      {
      // If there's nothing to Carry then no reorganization is needed.
      if( Index == 0 )
        return; // Nothing to Carry.

      if( (D[1] >> 24) == 0 )
        return; // Nothing to Carry.
        
      }

    long Carry = D[0] >> 24;
    D[0] = D[0] & 0xFFFFFF;
    for( int Count = 1; Count <= Index; Count++ )
      {
      long Total = Carry + D[Count]; 
      D[Count] = Total & 0xFFFFFF;
      Carry = Total >> 24;
      }

    if( Carry != 0 )
      {
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "Integer.Add() overflow." ));

      D[Index] = Carry;
      }
    }

  


  protected void Add( ECInteger ToAdd ) throws Exception
    {
    if( ToAdd.IsLong() )
      {
      AddLong( ToAdd.GetAsLong() );
      return;
      }

    if( Index < ToAdd.Index )
      {
      for( int Count = Index + 1; Count <= ToAdd.Index; Count++ )
        D[Count] = ToAdd.D[Count]; 

      for( int Count = 0; Count <= Index; Count++ )
        D[Count] += ToAdd.D[Count];

      Index = ToAdd.Index;
      }
    else
      { 
      for( int Count = 0; Count <= ToAdd.Index; Count++ )
        D[Count] += ToAdd.D[Count];

      }

    // After they've been added, reorganize it.
    long Carry = D[0] >> 24;
    D[0] = D[0] & 0xFFFFFF;
    for( int Count = 1; Count <= Index; Count++ )
      {
      long Total = Carry + D[Count];
      D[Count] = Total & 0xFFFFFF;
      Carry = Total >> 24;
      }

    if( Carry != 0 )
      {
      // IncrementIndex();
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "Integer.Add() overflow." ));

      D[Index] = Carry;
      }
    }




  // This is an optimization for small squares.
  protected void Square0()
    {
    // If this got called then Index is 0.
    long Square = D[0] * D[0];
    D[0] = Square & 0xFFFFFF;
    D[1] = Square >> 24;
    if( D[1] != 0 )
      Index = 1;

    }




  protected void Square1()
    {
    // If this got called then Index is 1.
    long D0 = D[0];
    long D1 = D[1];

    // If you were multiplying 23 * 23 on paper
    // it would look like:
    //                            2     3
    //                            2     3
    //                           3*2   3*3 
    //                     2*2   2*3 
    
    // And then you add up the columns.

    //                           D1    D0
    //                           D1    D0
    //                         M1_0  M0_0
    //                   M2_1  M1_1

    // Top row: 
    long M0_0 = D0 * D0;
    long M1_0 = D0 * D1;

    // Second row:
    // ulong M1_1 = M1_0; // Avoiding D1 * D0 again. 
    long M2_1 = D1 * D1; 

    // Add them up:
    D[0] = M0_0 & 0xFFFFFF;
    long Carry = M0_0 >> 24;

    // D[1] = M1_0 + M1_1
    long M0Right = M1_0 & 0xFFFFFF;
    long M0Left = M1_0 >> 24;

    // Avoiding a redundancy:
    // M1_1 is the same as M1_0.
    // long M1Right = M1_1 & 0xFFFFFF;
    // long M1Left = M1_1 >> 24;
    // long Total = M0Right + M1Right + Carry;
    long Total = M0Right + M0Right + Carry;
    D[1] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += M0Left + M0Left;

    Total = (M2_1 & 0xFFFFFF) + Carry;
    D[2] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += (M2_1 >> 24); 

    Index = 2;
    if( Carry != 0 )
      {
      Index++;
      D[3] = Carry;
      }

    // Bitwise multiplication with two bits is:
    //       1  1
    //       1  1
    //     ------
    //       1  1
    //    1  1  
    // ----------
    // 1  0  0  1
    // Biggest bit is at position 3 (zero based index). 
    // Adding Indexes: (1 + 1) + 1.

    //       1  0
    //       1  0
    //       0  0
    //    1  0
    //    1  0  0
    // Biggest bit is at 2.
    // Adding Indexes: (1 + 1).


    // 7 * 7 = 49
    //                 1  1  1
    //                 1  1  1
    //                --------
    //                 1  1  1
    //              1  1  1
    //           1  1  1
    //          --------------
    //        1  1  0  0  0  1
    //       32 16           1 = 49
    // Biggest bit is at 5 (2 + 2) + 1.

    // The highest bit is at either index + index or it's
    // at index + index + 1.

    // For this Integer class the Index might have to
    // be incremented once for a Carry, but not more than once.
    }





  protected void Square2()
    {
    // If this got called then Index is 2.
    long D0 = D[0];
    long D1 = D[1];
    long D2 = D[2];

    //                   M2_0   M1_0  M0_0
    //            M3_1   M2_1   M1_1
    //     M4_2   M3_2   M2_2

    // Top row: 
    long M0_0 = D0 * D0;
    long M1_0 = D0 * D1;
    long M2_0 = D0 * D2;

    // Second row:
    // long M1_1 = M1_0;  
    long M2_1 = D1 * D1; 
    long M3_1 = D1 * D2; 

    // Third row:
    // long M2_2 = M2_0;
    // long M3_2 = M3_1;
    long M4_2 = D2 * D2;

    // Add them up:
    D[0] = M0_0 & 0xFFFFFF;
    long Carry = M0_0 >> 24;

    // D[1] 
    long M0Right = M1_0 & 0xFFFFFF;
    long M0Left = M1_0 >> 24;
    // long M1Right = M1_1 & 0xFFFFFF;
    // long M1Left = M1_1 >> 24;
    long Total = M0Right + M0Right + Carry;
    D[1] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += M0Left + M0Left;

    // D[2] 
    M0Right = M2_0 & 0xFFFFFF;
    M0Left = M2_0 >> 24;
    long M1Right = M2_1 & 0xFFFFFF;
    long M1Left = M2_1 >> 24;
    // long M2Right = M2_2 & 0xFFFFFF;
    // long M2Left = M2_2 >> 24;
    Total = M0Right + M1Right + M0Right + Carry;
    D[2] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += M0Left + M1Left + M0Left;

    // D[3] 
    M1Right = M3_1 & 0xFFFFFF;
    M1Left = M3_1 >> 24;
    // M2Right = M3_2 & 0xFFFFFF;
    // M2Left = M3_2 >> 24;
    Total = M1Right + M1Right + Carry;
    D[3] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += M1Left + M1Left;

    // D[4]
    long M2Right = M4_2 & 0xFFFFFF;
    long M2Left = M4_2 >> 24;
    Total = M2Right + Carry;
    D[4] = Total & 0xFFFFFF;
    Carry = Total >> 24;
    Carry += M2Left;

    Index = 4;
    if( Carry != 0 )
      {
      Index++;
      D[5] = Carry; 
      }
    }




  protected void ShiftLeft( int ShiftBy ) throws Exception
    {
    // This one is not meant to shift more than 24 bits
    // at a time.  Obviously you could call it several times.
    // Or put a wrapper function around this that calls it
    // several times.
    if( ShiftBy > 24 )
      throw( new Exception( "ShiftBy > 24 on ShiftLeft." ));

    long Carry = 0;
    for( int Count = 0; Count <= Index; Count++ )
      {
      long Digit = D[Count];
      Digit <<= ShiftBy;
      D[Count] = Digit & 0xFFFFFF;
      D[Count] |= Carry;
      Carry = Digit >> 24;
      }

    if( Carry != 0 )
      {
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "ShiftLeft overflowed." ));

      D[Index] = Carry;
      }
    }




  protected void ShiftRight( int ShiftBy ) throws Exception
    {
    if( ShiftBy > 24 )
      throw( new Exception( "ShiftBy > 24 on ShiftRight." ));

    long Carry = 0;
    for( int Count = Index; Count >= 0; Count-- )
      {
      long Digit = D[Count] << 24;
      Digit >>= ShiftBy;
      D[Count] = Digit >> 24;
      D[Count] |= Carry;
      Carry = Digit & 0xFFFFFF;
      }

    if( D[Index] == 0 )
      {
      if( Index > 0 )
        Index--;

      }

    // Let it shift bits over the edge.
    // if( Carry != 0 )
      // throw( new Exception( "ShiftRight() Carry not zero." ));
    }




  // This is used in some algorithms to set one particular
  // digit and have all other digits set to zero.
  protected void SetDigitAndClear( int Where, long ToSet ) throws Exception
    {
    // For testing:
    // This would lead to an undefined number that's zero
    // but not zero since the Index isn't zero.
    if( (ToSet == 0) && (Where != 0) )
      throw( new Exception( "Calling SetDigitAndClear() with a bad zero." ));

    if( Where < 0 )
      throw( new Exception( "Where < 0 in SetDigitAndClear()." ));

    if( (ToSet >> 24) != 0 )
      throw( new Exception( "SetDigitAndClear() Bad stuff..." ));

    Index = Where;
    D[Index] = ToSet;
    for( int Count = 0; Count < Index; Count++ )
      D[Count] = 0;

    }



  protected boolean MakeRandomOdd( int SetToIndex, byte[] RandBytes ) throws Exception
    {
    if( SetToIndex > (DigitArraySize - 3))
      SetToIndex = DigitArraySize - 3;

    int HowManyBytes = (SetToIndex * 3) + 3;
    if( RandBytes.length < HowManyBytes )
      {
      return false;
      }

    Index = SetToIndex;

    int Where = 0;
    // The Index value is part of the number.
    // So it's Count <= Index
    for( int Count = 0; Count <= Index; Count++ )
      {
      long Digit = ByteToShort( RandBytes[Where] );
      Digit <<= 8;
      Digit |= ByteToShort( RandBytes[Where + 1] );
      Digit <<= 8;
      Digit |= ByteToShort( RandBytes[Where + 2] );

      D[Count] = Digit;
      Where += 3;
      }

    // Make sure there isn't a zero at the top.
    for( int Count = Index; Count >= 0; Count-- )
      {
      if( D[Count] != 0 )
        break;

      Index--;
      if( Index == 0 )
        break;

      }

    // Test:
    for( int Count = 0; Count <= Index; Count++ )
      {
      if( (D[Count] >> 24) != 0 )
        throw( new Exception( "(D[Count] >> 24) != 0 for MakeRandom()." ));

      }

    D[0] |= 1; // Make it odd.
    return true;
    }




  private void SetOneDValueFromChar( long ToSet, int Position, int Offset )
    {
    // These are ASCII values so they're between 32 and 127.
    if( Position >= D.length )
      return;

    if( Offset == 1 )
      ToSet <<= 8;

    if( Offset == 2 )
      ToSet <<= 16;

    // This assumes I'm setting them from zero upward.
    if( Offset == 0 )
      D[Position] = ToSet;
    else
      D[Position] |= ToSet;

    if( Index < Position )
      Index = Position;

    }



  private char GetOneCharFromDValue( int Position, int Offset )
    {
    // These are ASCII values so they're between 32 and 127.
    if( Position >= D.length )
      return (char)0;

    if( Offset == 0 )
      {
      return (char)(D[Position] & 0xFF);
      }

    if( Offset == 1 )
      {
      return (char)((D[Position] >> 8) & 0xFF);
      }

    if( Offset == 2 )
      {
      return (char)((D[Position] >> 16) & 0xFF);
      }

    return (char)0; // This should never happen if Offset is right.
    }



  protected boolean SetFromAsciiString( String InString )
    {
    Index = 0;
    if( InString.length() > (DigitArraySize - 3))
      return false;

    for( int Count = 0; Count < DigitArraySize; Count++ )
      D[Count] = 0;

    int OneChar = 0;
    int Position = 0;
    int Offset = 0;
    for( int Count = 0; Count < InString.length(); Count++ )
      {
      OneChar = InString.charAt( Count );
      SetOneDValueFromChar( OneChar, Position, Offset );
      if( Offset == 2 )
        Position++;

      Offset++;
      Offset = Offset % 3;
      }

    return true;
    }




  protected String GetAsciiString()
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count <= Index; Count++ )
      {
      int Offset = 0;
      char OneChar = GetOneCharFromDValue( Count, Offset );
      if( OneChar >= ' ' )
        SBuilder.append( OneChar );
  
      Offset = 1;
      OneChar = GetOneCharFromDValue( Count, Offset );
      // It could be missing upper characters at the top, so they'd be zero.
      if( OneChar >= ' ' )
        SBuilder.append( OneChar );
  
      Offset = 2;
      OneChar = GetOneCharFromDValue( Count, Offset );
      if( OneChar >= ' ' )
        SBuilder.append( OneChar );

      }

    return SBuilder.toString();
    }



  }



