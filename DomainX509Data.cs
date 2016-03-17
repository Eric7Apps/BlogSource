// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace ExampleServer
{
  class DomainX509Data
  {
  private MainForm MForm;
  private DomainX509Record[] DomainX509RecArray;
  private int DomainX509RecArrayLast = 0;
  private string FileName = "";


  private DomainX509Data()
    {

    }


  internal DomainX509Data( MainForm UseForm, string FileToUseName )
    {
    MForm = UseForm;
    FileName = FileToUseName;
    DomainX509RecArray = new DomainX509Record[8]; // It will resize it.
    }



  internal bool AddDomainX509Rec( DomainX509Record Rec )
    {
    if( Rec == null )
      return false;

    DomainX509RecArray[DomainX509RecArrayLast] = Rec;
    DomainX509RecArrayLast++;

    if( DomainX509RecArrayLast >= DomainX509RecArray.Length )
      {
      try
      {
      Array.Resize( ref DomainX509RecArray, DomainX509RecArray.Length + (1024 * 4));
      }
      catch( Exception Except )
        {
        MForm.ShowStatus( "Error: Couldn't resize the arrays for X.509 data." );
        MForm.ShowStatus( Except.Message );
        return false;
        }
      }

    return true;
    }



  internal bool UpdateDomainX509Rec( DomainX509Record Rec )
    {
    if( Rec == null )
      return false;

     if( Rec.DomainName.Length < 2 )
       {
       MForm.ShowStatus( "Trying to update a DomainX509 record with no domain name." );
       return false;
       }

    // ToDo: Use a dictionary to index this and find it quickly.
    for( int Count = 0; Count < DomainX509RecArrayLast; Count++ )
      {
      if( Rec.DomainName == DomainX509RecArray[Count].DomainName )
        {
        Rec.SetModifyTimeToNow();
        DomainX509RecArray[Count].Copy( Rec );
        return true;
        }
      }

    // Didn't find a match so add it.
    return AddDomainX509Rec( Rec );
    }




  internal string GetRandomDomainName()
    {
    try
    {
    ECTime OldDate = new ECTime();
    OldDate.SetToNow();
    OldDate.AddMinutes( -(60 * 24 * 90)); // Go back 90 days.
    ulong OldDateIndex = OldDate.GetIndex();

    // A limited while( true ) that won't go forever.
    for( int Count = 0; Count < 10000; Count++ )
      {
      int Index = MForm.GetRandomNumber();
      Index = Index % DomainX509RecArrayLast;
      DomainX509Record Rec = DomainX509RecArray[Index];
      if( Rec.GetModifyTimeIndex() > OldDateIndex )
        continue; // Don't get a recently used one.

      // if( anything else )
        // continue;

      // return "127.0.0.1"; // For testing with local loopback.
      // return "promocodeclub.com"; // Good for testing X.509.
      return "secure.ballantinecommunications.net";
      // return "schneier.com"; //  Bruce Schneier, the cryptographer.
      // return "vantiv.com";


      // return Rec.DomainName;
      }

    return ""; // It shouldn't get here.

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in GetRandomDomainName():" );
      MForm.ShowStatus( Except.Message );
      return "";
      }
    }


  internal bool ImportFromOriginalListFile()
    {
    // string FileName = MForm.GetDataDirectory() + "Top1MillionDomains.txt";
    string FileName = MForm.GetDataDirectory() + "Top10KDomains.txt";
    // ECTime RecTime = new ECTime();

    try
    {
    string Line;
    using( StreamReader SReader = new StreamReader( FileName  )) 
      {
      int HowMany = 0;
      for( int Count = 0; Count < 1000000; Count++ )
        {
        /*
        if( (Count & 0xFF) == 1 )
          {
          MForm.ShowStatus( "Count is: " + Count.ToString( "N0" ));
          if( !MForm.CheckEvents())
            return false;

          }
          */

        if( SReader.Peek() < 0 ) 
          break;

        Line = SReader.ReadLine();
        if( Line == null )
          break;
          
        Line = Line.Trim();
        if( Line.Length < 3 )
          break;

        if( !Line.Contains( "," ))
          continue;

        DomainX509Record Rec = new DomainX509Record();
        if( !Rec.ImportOriginalStringToObject( Line ))
          {
          MForm.ShowStatus( "Got false for: " + Line );
          continue;
          }

        if( !Rec.DomainName.EndsWith( ".com" ))
          continue;

        if( !AddDomainX509Rec( Rec ))
          break; // Out of RAM.

        HowMany++;
        }

      MForm.ShowStatus( " " );
      MForm.ShowStatus( "Records: " + HowMany.ToString( "N0" ));
      }

    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not import the X.509 data file." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  internal bool WriteToTextFile()
    {
    try
    {
    using( StreamWriter SWriter = new StreamWriter( FileName  )) 
      {
      for( int Count = 0; Count < DomainX509RecArrayLast; Count++ )
        {
        DomainX509Record Rec = DomainX509RecArray[Count];
        string Line = Rec.ObjectToString();
        if( Line.Length > 3 )
          SWriter.WriteLine( Line );

        }

      SWriter.WriteLine( " " );
      }

    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not write to the file in DomainX509Data.WriteToTextFile()." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  internal bool ReadFromTextFile()
    {
    try
    {
    string Line;
    using( StreamReader SReader = new StreamReader( FileName  )) 
      {
      int HowMany = 0;
      for( int Count = 0; Count < 1000000; Count++ )
        {
        /*
        if( (Count & 0xFF) == 1 )
          {
          MForm.ShowStatus( "Count is: " + Count.ToString( "N0" ));
          if( !MForm.CheckEvents())
            return false;

          }
          */

        if( SReader.Peek() < 0 ) 
          break;

        Line = SReader.ReadLine();
        if( Line == null )
          break;

        if( !Line.Contains( "\t" ))
          continue;

        DomainX509Record Rec = new DomainX509Record();
        if( !Rec.StringToObject( Line ))
          {
          // MForm.ShowStatus( "Got false for: " + Line );
          continue;
          }

        if( !AddDomainX509Rec( Rec ))
          break; // Out of RAM.

        HowMany++;
        }

      MForm.ShowStatus( " " );
      MForm.ShowStatus( "Records: " + HowMany.ToString( "N0" ));
      }

    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not read the X.509 data file." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  }
}


