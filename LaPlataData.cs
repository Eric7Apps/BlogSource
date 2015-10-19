// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://ericsoftwarenotes.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;



namespace ExampleServer
{

  class LaPlataData
  {
  private MainForm MForm;
  private LaPlataRecord[] LaPlataRecArray;
  private int LaPlataRecArrayLast = 0;
  private int[] SortIndexArray;


  private LaPlataData()
    {

    }


  internal LaPlataData( MainForm UseForm )
    {
    MForm = UseForm;

    LaPlataRecArray = new LaPlataRecord[8]; // It will resize it.
    SortIndexArray = new int[8];
    }



  internal bool AddLaPlataRec( LaPlataRecord Rec )
    {
    if( Rec == null )
      return false;

    LaPlataRecArray[LaPlataRecArrayLast] = Rec;
    SortIndexArray[LaPlataRecArrayLast] = LaPlataRecArrayLast;
    LaPlataRecArrayLast++;

    if( LaPlataRecArrayLast >= LaPlataRecArray.Length )
      {
      try
      {
      Array.Resize( ref LaPlataRecArray, LaPlataRecArray.Length + (1024 * 4));
      Array.Resize( ref SortIndexArray, LaPlataRecArray.Length );
      }
      catch( Exception Except )
        {
        MForm.ShowStatus( "Error: Couldn't resize the arrays." );
        MForm.ShowStatus( Except.Message );
        return false;
        }
      }

    return true;
    }



  internal bool ReadFromFile()
    {
    string FileName = MForm.GetDataDirectory() + "Parcels.csv";
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
          
        // Keep the tabs here.
        // Line = Line.Trim();
        if( Line.Length < 3 )
          continue;

        if( Line.StartsWith( "APN,C,12\tOWNER,C,50\t" ))
          continue;

        if( !Line.Contains( "\t" ))
          continue;

        LaPlataRecord Rec = new LaPlataRecord( MForm );
        if( !Rec.StringToObject( Line ))
          continue;

        if( !AddLaPlataRec( Rec ))
          break; // Out of RAM.

        HowMany++;
        }

      MForm.ShowStatus( " " );
      MForm.ShowStatus( "Records: " + HowMany.ToString( "N0" ));
      }

    // Don't sort the whole thing.  At least not with a bubble sort.
    // Sort a small subset of results.
    // SortByName();
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not read the La Plata data file." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }


  // You would do a sort on the results of a search, or on an index structure,
  // not the entire set of data.  But if you wanted to sort the entire set of data
  // you should use a Library Sort, not a bubble sort.  A bubble sort on this
  // much data takes a long time.
  internal void SortByName()
    {
    MForm.ShowStatus( "Sorting data..." );

    while( true )
      {
      if( !MForm.CheckEvents())
        return;

      bool Swapped = false;
      for( int Count = 0; Count < (LaPlataRecArrayLast - 1); Count++ )
        {
        if( 0 < String.Compare( LaPlataRecArray[SortIndexArray[Count]].NAME,
                                LaPlataRecArray[SortIndexArray[Count + 1]].NAME,
                                true )) // True is ignore case.
          {
          int TempIndex = SortIndexArray[Count];
          SortIndexArray[Count] = SortIndexArray[Count + 1];
          SortIndexArray[Count + 1] = TempIndex;
          Swapped = true;
          }
        }
      
      if( !Swapped )
        break;
        
      }

    MForm.ShowStatus( "Finished sorting." );
    }



  internal byte[] GetHTML( string FindName )
    {
    FindName = FindName.ToUpper();

    StringBuilder SBuilder = new StringBuilder();
    SBuilder.Append( "<!DOCTYPE html>\r\n" );
    SBuilder.Append( "<html>\r\n" );
    SBuilder.Append( "<head>\r\n" );
    SBuilder.Append( "<meta charset=\"UTF-8\">\r\n" );
    SBuilder.Append( "<title>La Plata County Parcels</title>\r\n" );
    SBuilder.Append( "</head>\r\n" );
    SBuilder.Append( "<body>\r\n" );
    SBuilder.Append( "<p><h1>La Plata Data</h1></p>\r\n" );
    SBuilder.Append( "<ul>\r\n" );

    int HowMany = 0;
    for( int Count = 0; Count < LaPlataRecArrayLast; Count++ )
      {
      if( !LaPlataRecArray[SortIndexArray[Count]].NAME.StartsWith( FindName ))
        continue;

      string ShowName = LaPlataRecArray[SortIndexArray[Count]].NAME;
      // Show the address or whatever too...

      SBuilder.Append( "<li>" + ShowName + "</li>\r\n" );
      HowMany++;
      }

    SBuilder.Append( "</ul>\r\n" );
    SBuilder.Append( "<p>Records found: " + HowMany.ToString( "N0" ) + "</p>\r\n" );
    SBuilder.Append( "</body>\r\n" );
    SBuilder.Append( "</html> \r\n" );

    return UTF8Strings.StringToBytes( SBuilder.ToString() );
    }



  }
}
