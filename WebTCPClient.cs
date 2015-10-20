// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;



namespace ExampleServer
{
  class WebTCPClient
  {
  private MainForm MForm;
  private NetworkStream NetStream;
  private TcpClient Client;
  private string InBufferS = "";
  private string AllInputS = "";
  private ECTime LastTransactTime;
  private Dictionary<string, string> LinesDictionary;
  private IAsyncResult AsyncResult = null;
  private const int MaximumLineLength = 1024 * 1024;
  private bool IsAWebRequest = false;
  private bool IsHeadOnly = false;
  private string RemoteAddress = "";
  // private ECTime StartTime;
  private bool ProcessingStarted = false;


  private WebTCPClient()
    {
    }




  public WebTCPClient( MainForm UseForm, TcpClient UseClient, string UseAddress )
    {
    MForm = UseForm;
    RemoteAddress = UseAddress;

    LastTransactTime = new ECTime();
    LastTransactTime.SetToNow();
    LinesDictionary = new Dictionary<string, string>();
    
    if( UseClient == null )
      Client = new TcpClient();
    else
      Client = UseClient;

    try
    {
    NetStream = Client.GetStream();
    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Exception in Creating the NetStream:" );
      MForm.ShowWebListenerFormStatus( Except.Message );
      NetStream = null;
      return;
      }

    Client.ReceiveTimeout = 3 * 1000;
    Client.SendTimeout = 3 * 1000;
    Client.SendBufferSize = 1024 * 32;
    }




  internal void FreeEverything()
    {
    // InBufferS = "";

    /*
    if( LinesDictionary != null )
      {
      LinesDictionary.Clear();
      }
      */

    if( NetStream != null )
      {
      NetStream.Close();
      NetStream = null;
      }

    if( Client != null )
      {
      Client.Close();
      Client = null;
      }
    }



  internal bool GetIsHeadOnly()
    {
    return IsHeadOnly;
    }



  internal ulong GetLastTransactTimeIndex()
    {
    return LastTransactTime.GetIndex();
    }



  internal double GetLastTransactSecondsToNow()
    {
    return LastTransactTime.GetSecondsToNow();
    }



  internal bool IsProcessingInBackground()
    {
    if( Client == null )
      return false;

    // AsyncResult doesn't get created unless an Async transfer gets started.
    if( AsyncResult == null )
      return false;

    // This is checked whenever it checks to see if it is shut down, which is very often.
    if( AsyncResult.IsCompleted )
      {
      AsyncResult = null;
      // Then give it time before it gets shut down.
      LastTransactTime.SetToNow();
      return false;
      }

    return true;
    }




  internal bool IsShutDown()
    {
    if( Client == null )
      return true;

    if( IsProcessingInBackground())
      return false;

    // This only knows if it's connected as of the last socket operation.
    if( !Client.Connected )
      {
      FreeEverything();
      return true;
      }

    return false;
    }



  private int GetAvailable()
    {
    if( IsShutDown())
      return 0;

    try
    {
    return Client.Available;
    }
    catch( Exception )
      {
      FreeEverything();
      return 0;
      }
    }




  private bool DataIsAvailable()
    {
    if( NetStream == null )
      return false;
    
    try
    {
    if( NetStream.DataAvailable )
      return true;

    return false;

    }
    catch
      {
      return false;
      }
    }




  private bool ReadToInBuffer()
    {
    if( IsShutDown())
      return false;

    try
    {
    if( 0 == GetAvailable())
      return true; // No error.

    if( NetStream == null )
      return false; // NetStream = Client.GetStream();
      
    if( !DataIsAvailable() )
      return true;

    // 64K is way more than most protocols are needing when 
    // they send something.  Also, this doesn't get allocated
    // unless data is available to read.
    byte[] RawData = new byte[1024 * 64];

    int BytesRead = NetStream.Read( RawData, 0, RawData.Length );
    if( BytesRead == 0 )
      return true; // break;

    // It's possible that this RawData can end in the middle of a UTF8
    // sequence so that it loses a character.  But only if it's sending
    // non-ASCII characters directly here.
    // It's only sending strings.
    string TempS = UTF8Strings.BytesToString( RawData, BytesRead );

    // Notice that just about _anything_ that a hacker sends is added here.
    // The only thing that gets rejected from the raw data here is what ever
    // the UTF8Strings.BytesToString() function doesn't like.
    InBufferS += TempS;
    AllInputS += TempS;

    LastTransactTime.SetToNow();
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Exception in ReadToInBuffer():" );
      MForm.ShowWebListenerFormStatus( Except.Message );
      FreeEverything();
      return false;
      }
    }



