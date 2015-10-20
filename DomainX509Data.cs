// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

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


  private DomainX509Data()
    {

    }


  internal DomainX509Data( MainForm UseForm )
    {
    MForm = UseForm;

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
      return Rec.DomainName;
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
    string FileName = MForm.GetDataDirectory() + "Top1MillionDomains.txt";
    // ECTime RecTime = new ECTime();

    try
    {
    string Line;
    using( StreamReader SReader = new StreamReader( FileName  )) 
      {
      int HowMany = 0;
      while( SReader.Peek() >= 0 ) 
        {
        Line = SReader.ReadLine();
        if( Line == null )
          continue;
          
        // Line = Line.Trim();
        if( Line.Length < 3 )
          continue;

        if( !Line.Contains( "," ))
          continue;

        DomainX509Record Rec = new DomainX509Record( MForm );
        if( !Rec.ImportOriginalStringToObject( Line ))
          continue;

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



  }
}



