// Programming by Eric Chauvin.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;



namespace ExampleServer
{
  class ConfigureFile
  {
  private MainForm MForm;
  private Dictionary<string, string> CDictionary;
  private string FileName;
  private AESEncryption AESEncrypt;


  internal ConfigureFile( MainForm UseForm, string FileToUseName )
    {
    MForm = UseForm;
    FileName = FileToUseName;
    AESEncrypt = new AESEncryption();
    string ExampleKey = "Where does this key come from on a production server" +
      " that is running on an Amazon EC2 instance (for example) where you don't" +
      " have access to the physical hardware?";

    AESEncrypt.SetKey( ExampleKey );

    CDictionary = new Dictionary<string, string>();
    ReadFromTextFile();
    }
    



  internal string GetString( string KeyWord )
    {
    KeyWord = KeyWord.ToLower().Trim();
    
    string Value;
    if( CDictionary.TryGetValue( KeyWord, out Value ))
      return Value;
    else
      return "";

    }




  internal void SetString( string KeyWord, string Value )
    {
    KeyWord = KeyWord.ToLower().Trim();
    if( KeyWord == "" )
      {
      MForm.ShowStatus( "Can't add an empty keyword to the dictionary in ConfigureFile.cs." );
      return;
      }

    CDictionary[KeyWord] = Value;
    }




  private bool ReadFromTextFile()
    {
    // AESEncryption AESEncrypt = null;

    char TabChar = (char)9;
    
    CDictionary.Clear();
    
    if( !File.Exists( FileName ))
      return false;
      
    try
    {
    string Line;
    using( StreamReader SReader = new StreamReader( FileName  )) 
      {
      while( SReader.Peek() >= 0 ) 
        {
        Line = SReader.ReadLine();
        if( Line == null )
          continue;
          
        Line = Line.Trim();
        if( Line == "" )
          continue;
          
        if( !Line.Contains( "\t" ))
          Line = AESEncrypt.DecryptString( Line );

        if( !Line.Contains( "\t" ))
          continue;
          
        string[] SplitString = Line.Split( new Char[] { TabChar } );
        if( SplitString.Length < 2 )
          continue;

        string KeyWord = SplitString[0].Trim();
        string Value = SplitString[1].Trim();

        KeyWord = KeyWord.Replace( "\"", "" );
        Value = Value.Replace( "\"", "" );

        if( KeyWord == "" )
          continue;

        CDictionary[KeyWord] = Value;
        // try
        // CDictionary.Add( KeyWord, Value );
        }
      }

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not read the file: \r\n" + FileName );
      MForm.ShowStatus( Except.Message );
      return false;
      }

    return true;
    }
    
    



  internal bool WriteToTextFile()
    {
    try
    {
    using( StreamWriter SWriter = new StreamWriter( FileName  )) 
      {
      foreach( KeyValuePair<string, string> Kvp in CDictionary )
        {
        string Line = Kvp.Key + "\t" + Kvp.Value;
        Line = AESEncrypt.EncryptString( Line );
        SWriter.WriteLine( Line );
        }

      SWriter.WriteLine( " " );
      }
    
    
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not write the configuration data to the file." );
      MForm.ShowStatus( Except.Message );
      return false;
      }

    return true;
    }





  }
}