  internal string GetAllInputS()
    {
    return AllInputS;
    }



  private int InBufferLineEndPosition()
    {
    if( InBufferS.Length < 2 )
      return -1;

    return InBufferS.IndexOf( "\r\n" );
    }



  internal bool AddNextStringFromInBuffer()
    {
    int Where = InBufferLineEndPosition();
    if( Where < 0 )
      {
      if( InBufferS.Length > MaximumLineLength )
        {
        MForm.ShowWebListenerFormStatus( "In WebTCPClient. Line is too long for the protocol." );
        FreeEverything(); // Close down the connection.
        }

      return false; // No more lines to read.
      }

    if( Where > MaximumLineLength )
      {
      MForm.ShowWebListenerFormStatus( "In WebTCPClient. Line is too long for the protocol." );
      FreeEverything(); // Close down the connection.
      return false; // No more lines to read.
      }

    if( Where == 0 )
      {
      // Marker string for end of header.
      InBufferS = InBufferS.Remove( 0, 2 );
      return true;
      }

    string Line = InBufferS;

    Line = Line.Remove( Where ); // Remove from Where to the end.

    InBufferS = InBufferS.Remove( 0, Where + 2 );
    string[] SplitS = Line.Split( new Char[] { ':' } );
    if( SplitS.Length < 2 )
      {
      return true; // Maybe more lines to read, but this line is not useful.
      }

    string KeyWord = SplitS[0].ToLower().Trim();
    string Value = SplitS[1].Trim();
    if( KeyWord.Length < 2 )
      {
      MForm.ShowWebListenerFormStatus( "In AddNextStringFromInBuffer(). KeyWord.Length < 2." );
      return false; // No more lines to read.
      }

    if( KeyWord.Length > 30 )
      {
      MForm.ShowWebListenerFormStatus( "In AddNextStringFromInBuffer(). KeyWord is too long for protocol." );
      return false; // No more lines to read.
      }

    if( Value.Length > MaximumLineLength )
      {
      MForm.ShowWebListenerFormStatus( "In AddNextStringFromInBuffer(). Value is too long for protocol." );
      MForm.ShowWebListenerFormStatus( "Header key is: " + KeyWord );
      FreeEverything(); // Close down the connection.
      return false; // No more lines to read.
      }

    // Value is a string that's not more than 1024 bytes minus the key
    // word size.
    LinesDictionary[KeyWord] = Value;
    return true;
    }



  internal bool FillIncomingLines()
    {
    if( !ReadToInBuffer())
      return false; // It's shut down or had an error that shut it down.

    for( int Count = 0; Count < 100; Count++ )
      {
      if( !AddNextStringFromInBuffer())
        break;

      }

    return true;
    }




  internal string GetLineByName( string Name )
    {
    Name = Name.ToLower().Trim();
    if( !LinesDictionary.ContainsKey( Name ))
      return "";

    return LinesDictionary[Name];
    }




  internal int GetLinesCount()
    {
    return LinesDictionary.Count;
    }



  internal string GetReceivedLines()
    {
    string Result = "";
    foreach( KeyValuePair<string, string> Kvp in LinesDictionary )
      Result += Kvp.Key + ": " + Kvp.Value + "\r\n";

    Result += InBufferS;
    // This is just for ShowStatus().  Cut it off at 300 characters just
    // to show the first part.
    // if( Result.Length > 300 )
      // Result = Result.Remove( 300 );

    return Result;
    }
  



  internal bool WriteBytesAsync( byte[] Bytes )
    {
    if( IsShutDown())
      return false;

    // This can only be called once.
    if( AsyncResult != null )
      {
      MForm.ShowWebListenerFormStatus( "AsyncResult != null in WriteBytesAsync." );
      return false;
      }

    if( NetStream == null )
      return false;

    // This just means it's a writeable stream.  It's not a test 
    // for the write buffer being ready to write.
    // if( NetStream.CanWrite )

    try
    {
    AsyncResult = NetStream.BeginWrite( Bytes, 
                  0, 
                  Bytes.Length,
                  new AsyncCallback( WebTCPClient.ProcessAsynchCallback ),
                  NetStream );

    LastTransactTime.SetToNow();
    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowWebListenerFormStatus( "Exception in WriteBytesAsync." );
      MForm.ShowWebListenerFormStatus( Except.Message );
      FreeEverything();
      return false;
      }
    }




