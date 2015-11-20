// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker



namespace ExampleServer
{
  internal struct OneFactorRec
    {
    internal Integer Factor;
    internal bool IsDefinitelyAPrime;
    internal bool IsDefinitelyNotAPrime;
    }


  class FindFactors
  {
  private IntegerMath IntMath;
  private Integer Quotient;
  private Integer Remainder;
  private BackgroundWorker Worker;
  private uint[] DivisionArray;
  private uint[] QuadResArray;
  private uint QuadResArrayLast = 0;
  private uint QuadResBigBase = 0;
  private OneFactorRec[] FactorsArray;
  private int FactorsArrayLast = 0;
  private int[] SortIndexArray;
  private Integer OriginalFindFrom;
  private Integer FindFrom;
  private SortedDictionary<uint, uint> StatsDictionary;
  private int NumbersTested = 0;


  private FindFactors()
    {

    }


  internal FindFactors(  BackgroundWorker UseWorker, IntegerMath UseIntMath )
    {
    Worker = UseWorker;
    IntMath = UseIntMath;
    Quotient = new Integer();
    Remainder = new Integer();
    OriginalFindFrom = new Integer();
    FindFrom = new Integer();
    FactorsArray = new OneFactorRec[8];
    SortIndexArray = new int[8];
    StatsDictionary = new SortedDictionary<uint, uint>();
    }



  private void AddFactorRec( OneFactorRec Rec )
    {
    // if( Rec == null )
      // return false;

    FactorsArray[FactorsArrayLast] = Rec;
    SortIndexArray[FactorsArrayLast] = FactorsArrayLast;
    FactorsArrayLast++;

    if( FactorsArrayLast >= FactorsArray.Length )
      {
      try
      {
      Array.Resize( ref FactorsArray, FactorsArray.Length + 16 );
      Array.Resize( ref SortIndexArray, FactorsArray.Length );
      }
      catch( Exception Except )
        {
        throw( new Exception( "Couldn't resize the arrays for FindFactors.cs. " + Except.Message ));
        }
      }
    }



  private void ClearFactorsArray()
    {
    for( int Count = 0; Count < FactorsArrayLast; Count++ )
      FactorsArray[Count].Factor = null;

    FactorsArrayLast = 0;
    // Don't resize the array.
    }


  internal void ShowStats()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Stats NumbersTested: " + NumbersTested.ToString() );

    uint Count = 0;
    ulong Total = 0;
    uint TotalPrimes = 0;
    foreach( KeyValuePair<uint, uint> Kvp in StatsDictionary )
      {
      Count++;
      TotalPrimes += Kvp.Value; // Value is how many times it found a number.
      Total += (Kvp.Key * Kvp.Value); // The prime times how many times it found it.
      }

    if( Count > 0 )
      {
      ulong Average = Total / Count;
      Worker.ReportProgress( 0, "Number of primes found: " + TotalPrimes.ToString( "N0" ) );
      Worker.ReportProgress( 0, "Total: " + Total.ToString( "N0" ) );
      Worker.ReportProgress( 0, "Average: " + Average.ToString( "N0" ) );
      }

    foreach( KeyValuePair<uint, uint> Kvp in StatsDictionary )
      Worker.ReportProgress( 0, Kvp.Key.ToString( "N0" ) + "\t" + Kvp.Value.ToString( "N0" ) );

    }




  private void AddToStats( uint Prime )
    {
    if( StatsDictionary.ContainsKey( Prime ))
      StatsDictionary[Prime] = StatsDictionary[Prime] + 1;
    else
      StatsDictionary[Prime] = 1;

    }




