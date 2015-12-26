// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace ExampleServer
{
  class FactorDictionary
  {
  private MainForm MForm;
  private SortedDictionary<uint, ListRec> MainDictionary;
  private List<NumberRec> NumberList;
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private Integer Product;
  private Integer SolutionP;
  private Integer SolutionQ;



  internal struct ListRec
    {
    internal List<NumberRec> OnePrimeList;
    }



  internal struct NumberRec
    {
    internal Integer Y;
    internal ExponentVectorNumber X;
    }


  private FactorDictionary()
    {

    }



  internal FactorDictionary( MainForm UseForm )
    {
    MForm = UseForm;
    IntMath = new IntegerMath();
    Product = new Integer();
    SolutionP = new Integer();
    SolutionQ = new Integer();
    Quotient = new Integer();
    Remainder = new Integer();

    MainDictionary = new SortedDictionary<uint, ListRec>();
    NumberList = new List<NumberRec>();
    }




  internal void AddDelimString( string ToAdd )
    {
    // string DelimS = IntMath.ToString10( Y ) + "\t" +
    //                         ExpNumber.ToDelimString();

    string[] SplitS = ToAdd.Split( new Char[] { '\t' } );
    if( SplitS.Length < 2 )
      return;

    // MForm.ShowStatus( "Adding: " + ToAdd );
    Integer Y = new Integer();
    IntMath.SetFromString( Y, SplitS[0] );

    ExponentVectorNumber X = new ExponentVectorNumber( IntMath );
    X.SetFromDelimString( SplitS[1] );

    for( int Count = 0; ; Count++ )
      {
      VectorValueRec Rec = X.GetValueRecordAt( Count );
      if( Rec.Prime == 0 )
        break;

      // The one X number is being added as a reference in every
      // dictionary it belongs to.
      NumberRec NRec = new NumberRec();
      NRec.Y = Y;
      NRec.X = X;

      NumberList.Add( NRec );

      if( MainDictionary.ContainsKey( Rec.Prime ))
        {
        MainDictionary[Rec.Prime].OnePrimeList.Add( NRec );
        }
      else
        {
        ListRec LRec = new ListRec();
        LRec.OnePrimeList = new List<NumberRec>();
        LRec.OnePrimeList.Add( NRec );
        MainDictionary[Rec.Prime] = LRec;
        }
      }
    }


  internal void SetProduct( string ProductS )
    {
    IntMath.SetFromString( Product, ProductS );
    }



  internal bool FindFactors()
    {
    try
    {
    CountGoodVectors();
    MarkFirstSingleBadVectors();
    CleanMainDictionary();

    CountGoodVectors();

    while( true )
      {
      if( !MForm.CheckEvents())
        return false;

      if( !MarkSingleBadVectors() )
        break;

      }

    CleanMainDictionary();

    EliminateBadVectors();
    CountGoodVectors();

    EliminateBadVectors();
    CountGoodVectors();

    CheckForAllEven();

    MForm.ShowStatus( "Done marking vectors." );
    return false;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in FindFactors():\r\n" + Except.Message );
      return false;
      }
    }


  private void EliminateBadVectors()
    {
    while( true )
      {
      if( !MForm.CheckEvents())
        return;

      CountGoodVectors();
      MarkEvenOddBadVectors();
      CountGoodVectors();

      MarkSingleOddBadVectors();
      CountGoodVectors();

      if( !MarkSingleBadVectors() )
        return;

      }
    }


  private void MarkFirstSingleBadVectors()
    {
    MForm.ShowStatus( "Top of MarkFirstSingleBadVectors()." );

    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;
      if( Rec.OnePrimeList.Count == 1 )
        {
        foreach( NumberRec NRec in Rec.OnePrimeList )
          {
          VectorValueRec VRec = NRec.X.GetVectorElement( Prime );
          // This is the only number that has this prime factor.  If it's
          // odd then it can never be used.
          if( (VRec.Exponent & 1) == 1 )
            {
            NRec.X.UseThis = false;
            // MForm.ShowStatus( "Removed vector for: " + Prime.ToString());
            }
          }
        }
      }
    }



  private bool MarkSingleBadVectors()
    {
    bool MarkedSome = false;
    MForm.ShowStatus( "Top of MarkSingleBadVectors()." );

    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;

      int HowMany = 0;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( NRec.X.UseThis )
          HowMany++;

        }

      if( HowMany != 1 )
        continue;

      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( !NRec.X.UseThis )
          continue;

        VectorValueRec VRec = NRec.X.GetVectorElement( Prime );
        if( (VRec.Exponent & 1) == 1 )
          {
          MarkedSome = true;
          NRec.X.UseThis = false;
          // MForm.ShowStatus( "Removed vector for: " + Prime.ToString());
          }
        }
      }

    return MarkedSome;
    }




  private bool MarkEvenOddBadVectors()
    {
    bool MarkedSome = false;
    MForm.ShowStatus( "Top of MarkEvenOddBadVectors()." );

    int HowManyPairs = 0;
    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;

      int HowMany = 0;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( NRec.X.UseThis )
          HowMany++;

        }

      if( HowMany != 2 )
        continue;

      HowManyPairs++;
      ExponentVectorNumber First = null;
      ExponentVectorNumber Second = null;
      Integer FirstY = null;
      Integer SecondY = null;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( !NRec.X.UseThis )
          continue;

        if( First == null )
          {
          First = NRec.X;
          FirstY = NRec.Y;
          continue;
          }

        Second = NRec.X;
        SecondY = NRec.Y;
        break;
        }

      VectorValueRec VRec1 = First.GetVectorElement( Prime );
      VectorValueRec VRec2 = Second.GetVectorElement( Prime );

      if( ((VRec1.Exponent & 1) == 1) && ((VRec2.Exponent & 1) == 0) )
        {
        MarkedSome = true;
        First.UseThis = false;
        MForm.ShowStatus( "Removed odd from pair for: " + Prime.ToString());
        continue;
        }

      if( ((VRec1.Exponent & 1) == 0) && ((VRec2.Exponent & 1) == 1) )
        {
        MarkedSome = true;
        Second.UseThis = false;
        MForm.ShowStatus( "Removed (2nd) odd from pair for: " + Prime.ToString());
        continue;
        }

      if( ((VRec1.Exponent & 1) == 1) && ((VRec2.Exponent & 1) == 1) )
        {
        // If they are both odd then they have to either be added
        // together as a pair, or they have to both be eliminated.
        // 
        // Adding the vectors is the same as multiplying the numbers.
        First.Multiply( Second );
        IntMath.Multiply( FirstY, SecondY );
        Second.UseThis = false;
        // MForm.ShowStatus( "Combined a pair at: " + Prime.ToString());
        continue;
        }
      }

    MForm.ShowStatus( "There were " + HowManyPairs.ToString() + " pairs." );
    return MarkedSome;
    }



  private bool MarkSingleOddBadVectors()
    {
    bool MarkedSome = false;
    MForm.ShowStatus( "Top of MarkSingleOddBadVectors()." );

    int HowManyOddBad = 0;
    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;

      int HowMany = 0;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( !NRec.X.UseThis )
          continue;

        VectorValueRec VRec = NRec.X.GetVectorElement( Prime );
        if( (VRec.Exponent & 1) == 1 )
          HowMany++;

        }

      if( HowMany != 1 )
        continue;

      HowManyOddBad++;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( !NRec.X.UseThis )
          continue;

        VectorValueRec VRec = NRec.X.GetVectorElement( Prime );
        if( (VRec.Exponent & 1) == 1 )
          {
          NRec.X.UseThis = false;
          break;
          }
        }
      }

    MForm.ShowStatus( "There were " + HowManyOddBad.ToString() + " single odd vectors." );
    return MarkedSome;
    }



  private int CountGoodVectors()
    {
    SortedDictionary<string, uint> UniqueDictionary = new SortedDictionary<string, uint>();

    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;

      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( NRec.X.UseThis )
          {
          string Unique = IntMath.ToString10( NRec.Y );
          UniqueDictionary[Unique] = 1;
          }

        }
      }

    int HowMany = UniqueDictionary.Count;
    MForm.ShowStatus( "There are " + HowMany.ToString( "N0" ) + " good vectors." );
    return HowMany;
    }



  private void CleanMainDictionary()
    {
    SortedDictionary<uint, ListRec> TempMainDictionary = new SortedDictionary<uint, ListRec>();

    int HowManyPrimes = 0;
    foreach( KeyValuePair<uint, ListRec> KvpMain in MainDictionary )
      {
      uint Prime = KvpMain.Key;
      ListRec Rec = KvpMain.Value;

      int HowMany = 0;
      foreach( NumberRec NRec in Rec.OnePrimeList )
        {
        if( NRec.X.UseThis )
          HowMany++;

        }
      
      if( HowMany > 0 )
        {
        HowManyPrimes++;
        TempMainDictionary[KvpMain.Key] = KvpMain.Value;
        }
      }

    MainDictionary = TempMainDictionary;
    MForm.ShowStatus( "There are " + HowManyPrimes.ToString( "N0" ) + " primes." );
    }



  private void CheckForAllEven()
    {
    foreach( NumberRec NRec in NumberList )
      {
      if( !NRec.X.UseThis )
        continue;

      if( NRec.X.IsAllEven() )
        {
        MForm.ShowStatus( "All even: " + NRec.X.ToDelimString());
        // if( 
        GetFactors( NRec.Y, NRec.X );

        }
      }
    }



  private bool GetFactors( Integer Y, ExponentVectorNumber XExp )
    {
    Integer XRoot = new Integer();
    Integer X = new Integer();
    Integer XwithY = new Integer();
    Integer Gcd = new Integer();
    XExp.GetTraditionalInteger( X );
    if( !IntMath.SquareRoot( X, XRoot ))
      throw( new Exception( "Bug. X should have an exact square root." ));

    XwithY.Copy( Y );
    XwithY.Add( XRoot );
    IntMath.GreatestCommonDivisor( Product, XwithY, Gcd );
    if( !Gcd.IsOne())
      {
      if( !Gcd.IsEqual( Product ))
        {
        SolutionP.Copy( Gcd );
        IntMath.Divide( Product, SolutionP, Quotient, Remainder );
        if( !Remainder.IsZero())
          throw( new Exception( "The Remainder with SolutionP can't be zero." ));

        SolutionQ.Copy( Quotient );
        MForm.ShowStatus( "SolutionP: " + IntMath.ToString10( SolutionP ));
        MForm.ShowStatus( "SolutionQ: " + IntMath.ToString10( SolutionQ ));
        return true;
        }
      else
        {
        MForm.ShowStatus( "GCD was Product." );
        }
      }
    else
      {
      MForm.ShowStatus( "GCD was one." );
      }

    MForm.ShowStatus( "XRoot: " + IntMath.ToString10( XRoot ));
    MForm.ShowStatus( "Y: " + IntMath.ToString10( Y ));

    XwithY.Copy( Y );
    if( Y.ParamIsGreater( XRoot ))
      throw( new Exception( "This can't be right. XRoot is bigger than Y." ));

    IntMath.Subtract( Y, XRoot );
    IntMath.GreatestCommonDivisor( Product, XwithY, Gcd );
    if( !Gcd.IsOne())
      {
      if( !Gcd.IsEqual( Product ))
        {
        SolutionP.Copy( Gcd );
        IntMath.Divide( Product, SolutionP, Quotient, Remainder );
        if( !Remainder.IsZero())
          throw( new Exception( "The Remainder with SolutionP can't be zero." ));

        SolutionQ.Copy( Quotient );
        MForm.ShowStatus( "SolutionP: " + IntMath.ToString10( SolutionP ));
        MForm.ShowStatus( "SolutionQ: " + IntMath.ToString10( SolutionQ ));
        return true;
        }
      else
        {
        MForm.ShowStatus( "GCD was Product." );
        }
      }
    else
      {
      MForm.ShowStatus( "GCD was one." );
      }

    return false;
    }



  }
}