  static void ProcessAsynchCallback( IAsyncResult Result )
    {
    // The Async send is done only once, then it closes the socket.
    // But Result.AsyncState is the NetworkStream passed to BeginWrite.

    try
    {
    NetworkStream TheStream = (NetworkStream)(Result.AsyncState);
    // Did it send all the bytes?
    TheStream.EndWrite( Result );
    }
    catch( Exception )
      {
      //  "The socket got closed, or it couldn't write to it, etc.\r\n";
      }
    }



  internal bool IsBrowserRequestReady()
    {
    if( AllInputS.Contains( "\r\n\r\n" ))
      return true;

    return false;
    }



  internal bool IsBrowserRequest()
    {
    // Does it look like a reasonable HTTP request is there?
    // "GET /Test.htm HTTP/1.1\r\n" +

    if( AllInputS.StartsWith( "POST /" ))
      {
      if( AllInputS.Contains( "HTTP" ))
        return true;

      }

    if( AllInputS.StartsWith( "HEAD /" ))
      {
      if( AllInputS.Contains( "HTTP" ))
        return true;

      }

    if( !AllInputS.StartsWith( "GET " ))
      return false;

    if( !AllInputS.Contains( "HTTP/" ))
      return false;

    return true;
    }



  internal bool GetIsAWebRequest()
    {
    return IsAWebRequest;
    }



  internal string GetHTTPFileRequested()
    {
    // If it gets as far as calling this then it's a web request.
    IsAWebRequest = true;

    if( GetRequestHasHackingStuff( AllInputS ))
      return "Hacking: " + Utility.CleanAsciiString( AllInputS, 1024 );

    string[] SplitS = AllInputS.Split( new Char[] { '\r' } );
    if( SplitS.Length < 2 )
      return "Bad HTTP: Only one line."; // The protocol requires the Host: line to be sent at least.

    // Only using ASCII in file names.
    string FirstLine = Utility.CleanAsciiString( SplitS[0], 1024 );
    // Don't use spaces in file names.
    string[] SplitLine = FirstLine.Split( new Char[] { ' ' } );
    if( SplitLine.Length < 3 )
      return "Bad HTTP: Get line < 3 parts.";

    if( SplitLine[0].Trim() == "POST" )
      return "Bad HTTP: Post."; // This example server doesn't do anything with a POST message.

    if( SplitLine[0].Trim() == "HEAD" )
      IsHeadOnly = true;
    else
      IsHeadOnly = false;

    if( !((SplitLine[0].Trim() == "GET") || (SplitLine[0].Trim() == "HEAD")))
      return "Bad HTTP: No GET or HEAD in first part.";

    if( !SplitLine[2].Contains( "HTTP" ) )
      return "Bad HTTP: No HTTP in third part.";

    // Make it case insensitive.
    string FileToGet = SplitLine[1].Trim(); // Not .ToLower() yet,

    // This is used for fishing for web servers, so be careful who this goes out to.
    // Also, it can be used in a denial of service attack.
    if( FileToGet == "/" )
      FileToGet = "index.htm";

    if( FileToGet.Length < 7 ) // abc.htm
      return "Hacking: " + FileToGet;

    if( FileToGet.Length > 1024 )
      return "Hacking: " + FileToGet;

    if( FileRequestedHasHackingStuff( FileToGet ))
      return "Hacking: " + FileToGet;

    return FileToGet;
    }



  internal bool GetRequestHasHackingStuff( string Test )
    {
    // Just do the main obvious ones here.

    // Some obvious things that show they're testing to see what kind of 
    // server it is and what vulnerabilites it might have.
    if( Test.Contains( "local-bin" ))
      return true;

    if( Test.Contains( "cgi-bin" ))
      return true;

    if( Test.Contains( "cgi-sys" ))
      return true;

    if( Test.Contains( "/bin/bash" ))
      return true;

    if( Test.Contains( "/dev/tcp" ))
      return true;

    if( Test.Contains( "/dev/tcp" ))
      return true;


    return false;
    }



  internal bool FileRequestedHasHackingStuff( string Test )
    {
    Test = Test.ToLower();

    // This server doesn't do ASP.
    if( Test.EndsWith( ".asp" ))
      return true;

    if( Test.EndsWith( ".action" ))
      return true;

    if( Test.Contains( ":" )) // A normal browser won't send it like this.
      return true;

    if( Test.EndsWith( ".php" ))
      return true;

    if( Test.EndsWith( ".jsp" ))
      return true;

    return false;
    }