  internal void FindAllFactors( Integer FindFromNotChanged )
    {
    // ShowStats(); // So far.

    OriginalFindFrom.Copy( FindFromNotChanged );
    FindFrom.Copy( FindFromNotChanged );

    NumbersTested++;
    ClearFactorsArray();
    Integer OneFactor;
    OneFactorRec Rec;

    // while( not forever )
    for( int Count = 0; Count < 1000; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      uint SmallPrime = IntMath.IsDivisibleBySmallPrime( FindFrom );
      if( SmallPrime == 0 )
        break; // No more small primes.

      // Worker.ReportProgress( 0, "Found a small prime factor: " + SmallPrime.ToString() );
      AddToStats( SmallPrime );
      OneFactor = new Integer();
      OneFactor.SetFromULong( SmallPrime );
      Rec = new OneFactorRec();
      Rec.Factor = OneFactor;
      Rec.IsDefinitelyAPrime = true;
      AddFactorRec( Rec );
      if( FindFrom.IsULong())
        {
        ulong CheckLast = FindFrom.GetAsULong();
        if( CheckLast == SmallPrime )
          {
          Worker.ReportProgress( 0, "It only had small prime factors." );
          VerifyFactors();
          return; // It had only small prime factors.
          }
        }

      IntMath.Divide( FindFrom, OneFactor, Quotient, Remainder );
      if( !Remainder.IsZero())
        throw( new Exception( "Bug in FindAllFactors. Remainder is not zero." ));

      FindFrom.Copy( Quotient );
      if( FindFrom.IsOne())
        throw( new Exception( "Bug in FindAllFactors. This was already checked for 1." ));

      }

    // Worker.ReportProgress( 0, "No more small primes." );

    if( IsFermatPrimeAdded( FindFrom ))
      {
      VerifyFactors();
      return;
      }

    // while( not forever )
    for( int Count = 0; Count < 1000; Count++ )
      {
      if( Worker.CancellationPending )
        return;

      // If FindFrom is a ulong then this will go up to the square root of
      // FindFrom and return zero if it doesn't find it there.  So it can't
      // go up to the whole value of FindFrom.
      uint SmallFactor = NumberIsDivisibleByUInt( FindFrom );
      if( SmallFactor == 0 )
        break;

      // This is necessarily a prime because it was the smallest one found.
      AddToStats( SmallFactor );

      // Worker.ReportProgress( 0, "Found a small factor: " + SmallFactor.ToString( "N0" ));
      OneFactor = new Integer();
      OneFactor.SetFromULong( SmallFactor );
      Rec = new OneFactorRec();
      Rec.Factor = OneFactor;
      Rec.IsDefinitelyAPrime = true; // The smallest factor.  It is necessarily a prime.
      AddFactorRec( Rec );

      IntMath.Divide( FindFrom, OneFactor, Quotient, Remainder );
      if( !Remainder.IsZero())
        throw( new Exception( "Bug in FindAllFactors. Remainder is not zero. Second part." ));

      if( Quotient.IsOne())
        throw( new Exception( "This can't happen here.  It can't go that high." ));

      FindFrom.Copy( Quotient );

      if( IsFermatPrimeAdded( FindFrom ))
        {
        VerifyFactors();
        return;
        }
      }

    if( IsFermatPrimeAdded( FindFrom ))
      {
      VerifyFactors();
      return;
      }

    Integer P = new Integer();
    Integer Q = new Integer();
    FindTwoFactorsWithFermat( FindFrom, P, Q );

    if( !P.IsZero())
      {
      if( IsFermatPrimeAdded( P ))
        {
        Worker.ReportProgress( 0, "P from Fermat method was probably a prime." );
        }
      else
        {
        OneFactor = new Integer();
        OneFactor.Copy( P );
        Rec = new OneFactorRec();
        Rec.Factor = OneFactor;
        Rec.IsDefinitelyNotAPrime = true; // Because Fermat returned false.
        AddFactorRec( Rec );
        }

      if( IsFermatPrimeAdded( Q ))
        {
        Worker.ReportProgress( 0, "Q from Fermat method was probably a prime." );
        }
      else
        {
        OneFactor = new Integer();
        OneFactor.Copy( Q );
        Rec = new OneFactorRec();
        Rec.Factor = OneFactor;
        Rec.IsDefinitelyNotAPrime = true; // Because Fermat returned false.
        AddFactorRec( Rec );
        }
      }
    else
      {
      // Didn't find any with Fermat.
      OneFactor = new Integer();
      OneFactor.Copy( FindFrom );
      Rec = new OneFactorRec();
      Rec.Factor = OneFactor;
      Rec.IsDefinitelyNotAPrime = true; // Because Fermat returned false.
      AddFactorRec( Rec );
      }

    Worker.ReportProgress( 0, "That's all it could find." );
    VerifyFactors();
    }



