// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


// These ideas mainly come from a set of books written by Donald Knuth
// in the 1960s called "The Art of Computer Programming", especially
// Volume 2 – Seminumerical Algorithms.
// But it is also a whole lot like the ordinary arithmetic you'd do on
// paper, and there are some comments in the code below about that.


using System;
using System.Text;


namespace ExampleServer
{
  class Integer
  {
  internal bool IsNegative = false;
  private ulong[] D; // The digits.
  private int Index; // Highest digit.
  // The digit array size is fixed to keep it simple, but you might
  // want to make it dynamic so it can grow as needed.

  // If this array size is changed then test it with ChineseRemainder.
  // When this is set to ((1024 * 12) / 32) + 1 it's barely big
  // enough to work for 2048-bit primes in CRTMath.cs in
  // ModularReduction().  The number 770 in this example is
  // ChineseRemainder.DigitsArraySize.
  // HighestMatchForInverse: 760 of 770 and 127
  internal const int DigitArraySize = ((1024 * 12) / 32) + 1;



  internal Integer()
    {
    IsNegative = false;
    D = new ulong[DigitArraySize];
    SetToZero();
    }



  internal void SetToZero()
    {
    IsNegative = false;
    Index = 0;
    D[0] = 0;
    }



  internal void SetToOne()
    {
    IsNegative = false;
    Index = 0;
    D[0] = 1;
    }


  internal bool IsZero()
    {
    if( (Index == 0) && (D[0] == 0) )
      return true;
    else
      return false;

    }



  internal bool IsOne()
    {
    if( IsNegative )
      return false;

    if( (Index == 0) && (D[0] == 1) )
      return true;
    else
      return false;

    }



  internal ulong GetD( int Where )
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



  internal void SetD( int Where, ulong ToWhat )
    {
    D[Where] = ToWhat;
    }


  internal int GetIndex()
    {
    return Index;
    }


  internal void SetIndex( int Where )
    {
    Index = Where;
    }


  internal void IncrementIndex()
    {
    Index++;
    if( Index >= DigitArraySize )
      throw( new Exception( "Integer IncrementIndex() overflow." ));
      // Instead of throwing the exception you could allocate a
      // bigger array here and copy it and all that if you want
      // to allow for any arbitrary size integer.

    }



  internal void SetToMaxValue()
    {
    IsNegative = false;
    Index = DigitArraySize - 1;
    for( int Count = 0; Count <= Index; Count++ )
      D[Count] = 0xFFFFFFFF;

    }



  internal void SetToMaxValueForCRT()
    {
    // DigitArraySize is 512.
    // MaxValue index is 256.
    IsNegative = false;
    Index = DigitArraySize >> 1;
    for( int Count = 0; Count <= Index; Count++ )
      D[Count] = 0xFFFFFFFF;

    }



  internal void SetFromULong( ulong ToSet )
    {
    IsNegative = false;
    // If ToSet was zero then D[0] would be zero and
    // Index would be zero.
    D[0] = ToSet & 0xFFFFFFFF;
    D[1] = ToSet >> 32;

    if( D[1] == 0 )
      Index = 0;
    else
      Index = 1;

    }




  internal void Copy( Integer CopyFrom )
    {
    IsNegative = CopyFrom.IsNegative;
    Index = CopyFrom.Index;
    for( int Count = 0; Count <= Index; Count++ )
      D[Count] = CopyFrom.D[Count];

    }




  internal bool IsEqualToULong( ulong ToTest )
    {
    if( IsNegative )
      return false;

    if( Index > 1 )
      return false;

    if( D[0] != (ToTest & 0xFFFFFFFF))
      return false;

    // The bottom 32 bits are equal.

    if( Index == 0 )
      {
      if( (ToTest >> 32) != 0 )
        return false;
      else
        return true;

      }

    // Index is equal to 1.

    if( (ToTest >> 32) != D[1] )
      return false;

    return true;
    }



