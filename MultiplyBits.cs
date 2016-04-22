// Programming by Eric Chauvin.
// Copyright Eric Chauvin 2016.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com



using System;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;



namespace ExampleServer
{
  class MultiplyBits
  {
  private MainForm MForm;
  internal const int MultArraySize = 128;
  // Each one marks a bit.
  private const uint Mult1 = 1;
  private const uint Mult2 = 2;
  private const uint Mult = 4;
  private const uint AccumIn = 8;
  private const uint CarryIn = 16;
  private const uint AccumOut = 32;
  private const uint CarryOut = 64;
  private const uint Mult1Known = 128;
  private const uint Mult2Known = 256;
  private const uint MultKnown = 512;
  private const uint AccumInKnown = 1024;
  private const uint CarryInKnown = 2048;
  private const uint AccumOutKnown = 4096;
  private const uint CarryOutKnown = 8192;
  private LineRec[] MultArray;
  private LineRec[] MultArrayCopyForMult2;
  private IntegerMath IntMath;
  private Integer Product;
  private Integer TestProduct;
  private Integer Factor1;
  private Integer Factor2;
  private Integer Quotient;
  private Integer Remainder;
  // private int Factor2BitIndex = -1;
  private int ProductBitIndex = -1;
  private SortedDictionary<uint, uint> InputOutputDictionary;
  private int HighestCalculationColumn = MultArraySize - 1;
  private int HighestCalculationRow = MultArraySize - 1;

 /*
13	8192
14	16384
15	32768
16	65536
17	131072
18	262144
19	524288
20	1048576
21	2097152
22	4194304
23	8388608
24	16777216
25	33554432
26	67108864
27	134217728
28	268435456
29	536870912
30	1073741824
31	2147483648
32	4294967296
 */



  internal struct LineRec
    {
    internal uint[] OneLine;
    }


  private MultiplyBits()
    {
    }



  internal MultiplyBits( MainForm UseForm )
    {
    MForm = UseForm;
    try
    {
    IntMath = new IntegerMath();
    // MForm.ShowStatus( IntMath.GetStatusString());

    InputOutputDictionary = new SortedDictionary<uint, uint>();

    Product = new Integer();
    TestProduct = new Integer();
    Factor1 = new Integer();
    Factor2 = new Integer();
    Quotient = new Integer();
    Remainder = new Integer();

    MultArray = new LineRec[MultArraySize];
    MultArrayCopyForMult2 = new LineRec[MultArraySize];
    for( int Count = 0; Count < MultArraySize; Count++ )
      {
      MultArray[Count].OneLine = new uint[MultArraySize];
      MultArrayCopyForMult2[Count].OneLine = new uint[MultArraySize];
      }

    SetupFermatLittle( 107 );

    }
    catch( Exception )
      {
      MForm.ShowStatus( "Not enough RAM for the MultArray." );
      }
    }



  internal void SetupFermatLittle( uint PrimeToTest )
    {
    try
    {
    ClearAll();
    SetTopAccumAndCarry();
    ClearLeftsideAccum();
    ClearBottomBits();

    Factor1.SetFromULong( PrimeToTest );

    // 1 shifted left once is 2^1.
    // 1 shifted left twice is 2^2.
    // 1 shifted left 106 times is 2^106.
    uint Exponent = PrimeToTest - 1;
    Product.SetToOne();
    for( uint Count = 0; Count < Exponent; Count++ )
      Product.ShiftLeft( 1 );

    MForm.ShowStatus( "Exponent part with zeros is: " + Product.GetAsHexString());
    IntMath.SubtractULong( Product, 1 );
    MForm.ShowStatus( "Product with all ones is: " + Product.GetAsHexString());

    IntMath.Divide( Product, Factor1, Quotient, Remainder );
    if( !Remainder.IsZero())
      {
      // If the numbers don't pass the Fermat Primality Test.
      MForm.ShowStatus( "The number is composite.  The remainder isn't zero for Factor2." );
      // return;
      }
    else
      {
      Factor2.Copy( Quotient );
      }

    MForm.ShowStatus( "Factor1: 0x" + Factor1.GetAsHexString());
    MForm.ShowStatus( "Factor2: 0x" + Factor2.GetAsHexString());

    SetFactor1( Factor1 );
    // SetFactor2( Factor2 );
    SetProduct( Product );
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in SetInitialValues()." );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void ClearAll()
    {
    HighestCalculationColumn = MultArraySize - 1;
    HighestCalculationRow = MultArraySize - 1;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column < MultArraySize; Column++ )
        {
        // Set everything to unknown/zero.
        MultArray[Row].OneLine[Column] = 0;
        }
      }
    }



  private void SetTopAccumAndCarry()
    {
    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      uint ToSet = MultArray[0].OneLine[Column];
      ToSet = ClearAccumIn( ToSet );
      ToSet = ClearCarryIn( ToSet );
      MultArray[0].OneLine[Column] = ToSet;
      }
    }