  private bool IsFermatPrimeAdded( Integer FindFrom )
    {
    if( FindFrom.IsULong())
      {
      // The biggest size that NumberIsDivisibleByUInt() will check to 
      // see if it has primes for sure.
      if( FindFrom.GetAsULong() < (223092870UL * 223092870UL))
        return false; // Factor this.

      }

    int HowManyTimes = 20; // How many primes it will be checked with.
    if( !IntMath.IsFermatPrime( FindFrom, HowManyTimes ))
      return false;

    Integer OneFactor = new Integer();
    OneFactor.Copy( FindFrom );
    OneFactorRec Rec = new OneFactorRec();
    Rec.Factor = OneFactor;
    // Neither one of these is set to true here because it's probably
    // a prime, but not definitely.
    // Rec.IsDefinitelyAPrime = false;
    // Rec.IsDefinitelyNotAPrime = false;
    AddFactorRec( Rec );
    Worker.ReportProgress( 0, "Fermat thinks this one is a prime." );
    return true; // It's a Fermat prime and it was added.
    }



  internal void ShowAllFactors()
    {
    Worker.ReportProgress( 0, " " );
    Worker.ReportProgress( 0, "Factors:" );

    for( int Count = 0; Count < FactorsArrayLast; Count++ )
      {
      Worker.ReportProgress( 0, IntMath.ToString10( FactorsArray[Count].Factor ) );
      if( FactorsArray[Count].IsDefinitelyAPrime )
        Worker.ReportProgress( 0, "Is a prime." );

      if( FactorsArray[Count].IsDefinitelyNotAPrime )
        Worker.ReportProgress( 0, "Is not a prime." );

      if( !(FactorsArray[Count].IsDefinitelyAPrime || FactorsArray[Count].IsDefinitelyNotAPrime ))
        Worker.ReportProgress( 0, "It's likely to be a prime, but it might not be." );

      }

    Worker.ReportProgress( 0, " " );
    }



  internal void VerifyFactors()
    {
    Integer TestFactors = new Integer();
    TestFactors.SetToOne();
    for( int Count = 0; Count < FactorsArrayLast; Count++ )
      IntMath.Multiply( TestFactors, FactorsArray[Count].Factor );

    if( !TestFactors.IsEqual( OriginalFindFrom ))
      throw( new Exception( "VerifyFactors didn't come out right." ));

    }