  internal bool IsEqual( Integer X )
    {
    if( IsNegative != X.IsNegative )
      return false;

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



  internal bool IsULong()
    {
    // if( IsNegative )

    if( Index > 1 )
      return false;
    else
      return true;
    
    }



  internal ulong GetAsULong()
    {
    // This is normally used after calling IsULong().
    // It is assumed here that it is a ulong.
    if( Index == 0 ) // Then D[1] is undefined.
      return D[0];

    ulong Result = D[1] << 32;
    Result |= D[0];
    return Result;
    }



  internal string GetAsHexString()
    {
    string Result = "";

    for( int Count = Index; Count >= 0; Count-- )
      Result += D[Count].ToString( "X" ) + ", ";
    
    return Result;
    }



  internal bool ParamIsGreater( Integer X )
    {
    if( IsNegative )
      throw( new Exception( "ParamIsGreater() can't be called with negative numbers." ));

    if( X.IsNegative )
      throw( new Exception( "ParamIsGreater() can't be called with negative numbers." ));

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




  internal bool ParamIsGreaterOrEq( Integer X )
    {
    if( IsEqual( X ))
      return true;

    return ParamIsGreater( X );
    }




  internal void AddULong( ulong ToAdd )
    {
    D[0] += ToAdd & 0xFFFFFFFF;

    if( Index == 0 ) // Then D[1] would be an undefined value.
      {
      D[1] = ToAdd >> 32;
      if( D[1] != 0 )
        Index = 1;

      }
    else
      {
      D[1] += ToAdd >> 32;
      }
 
    if( (D[0] >> 32) == 0 ) 
      {
      // If there's nothing to Carry then no reorganization is needed.
      if( Index == 0 )
        return; // Nothing to Carry.

      if( (D[1] >> 32) == 0 )
        return; // Nothing to Carry.
        
      }

    ulong Carry = D[0] >> 32;
    D[0] = D[0] & 0xFFFFFFFF;
    for( int Count = 1; Count <= Index; Count++ )
      {
      ulong Total = Carry + D[Count]; 
      D[Count] = Total & 0xFFFFFFFF;
      Carry = Total >> 32;
      }

    if( Carry != 0 )
      {
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "Integer.AddULong() overflow." ));

      D[Index] = Carry;
      }
    }