  internal string GetUserAgent()
    {
    string[] SplitS = AllInputS.Split( new Char[] { '\r' } );
    if( SplitS.Length < 2 )
      return "Bad HTTP: Only one line."; // The protocol requires the Host: line to be sent at least.

    for( int Count = 0; Count < SplitS.Length; Count++ )
      {
      string CheckLine = Utility.CleanAsciiString( SplitS[Count], 2048 );
      string TestLine = CheckLine.ToLower();

      if( TestLine.StartsWith( "user-agent:" ))
        {
        // string[] SplitLine = FirstLine.Split( new Char[] { ':' } );
        // if( SplitLine.Length < 3 )
        string Result = CheckLine.Replace( "User-Agent:", "" ).Trim();
        return Result;
        }
      }

    return "None";
    }




  // It's spelled wrong from the original HTTP protocol, and it stuck.
  internal string GetReferer()
    {
    string[] SplitS = AllInputS.Split( new Char[] { '\r' } );
    if( SplitS.Length < 2 )
      return "Bad HTTP: Only one line."; // The protocol requires the Host: line to be sent at least.

    for( int Count = 0; Count < SplitS.Length; Count++ )
      {
      string CheckLine = Utility.CleanAsciiString( SplitS[Count], 2048 );
      string TestLine = CheckLine.ToLower();

      if( TestLine.StartsWith( "referer:" ))
        {
        string Result = CheckLine.Replace( "Referer:", "" ).Trim();
        return Result;
        }
      }

    return "None";
    }



  internal bool GetProcessingStarted()
    {
    return ProcessingStarted;
    }


  internal void SetProcessingStarted( bool SetTo )
    {
    ProcessingStarted = SetTo;
    }


  internal string GetRemoteAddress()
    {
    return RemoteAddress;
    }




  // http://en.wikipedia.org/wiki/Internet_media_type
  internal void SendGenericWebResponse( byte[] Buffer, ulong ModifiedIndex, ulong UniqueEntity, string ContentType )
    {
    if( Client == null )
      return;

    try
    {
    // Set the initial UniqueEntity to the current date time index and then just
    // keep incrementing it.
    ECTime RightNow = new ECTime();
    RightNow.SetToNow();

    ECTime ExpireTime = new ECTime();
    ExpireTime.SetToNow();
    ExpireTime.AddSeconds( 120 );

    ECTime ModifiedTime = new ECTime( ModifiedIndex );

    // ETag is an Entity Tag.
    // "An entity tag MUST be unique across all versions of all entities
    // associated with a particular resource."
    string Header = "HTTP/1.1 200 OK\r\n" +
           "Date: " + RightNow.GetHTTPHeaderDateTime() + "\r\n" +
           "Server: Eric Example\r\n" +
           "Last-Modified: " + ModifiedTime.GetHTTPHeaderDateTime() + "\r\n" +
           "ETag: " + UniqueEntity.ToString() + "\r\n" +
           "Accept-Ranges: bytes\r\n" +
           "Content-Length: " + Buffer.Length.ToString() + "\r\n" +
           // "Cache-Control: max-age=5184000
           "Expires: " + ExpireTime.GetHTTPHeaderDateTime() + "\r\n" +
           "Keep-Alive: timeout=5, max=100\r\n" +
           "Connection: Keep-Alive\r\n" +
           "Content-Type: " + ContentType + "\r\n" +
           "\r\n"; // Empty line and then the actual bytes.

    byte[] HeaderBytes = UTF8Strings.StringToBytes( Header );
    if( HeaderBytes == null )
      return;

    byte[] AllSendBytes;

    if( GetIsHeadOnly() )
      AllSendBytes = new byte[HeaderBytes.Length];
    else
      AllSendBytes = new byte[HeaderBytes.Length + Buffer.Length];

    int Where = 0;
    for( int Count = 0; Count < HeaderBytes.Length; Count++ )
      {
      AllSendBytes[Where] = HeaderBytes[Count];
      Where++;
      }

    if( !GetIsHeadOnly() )
      {
      for( int Count = 0; Count < Buffer.Length; Count++ )
        {
        AllSendBytes[Where] = Buffer[Count];
        Where++;
        }
      }

    // This returns immediately.
    WriteBytesAsync( AllSendBytes );

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in SendHTMLOrText():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  }
}