  internal void SetupDivisionArray()
    {
    // If you were going to try and find the prime factors of a number,
    // the most basic way would be to divide it by every prime up to the
    // prime that finally divides it evenly.  The problem with doing that
    // is that it takes longer to figure out if a number is a prime than
    // it does to just test a lot of numbers that are less likely to be
    // composite than other numbers.  Any table of primes like the one
    // in IntegerMath.PrimeArray would only last for a split second, then
    // you'd have to go on to some other method for bigger numbers.

    // If you pick a number at random, then statistically, half of all
    // numbers are odd, a third of all numbers are divisible by 3, a 
    // fifth of all numbers are divisible by 5, a seventh of all numbers
    // are divisible by 7, and so on.  So you can reduce the amount of
    // numbers that you test with by getting rid of those numbers that
    // are divisible by small primes.

    // The Euler Phi function shows the number of numbers that are relatively
    // prime to some other number.  So it gives you the size of the array
    // for these numbers.  It is (2 - 1)(3 - 1)(5 - 1)... and so on.
    uint Base = 2 * 3 * 5 * 7 * 11 * 13 * 17;
    uint EulerPhi = 2 * 4 * 6 * 10 * 12 * 16;
    DivisionArray = new uint[EulerPhi];

    // The first few numbers in this array are: 
    // 1, 19, 23, 29, 31, 37, 41 ... and so on.

    int Index = 0;
    for( uint Count = 0; Count < Base; Count++ )
      {
      if( (Count & 1) == 0 ) // If its an even number.
        continue;

      if( (Count % 3) == 0 ) // If its divisible by 3.
        continue;

      if( (Count % 5) == 0 )
        continue;

      if( (Count % 7) == 0 )
        continue;

      if( (Count % 11) == 0 )
        continue;

      if( (Count % 13) == 0 )
        continue;

      if( (Count % 17) == 0 )
        continue;

      DivisionArray[Index] = Count;
      Index++;
      }
    }



  internal uint NumberIsDivisibleByUInt( Integer ToCheck )
    {
    if( DivisionArray == null )
      SetupDivisionArray(); // Set it up once, when it's needed.

    uint Max = 0;
    if( ToCheck.IsULong())
      {
      ulong ForMax = ToCheck.GetAsULong();
      // It can't be bigger than the square root.
      Max = (uint)IntMath.FindULSqrRoot( ForMax );
      }

    uint Base = 2 * 3 * 5 * 7 * 11 * 13 * 17;
    uint EulerPhi = 2 * 4 * 6 * 10 * 12 * 16;
    uint Base19 = Base * 19;
    uint Base23 = Base19 * 23;

    // The first few base numbers like this:
    // 2             2
    // 3             6
    // 5            30
    // 7           210
    // 11        2,310
    // 13       30,030
    // 17      510,510
    // 19    9,699,690
    // 23  223,092,870

    // These loops count up to 223,092,870 - 1.
    for( uint Count23 = 0; Count23 < 23; Count23++ )
      {
      Worker.ReportProgress( 0, "Count23 loop: " + Count23.ToString());

      uint Base23Part = (Base19 * Count23);
      for( uint Count19 = 0; Count19 < 19; Count19++ )
        {
        uint Base19Part = Base * Count19;
        if( Worker.CancellationPending )
          return 0;

        for( int Count = 0; Count < EulerPhi; Count++ )
          {
          if( Worker.CancellationPending )
            return 0;

          uint Test = Base23Part + Base19Part + DivisionArray[Count];
          if( Test == 1 )
            continue;

          if( (Test % 19) == 0 )
            continue;

          if( (Test % 23) == 0 )
            continue;

          if( Max > 0 )
            {
            if( Test > Max )
              return 0;

            }

          if( 0 == IntMath.GetMod32( ToCheck, Test ))
            {
            Worker.ReportProgress( 0, "The number is divisible by: " + Test.ToString( "N0" ));
            return Test;
            }
          }
        }
      }

    return 0; // Didn't find a number to divide it.
    }