  internal void Add( Integer ToAdd )
    {
    // There is a separate IntegerMath.Add() that is a wrapper to handle 
    // negative numbers too.

    if( IsNegative )
      throw( new Exception( "Integer.Add() is being called when it's negative." ));

    if( ToAdd.IsNegative )
      throw( new Exception( "Integer.Add() is being called when ToAdd is negative." ));

    if( ToAdd.IsULong() )
      {
      AddULong( ToAdd.GetAsULong() );
      return;
      }

    // Tell the compiler these aren't going to change for the for-loop.
    int LocalIndex = Index;
    int LocalToAddIndex = ToAdd.Index;

    if( LocalIndex < ToAdd.Index )
      {
      for( int Count = LocalIndex + 1; Count <= LocalToAddIndex; Count++ )
        D[Count] = ToAdd.D[Count]; 

      for( int Count = 0; Count <= LocalIndex; Count++ )
        D[Count] += ToAdd.D[Count];

      Index = ToAdd.Index;
      }
    else
      { 
      for( int Count = 0; Count <= LocalToAddIndex; Count++ )
        D[Count] += ToAdd.D[Count];

      }

    // After they've been added, reorganize it.
    ulong Carry = D[0] >> 32;
    D[0] = D[0] & 0xFFFFFFFF;
    LocalIndex = Index;
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



  // This is an optimization for small squares.
  internal void Square0()
    {
    // If this got called then Index is 0.
    ulong Square = D[0] * D[0];
    D[0] = Square & 0xFFFFFFFF;
    D[1] = Square >> 32;
    if( D[1] != 0 )
      Index = 1;

    }



  internal void Square1()
    {
    // If this got called then Index is 1.
    ulong D0 = D[0];
    ulong D1 = D[1];

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
    ulong M0_0 = D0 * D0;
    ulong M1_0 = D0 * D1;

    // Second row:
    // ulong M1_1 = M1_0; // Avoiding D1 * D0 again. 
    ulong M2_1 = D1 * D1; 

    // Add them up:
    D[0] = M0_0 & 0xFFFFFFFF;
    ulong Carry = M0_0 >> 32;

    // This test will cause an overflow exception:
    // ulong TestBits = checked( (ulong)0xFFFFFFFF * (ulong)0xFFFFFFFF ); 
    // ulong TestCarry = TestBits >> 32;
    // TestBits = checked( TestBits + TestBits );
    // TestBits = checked( TestBits + TestCarry );

    // To avoid an overflow, split the ulongs into
    // left and right halves and then add them up.
    // D[1] = M1_0 + M1_1
    ulong M0Right = M1_0 & 0xFFFFFFFF;
    ulong M0Left = M1_0 >> 32;

    // Avoiding a redundancy:
    // M1_1 is the same as M1_0.
    // ulong M1Right = M1_1 & 0xFFFFFFFF;
    // ulong M1Left = M1_1 >> 32;
    // ulong Total = M0Right + M1Right + Carry;
    ulong Total = M0Right + M0Right + Carry;
    D[1] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += M0Left + M0Left;

    Total = (M2_1 & 0xFFFFFFFF) + Carry;
    D[2] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += (M2_1 >> 32); 

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



  internal void Square2()
    {
    // If this got called then Index is 2.
    ulong D0 = D[0];
    ulong D1 = D[1];
    ulong D2 = D[2];

    //                   M2_0   M1_0  M0_0
    //            M3_1   M2_1   M1_1
    //     M4_2   M3_2   M2_2

    // Top row: 
    ulong M0_0 = D0 * D0;
    ulong M1_0 = D0 * D1;
    ulong M2_0 = D0 * D2;

    // Second row:
    // ulong M1_1 = M1_0;  
    ulong M2_1 = D1 * D1; 
    ulong M3_1 = D1 * D2; 

    // Third row:
    // ulong M2_2 = M2_0;
    // ulong M3_2 = M3_1;
    ulong M4_2 = D2 * D2;

    // Add them up:
    D[0] = M0_0 & 0xFFFFFFFF;
    ulong Carry = M0_0 >> 32;

    // D[1] 
    ulong M0Right = M1_0 & 0xFFFFFFFF;
    ulong M0Left = M1_0 >> 32;
    // ulong M1Right = M1_1 & 0xFFFFFFFF;
    // ulong M1Left = M1_1 >> 32;
    ulong Total = M0Right + M0Right + Carry;
    D[1] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += M0Left + M0Left;

    // D[2] 
    M0Right = M2_0 & 0xFFFFFFFF;
    M0Left = M2_0 >> 32;
    ulong M1Right = M2_1 & 0xFFFFFFFF;
    ulong M1Left = M2_1 >> 32;
    // ulong M2Right = M2_2 & 0xFFFFFFFF;
    // ulong M2Left = M2_2 >> 32;
    Total = M0Right + M1Right + M0Right + Carry;
    D[2] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += M0Left + M1Left + M0Left;

    // D[3] 
    M1Right = M3_1 & 0xFFFFFFFF;
    M1Left = M3_1 >> 32;
    // M2Right = M3_2 & 0xFFFFFFFF;
    // M2Left = M3_2 >> 32;
    Total = M1Right + M1Right + Carry;
    D[3] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += M1Left + M1Left;

    // D[4]
    ulong M2Right = M4_2 & 0xFFFFFFFF;
    ulong M2Left = M4_2 >> 32;
    Total = M2Right + Carry;
    D[4] = Total & 0xFFFFFFFF;
    Carry = Total >> 32;
    Carry += M2Left;

    Index = 4;
    if( Carry != 0 )
      {
      Index++;
      D[5] = Carry; 
      }
    }



  internal void ShiftLeft( int ShiftBy )
    {
    // This one is not meant to shift more than 32 bits
    // at a time.  Obviously you could call it several times.
    // Or put a wrapper function around this that calls it
    // several times.
    if( ShiftBy > 32 )
      throw( new Exception( "ShiftBy > 32 on ShiftLeft." ));

    ulong Carry = 0;
    for( int Count = 0; Count <= Index; Count++ )
      {
      ulong Digit = D[Count];
      Digit <<= ShiftBy;
      D[Count] = Digit & 0xFFFFFFFF;
      D[Count] |= Carry;
      Carry = Digit >> 32;
      }

    if( Carry != 0 )
      {
      Index++;
      if( Index >= DigitArraySize )
        throw( new Exception( "ShiftLeft overflowed." ));

      D[Index] = Carry;
      }
    }



  internal void ShiftRight( int ShiftBy )
    {
    if( ShiftBy > 32 )
      throw( new Exception( "ShiftBy > 32 on ShiftRight." ));

    ulong Carry = 0;
    for( int Count = Index; Count >= 0; Count-- )
      {
      ulong Digit = D[Count] << 32;
      Digit >>= ShiftBy;
      D[Count] = Digit >> 32;
      D[Count] |= Carry;
      Carry = Digit & 0xFFFFFFFF;
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
  internal void SetDigitAndClear( int Where, ulong ToSet )
    {
    // For testing:
    // This would lead to an undefined number that's zero
    // but not zero since the Index isn't zero.
    if( (ToSet == 0) && (Where != 0) )
      throw( new Exception( "Calling SetDigitAndClear() with a bad zero." ));

    if( Where < 0 )
      throw( new Exception( "Where < 0 in SetDigitAndClear()." ));

    if( (ToSet >> 32) != 0 )
      throw( new Exception( "SetDigitAndClear() Bad stuff..." ));

    Index = Where;
    D[Index] = ToSet;
    for( int Count = 0; Count < Index; Count++ )
      D[Count] = 0;

    }




  internal bool MakeRandomOdd( int SetToIndex, byte[] RandBytes )
    {
    IsNegative = false;
    if( SetToIndex > (DigitArraySize - 3))
      throw( new Exception( "MakeRandomOdd Index is too high." ));

    if( (RandBytes.Length & 3) != 0 )
      throw( new Exception( "MakeRandomOdd RandBytes.Length is not divisible by 4." ));

    int HowManyBytes = (SetToIndex * 4) + 4;
    if( RandBytes.Length < HowManyBytes )
      throw( new Exception( "MakeRandomOdd RandBytes.Length < HowManyBytes." ));

    Index = SetToIndex;

    int Where = 0;
    // The Index value is part of the number.
    // So it's Count <= Index
    for( int Count = 0; Count <= Index; Count++ )
      {
      ulong Digit = RandBytes[Where];
      Digit <<= 8;
      Digit |= RandBytes[Where + 1];
      Digit <<= 8;
      Digit |= RandBytes[Where + 2];
      Digit <<= 8;
      Digit |= RandBytes[Where + 3];

      D[Count] = Digit;
      Where += 4;
      }

    // Make sure there isn't a zero at the top.
    for( int Count = Index; Count >= 0; Count-- )
      {
      if( D[Count] != 0 )
        break;
      else
        throw( new Exception( "How often does this happen with the top being zero?" ));

      Index--;
      if( Index == 0 )
        break;

      }

    // Test:
    for( int Count = 0; Count <= Index; Count++ )
      {
      if( (D[Count] >> 32) != 0 )
        throw( new Exception( "(D[Count] >> 32) != 0 for MakeRandom()." ));

      }

    D[0] |= 1; // Make it odd.
    return true;
    }



  private void SetOneDValueFromChar( ulong ToSet, int Position, int Offset )
    {
    // These are ASCII values so they're between 32 and 127.
    if( Position >= D.Length )
      return;

    if( Offset == 1 )
      ToSet <<= 8;

    if( Offset == 2 )
      ToSet <<= 16;

    if( Offset == 3 )
      ToSet <<= 24;

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
    if( Position >= D.Length )
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

    if( Offset == 3 )
      {
      return (char)((D[Position] >> 24) & 0xFF);
      }

    return (char)0; // This should never happen if Offset is right.
    }



  internal bool SetFromAsciiString( string InString )
    {
    IsNegative = false;
    Index = 0;
    if( InString.Length > (DigitArraySize - 3))
      return false;

    for( int Count = 0; Count < DigitArraySize; Count++ )
      D[Count] = 0;

    int OneChar = 0;
    int Position = 0;
    int Offset = 0;
    for( int Count = 0; Count < InString.Length; Count++ )
      {
      OneChar = InString[Count];
      SetOneDValueFromChar( (ulong)OneChar, Position, Offset );
      if( Offset == 3 )
        Position++;

      Offset++;
      Offset = Offset % 4;
      // Offset = Offset & 0x3;
      }

    return true;
    }



  internal string GetAsciiString()
    {
    StringBuilder SBuilder = new StringBuilder();
    for( int Count = 0; Count <= Index; Count++ )
      {
      int Offset = 0;
      char OneChar = GetOneCharFromDValue( Count, Offset );
      if( OneChar >= ' ' )
        SBuilder.Append( OneChar );
  
      Offset = 1;
      OneChar = GetOneCharFromDValue( Count, Offset );
      // It could be missing upper characters at the top, so they'd be zero.
      if( OneChar >= ' ' )
        SBuilder.Append( OneChar );
  
      Offset = 2;
      OneChar = GetOneCharFromDValue( Count, Offset );
      if( OneChar >= ' ' )
        SBuilder.Append( OneChar );

      Offset = 3;
      OneChar = GetOneCharFromDValue( Count, Offset );
      if( OneChar >= ' ' )
        SBuilder.Append( OneChar );

      }

    return SBuilder.ToString();
    }




  private void SetOneDValueFromByte( ulong ToSet, int SetIndex, int Offset )
    {
    if( SetIndex >= D.Length )
      throw( new Exception( "SetIndex >= D.Length in SetOneDValueFromByte." ));

    if( Offset == 1 )
      ToSet <<= 8;

    if( Offset == 2 )
      ToSet <<= 16;

    if( Offset == 3 )
      ToSet <<= 24;

    // This assumes I'm setting them from zero upward.
    // So if the position is zero it's not ORed with the value at D.
    if( Offset == 0 )
      D[SetIndex] = ToSet;
    else
      D[SetIndex] |= ToSet;

    if( Index < SetIndex )
      Index = SetIndex;

    }




  private byte GetOneByteFromDValue( int AtIndex, int Offset )
    {
    if( AtIndex >= D.Length )
      throw( new Exception( "AtIndex >= D.Length in GetOneByteFromDValue." ));

    if( Offset == 0 )
      {
      return (byte)(D[AtIndex] & 0xFF);
      }

    if( Offset == 1 )
      {
      return (byte)((D[AtIndex] >> 8) & 0xFF);
      }

    if( Offset == 2 )
      {
      return (byte)((D[AtIndex] >> 16) & 0xFF);
      }

    if( Offset == 3 )
      {
      return (byte)((D[AtIndex] >> 24) & 0xFF);
      }

    throw( new Exception( "The offset was not right in GetOneByteFromDValue()." ));
    }



  // Because of the standards used with TLS, this will typically have one
  // leading zero byte so that it doesn't get confused with a negative
  // sign bit.  Sometimes it will, sometimes it won't.
  internal void SetFromBigEndianByteArray( byte[] InArray )
    {
    if( InArray.Length > (DigitArraySize - 3))
      throw( new Exception( "Position >= D.Length in SetFromBigEndianByteArray()." ));

    IsNegative = false;
    Index = 0;

    // This is unnecessary.
    // for( int Count = 0; Count < DigitArraySize; Count++ )
      // D[Count] = 0;

    Array.Reverse( InArray ); // Now the least significant byte is at InArray[0].

    int Offset = 0;
    int SetIndex = 0;
    for( int Count = 0; Count < InArray.Length; Count++ )
      {
      ulong ToSet = InArray[Count];
      SetOneDValueFromByte( ToSet, SetIndex, Offset );
      Offset++;
      if( (Offset & 3) == 0 )
        {
        Offset = 0;
        SetIndex++;
        }
      }

    // Make sure it doesn't have leading zeros.
    for( int Count = Index; Count >= 0; Count-- )
      {
      if( D[Count] != 0 )
        {
        Index = Count;
        break;
        }
      }
    }




  internal byte[] GetBigEndianByteArray()
    {
    byte[] Result = new byte[(Index + 1) * 4];
    int Where = 0;
    for( int Count = 0; Count <= Index; Count++ )
      {
      for( int Offset = 0; Offset < 4; Offset++ )
        {
        byte OneByte = GetOneByteFromDValue( Count, Offset );
        Result[Where] = OneByte;
        Where++;
        }
      }

    Array.Reverse( Result );

    // Now the most significant byte is at Result[0].
    // This might have leading zero bytes.
    return Result;
    }




  }
}