  private void ClearBottomBits()
    {
    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      uint ToSet = MultArray[MultArraySize - 1].OneLine[Column];
      ToSet = ClearAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      ToSet = ClearMult( ToSet );
      MultArray[MultArraySize - 1].OneLine[Column] = ToSet;
      }
    }



  private void ClearLeftsideAccum()
    {
    // Left Column.
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      uint ToSet = MultArray[Row].OneLine[MultArraySize - 1];
      // There is no AccumIn on the left side because
      // of how it's offset.
      ToSet = ClearAccumIn( ToSet );
      // There can't be a carry out on the left side
      // because that would be an overflow condition.
      ToSet = ClearCarryOut( ToSet );
      MultArray[Row].OneLine[MultArraySize - 1] = ToSet;
      }
    }



  // Factor1 is along the top.
  internal void SetFactor1( Integer UseBigIntToSet )
    {
    Integer BigIntToSet = new Integer();
    BigIntToSet.Copy( UseBigIntToSet );

    if( (BigIntToSet.GetD( 0 ) & 1 ) != 1 )
      throw( new Exception( "Factor1 can't be even." ));

    uint ToSet = MultArray[0].OneLine[0];
    ToSet = SetMult1( ToSet );
    ToSet = SetMult2( ToSet );
    ToSet = SetMult( ToSet );
    ToSet = SetAccumOut( ToSet );
    ToSet = ClearCarryOut( ToSet );
    MultArray[0].OneLine[0] = ToSet;

    // Factor1 has to be odd so Mult1 is set to
    // 1 all the way down the side.

    SetMult1AtColumn( 0 );

    BigIntToSet.ShiftRight( 1 );
    if( BigIntToSet.IsZero())
      throw( new Exception( "Factor1 can't be 1." ));

    // 107 = 64 + 32 + 8 + 2 + 1
    int Where = 1;
    for( int Column = 1; Column < MultArraySize; Column++ )
      {
      ToSet = MultArray[0].OneLine[Column];
      if( (BigIntToSet.GetD( 0 ) & 1 ) == 1 )
        ToSet = SetMult1( ToSet );
      else
        ToSet = ClearMult1( ToSet );

      MultArray[0].OneLine[Column] = ToSet;

      Where = Column;
      BigIntToSet.ShiftRight( 1 );
      if( BigIntToSet.IsZero())
        break;

      }

    // At the last 1 bit.
    // ToSet = MultArray[0].OneLine[Where];

    for( int Column = Where + 1; Column < MultArraySize; Column++ )
      {
      ToSet = MultArray[0].OneLine[Column];
      ToSet = ClearMult1( ToSet );
      ToSet = ClearMult( ToSet );
      ToSet = ClearAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      MultArray[0].OneLine[Column] = ToSet;
      }
    }



  // Factor 2 is along the right side diagonal.
  internal void SetFactor2( Integer UseBigIntToSet )
    {
    Integer BigIntToSet = new Integer();
    BigIntToSet.Copy( UseBigIntToSet );

    if( (BigIntToSet.GetD( 0 ) & 1 ) != 1 )
      throw( new Exception( "Factor2 can't be even." ));

    uint ToSet = MultArray[0].OneLine[0];
    ToSet = SetMult1( ToSet );
    ToSet = SetMult2( ToSet );
    ToSet = SetMult( ToSet );
    ToSet = SetAccumOut( ToSet );
    ToSet = ClearCarryOut( ToSet );
    MultArray[0].OneLine[0] = ToSet;

    // Factor2 has to be odd so Mult2 is set to
    // 1 all the way across the top.
    SetMult2AtRow( 0 );

    BigIntToSet.ShiftRight( 1 );
    if( BigIntToSet.IsZero())
      throw( new Exception( "Factor2 can't be 1." ));

    // 107 = 64 + 32 + 8 + 2 + 1
    int Where = 1;
    for( int Row = 1; Row < MultArraySize; Row++ )
      {
      ToSet = MultArray[Row].OneLine[0];
      if( (BigIntToSet.GetD( 0 ) & 1 ) == 1 )
        ToSet = SetMult2( ToSet );
      else
        ToSet = ClearMult2( ToSet );

      MultArray[Row].OneLine[0] = ToSet;

      Where = Row;
      BigIntToSet.ShiftRight( 1 );
      if( BigIntToSet.IsZero())
        break;

      }

    // At the last 1 bit.
    // ToSet = MultArray[Where].OneLine[0];

    for( int Row = Where + 1; Row < MultArraySize; Row++ )
      {
      ToSet = MultArray[Row].OneLine[0];
      ToSet = ClearMult2( ToSet );
      MultArray[Row].OneLine[0] = ToSet;
      }
    }



  private int GetHighestMult1BitIndex()
    {
    int Highest = 0;
    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      // Set everything to unknown/zero.
      uint ToCheck = MultArray[0].OneLine[Column];
      if( !Mult1IsKnown( ToCheck ))
        return Column;

      if( GetMult1Value( ToCheck ))
        Highest = Column;

      }

    return Highest;
    }



  // Product is along the right side diagonal.
  internal void SetProduct( Integer UseBigIntToSet )
    {
    Integer BigIntToSet = new Integer();
    BigIntToSet.Copy( UseBigIntToSet );

    // Primes: 29, 31, 37, 41, 43, 47, 53, 59, 61, 67,
    // 71, 73, 79, 83, 89, 97, 101, 103, 107

    if( (BigIntToSet.GetD( 0 ) & 1 ) != 1 )
      throw( new Exception( "Product can't be even." ));

    int Where = 0;
    uint ToSet = 0;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      ToSet = MultArray[Row].OneLine[0];
      if( (BigIntToSet.GetD( 0 ) & 1 ) == 1 )
        ToSet = SetAccumOut( ToSet );
      else
        ToSet = ClearAccumOut( ToSet );

      MultArray[Row].OneLine[0] = ToSet;

      Where = Row;
      BigIntToSet.ShiftRight( 1 );
      if( BigIntToSet.IsZero())
        break;

      }

    ProductBitIndex = Where;
    int HowMany = ProductBitIndex + 1;
    MForm.ShowStatus( "There were " + HowMany.ToString() + " bits in the product." );

    // At the last 1 bit.
    ToSet = MultArray[Where].OneLine[0];
    ToSet = ClearCarryOut( ToSet );
    MultArray[Where].OneLine[0] = ToSet;

    // The ProductBits can be:
    // Factor1Bits + Factor2Bits
    // or it can be:
    // Factor1Bits + Factor2Bits + 1
    // ProductBits - Factor1Bits = Factor2Bits ( + 1 or not)
    int Factor1Bits = GetHighestMult1BitIndex() + 1;
    int ProductBits = (Where + 1);
    // Factor2Bits might be this or it might be one
    // less than this.
    int MaximumFactor2Bits = ProductBits - Factor1Bits;
    // Factor2BitIndex = Factor2Bits - 1;

    // Test if the top bit of Factor2 is in the right place.
    // ToSet = MultArray[Factor2BitIndex].OneLine[0];
    // ToSet = SetMult( ToSet );
    // MultArray[Factor2BitIndex].OneLine[0] = ToSet;

    for( int Row = MaximumFactor2Bits + 1; Row < MultArraySize; Row++ )
      {
      ToSet = MultArray[Row].OneLine[0];
      ToSet = ClearMult2( ToSet );
      MultArray[Row].OneLine[0] = ToSet;
      }

    for( int Row = Where + 1; Row < MultArraySize; Row++ )
      {
      ToSet = MultArray[Row].OneLine[0];
      ToSet = ClearMult2( ToSet );
      ToSet = ClearMult( ToSet );
      ToSet = ClearAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      MultArray[Row].OneLine[0] = ToSet;
      }
    }



  private bool AllProductBitsAreKnown()
    {
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      uint ToSet = MultArray[Row].OneLine[0];
      if( !AccumOutIsKnown( ToSet ))
        return false;

      }

    return true;
    }



  private int GetHighestProductBitIndex()
    {
    // They have to all be known at this point.
    int Highest = 0;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      uint ToSet = MultArray[Row].OneLine[0];
      if( GetAccumOutValue( ToSet ))
        Highest = Row;

      }

    return Highest;
    }



  internal bool GetProduct( Integer BigIntToSet )
    {
    BigIntToSet.SetToZero();
    if( !AllProductBitsAreKnown())
      return false;

    int Highest = GetHighestProductBitIndex();
    for( int Row = Highest; Row >= 0; Row-- )
      {
      // The first time through it will just shift zero
      // to the left, so nothing happens with zero.
      BigIntToSet.ShiftLeft( 1 );

      uint ToSet = MultArray[Row].OneLine[0];
      if( GetAccumOutValue( ToSet ))
        {
        ulong D = BigIntToSet.GetD( 0 );
        D |= 1;
        BigIntToSet.SetD( 0, D );
        }
      }

    return true;
    }



  private void SetInsAndOuts()
    {
    // OneLine[0] is on the right side.
    // These arrays are offset so that 
    // MultArray[Row - 1].OneLine[Column] is shifted
    // one to the right of:
    // MultArray[Row].OneLine[Column].
    // See the Draw() method and OffsetLeft to
    // visualize the offset.

    // MultArray[Row - 1].OneLine[Column + 1] is 
    // directly above
    // MultArray[Row].OneLine[Column].

    int HighestRow = HighestCalculationRow + 1;
    int HighestColumn = HighestCalculationColumn + 1;
    if( HighestRow >= MultArraySize )
      HighestRow = MultArraySize - 1;

    if( HighestColumn >= MultArraySize )
      HighestColumn = MultArraySize - 1;

    for( int Row = 1; Row <= HighestRow; Row++ )
      {
      for( int Column = 0; (Column + 1) <= HighestColumn; Column++ )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint Above = MultArray[Row - 1].OneLine[Column + 1];
        if( AccumInIsKnown( ToSet ) &&
            AccumOutIsKnown( Above ))
          {
          if( GetAccumOutValue( Above ) !=
              GetAccumInValue( ToSet ))
            throw( new Exception( "Accum above doesn't match Accum ToSet." ));

          }

        if( !AccumInIsKnown( ToSet ) &&
            AccumOutIsKnown( Above ))
          {
          if( GetAccumOutValue( Above ))
            ToSet = SetAccumIn( ToSet );
          else
            ToSet = ClearAccumIn( ToSet );

          MultArray[Row].OneLine[Column] = ToSet;
          continue;
          }

        if( AccumInIsKnown( ToSet ) &&
            !AccumOutIsKnown( Above ))
          {
          if( GetAccumInValue( ToSet ))
            Above = SetAccumOut( Above );
          else
            Above = ClearAccumOut( Above );

          MultArray[Row - 1].OneLine[Column + 1] = Above;
          continue;
          }
        }
      }

    /////////
    for( int Row = 1; Row <= HighestRow; Row++ )
      {
      for( int Column = 0; Column <= HighestColumn; Column++ )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint AboveRight = MultArray[Row - 1].OneLine[Column]; // Same column.
        if( CarryInIsKnown( ToSet ) &&
            CarryOutIsKnown( AboveRight ))
          {
          if( GetCarryOutValue( AboveRight ) !=
              GetCarryInValue( ToSet ))
            throw( new Exception( "Carry AboveRight doesn't match Carry ToSet." ));

          }

        if( CarryInIsKnown( ToSet ) &&
            !CarryOutIsKnown( AboveRight ))
          {
          if( GetCarryInValue( ToSet ))
            AboveRight = SetCarryOut( AboveRight );
          else
            AboveRight = ClearCarryOut( AboveRight );

          MultArray[Row - 1].OneLine[Column] = AboveRight;
          continue;
          }

        if( !CarryInIsKnown( ToSet ) &&
            CarryOutIsKnown( AboveRight ))
          {
          if( GetCarryOutValue( AboveRight ))
            ToSet = SetCarryIn( ToSet );
          else
            ToSet = ClearCarryIn( ToSet );

          MultArray[Row].OneLine[Column] = ToSet;
          continue;
          }
        }
      }
    }



  private void SetAllMult2Values()
    {
    // Mult1 is along the top and Mult2 is along
    // the right side diagonal.

    int HighestColumn = HighestCalculationColumn + 1;
    if( HighestColumn >= MultArraySize )
      HighestColumn = MultArraySize - 1;

    for( int Row = 0; Row <= HighestCalculationRow; Row++ )
      {
      for( int Column = 1; Column <= HighestColumn; Column++ )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint Right = MultArray[Row].OneLine[Column - 1];

        if( Mult2IsKnown( ToSet ) &&
            Mult2IsKnown( Right ))
          {
          if( GetMult2Value( ToSet ) !=
              GetMult2Value( Right ))
            throw( new Exception( "Mult2 values don't match at Row: " + Row.ToString() + "  Column: " + Column.ToString() ));

          continue;
          }

        if( !Mult2IsKnown( ToSet ) &&
             Mult2IsKnown( Right ))
          {
          if( GetMult2Value( Right ))
            ToSet = SetMult2( ToSet );
          else
            ToSet = ClearMult2( ToSet );

          MultArray[Row].OneLine[Column] = ToSet;
          continue;
          }

        if( Mult2IsKnown( ToSet ) &&
            !Mult2IsKnown( Right ))
          {
          if( GetMult2Value( ToSet ))
            Right = SetMult2( Right );
          else
            Right = ClearMult2( Right );

          MultArray[Row].OneLine[Column - 1] = Right;
          continue;
          }
        }
      }
    }



  private void SetAllMult1Values()
    {
    // Mult1 is along the top and Mult2 is along
    // the right side diagonal.

    int HighestRow = HighestCalculationRow + 1;
    if( HighestRow >= MultArraySize )
      HighestRow = MultArraySize - 1;

    for( int Column = 0; Column <= HighestCalculationColumn; Column++ )
      {
      for( int Row = 1; Row <= HighestRow; Row++ )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint Top = MultArray[Row - 1].OneLine[Column];

        if( Mult1IsKnown( ToSet ) &&
            Mult1IsKnown( Top ))
          {
          if( GetMult1Value( ToSet ) !=
              GetMult1Value( Top ))
            throw( new Exception( "Mult1 values don't match." ));

          continue;
          }

        if( Mult1IsKnown( ToSet ) &&
            !Mult1IsKnown( Top ))
          {
          if( GetMult1Value( ToSet ))
            {
            Top = SetMult1( Top );
            }
          else
            {
            Top = ClearMult1( Top );
            Top = ClearCarryOut( Top );
            }

          MultArray[Row - 1].OneLine[Column] = Top;
          continue;
          }

        if( !Mult1IsKnown( ToSet ) &&
            Mult1IsKnown( Top ))
          {
          if( GetMult1Value( Top ))
            {
            ToSet = SetMult1( ToSet );
            }
          else
            {
            ToSet = ClearMult1( ToSet );
            ToSet = ClearCarryOut( ToSet );
            }

          MultArray[Row].OneLine[Column] = ToSet;
          continue;
          }
        }
      }
    }



  private bool Mult1IsKnown( uint ToCheck )
    {
    return ( (ToCheck & Mult1Known) == Mult1Known );
    }

  private bool Mult2IsKnown( uint ToCheck )
    {
    return ( (ToCheck & Mult2Known) == Mult2Known );
    }

  private bool MultIsKnown( uint ToCheck )
    {
    return ( (ToCheck & MultKnown) == MultKnown );
    }

  private bool AccumInIsKnown( uint ToCheck )
    {
    return ( (ToCheck & AccumInKnown) == AccumInKnown );
    }

  private bool CarryInIsKnown( uint ToCheck )
    {
    return ( (ToCheck & CarryInKnown) == CarryInKnown );
    }

  private bool AccumOutIsKnown( uint ToCheck )
    {
    return ( (ToCheck & AccumOutKnown) == AccumOutKnown );
    }

  private bool CarryOutIsKnown( uint ToCheck )
    {
    return ( (ToCheck & CarryOutKnown) == CarryOutKnown );
    }

  private bool GetMult1Value( uint ToCheck )
    {
    return ( (ToCheck & Mult1) == Mult1 );
    }

  private bool GetMult2Value( uint ToCheck )
    {
    return ( (ToCheck & Mult2) == Mult2 );
    }

  private bool GetMultValue( uint ToCheck )
    {
    return ( (ToCheck & Mult) == Mult );
    }

  private bool GetAccumInValue( uint ToCheck )
    {
    return ( (ToCheck & AccumIn) == AccumIn );
    }

  private bool GetCarryInValue( uint ToCheck )
    {
    return ( (ToCheck & CarryIn) == CarryIn );
    }

  private bool GetAccumOutValue( uint ToCheck )
    {
    return ( (ToCheck & AccumOut) == AccumOut );
    }

  private bool GetCarryOutValue( uint ToCheck )
    {
    return ( (ToCheck & CarryOut) == CarryOut );
    }



  private uint SetMult1( uint ToSet )
    {
    ToSet |= Mult1;
    ToSet |= Mult1Known;
    return ToSet;
    }

  private uint SetMult2( uint ToSet )
    {
    ToSet |= Mult2;
    ToSet |= Mult2Known;
    return ToSet;
    }

  private uint SetMult( uint ToSet )
    {
    ToSet |= Mult;
    ToSet |= MultKnown;
    return ToSet;
    }

  private uint SetAccumIn( uint ToSet )
    {
    ToSet |= AccumIn;
    ToSet |= AccumInKnown;
    return ToSet;
    }

  private uint SetCarryIn( uint ToSet )
    {
    ToSet |= CarryIn;
    ToSet |= CarryInKnown;
    return ToSet;
    }

  private uint SetAccumOut( uint ToSet )
    {
    ToSet |= AccumOut;
    ToSet |= AccumOutKnown;
    return ToSet;
    }

  private uint SetCarryOut( uint ToSet )
    {
    ToSet |= CarryOut;
    ToSet |= CarryOutKnown;
    return ToSet;
    }


  private uint ClearMult1( uint ToSet )
    {
    ToSet &= ~Mult1;
    ToSet |= Mult1Known;
    return ToSet;
    }

  private uint ClearMult2( uint ToSet )
    {
    ToSet &= ~Mult2;
    ToSet |= Mult2Known;
    return ToSet;
    }

  private uint ClearMult( uint ToSet )
    {
    ToSet &= ~Mult;
    ToSet |= MultKnown;
    return ToSet;
    }

  private uint ClearAccumIn( uint ToSet )
    {
    ToSet &= ~AccumIn;
    ToSet |= AccumInKnown;
    return ToSet;
    }

  private uint ClearCarryIn( uint ToSet )
    {
    ToSet &= ~CarryIn;
    ToSet |= CarryInKnown;
    return ToSet;
    }

  private uint ClearAccumOut( uint ToSet )
    {
    ToSet &= ~AccumOut;
    ToSet |= AccumOutKnown;
    return ToSet;
    }

  private uint ClearCarryOut( uint ToSet )
    {
    ToSet &= ~CarryOut;
    ToSet |= CarryOutKnown;
    return ToSet;
    }



   private void DrawOneValue( uint Value,
                               Graphics BitGraph,
                               int X,
                               int Y,
                               SolidBrush WhiteBrush,
                               SolidBrush RedBrush,
                               SolidBrush BlueBrush,
                               SolidBrush DarkGreenBrush )
    {
    int Height = 2;
    int Width = 2;
    if( !AccumOutIsKnown( Value ))
      {
      // BitGraph.DrawRectangle( GreenPen, X, Y, Width, Height );
      BitGraph.FillRectangle( DarkGreenBrush, X, Y, Width - 1 , Height - 1 );
      return;
      }

    if( GetAccumOutValue( Value ))
      BitGraph.FillRectangle( WhiteBrush, X, Y, Width, Height );
    else
      BitGraph.FillRectangle( RedBrush, X, Y, Width, Height );

    }



  internal void Draw( Graphics BitGraph, int Height, int Width )
    {
    /*
    Pen GreenPen = new Pen( Brushes.DarkGreen );
    GreenPen.Width = 0.5F;
    GreenPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round; // Bevel
    GreenPen.DashStyle = DashStyle.Solid; // DashDot, DashDotDot, Custom
    */

    SolidBrush WhiteBrush = new SolidBrush( Color.White );
    SolidBrush DarkGreenBrush = new SolidBrush( Color.DarkGreen );
    SolidBrush RedBrush = new SolidBrush( Color.Red );
    SolidBrush BlueBrush = new SolidBrush( Color.LightBlue );

    int CellHeight = 4;
    int CellWidth = 4;
    int OffsetLeft = 0;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      int Y = (Row * CellHeight) + 10;
      if( Y > Height )
          break;

      for( int Column = 0; Column < MultArraySize; Column++ )
        {
        int X = Width - (Column * CellWidth) - 20 - OffsetLeft;
        if( X < 0 )
          break;

        uint Value = MultArray[Row].OneLine[Column];
        DrawOneValue( Value,
                      BitGraph,
                      X,
                      Y,
                      WhiteBrush,
                      RedBrush,
                      BlueBrush,
                      DarkGreenBrush );

        }

      OffsetLeft += CellWidth;
      }
    }



  private uint CalculateValues( uint ToSet )
    {
    if( AllFiveInsOutsAreKnown( ToSet ))
      return ToSet;

    uint TestIn = ToSet;
    uint TestOut = GetOutputFromInput( ToSet );

    ToSet = SetMultFrom1and2( ToSet );
    ToSet = ClearFromAllZerosIn( ToSet );
    ToSet = SetCarryOutputFromTopTwo( ToSet );
    ToSet = SetOutputsFromTopThree( ToSet );
    ToSet = SetFromTheBottomWith11( ToSet );
    ToSet = SetFromTheBottomWith00( ToSet );
    ToSet = SetFromTheBottomWith10( ToSet );
    ToSet = SetFromTheBottomWith01( ToSet );
    ToSet = SetAccumOutKnown( ToSet );
    ToSet = SetCarryOutKnown( ToSet );

    if( TestOut != ToSet )
      {
      string ShowS = "      case 0x" + TestIn.ToString( "X" ) + ": return 0x" + ToSet.ToString( "X" ) + ";";
      MForm.ShowStatus( ShowS );
      // throw( new Exception( "Missed a value for GetOutputFromInput():\r\n" + ShowS ));
      }

    return ToSet;
    }



  private void GetHighestCalcRowAndColumn()
    {
    int HighestRow = 0;
    int HighestColumn = 0;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column < MultArraySize; Column++ )
        {
        uint ToCheck = MultArray[Row].OneLine[Column];
        if( !AllValuesAreKnown( ToCheck ))
          {
          if( HighestColumn < Column )
            HighestColumn = Column;

          if( HighestRow < Row )
            HighestRow = Row;

          }
        }
      }

    HighestCalculationColumn = HighestColumn;
    HighestCalculationRow = HighestRow;
    }



  internal void TestValues()
    {
    GetHighestCalcRowAndColumn();
    CalculateAllValues();
    if( !MForm.CheckEvents())
      return;

    SetAllMult1Values();
    CalculateAllValues();
    SetAllMult2Values();
    CalculateAllValues();
    if( !MForm.CheckEvents())
      return;

    CheckMult2ValuesForConflicts();
    CalculateAllValues();

    // This uses the copy array.
    TestMult2Values();
    CalculateAllValues();
    }



  private void CalculateAllValues()
    {
    SetInsAndOuts();

    for( int Row = 0; Row <= HighestCalculationRow; Row++ )
      {
      for( int Column = 0; Column <= HighestCalculationColumn; Column++ )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint Input = ToSet;
        ToSet = CalculateValues( ToSet );
        uint Output = ToSet;
        InputOutputDictionary[Input] = Output;
        MultArray[Row].OneLine[Column] = ToSet;
        }
      }

    SetInsAndOuts();

    // Calculate from the bottom up.
    for( int Row = HighestCalculationRow; Row >= 0; Row-- )
      {
      for( int Column = HighestCalculationColumn; Column >= 0; Column-- )
        {
        uint ToSet = MultArray[Row].OneLine[Column];
        uint Input = ToSet;
        ToSet = CalculateValues( ToSet );
        uint Output = ToSet;
        InputOutputDictionary[Input] = Output;
        MultArray[Row].OneLine[Column] = ToSet;
        }
      }

    SetInsAndOuts();
    }



  internal void MakeUnknownsDictionary()
    {
    SortedDictionary<uint, uint> UnsolvedDictionary =
               new SortedDictionary<uint, uint>();

    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column < MultArraySize; Column++ )
        {
        uint ToCheck = MultArray[Row].OneLine[Column];
        if( !AllValuesAreKnown( ToCheck ))
          {
          if( UnsolvedDictionary.ContainsKey( ToCheck ))
            UnsolvedDictionary[ToCheck] = UnsolvedDictionary[ToCheck] + 1;
          else
            UnsolvedDictionary[ToCheck] = 1;

          }
        }
      }

    foreach( KeyValuePair<uint, uint> Kvp in UnsolvedDictionary )
      {
      MForm.ShowStatus( " " );

      MForm.ShowStatus( "0x" + Kvp.Key.ToString( "X" ) + "    HowMany: " + Kvp.Value.ToString() );
      // ShowUnknownValues( Kvp.Key );
      MForm.ShowStatus( "Known:" );
      ShowKnownValues( Kvp.Key );
      }
    }



  private void ShowKnownValues( uint ToCheck )
    {
    if( Mult1IsKnown( ToCheck ))
      MForm.ShowStatus( "Mult1 " + GetMult1Value( ToCheck ) );

    if( Mult2IsKnown( ToCheck ))
      MForm.ShowStatus( "Mult2 " + GetMult2Value( ToCheck ) );

    if( MultIsKnown( ToCheck ))
      MForm.ShowStatus( "Mult " + GetMultValue( ToCheck ) );

    if( AccumInIsKnown( ToCheck ))
      MForm.ShowStatus( "AccumIn " + GetAccumInValue( ToCheck ) );

    if( CarryInIsKnown( ToCheck ))
      MForm.ShowStatus( "CarryIn " + GetCarryInValue( ToCheck ) );

    if( AccumOutIsKnown( ToCheck ))
      MForm.ShowStatus( "AccumOut " + GetAccumOutValue( ToCheck ) );

    if( CarryOutIsKnown( ToCheck ))
      MForm.ShowStatus( "CarryOut " + GetCarryOutValue( ToCheck ) );

    }



  private bool AllValuesAreKnown( uint ToCheck )
    {
    if( // Mult1IsKnown( ToCheck ) &&
        // Mult2IsKnown( ToCheck ) &&
        MultIsKnown( ToCheck ) &&
        AccumInIsKnown( ToCheck ) &&
        CarryInIsKnown( ToCheck ) &&
        AccumOutIsKnown( ToCheck ) &&
        CarryOutIsKnown( ToCheck ))
      return true;
    else
      return false;

    }




  private uint SetMultFrom1and2( uint ToSet )
    {
    if( !( Mult1IsKnown( ToSet ) ||
           Mult2IsKnown( ToSet ) ||
           MultIsKnown( ToSet )))
      return ToSet;

    if( Mult1IsKnown( ToSet ))
      {
      if( !GetMult1Value( ToSet ))
        {
        if( MultIsKnown( ToSet ))
          {
          if( GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be true here." ));

          }

        ToSet = ClearMult( ToSet );
        return ToSet;
        }
      }

    ///////
    if( Mult2IsKnown( ToSet ))
      {
      if( !GetMult2Value( ToSet ))
        {
        if( MultIsKnown( ToSet ))
          {
          if( GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be true here." ));

          }

        ToSet = ClearMult( ToSet );
        return ToSet;
        }
      }

    ///////
    if( Mult1IsKnown( ToSet ) &&
        Mult2IsKnown( ToSet ) &&
        GetMult1Value( ToSet ) &&
        GetMult2Value( ToSet ))
      {
      if( MultIsKnown( ToSet ))
        {
        if( !GetMultValue( ToSet ))
          throw( new Exception( "Mult can't be false here." ));

        }

      ToSet = SetMult( ToSet );
      return ToSet;
      }

    // If it got this far then either Mult1 and Mult2
    // are both unknown, or only one of them is
    // known and its value is 1.
    if( !MultIsKnown( ToSet ))
      return ToSet; // It is unknown what it can be
                    // at this point.

    // Neither of Mult1 or Mult2 can be zero at
    // this point so they can't throw an exception
    // because of this.
    if( !GetMultValue( ToSet ))
      {
      // It doesn't know which one of these (or both)
      // is set to false.
      if( !Mult1IsKnown( ToSet ) &&
          !Mult2IsKnown( ToSet ))
        {
        return ToSet;
        }

      if( Mult1IsKnown( ToSet ))
        {
        if( GetMult1Value( ToSet ))
          {
          ToSet = ClearMult2( ToSet );
          return ToSet;
          }
        }

      if( Mult2IsKnown( ToSet ))
        {
        if( GetMult2Value( ToSet ))
          {
          ToSet = ClearMult1( ToSet );
          return ToSet;
          }
        }
      }
    else
      {
      // Mult value is true.
      // Neither of Mult1 or Mult2 is set to false at
      // this point.
      ToSet = SetMult1( ToSet );
      ToSet = SetMult2( ToSet );
      return ToSet;
      }

    /*
    string ShowS = "Mult1IsKnown: " + Mult1IsKnown( ToSet ) + "\r\n" + 
                   "Mult2IsKnown: " + Mult2IsKnown( ToSet ) + "\r\n" + 
                   "MultIsKnown: " + MultIsKnown( ToSet ) + "\r\n" + 
                   "Mult1Value: " + GetMult1Value( ToSet ) + "\r\n" + 
                   "Mult2Value: " + GetMult2Value( ToSet ) + "\r\n" + 
                   "MultValue: " + GetMultValue( ToSet ); 

    throw( new Exception( "Somehow it got here to the bottom of SetMultFrom1and2().\r\n" + ShowS ));
    */

    throw( new Exception( "Somehow it got here to the bottom of SetMultFrom1and2()." ));
    }



  private uint SetCarryOutputFromTopTwo( uint ToSet )
    {
    if( MultIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
          !GetCarryInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be true here." ));

          }

        ToSet = ClearCarryOut( ToSet );
        return ToSet;
        }
      }

    if( AccumInIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetAccumInValue( ToSet ) &&
          !GetCarryInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be true here." ));

          }

        ToSet = ClearCarryOut( ToSet );
        return ToSet;
        }
      }

    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
          !GetAccumInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be true here." ));

          }

        ToSet = ClearCarryOut( ToSet );
        return ToSet;
        }
      }


    if( MultIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( GetMultValue( ToSet ) &&
          GetCarryInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( !GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be false here." ));

          }

        ToSet = SetCarryOut( ToSet );
        return ToSet;
        }
      }

    if( AccumInIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( GetAccumInValue( ToSet ) &&
          GetCarryInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( !GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be false here." ));

          }

        ToSet = SetCarryOut( ToSet );
        return ToSet;
        }
      }

    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( GetMultValue( ToSet ) &&
          GetAccumInValue( ToSet ))
        {
        if( CarryOutIsKnown( ToSet ))
          {
          if( !GetCarryOutValue( ToSet ))
            throw( new Exception( "CarryOut can't be false here." ));

          }

        ToSet = SetCarryOut( ToSet );
        return ToSet;
        }
      }

    return ToSet;
    }



  private bool AllThreeInsAreKnown( uint ToSet )
    {
    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      return true;
    else
      return false;

    }



  private bool AllFiveInsOutsAreKnown( uint ToSet )
    {
    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ) &&
        AccumOutIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ) &&
        CarryOutIsKnown( ToSet ))
      return true;
    else
      return false;

    }



  private uint SetFromTheBottomWith11( uint ToSet )
    {
    if( AllThreeInsAreKnown( ToSet ))
      return ToSet;

    if( !( AccumOutIsKnown( ToSet ) &&
           CarryOutIsKnown( ToSet ) ))
      return ToSet;

    //////////
    if( !(GetCarryOutValue( ToSet ) &&
          GetAccumOutValue( ToSet )))
      return ToSet;

    if( MultIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ))
        throw( new Exception( "Mult can't be false when C and A out are both true." ));

      }
    else
      {
      ToSet = SetMult( ToSet );
      }

    if( AccumInIsKnown( ToSet ))
      {
      if( !GetAccumInValue( ToSet ))
        throw( new Exception( "AccumIn can't be false when C and A out are both true." ));

      }
    else
      {
      ToSet = SetAccumIn( ToSet );
      }

    if( CarryInIsKnown( ToSet ))
      {
      if( !GetCarryInValue( ToSet ))
        throw( new Exception( "CarryIn can't be false when C and A out are both true." ));

      }
    else
      {
      ToSet = SetCarryIn( ToSet );
      }

    return ToSet;
    }



  private uint SetFromTheBottomWith00( uint ToSet )
    {
    if( AllThreeInsAreKnown( ToSet ))
      return ToSet;

    if( !( AccumOutIsKnown( ToSet ) &&
           CarryOutIsKnown( ToSet ) ))
      return ToSet;

    /////////////
    if( !( !GetCarryOutValue( ToSet ) &&
           !GetAccumOutValue( ToSet )))
      return ToSet;

    if( MultIsKnown( ToSet ))
      {
      if( GetMultValue( ToSet ))
        throw( new Exception( "Mult can't be true when C and A out are both false." ));

      }
    else
      {
      ToSet = ClearMult( ToSet );
      }

    if( AccumInIsKnown( ToSet ))
      {
      if( GetAccumInValue( ToSet ))
        throw( new Exception( "AccumIn can't be true when C and A out are both false." ));

      }
    else
      {
      ToSet = ClearAccumIn( ToSet );
      }

    if( CarryInIsKnown( ToSet ))
      {
      if( GetCarryInValue( ToSet ))
        throw( new Exception( "CarryIn can't be true when C and A out are both false." ));

      }
    else
      {
      ToSet = ClearCarryIn( ToSet );
      }

    return ToSet;
    }



  private uint SetFromTheBottomWith01( uint ToSet )
    {
    if( AllThreeInsAreKnown( ToSet ))
      return ToSet;

    if( !( AccumOutIsKnown( ToSet ) &&
           CarryOutIsKnown( ToSet ) ))
      return ToSet;

    if( !( !GetCarryOutValue( ToSet ) &&
            GetAccumOutValue( ToSet )))
      return ToSet;

    // The input has to be 100.

    if( MultIsKnown( ToSet ))
      {
      if( GetMultValue( ToSet ))
        {
        if( AccumInIsKnown( ToSet ))
          {
          if( GetAccumInValue( ToSet ))
            throw( new Exception( "AccumIn can't be true if Mult is true." ));

          }

        if( CarryInIsKnown( ToSet ))
          {
          if( GetCarryInValue( ToSet ))
            throw( new Exception( "CarryIn can't be true if Mult is true." ));

          }

        ToSet = ClearAccumIn( ToSet );
        ToSet = ClearCarryIn( ToSet );
        return ToSet;
        }
      }

    if( AccumInIsKnown( ToSet ))
      {
      if( GetAccumInValue( ToSet ))
        {
        if( MultIsKnown( ToSet ))
          {
          if( GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be true if AccumIn is true." ));

          }

        if( CarryInIsKnown( ToSet ))
          {
          if( GetCarryInValue( ToSet ))
            throw( new Exception( "CarryIn can't be true if AccumIn is true." ));

          }

        ToSet = ClearMult( ToSet );
        ToSet = ClearCarryIn( ToSet );
        return ToSet;
        }
      }

    if( CarryInIsKnown( ToSet ))
      {
      if( GetCarryInValue( ToSet ))
        {
        if( MultIsKnown( ToSet ))
          {
          if( GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be true if CarryIn is true." ));

          }

        if( AccumInIsKnown( ToSet ))
          {
          if( GetAccumInValue( ToSet ))
            throw( new Exception( "AccumIn can't be true if CarryIn is true." ));

          }

        ToSet = ClearMult( ToSet );
        ToSet = ClearAccumIn( ToSet );
        return ToSet;
        }
      }

    //////
    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
           GetAccumInValue( ToSet ))
        {
        ToSet = ClearCarryIn( ToSet );
        }

      if( GetMultValue( ToSet ) &&
          !GetAccumInValue( ToSet ))
        {
        ToSet = ClearCarryIn( ToSet );
        }
      }

    if( MultIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
           GetCarryInValue( ToSet ))
        {
        ToSet = ClearAccumIn( ToSet );
        }

      if( GetMultValue( ToSet ) &&
         !GetCarryInValue( ToSet ))
        {
        ToSet = ClearAccumIn( ToSet );
        }
      }

    if( AccumInIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetAccumInValue( ToSet ) &&
           GetCarryInValue( ToSet ))
        {
        ToSet = ClearMult( ToSet );
        }

      if( GetAccumInValue( ToSet ) &&
         !GetCarryInValue( ToSet ))
        {
        ToSet = ClearMult( ToSet );
        }
      }


    ///////
    if( AccumInIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetAccumInValue( ToSet ) &&
          !GetCarryInValue( ToSet ))
        {
        ToSet = SetMult( ToSet );
        }
      }

    if( MultIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
          !GetCarryInValue( ToSet ))
        {
        ToSet = SetAccumIn( ToSet );
        }
      }

    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
          !GetAccumInValue( ToSet ))
        {
        ToSet = SetCarryIn( ToSet );
        }
      }

    return ToSet;
    }



  private uint SetFromTheBottomWith10( uint ToSet )
    {
    if( AllThreeInsAreKnown( ToSet ))
      return ToSet;

    if( !( AccumOutIsKnown( ToSet ) &&
           CarryOutIsKnown( ToSet ) ))
      return ToSet;

    ////////  This can only have 110 as input.
    if( !( GetCarryOutValue( ToSet ) &&
           !GetAccumOutValue( ToSet )))
      return ToSet;

    // Input has to be 110.
    if( CarryInIsKnown( ToSet ))
      {
      if( !GetCarryInValue( ToSet ))
        {
        if( AccumInIsKnown( ToSet ))
          {
          if( !GetAccumInValue( ToSet ))
            throw( new Exception( "AccumIn can't be false here." ));

          }

        if( MultIsKnown( ToSet ))
          {
          if( !GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be false here." ));

          }

        ToSet = SetAccumIn( ToSet );
        ToSet = SetMult( ToSet );
        return ToSet;
        }
      }

    if( AccumInIsKnown( ToSet ))
      {
      if( !GetAccumInValue( ToSet ))
        {
        if( CarryInIsKnown( ToSet ))
          {
          if( !GetCarryInValue( ToSet ))
            throw( new Exception( "CarryIn can't be false here." ));

          }

        if( MultIsKnown( ToSet ))
          {
          if( !GetMultValue( ToSet ))
            throw( new Exception( "Mult can't be false here." ));

          }

        ToSet = SetCarryIn( ToSet );
        ToSet = SetMult( ToSet );
        return ToSet;
        }
      }

    if( MultIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ))
        {
        if( CarryInIsKnown( ToSet ))
          {
          if( !GetCarryInValue( ToSet ))
            throw( new Exception( "CarryIn can't be false here." ));

          }

        if( AccumInIsKnown( ToSet ))
          {
          if( !GetAccumInValue( ToSet ))
            throw( new Exception( "AccumIn can't be false here." ));

          }

        ToSet = SetCarryIn( ToSet );
        ToSet = SetAccumIn( ToSet );
        return ToSet;
        }
      }


    /////////
    if( CarryInIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( !GetCarryInValue( ToSet ) &&
           GetAccumInValue( ToSet ))
        {
        ToSet = SetMult( ToSet );
        return ToSet;
        }

      if( GetCarryInValue( ToSet ) &&
          !GetAccumInValue( ToSet ))
        {
        ToSet = SetMult( ToSet );
        return ToSet;
        }
      }

    if( MultIsKnown( ToSet ) &&
        AccumInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
           GetAccumInValue( ToSet ))
        {
        ToSet = SetCarryIn( ToSet );
        return ToSet;
        }

      if( GetMultValue( ToSet ) &&
          !GetAccumInValue( ToSet ))
        {
        ToSet = SetCarryIn( ToSet );
        return ToSet;
        }
      }

    if( MultIsKnown( ToSet ) &&
        CarryInIsKnown( ToSet ))
      {
      if( !GetMultValue( ToSet ) &&
           GetCarryInValue( ToSet ))
        {
        ToSet = SetAccumIn( ToSet );
        return ToSet;
        }

      if( GetMultValue( ToSet ) &&
          !GetCarryInValue( ToSet ))
        {
        ToSet = SetAccumIn( ToSet );
        return ToSet;
        }
      }

    return ToSet;
    }


  private uint ClearFromAllZerosIn( uint ToSet )
    {
    if( !AllThreeInsAreKnown( ToSet ))
      return ToSet;

    ////////// All Zeros in.
    if( !GetMultValue( ToSet ) &&
        !GetAccumInValue( ToSet ) &&
        !GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be true here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( GetCarryOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be true here." ));

        }

      ToSet = ClearAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      return ToSet;
      }

    return ToSet;
    }



  private uint SetOutputsFromTopThree( uint ToSet )
    {
    if( !AllThreeInsAreKnown( ToSet ))
      return ToSet;

    ////// All true values.
    if( GetMultValue( ToSet ) &&
        GetAccumInValue( ToSet ) &&
        GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( !GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be false here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( !GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be false here." ));

        }

      ToSet = SetAccumOut( ToSet );
      ToSet = SetCarryOut( ToSet );
      return ToSet;
      }

    ///////// One true value.
    ////////
    if( GetMultValue( ToSet ) &&
        !GetAccumInValue( ToSet ) &&
        !GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( !GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be false here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be true here." ));

        }

      ToSet = SetAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      return ToSet;
      }

    ////////
    if( !GetMultValue( ToSet ) &&
        GetAccumInValue( ToSet ) &&
        !GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( !GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be false here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be true here." ));

        }

      ToSet = SetAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      return ToSet;
      }

    ///////
    if( !GetMultValue( ToSet ) &&
        !GetAccumInValue( ToSet ) &&
        GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( !GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be false here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be true here." ));

        }

      ToSet = SetAccumOut( ToSet );
      ToSet = ClearCarryOut( ToSet );
      return ToSet;
      }

    /////////// Two true values.
    ///////
    if( !GetMultValue( ToSet ) &&
        GetAccumInValue( ToSet ) &&
        GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be true here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( !GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be false here." ));

        }

      ToSet = ClearAccumOut( ToSet );
      ToSet = SetCarryOut( ToSet );
      return ToSet;
      }

    ///////
    if( GetMultValue( ToSet ) &&
        !GetAccumInValue( ToSet ) &&
        GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be true here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( !GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be false here." ));

        }

      ToSet = ClearAccumOut( ToSet );
      ToSet = SetCarryOut( ToSet );
      return ToSet;
      }

    ///////
    if( GetMultValue( ToSet ) &&
        GetAccumInValue( ToSet ) &&
        !GetCarryInValue( ToSet ))
      {
      if( AccumOutIsKnown( ToSet ))
        {
        if( GetAccumOutValue( ToSet ))
          throw( new Exception( "AccumOut can't be true here." ));

        }

      if( CarryOutIsKnown( ToSet ))
        {
        if( !GetCarryOutValue( ToSet ))
          throw( new Exception( "CarryOut can't be false here." ));

        }

      ToSet = ClearAccumOut( ToSet );
      ToSet = SetCarryOut( ToSet );
      return ToSet;
      }

    return ToSet;
    }



  private uint SetAccumOutKnown( uint ToSet )
    {
    if( !( AccumOutIsKnown( ToSet ) &&
           !CarryOutIsKnown( ToSet )))
      return ToSet;

    if( GetAccumOutValue( ToSet ))
      {
      // AccumOut is true, so it can only be 111 or
      // 100, but not 110.  So if anything is false
      // then one other one has to be false too.
      // If two of them are true then the other one
      // has to be true also.
      if( AccumInIsKnown( ToSet ) &&
          CarryInIsKnown( ToSet ))
        {
        if( GetAccumInValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = SetMult( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( !GetAccumInValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = ClearMult( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( GetAccumInValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = ClearMult( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }


      if( MultIsKnown( ToSet ) &&
          CarryInIsKnown( ToSet ))
        {
        if( GetMultValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = SetAccumIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( !GetMultValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = ClearAccumIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( GetMultValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = ClearAccumIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ) &&
          AccumInIsKnown( ToSet ))
        {
        if( GetMultValue( ToSet ) &&
            GetAccumInValue( ToSet ))
          {
          ToSet = SetCarryIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( !GetMultValue( ToSet ) &&
            GetAccumInValue( ToSet ))
          {
          ToSet = ClearCarryIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( GetMultValue( ToSet ) &&
            !GetAccumInValue( ToSet ))
          {
          ToSet = ClearCarryIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }

      //////
      if( AccumInIsKnown( ToSet ))
        {
        if( !GetAccumInValue( ToSet ))
          {
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }

      if( CarryInIsKnown( ToSet ))
        {
        if( !GetCarryInValue( ToSet ))
          {
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ))
        {
        if( !GetMultValue( ToSet ))
          {
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }
        }
      }
    else
      {
      // AccumOut is false.
      // So it can be 110 or 000, but not 100.
      if( AccumInIsKnown( ToSet ) &&
          CarryInIsKnown( ToSet ))
        {
        if( !GetAccumInValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = ClearMult( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( !GetAccumInValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = SetMult( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( GetAccumInValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = SetMult( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ) &&
          CarryInIsKnown( ToSet ))
        {
        if( !GetMultValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = ClearAccumIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( !GetMultValue( ToSet ) &&
            GetCarryInValue( ToSet ))
          {
          ToSet = SetAccumIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( GetMultValue( ToSet ) &&
            !GetCarryInValue( ToSet ))
          {
          ToSet = SetAccumIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ) &&
          AccumInIsKnown( ToSet ))
        {
        if( !GetMultValue( ToSet ) &&
            !GetAccumInValue( ToSet ))
          {
          ToSet = ClearCarryIn( ToSet );
          ToSet = ClearCarryOut( ToSet );
          return ToSet;
          }

        if( !GetMultValue( ToSet ) &&
            GetAccumInValue( ToSet ))
          {
          ToSet = SetCarryIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }

        if( GetMultValue( ToSet ) &&
            !GetAccumInValue( ToSet ))
          {
          ToSet = SetCarryIn( ToSet );
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }

      ////////
      if( AccumInIsKnown( ToSet ))
        {
        if( GetAccumInValue( ToSet ))
          {
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }

      if( CarryInIsKnown( ToSet ))
        {
        if( GetCarryInValue( ToSet ))
          {
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ))
        {
        if( GetMultValue( ToSet ))
          {
          ToSet = SetCarryOut( ToSet );
          return ToSet;
          }
        }
      }

    return ToSet;
    }



  private uint SetCarryOutKnown( uint ToSet )
    {
    if( !( !AccumOutIsKnown( ToSet ) &&
           CarryOutIsKnown( ToSet )))
      return ToSet;

    if( GetCarryOutValue( ToSet ))
      {
      if( AccumInIsKnown( ToSet ))
        {
        if( !GetAccumInValue( ToSet ))
          {
          ToSet = SetMult( ToSet );
          ToSet = SetCarryIn( ToSet );
          ToSet = ClearAccumOut( ToSet );
          return ToSet;
          }
        }

      if( CarryInIsKnown( ToSet ))
        {
        if( !GetCarryInValue( ToSet ))
          {
          ToSet = SetMult( ToSet );
          ToSet = SetAccumIn( ToSet );
          ToSet = ClearAccumOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ))
        {
        if( !GetMultValue( ToSet ))
          {
          ToSet = SetCarryIn( ToSet );
          ToSet = SetAccumIn( ToSet );
          ToSet = ClearAccumOut( ToSet );
          return ToSet;
          }
        }
      }
    else
      {
      // CarryOut is false.
      if( AccumInIsKnown( ToSet ))
        {
        if( GetAccumInValue( ToSet ))
          {
          ToSet = ClearMult( ToSet );
          ToSet = ClearCarryIn( ToSet );
          ToSet = SetAccumOut( ToSet );
          return ToSet;
          }
        }

      if( CarryInIsKnown( ToSet ))
        {
        if( GetCarryInValue( ToSet ))
          {
          ToSet = ClearMult( ToSet );
          ToSet = ClearAccumIn( ToSet );
          ToSet = SetAccumOut( ToSet );
          return ToSet;
          }
        }

      if( MultIsKnown( ToSet ))
        {
        if( GetMultValue( ToSet ))
          {
          ToSet = ClearCarryIn( ToSet );
          ToSet = ClearAccumIn( ToSet );
          ToSet = SetAccumOut( ToSet );
          return ToSet;
          }
        }
      }

    return ToSet;
    }



  private void CheckMult2ValuesForConflicts()
    {
    int HighestIndex = MultArraySize - 1;
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column <= HighestIndex; Column++ )
        {
        uint ToCheck = MultArray[Row].OneLine[Column];
        if( Mult2IsKnown( ToCheck ))
          break; // To the next row.

        if( Mult2ConflictsForBothBottomKnown( ToCheck, true ))
          {
          // MForm.ShowStatus( "Mult2 was cleared 1." );
          ToCheck = ClearMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        if( Mult2ConflictsForBothBottomKnown( ToCheck, false ))
          {
          // MForm.ShowStatus( "Mult2 was set 1." );
          ToCheck = SetMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        if( Mult2ConflictsForBottomCarryKnown( ToCheck, true ))
          {
          // MForm.ShowStatus( "Mult2 was cleared 2." );
          ToCheck = ClearMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        if( Mult2ConflictsForBottomCarryKnown( ToCheck, false ))
          {
          // MForm.ShowStatus( "Mult2 was set 2." );
          ToCheck = SetMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        if( Mult2ConflictsForBottomAccumKnown( ToCheck, true ))
          {
          // MForm.ShowStatus( "Mult2 was cleared 3." );
          ToCheck = ClearMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        if( Mult2ConflictsForBottomAccumKnown( ToCheck, false ))
          {
          // MForm.ShowStatus( "Mult2 was set 3." );
          ToCheck = SetMult2( ToCheck );
          MultArray[Row].OneLine[Column] = ToCheck;
          break;
          }

        }
      }
    }



  private bool Mult2ConflictsForBothBottomKnown( uint ToCheck, bool TestValue )
    {
    if( !Mult1IsKnown( ToCheck ))
      return false;

    // It's zero all across the left side.
    if( !GetMult1Value( ToCheck ))
      return false; // Then Mult2 can be anything.

    if( !( AccumOutIsKnown( ToCheck ) &&
           CarryOutIsKnown( ToCheck )))
      return false;

    if( GetAccumOutValue( ToCheck ) &&
        GetCarryOutValue( ToCheck ))
      {
      // All three in values have to be true.
      // When Mult2 is false, that's a conflict.
      if( !TestValue )
        return true;

      }

    if( !GetAccumOutValue( ToCheck ) &&
        !GetCarryOutValue( ToCheck ))
      {
      if( TestValue )
        return true;

      }

    ////////
    if( !GetAccumOutValue( ToCheck ) &&
         GetCarryOutValue( ToCheck ))
      {

      if( TestValue )
        {
        if( AccumInIsKnown( ToCheck ) &&
            CarryInIsKnown( ToCheck ))
          {
          if( GetCarryInValue( ToCheck ) &&
              GetAccumInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      else
        {
        // TestValue is false.
        // There have to be two true values: 110.
        // If one other value is false then TestValue
        // can't be false.
        if( CarryInIsKnown( ToCheck ))
          {
          if( !GetCarryInValue( ToCheck ))
            return true;

          }

        if( AccumInIsKnown( ToCheck ))
          {
          if( !GetAccumInValue( ToCheck ))
            return true;

          }
        }
      }

    ////////
    if(  GetAccumOutValue( ToCheck ) &&
        !GetCarryOutValue( ToCheck ))
      {
      // Only one in value can be true.
      if( TestValue )
        {
        if( CarryInIsKnown( ToCheck ))
          {
          if( GetCarryInValue( ToCheck ))
            return true;

          }

        if( AccumInIsKnown( ToCheck ))
          {
          if( GetAccumInValue( ToCheck ))
            return true;

          }

        }
      else
        {
        // TestValue is false.
        if( CarryInIsKnown( ToCheck ) &&
            AccumInIsKnown( ToCheck ))
          {
          if( !GetCarryInValue( ToCheck ) &&
              !GetAccumInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      }

    return false;
    }



  private bool Mult2ConflictsForBottomCarryKnown( uint ToCheck, bool TestValue )
    {
    if( !Mult1IsKnown( ToCheck ))
      return false;

    // It's zero all across the left side.
    if( !GetMult1Value( ToCheck ))
      return false; // Then Mult2 can be anything.

    if( !( !AccumOutIsKnown( ToCheck ) &&
            CarryOutIsKnown( ToCheck )))
      return false;


    if( GetCarryOutValue( ToCheck ))
      {
      // It can only be 110 or 111.
      if( TestValue )
        {
        if( CarryInIsKnown( ToCheck ) &&
            AccumInIsKnown( ToCheck ))
          {
          if( !GetCarryInValue( ToCheck ) &&
              !GetAccumInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      else
        {
        // TestValue is false.
        // Only one value can be false.
        if( CarryInIsKnown( ToCheck ))
          {
          if( !GetCarryInValue( ToCheck ))
            return true;

          }

        if( AccumInIsKnown( ToCheck ))
          {
          if( !GetAccumInValue( ToCheck ))
            return true;

          }
        }
      }
    else
      {
      // CarryOut is false.
      // It can only be 001 or 000.
      if( TestValue )
        {
        if( AccumInIsKnown( ToCheck ))
          {
          if( GetAccumInValue( ToCheck ))
            return true;

          }

        if( CarryInIsKnown( ToCheck ))
          {
          if( GetCarryInValue( ToCheck ))
            return true;

          }
        }
      else
        {
        // TestValue is false.
        // It can only be 001 or 000.
        if( CarryInIsKnown( ToCheck ) &&
            AccumInIsKnown( ToCheck ))
          {
          if( GetCarryInValue( ToCheck ) &&
              GetAccumInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      }

    return false;
    }



  private bool Mult2ConflictsForBottomAccumKnown( uint ToCheck, bool TestValue )
    {
    if( !Mult1IsKnown( ToCheck ))
      return false;

    // It's zero all across the left side.
    if( !GetMult1Value( ToCheck ))
      return false; // Then Mult2 can be anything.

    if( !( AccumOutIsKnown( ToCheck ) &&
          !CarryOutIsKnown( ToCheck )))
      return false;

    if( GetAccumOutValue( ToCheck ))
      {
      // It can be 111 or 100

      if( TestValue )
        {
        if( AccumInIsKnown( ToCheck ) &&
            CarryInIsKnown( ToCheck ))
          {
          if( !GetAccumInValue( ToCheck ) &&
               GetCarryInValue( ToCheck ))
            {
            return true;
            }

          if( GetAccumInValue( ToCheck ) &&
              !GetCarryInValue( ToCheck ))
            {
            return true;
            }

          }
        }
      else
        {
        // TestValue is false.

      // It can be 111 or 100
        if( AccumInIsKnown( ToCheck ) &&
            CarryInIsKnown( ToCheck ))
          {
          if( GetAccumInValue( ToCheck ) &&
              GetCarryInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      }
    else
      {
      // Accum out is false.
      // It can only be 000 or 011.
      if( TestValue )
        {
        if( AccumInIsKnown( ToCheck ) &&
            CarryInIsKnown( ToCheck ))
          {
          if( GetAccumInValue( ToCheck ) &&
              GetCarryInValue( ToCheck ))
            {
            return true;
            }

          if( !GetAccumInValue( ToCheck ) &&
              !GetCarryInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      else
        {
        // TestValue is false.
        // It can only be 000 or 011.
        if( AccumInIsKnown( ToCheck ) &&
            CarryInIsKnown( ToCheck ))
          {
          if( !GetAccumInValue( ToCheck ) &&
              GetCarryInValue( ToCheck ))
            {
            return true;
            }

          if( GetAccumInValue( ToCheck ) &&
              !GetCarryInValue( ToCheck ))
            {
            return true;
            }
          }
        }
      }

    return false;
    }



  private uint GetOutputFromInput( uint Input )
    {
    switch( Input )
      {
      // Obviously this is a lot faster than doing
      // all the logic.  But it doesn't throw an
      // exception for a conflict.
      case 0x100: return 0x300;
      case 0x180: return 0x380;
      case 0x181: return 0x381;
      case 0x182: return 0x382;
      case 0x183: return 0x387;
      case 0x581: return 0x2781;
      case 0x583: return 0x787;
      case 0x589: return 0x789;
      case 0x58B: return 0x27CF;
      case 0x700: return 0x2700;
      case 0x780: return 0x2780;
      case 0x781: return 0x2781;
      case 0x78F: return 0x27CF;
      case 0x880: return 0x2A80;
      case 0x981: return 0x2B81;
      case 0x982: return 0x2B82;
      case 0x983: return 0xB87;
      case 0x991: return 0xB91;
      case 0x993: return 0x2BD7;
      case 0xB80: return 0x2B80;
      case 0xB81: return 0x2B81;
      case 0xB82: return 0x2B82;
      case 0xB97: return 0x2BD7;
      case 0xC00: return 0x2C00;
      case 0xC80: return 0x3E80;
      case 0xC81: return 0x2C81;
      case 0xC99: return 0x2CD9;
      case 0xD02: return 0x2D02;
      case 0xD08: return 0x3F28;
      case 0xD82: return 0x3F82;
      case 0xD83: return 0x3FA7;
      case 0xD89: return 0x3FA9;
      case 0xD8B: return 0x3FCF;
      case 0xD91: return 0x3FB1;
      case 0xD93: return 0x3FD7;
      case 0xF00: return 0x3F00;
      case 0xF80: return 0x3F80;
      case 0xF81: return 0x3F81;
      case 0xF82: return 0x3F82;
      case 0xF87: return 0x3FA7;
      case 0xF89: return 0x3FA9;
      case 0xF8F: return 0x3FCF;
      case 0xF91: return 0x3FB1;
      case 0xF97: return 0x3FD7;
      case 0xF99: return 0x3FD9;
      case 0xF9F: return 0x3FFF;
      case 0x1181: return 0x1381;
      case 0x1183: return 0x33C7;
      case 0x11A1: return 0x33A1;
      case 0x11A3: return 0x13A7;
      case 0x1320: return 0x3320;
      case 0x1387: return 0x33C7;
      case 0x13A0: return 0x33A0;
      case 0x13A1: return 0x33A1;
      case 0x1489: return 0x34C9;
      case 0x14A1: return 0x34A1;
      case 0x1581: return 0x3F81;
      case 0x1583: return 0x3FD7;
      case 0x1781: return 0x3F81;
      case 0x1789: return 0x3FD9;
      case 0x17A1: return 0x3FB1;
      case 0x17A7: return 0x3FA7;
      case 0x17A9: return 0x3FA9;
      case 0x17AF: return 0x3FFF;
      case 0x1891: return 0x38D1;
      case 0x18A1: return 0x38A1;
      case 0x1981: return 0x3F81;
      case 0x1983: return 0x3FCF;
      case 0x19A1: return 0x3FA9;
      case 0x19A3: return 0x3FA7;
      case 0x19B1: return 0x3FB1;
      case 0x19B3: return 0x3FFF;
      case 0x1B00: return 0x3F00;
      case 0x1B20: return 0x3F28;
      case 0x1B80: return 0x3F80;
      case 0x1B81: return 0x3F81;
      case 0x1B87: return 0x3FCF;
      case 0x1B91: return 0x3FD9;
      case 0x1BA0: return 0x3FA8;
      case 0x1BA1: return 0x3FA9;
      case 0x1BB7: return 0x3FFF;
      case 0x1BA7: return 0x3FA7;
      case 0x1BB1: return 0x3FB1;
      case 0x1F81: return 0x3F81;
      case 0x1C20: return 0x3E24;
      case 0x1C81: return 0x3E81;
      case 0x1C89: return 0x3ECD;
      case 0x1C91: return 0x3ED5;
      case 0x1C99: return 0x3CD9;
      case 0x1CA1: return 0x3EA5;
      case 0x1CA9: return 0x3EA9;
      case 0x1CB1: return 0x3EB1;
      case 0x1CB9: return 0x3EFD;
      case 0x1D2A: return 0x3F2A;
      case 0x1FA7: return 0x3FA7;
      case 0x2100: return 0x2300;
      case 0x2180: return 0x2380;
      case 0x2181: return 0x2381;
      case 0x2182: return 0x2382;
      case 0x2183: return 0x3FA7;
      case 0x21C3: return 0x23C7;
      case 0x2387: return 0x3FA7;
      case 0x23C1: return 0x3FD9;
      case 0x2489: return 0x3EA9;
      case 0x24C1: return 0x3ED5;
      case 0x2789: return 0x3FA9;
      case 0x27C7: return 0x3FD7;
      case 0x27C9: return 0x3FD9;
      case 0x2880: return 0x2A80;
      case 0x2891: return 0x3EB1;
      case 0x28C1: return 0x3ECD;
      case 0x2B87: return 0x3FA7;
      case 0x2BC7: return 0x3FCF;
      case 0x2C80: return 0x3E80;
      case 0x2C89: return 0x3EA9;
      case 0x2C91: return 0x3EB1;
      case 0x2CC9: return 0x3ECD;
      case 0x2CD1: return 0x3ED5;
      case 0x2D81: return 0x3F81;
      case 0x2D82: return 0x3F82;
      case 0x2D83: return 0x3FA7;
      case 0x2D89: return 0x3FA9;
      case 0x2DD9: return 0x3FD9;
      case 0x2DDB: return 0x3FFF;
      case 0x2E80: return 0x3E80;
      case 0x2E88: return 0x3EA8;
      case 0x2F00: return 0x3F00;
      case 0x2F80: return 0x3F80;
      case 0x2F81: return 0x3F81;
      case 0x2F82: return 0x3F82;
      case 0x2F88: return 0x3FA8;
      case 0x2F89: return 0x3FA9;
      case 0x2F8A: return 0x3FAA;
      case 0x2F91: return 0x3FB1;
      case 0x2FCF: return 0x3FCF;
      case 0x2FD7: return 0x3FD7;
      case 0x2FDF: return 0x3FFF;
      case 0x3000: return 0x3E00;
      case 0x3081: return 0x3E81;
      case 0x30E1: return 0x3EFD;
      case 0x3120: return 0x3320;
      case 0x3181: return 0x3F81;
      case 0x31A1: return 0x33A1;
      case 0x31A3: return 0x3FA7;
      case 0x31C3: return 0x33C7;
      case 0x3200: return 0x3E00;
      case 0x3300: return 0x3F00;
      case 0x3322: return 0x33A2;
      case 0x3380: return 0x3F80;
      case 0x3381: return 0x3F81;
      case 0x33A7: return 0x3FA7;
      case 0x33C1: return 0x3FD9;
      case 0x33E7: return 0x3FFF;
      case 0x3481: return 0x3E81;
      case 0x34A9: return 0x3EA9;
      case 0x34E9: return 0x3EFD;
      case 0x35A1: return 0x3FB1;
      case 0x35CB: return 0x37CF;
      case 0x37A1: return 0x3FB1;
      case 0x37A9: return 0x3FA9;
      case 0x37C7: return 0x3FD7;
      case 0x3800: return 0x3E00;
      case 0x3880: return 0x3E80;
      case 0x3881: return 0x3E81;
      case 0x38A0: return 0x3EA8;
      case 0x38B1: return 0x3EB1;
      case 0x3981: return 0x3F81;
      case 0x39A1: return 0x3FA9;
      case 0x39A3: return 0x3FA7;
      case 0x39D1: return 0x3FD9;
      case 0x39D3: return 0x3BD7;
      case 0x3A80: return 0x3E80;
      case 0x3AA0: return 0x3EA8;
      case 0x3B00: return 0x3F00;
      case 0x3B20: return 0x3F28;
      case 0x3B80: return 0x3F80;
      case 0x3B81: return 0x3F81;
      case 0x3B82: return 0x3F82;
      case 0x3BA0: return 0x3FA8;
      case 0x3BA1: return 0x3FA9;
      case 0x3BA2: return 0x3FAA;
      case 0x3BA7: return 0x3FA7;
      case 0x3BB1: return 0x3FB1;
      case 0x3BC7: return 0x3FCF;
      case 0x3BD1: return 0x3FD9;
      case 0x3BF7: return 0x3FFF;
      case 0x3C00: return 0x3E00;
      case 0x3C80: return 0x3E80;
      case 0x3C81: return 0x3E81;
      case 0x3CA1: return 0x3EA5;
      case 0x3CA9: return 0x3EA9;
      case 0x3CB1: return 0x3EB1;
      case 0x3CC9: return 0x3ECD;
      case 0x3CF9: return 0x3EFD;
      case 0x3D22: return 0x3F26;
      case 0x3D81: return 0x3F81;
      case 0x3D82: return 0x3F82;
      case 0x3DA3: return 0x3FA7;
      case 0x3DA9: return 0x3FA9;
      case 0x3DD9: return 0x3FD9;
      }

    return Input;
    }



  internal void ShowInputOutputDictionary()
    {
    int HowMany = 1;
    MForm.ShowStatus( " " );
    /*
    MForm.ShowStatus( "      // All known values:" );
    foreach( KeyValuePair<uint, uint> Kvp in InputOutputDictionary )
      {
      if( AllFiveInsOutsAreKnown( Kvp.Key ))
        {
        MForm.ShowStatus( "      case 0x" + Kvp.Key.ToString( "X" ) + ": return 0x" + Kvp.Value.ToString( "X" ) + "; // " + HowMany.ToString() );
        HowMany++;
        }
      }
      */

    MForm.ShowStatus( " " );
    MForm.ShowStatus( "      // Not all known:" );

    MForm.ShowStatus( " " );
    // MForm.ShowStatus( "      // No change:" );
    foreach( KeyValuePair<uint, uint> Kvp in InputOutputDictionary )
      {
      if( !AllFiveInsOutsAreKnown( Kvp.Key ))
        {
        if( Kvp.Key == Kvp.Value )
          {
          MForm.ShowStatus( " " );
          MForm.ShowStatus( " " );
          ShowKnownValues( Kvp.Key );

          // MForm.ShowStatus( "      case 0x" + Kvp.Key.ToString( "X" ) + ": return 0x" + Kvp.Value.ToString( "X" ) + "; // " + HowMany.ToString() );
          // HowMany++;
          }
        }
      }

    MForm.ShowStatus( " " );
    MForm.ShowStatus( "      // Changed Values:" );
    foreach( KeyValuePair<uint, uint> Kvp in InputOutputDictionary )
      {
      if( !AllFiveInsOutsAreKnown( Kvp.Key ))
        {
        if( Kvp.Key != Kvp.Value )
          {
          MForm.ShowStatus( "      case 0x" + Kvp.Key.ToString( "X" ) + ": return 0x" + Kvp.Value.ToString( "X" ) + "; // " + HowMany.ToString() );
          HowMany++;
          }
        }
      }
    }




  private void TestMult2Values()
    {
    HighestCalculationRow = MultArraySize - 1;
    HighestCalculationColumn = MultArraySize - 1;
    SetAllMult2Values();
    CopyMultArrayForMult2();

    int FirstMult2Index = MultArraySize - 1;
    uint ToSet = 0;
    for( int Row = MultArraySize - 1; Row >= 0; Row-- )
      {
      ToSet = MultArray[Row].OneLine[0];

      if( !Mult2IsKnown( ToSet ))
        {
        FirstMult2Index = Row;
        break;
        }
      }

    if( FirstMult2Index == (MultArraySize - 1))
      return;

    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      ToSet = MultArray[FirstMult2Index].OneLine[Column];
      if( Mult2IsKnown( ToSet ))
        return; // It's too early for this test.

      }

    SetMult2AtRow( FirstMult2Index );

    try
    {
    for( int Count = 0; Count < 10; Count++ )
      CalculateAllValues();

    }
    catch( Exception Except )
      {
      if( Except.Message.Contains( "Missed a value for GetOutputFromInput()" ))
        {
        throw( new Exception( Except.Message ));
        }

      RestoreMultArrayForMult2FromCopy();
      ClearMult2AtRow( FirstMult2Index );
      return;
      }

    RestoreMultArrayForMult2FromCopy();
    ClearMult2AtRow( FirstMult2Index );

    try
    {
    for( int Count = 0; Count < 10; Count++ )
      CalculateAllValues();

    }
    catch( Exception Except )
      {
      if( Except.Message.Contains( "Missed a value for GetOutputFromInput()" ))
        {
        throw( new Exception( Except.Message ));
        }

      RestoreMultArrayForMult2FromCopy();
      SetMult2AtRow( FirstMult2Index );
      return;
      }

    // If it never found a conflict.
    RestoreMultArrayForMult2FromCopy();
    }



  private void SetMult1AtColumn( int Column )
    {
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      uint ToSet = MultArray[Row].OneLine[Column];
      ToSet = SetMult1( ToSet );
      MultArray[Row].OneLine[Column] = ToSet;
      }
    }


  private void ClearMult1AtColumn( int Column )
    {
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      uint ToSet = MultArray[Row].OneLine[Column];
      ToSet = ClearMult1( ToSet );
      MultArray[Row].OneLine[Column] = ToSet;
      }
    }



  private void SetMult2AtRow( int Row )
    {
    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      uint ToSet = MultArray[Row].OneLine[Column];
      ToSet = SetMult2( ToSet );
      MultArray[Row].OneLine[Column] = ToSet;
      }
    }



  private void ClearMult2AtRow( int Row )
    {
    for( int Column = 0; Column < MultArraySize; Column++ )
      {
      uint ToSet = MultArray[Row].OneLine[Column];
      ToSet = ClearMult2( ToSet );
      MultArray[Row].OneLine[Column] = ToSet;
      }
    }



  private void CopyMultArrayForMult2()
    {
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column < MultArraySize; Column++ )
        MultArrayCopyForMult2[Row].OneLine[Column] = MultArray[Row].OneLine[Column];

      }
    }



  private void RestoreMultArrayForMult2FromCopy()
    {
    for( int Row = 0; Row < MultArraySize; Row++ )
      {
      for( int Column = 0; Column < MultArraySize; Column++ )
        MultArray[Row].OneLine[Column] = MultArrayCopyForMult2[Row].OneLine[Column];

      }
    }


  }
}