  private void SetupQuadResArray( Integer Product )
    {
    // I'm doing this differently from finding y^2 = x^2 - N,
    // which I think would be faster, unless it complicates it too
    // much by having to use large Integers and doing subtraction.
    // Here it's looking for when
    // P + x^2 = y^2.

    // private uint[] QuadResArraySmall;
    // private uint[] QuadResArray;
    uint SmallBase = 2 * 3 * 5 * 7 * 11 * 13; // 30,030
    //               2   3   3   4    6    7
    int SmallBaseArraySize = 2 * 3 * 3 * 4 * 6 * 7; // This is not exact.
    uint[] QuadResArraySmall = new uint[SmallBaseArraySize];
    uint QuadResArraySmallLast = 0;
    uint ProductModSmall = (uint)IntMath.GetMod32( Product, SmallBase );
    QuadResArraySmallLast = 0;
    uint ProdMod4 = (uint)Product.GetD( 0 ) & 3;
    for( ulong Count = 0; Count < SmallBase; Count++ )
      {
      /*
      // P is odd.
      // if x is even then y is odd.
      // if x is odd then y is even.
      // If x is even then x^2 is divisible by 4.
      // If y is even then y^2 is divisible by 4.
      if( (Count & 1) == 0 ) // if X is even.
        {
        // P + x^2 mod 4 = Y mod 4 = P mod 4.
        if( (Test & ProdMod4) != ProdMod4 )
          continue;

        }
      else
        {
        // X is odd, so y is even, so y is divisible by 4.
        if( (Test & 3) != 0 )
          continue;

        }
        */

      ulong Test = ProductModSmall + (Count * Count); // The Product plus a square.
      Test = Test % SmallBase;
      if( !IntegerMath.IsSmallQuadResidue( (uint)Test ))
        continue;

      // What Count was used to make a quad residue?
      QuadResArraySmall[QuadResArraySmallLast] = (uint)Count;
      QuadResArraySmallLast++;
      if( QuadResArraySmallLast >= SmallBaseArraySize )
        throw( new Exception( "Went past the small quad res array." ));

      }

    Worker.ReportProgress( 0, "Finished setting up small quad res array." );


    QuadResBigBase = SmallBase * 17 * 19 * 23; // 223,092,870
    //                                             17   19   23
    int QuadResBaseArraySize = SmallBaseArraySize * 9 * 10 * 12; // This is not exact.

    QuadResArray = new uint[QuadResBaseArraySize];

    uint ProductMod = (uint)IntMath.GetMod32( Product, QuadResBigBase );
    int MaxLength = QuadResArray.Length;

    QuadResArrayLast = 0;
    for( ulong Count23 = 0; Count23 < (17 * 19 * 23); Count23++ )
      {
      if( Worker.CancellationPending )
        return;

      ulong BasePart = Count23 * SmallBase;
      for( uint Count = 0; Count < QuadResArraySmallLast; Count++ )
        {
        ulong CountPart = BasePart + QuadResArraySmall[Count];
        ulong Test = ProductMod + (CountPart * CountPart); // The Product plus a square.
        Test = Test % QuadResBigBase;
        if( !IntegerMath.IsQuadResidue17To23( (uint)Test ))
          continue;

        // What Count was used to make a quad residue?
        QuadResArray[QuadResArrayLast] = (uint)CountPart;
        QuadResArrayLast++;
        if( QuadResArrayLast >= MaxLength )
          throw( new Exception( "Went past the quad res array." ));

        }
      }

    Worker.ReportProgress( 0, "Finished setting up main quad res array." );
    }



