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

    OneFactor = new Integer();
    OneFactor.Copy( FindFrom );
    Rec = new OneFactorRec();
    Rec.Factor = OneFactor;
    Rec.IsDefinitelyNotAPrime = true; // Because Fermat returned false.
    AddFactorRec( Rec );
    // Worker.ReportProgress( 0, "That's all it could find." );
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


  }
}

