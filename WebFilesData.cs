// Programming by Eric Chauvin.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace ExampleServer
{
  class WebFilesData
  {
  private MainForm MForm;
  private SortedDictionary<string, WebFilesRec> WebFilesDictionary;


  public struct WebFilesRec
    {
    // public string FullFileName; // The whole path and all.
    public byte[] FileBuffer;
    }



  private WebFilesData()
    {
    }



  internal WebFilesData( MainForm UseForm )
    {
    MForm = UseForm;
    WebFilesDictionary = new SortedDictionary<string, WebFilesRec>();
    }



  internal bool ContainsFile( string KeyWord )
    {
    KeyWord = KeyWord.ToLower().Trim();
    return WebFilesDictionary.ContainsKey( KeyWord );
    }




  internal WebFilesRec GetRecord( string KeyWord )
    {
    KeyWord = KeyWord.ToLower().Trim();

    WebFilesRec Value = new WebFilesRec();
    if( WebFilesDictionary.TryGetValue( KeyWord, out Value ))
      {
      return Value;
      }
    else
      {
      Value.FileBuffer = null;
      return Value;
      }
    }



  internal byte[] GetBuffer( string KeyWord )
    {
    KeyWord = KeyWord.ToLower().Trim();

    WebFilesRec Value;
    if( WebFilesDictionary.TryGetValue( KeyWord, out Value ))
      {
      return Value.FileBuffer;
      }
    else
      {
      return null;
      }
    }



  internal bool UpdateRecordBuffer( string KeyWord, byte[] Buffer )
    {
    if( KeyWord == null )
      return false;

    KeyWord = KeyWord.Trim().ToLower();
    if( KeyWord.Length < 4 )
      return false;

    WebFilesRec Rec = new WebFilesRec();
    Rec.FileBuffer = Buffer;
    SetRecord( KeyWord, Rec );
    return WriteBufferToFile( KeyWord );
    }



  internal void SetRecord( string KeyWord, WebFilesRec Rec )
    {
    if( KeyWord == null )
      return;

    KeyWord = KeyWord.ToLower().Trim();
    if( KeyWord.Length < 4 )
      return;

    WebFilesDictionary[KeyWord] = Rec;
    }



  private bool ReadFileToBuffer( string KeyName, string FileName )
    {
    WebFilesRec Rec = GetRecord( KeyName );
    // Rec.FullFileName = FileName;

    // if( !File.Exists( Rec.FullFileName ))
    if( !File.Exists( FileName ))
      {
      MForm.ShowWebListenerFormStatus( "The file does not exist:" );
      MForm.ShowWebListenerFormStatus( FileName );
      return false;
      }

    try
    {
    FileStream FStream = File.OpenRead( FileName );
    // try catch that surrounds this.
    Rec.FileBuffer = new byte[FStream.Length];
    FStream.Read( Rec.FileBuffer, 0, Rec.FileBuffer.Length );
    FStream.Close();

    SetRecord( KeyName, Rec );
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Error in ReadFileToBuffer:" );
      MForm.ShowWebListenerFormStatus( Except.Message );
      return false;
      }
    }



  private bool WriteBufferToFile( string KeyName )
    {
    try
    {
    WebFilesRec Rec = GetRecord( KeyName );
    // Rec.FullFileName = FileName;
    // if( !File.Exists( FileName ))
    string FullFileName = MForm.GetWebPagesDirectory() + KeyName;
    File.Delete( FullFileName );
    FileStream FStream = File.OpenWrite( FullFileName );
    FStream.Write( Rec.FileBuffer, 0, Rec.FileBuffer.Length );
    FStream.Close();
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Error in WriteFileToBuffer:" );
      MForm.ShowWebListenerFormStatus( Except.Message );
      MForm.ShowWebListenerFormStatus( "KeyName: " + KeyName );
      return false;
      }
    }




  internal void SearchWebPagesDirectory()
    {
    try
    {
    WebFilesDictionary.Clear();

    string[] FileEntries = Directory.GetFiles( MForm.GetWebPagesDirectory(), "*.*" );

    MForm.ShowWebListenerFormStatus( " " );
    MForm.ShowWebListenerFormStatus( " " );
    MForm.ShowWebListenerFormStatus( "Web Files:" );
    foreach( string FileName in FileEntries )
      {
      string ShortName = FileName.Replace( MForm.GetWebPagesDirectory(), "" );
      ShortName = ShortName.ToLower();

      ReadFileToBuffer( ShortName, FileName );
      if( !MForm.CheckEvents())
        return;

      MForm.ShowWebListenerFormStatus( "Read buffer for " + ShortName );
      }

    MForm.ShowWebListenerFormStatus( " " );
    MForm.ShowWebListenerFormStatus( " " );

    /*
    string [] SubDirEntries = Directory.GetDirectories( Name );
    foreach( string SubDir in SubDirEntries )
      {
      ProcessOneDirectory( SubDir );
      }
      */
    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Exception in SearchWebPagesDirectory():" );
      MForm.ShowWebListenerFormStatus( Except.Message );
      }
    }



  }
}