  internal void FindTwoFactorsWithFermat( Integer Product, Integer P, Integer Q )
    {
    Integer TestSqrt = new Integer();
    Integer TestSquared = new Integer();
    Integer SqrRoot = new Integer();

    TestSquared.Copy( Product );
    IntMath.Multiply( TestSquared, Product );
    IntMath.SquareRoot( TestSquared, SqrRoot );
    TestSqrt.Copy( SqrRoot );
    IntMath.DoSquare( TestSqrt );
    // IntMath.Multiply( TestSqrt, SqrRoot );
    if( !TestSqrt.IsEqual( TestSquared ))
      throw( new Exception( "The square test was bad." ));

    Integer SmallestFactor = new Integer();
    Integer MaxX = new Integer();
    SmallestFactor.SetFromULong( 223092870 );
    IntMath.Divide( Product, SmallestFactor, Quotient, Remainder );
    MaxX.Copy( Quotient ); // The biggets factor.
    IntMath.Subtract( MaxX, SmallestFactor );
    MaxX.ShiftRight( 1 ); // Half of that.
    ulong TestMax = 0;
    if( MaxX.IsULong())
      TestMax = MaxX.GetAsULong();

    // Some primes:
    // 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
    // 101, 103, 107

    P.SetToZero();
    Q.SetToZero();
    Integer TestX = new Integer();
    SetupQuadResArray( Product );

    // ulong BaseTo37 = QuadResBigBase * 29UL * 31UL * 37UL;
    ulong BaseTo31 = QuadResBigBase * 29UL * 31UL;
    // ulong ProdModTo37 = IntMath.GetMod64( Product, BaseTo37 );
    ulong ProdModTo31 = IntMath.GetMod64( Product, BaseTo31 );
    // for( ulong BaseCount = 0; BaseCount < (29 * 31 * 37); BaseCount++ )
    for( ulong BaseCount = 0; BaseCount < (29 * 31); BaseCount++ )
      {
      Worker.ReportProgress( 0, "Find with Fermat BaseCount: " + BaseCount.ToString() );
      if( Worker.CancellationPending )
        return;

      ulong Base = BaseCount * QuadResBigBase; // BaseCount times 223,092,870.
      for( uint Count = 0; Count < QuadResArrayLast; Count++ )
        {
        // The maximum CountPart can be is just under half the size of
        // the Product. (Like if Y - X was equal to 1, and Y + X was
        // equal to the Product.)  If it got anywhere near that big it
        // would be inefficient to try and find it this way.
        ulong CountPart = Base + QuadResArray[Count];
        // ulong Test = ProdModTo37 + (CountPart * CountPart);
        ulong Test = ProdModTo31 + (CountPart * CountPart);
        // Test = Test % BaseTo37;
        Test = Test % BaseTo31;
        if( !IntegerMath.IsQuadResidue29( Test ))
          continue;

        if( !IntegerMath.IsQuadResidue31( Test ))
          continue;

        // if( !IntegerMath.IsQuadResidue37( Test ))
          // continue;

        ulong TestBytes = (CountPart & 0xFFFFF);
        TestBytes *= (CountPart & 0xFFFFF);
        ulong ProdBytes = Product.GetD( 1 );
        ProdBytes <<= 8;
        ProdBytes |= Product.GetD( 0 );

        uint FirstBytes = (uint)(TestBytes + ProdBytes);
        if( !IntegerMath.FirstBytesAreQuadRes( FirstBytes ))
          {
          // Worker.ReportProgress( 0, "First bytes aren't quad res." );
          continue;
          }

        if( TestMax != 0 )
          {
          if( CountPart > TestMax );
            {
            Worker.ReportProgress( 0, "Went past MaxX for Fermat." );
            return;
            }
          }

        TestX.SetFromULong( CountPart );
        IntMath.MultiplyULong( TestX, CountPart );
        TestX.Add( Product );

        if( IntMath.SquareRoot( TestX, SqrRoot ))
          {
          Worker.ReportProgress( 0, " " );
          if( (CountPart & 1) == 0 )
            Worker.ReportProgress( 0, "CountPart was even." );
          else
            Worker.ReportProgress( 0, "CountPart was odd." );

          // Found an exact square root.
          // P + (CountPart * CountPart) = Y*Y
          // P = (Y + CountPart)Y - CountPart)

          P.Copy( SqrRoot );
          P.AddULong( CountPart );

          Q.Copy( SqrRoot );
          Integer ForSub = new Integer();
          ForSub.SetFromULong( CountPart );
          IntMath.Subtract( Q, ForSub );

          Worker.ReportProgress( 0, "Found P: " + IntMath.ToString10( P ) );
          Worker.ReportProgress( 0, "Found Q: " + IntMath.ToString10( Q ) );
          Worker.ReportProgress( 0, " " );
          throw( new Exception( "Testing this." ));
          // return; // With P and Q.
          }
        // else
          // Worker.ReportProgress( 0, "It was not an exact square root." );

        }
      }

    // P and Q would still be zero if it never found them.
    }



  }
}

