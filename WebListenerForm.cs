// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://ericsoftwarenotes.blogspot.com/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;



namespace ExampleServer
{

  public struct DnsWorkerInfo
    {
    public string IP;
    // public int ServerPort;
    }


  public partial class WebListenerForm : Form
  {
  private MainForm MForm;
  private bool IsEnabled = false;
  private TcpListener Listener;
  private int ClientsLast = 0;
  private WebTCPClient[] Clients;
  private ulong UniqueEntityTag = 0;
  private string DnsString = "";
  private int DailyHackCount = 0;



  private WebListenerForm()
    {
    InitializeComponent();
    }


  internal WebListenerForm( MainForm UseForm )
    {
    InitializeComponent();

    MForm = UseForm;

    DNSBackgroundWorker.WorkerReportsProgress = true;
    DNSBackgroundWorker.WorkerSupportsCancellation = true;

    ClientsLast = 0;
    // It will resize this as it needs more.
    Clients = new WebTCPClient[8];

    Listener = new TcpListener( IPAddress.Any, 80 );
    Listener.ExclusiveAddressUse = true;

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    UniqueEntityTag = RightNow.GetIndex(); // Start it with something new.
    }




  private void clearStatusToolStripMenuItem_Click(object sender, EventArgs e)
    {
    MainTextBox.Text = "";
    }



  internal void FreeEverythingAndStopServer()
    {
    IsEnabled = false;
    CheckTimer.Stop();

    StopServer();
    }




  internal void ShowStatus( string Status )
    {
    if( MainTextBox.Text.Length > (80 * 5000))
      MainTextBox.Text = "";

    MainTextBox.AppendText( Status + "\r\n" ); 
    }




  internal bool AddClient( WebTCPClient Client )
    {
    if( Client == null )
      return false;

    Clients[ClientsLast] = Client;
    ClientsLast++;

    if( ClientsLast >= Clients.Length )
      {
      try
      {
      Array.Resize( ref Clients, Clients.Length + 512 );
      }
      catch
        {
        return false;
        }
      }

    // ShowStatus( "Added client at: " + ClientsLast.ToString() );
    return true;
    }




  internal bool StartServer()
    {
    try
    {
    Listener.Start();
    }
    catch( Exception Except )
      {
      ShowStatus( "Could not start the server listen process." );
      ShowStatus( Except.Message );
      return false;
      }

    CheckTimer.Interval = 50;
    CheckTimer.Start();
    IsEnabled = true;
    // StartTimeCheck();
    ShowStatus( "Server process has been started." );
    return true;
    }




  internal bool StopServer()
    {
    IsEnabled = false;
    CheckTimer.Stop();

    try
    { 
    // if( Listener.Active ) // Active is a protected property.
    Listener.Stop();

    // Close each socket and all...
    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] == null ) // This should never happen but...
        continue;

      Clients[Count].FreeEverything();
      Clients[Count] = null;
      }

    ClientsLast = 0;

    }
    catch( Exception Except )
      {
      ShowStatus( "Error in closing the server." );
      ShowStatus( Except.Message );
      return false;
      }

    ShowStatus( "Server has been stopped." );
    return true;
    }



  internal void CloseTimedOut()
    {
    ECTime OldTime = new ECTime();
    OldTime.SetToNow();
    ////////////////////////
    OldTime.AddSeconds( -4 );

    ECTime OldWebTime = new ECTime();
    OldWebTime.SetToNow();
    // Browsers that are cooperative don't open too many connections at once.
    // Denial of service people will.
    // This delay makes browsers that are cooperative hold off on sending more
    // requests until they've closed some sockets.
    // Apparently this is the operating system that is being cooperative.
    // And if this server is too busy it's not going to be calling this
    // function to close it until it can get around to it.
    // So cooperative web clients will wait.
    OldWebTime.AddSeconds( -0.1 );
    // OldWebTime.AddSeconds( 10 ); // If I put it 10 seconds in the _future_ it works.
    // OldWebTime.AddSeconds( -10 );
    // The socket is setting LastTransactTime when AsyncResult.IsCompleted.
    // So it's setting it when the socket thinks it's done sending it.
    ulong OldIndex = OldTime.GetIndex();
    ulong OldWebIndex = OldWebTime.GetIndex();
    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] == null ) // This should never happen but...
        continue;

      if( Clients[Count].IsShutDown())
        continue;

      // Close a web request only after it has started processing the request.
      if( Clients[Count].GetProcessingStarted())
        {
        if( Clients[Count].GetIsAWebRequest())
          {
          if( Clients[Count].GetLastTransactTimeIndex() < OldWebIndex )
            {
            if( Clients[Count].IsProcessingInBackground())
              {
              // ShowStatus( " " );
              // ShowStatus( "Web request is still sending." );
              // MForm.ServerLog.AddToLog( "Web Still Sending", "Nada", Clients[Count].GetRemoteAddress() );
              }

            Clients[Count].FreeEverything();
            }
          }
        }

      // If it's more recent than the old index then it's OK.
      if( Clients[Count].GetLastTransactTimeIndex() > OldIndex )
        continue;

      // If this message has not already been processed.
      if( !Clients[Count].GetProcessingStarted())
        {
        string InputS = Utility.GetCleanUnicodeString( Clients[Count].GetAllInputS(), 2000 );
        InputS = InputS.Trim();
        if( InputS.Length > 0 )
          {
          // ShowStatus( "Timed out with: " + InputS );
          }

        MForm.NetStats.AddToTimedOutCount( Clients[Count].GetRemoteAddress(), InputS );
        }

      if( Clients[Count].IsProcessingInBackground())
        {
        ShowStatus( " " );
        ShowStatus( "**************************************" );
        ShowStatus( "Still sending after time out period." );
        ShowStatus( "**************************************" );
        ShowStatus( " " );
        }

      // They normally time out.
      // ShowStatus( "Closing timed out: " + Clients[Count].GetStationName());

      Clients[Count].FreeEverything();
      }
    }




  internal void FreeClosed()
    {
    // This obviously only runs in the main UI thread.
    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] == null ) // This should never happen but...
        continue;

      // If it still has a background process running then
      // IsShutDown returns false.
      if( Clients[Count].IsShutDown())
        {
        Clients[Count] = null;
        }
      }

    int MoveTo = 0;
    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] != null )
        {
        // Copy the reference/pointer to it.
        Clients[MoveTo] = Clients[Count];
        MoveTo++;
        }
      }

    ClientsLast = MoveTo;
    }




  private void FillAllIncomingLines()
    {
    if( ClientsLast <= 0 )
      return;

    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] == null ) // This should never happen but...
        continue;

      // It doesn't need to get any more lines from the client once
      // processing for the message has started.

      if( Clients[Count].GetProcessingStarted())
        continue;

      if( Clients[Count].IsShutDown())
        continue;

      // This can close down the connection on the client if it
      // decides it's getting bad data or for some other reason.
      Clients[Count].FillIncomingLines();
      }
    }



  private void WebListenerForm_FormClosing(object sender, FormClosingEventArgs e)
    {
    // Don't let it close this.
    e.Cancel = true;
    Hide();
    }




  private void CheckTimer_Tick(object sender, EventArgs e)
    {
    // This timer event only gets called when the server isn't otherwise busy.
    // Even though the timer interval is set to once every 50 milliseconds it 
    // doesn't mean it will get called that often.
    // It has TestTime to check on how busy it is.

    if( !IsEnabled )
      return;

    CheckTimer.Stop();
    try // for finally
    {
    
    try // for catch
    {
    ECTime TestTime = new ECTime();
    TestTime.SetToNow();

    // 100 clients queued up per timer tick is 2,000 per second max,
    // assuming the server isn't busy and the timer events get called that often.
    // But if there were that many clients connecting you'd need more front end
    // servers to handle the I/O.  Or it's a denial of service attack, and you'd
    // need to deal with that.
    for( int Count = 0; Count < 100; Count++ )
      {
      if( !IsEnabled )
        return;

      if( Listener.Pending() )
        QueueConnectedClient();
      else
        break;

      }

    CloseTimedOut();
    if( !IsEnabled )
      return;

    FreeClosed();
    if( !IsEnabled )
      return;

    ProcessWebRequests();
    if( !IsEnabled )
      return;

    // Don't fill the incoming lines immediately after queueing 
    // connections.  Do some other processing first.
    FillAllIncomingLines();
    if( !IsEnabled )
      return;

    double Seconds = TestTime.GetSecondsToNow();
    if( Seconds > 1.0 )
      {
      ShowStatus( " " );
      ShowStatus( "**************************************************" );
      ShowStatus( "Web Listener Test time seconds: " + Seconds.ToString( "N0" ));
      ShowStatus( "Test time:: " + TestTime.ToLocalTimeString());
      ShowStatus( "**************************************************" );
      ShowStatus( " " );
      }
    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in CheckTimerTick: \r\n" + Except.Message );
      return;
      }

    }
    finally
      {
      CheckTimer.Start();
      }
    }



  private void QueueConnectedClient()
    {
    // MForm.ShowStatus( "Top of QueueConnectedClient()." );
    if( !IsEnabled )
      return;

    if( !Listener.Pending())
      return;

    WebTCPClient NewClient = null;

    try
    {
    TcpClient Client = Listener.AcceptTcpClient();

    // IPAddress.Parse( 
    IPEndPoint EndPt = (IPEndPoint)(Client.Client.RemoteEndPoint);
    string Address = EndPt.Address.ToString();
    // int PortNum = EndPt.Port;
    Address = Address.Trim();

    MForm.NetStats.AddToPort80Count( Address );

    if( MForm.NetStats.IsBlockedAddress( Address ))
      {
      Client.Close(); // Disconnect this guy.
      return;
      }

    // See if it needs to update the DNS.
    StartSendingForDns( Address );

    NewClient = new WebTCPClient( MForm, Client, Address );
    // ShowStatus( "Queue Remote Address: " + Address );
    AddClient( NewClient );

    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in QueueConnectedClient():\r\n" + Except.Message );

      if( NewClient != null )
        {
        // It could be in the Clients array, but it will get removed
        // if it is, since it's shut down.
        NewClient.FreeEverything();
        }
      }
    }



  internal void SaveToFile( string FileName )
    {
    try
    {
    using( StreamWriter SWriter = new StreamWriter( FileName  )) 
      {
      foreach( string Line in MainTextBox.Lines )
        {
        SWriter.WriteLine( Line );
        }
      }

    MForm.StartProgramOrFile( FileName );

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Error: Could not write the data to the file." );
      MForm.ShowStatus( FileName );
      MForm.ShowStatus( Except.Message );
      return;
      }
    }



  private void ProcessWebRequests()
    {
    if( MForm.GetIsClosing())
      return;

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    for( int Count = 0; Count < ClientsLast; Count++ )
      {
      if( Clients[Count] == null ) // This should never happen but...
        continue;

      // If this is something that has already been processed.
      if( Clients[Count].GetProcessingStarted())
        continue;

      // IsShutDown is a little slower than the above checks.
      if( Clients[Count].IsShutDown())
        continue;

      if( !Clients[Count].IsBrowserRequest())
        continue;

      if( !Clients[Count].IsBrowserRequestReady())
        continue;

      // ShowStatus( "Got a browser request 2." );

      Clients[Count].SetProcessingStarted( true );

      string InputS = Utility.GetCleanUnicodeString( Clients[Count].GetAllInputS(), 2000 );

      // This FileName is already cleaned ASCII.
      string FileName = Clients[Count].GetHTTPFileRequested();
      string OriginalFileName = FileName;

      FileName = FileName.ToLower();
      FileName = FileName.Replace( "/", "" );
      // ShowStatus( "FileName is: " + FileName );

      if( FileName.StartsWith( "bad http:" ))
        {
        MForm.NetStats.AddToHackerCount( Clients[Count].GetRemoteAddress(), InputS );
        // MForm.ServerLog.AddToLog( "Bad HTTP", InputS, Clients[Count].GetRemoteAddress() );
        Clients[Count].FreeEverything();
        ShowStatus( FileName );
        continue;
        }

      if( FileName.StartsWith( "hacking:" ))
        {
        DailyHackCount++;
        MForm.NetStats.AddToHackerCount( Clients[Count].GetRemoteAddress(), InputS );
        // MForm.ServerLog.AddToLog( "Hacking", InputS, Clients[Count].GetRemoteAddress() );
        Clients[Count].FreeEverything();
        RightNow.SetToNow();
        ShowStatus( RightNow.ToLocalTimeString() + " on " + RightNow.ToLocalDateString() );
        ShowStatus( FileName );
        ShowStatus( " " );
        continue;
        }

      RightNow.SetToNow();
      string Referer = "None";
      string UserAgent = "None";

      if( FileName == "laplata.htm" )
        {
        if( MForm.GetIsClosing())
          return;

        // MForm.NetStats.AddTo...

        byte[] ToSendBuf = MForm.LaPlataData1.GetHTML( "smith" );
        if( ToSendBuf != null )
          Clients[Count].SendGenericWebResponse( ToSendBuf, RightNow.GetIndex(), UniqueEntityTag, "text/html" );

        Referer = Clients[Count].GetReferer();
        UserAgent = Clients[Count].GetUserAgent();
        // MForm.ServerLog.AddToLog() ...
        continue;
        }

      if( !MForm.WebFData.ContainsFile( FileName ))
        {
        Clients[Count].FreeEverything();
         // This is already clean ASCII.
        string LogText = FileName + ": " + InputS;
        // MForm.ServerLog.AddToLog( "No Web File", LogText, Clients[Count].GetRemoteAddress() );
        MForm.NetStats.AddToBadWebPageCount( Clients[Count].GetRemoteAddress(), InputS );
        ShowStatus( " " );
        RightNow.SetToNow();
        ShowStatus( RightNow.ToLocalTimeString() + " on " + RightNow.ToLocalDateString() );
        ShowStatus( "No Web File" );
        ShowStatus( "Original: " + OriginalFileName );
        ShowStatus( "Fixed: " + FileName );
        ShowStatus( "From IP: " + Clients[Count].GetRemoteAddress() );
        continue;
        }

      if( FileName.EndsWith( ".exe" ) || FileName.EndsWith( ".apk" ))
        {
        ShowStatus( " " );
        RightNow.SetToNow();
        ShowStatus( RightNow.ToLocalTimeString() + " on " + RightNow.ToLocalDateString() );
        ShowStatus( "Request for: " + FileName );
        ShowStatus( "From IP: " + Clients[Count].GetRemoteAddress() );
        ShowStatus( " " );
        }

      Referer = Clients[Count].GetReferer();
      UserAgent = Clients[Count].GetUserAgent();
      MForm.NetStats.AddToUserAgentAndReferer( Clients[Count].GetRemoteAddress(), Referer, UserAgent );

      // Increment UniqueEntityTag when it sends something.
      UniqueEntityTag++;
      // FileName is already clean ASCII, it's lower case, and trimmed.
      
      // This is a reference to the buffer, but the client copies from it.
      byte[] Buffer = MForm.WebFData.GetBuffer( FileName );
      if( Buffer == null )
        {
        ShowStatus( "The buffer was null for a good web request: " + FileName );
        continue;
        }

      if( FileName.EndsWith( ".jpg" ))
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "image/jpeg" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      if( FileName.EndsWith( ".gif" ))
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "image/gif" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      if( FileName.EndsWith( ".htm" ))
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "text/html" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      if( FileName.EndsWith( ".txt" ))
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "text/plain" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      if( FileName.EndsWith( ".pdf" ))
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "application/pdf" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      // In 2003, the .ico format was registered with the Internet Assigned Numbers
      // Authority (IANA) under the MIME type image/vnd.microsoft.icon.[12] Ironically,
      // when using the .ico format to display as images (e.g. not as favicon), Internet
      // Explorer cannot display files served with this standardized MIME type. A workaround
      // for Internet Explorer is to associate .ico with the non-standard image/x-icon MIME
      // type in Web servers
      if( FileName == "favicon.ico" )
        {
        Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "image/vnd.microsoft.icon" );
        // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
        continue;
        }

      // Default to sending text unless there's some other way to send it.
      Clients[Count].SendGenericWebResponse( Buffer, RightNow.GetIndex(), UniqueEntityTag, "text/plain" );
      // MForm.ServerLog.AddToLog( "Finished Web Request", FileName, Clients[Count].GetRemoteAddress() );
      }
    }



  private void readAllWebFilesToolStripMenuItem_Click(object sender, EventArgs e)
    {
    MForm.ReadWebFileData();
    }




  internal void StartSendingForDns( string IP )
    {
    if( MForm.GetIsClosing())
      return;

    if( IsDisposed )
      return;

    ulong LastUpdate = MForm.NetStats.GetLastHostNameUpdate( IP );
    if( LastUpdate != 0 )
      {
      ECTime LastUpdateTime = new ECTime( LastUpdate );
      if( LastUpdateTime.GetDaysToNow() < 7 )
        return;

      }
    // MForm.ServerLog.AddToLog( "Description Data", "Getting description data." );

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    MForm.NetStats.UpdateHostNameCheckTime( IP );

    // ShowStatus( " " );
    // ShowStatus( "Dns for: " + IP + " " + ForWhat + " " + RightNow.ToLocalTimeString() );
    // ShowStatus( "Dns for: " + ForWhat + " " + RightNow.ToLocalTimeString() );

    DnsWorkerInfo WInfo = new DnsWorkerInfo();

    WInfo.IP = IP;

    try
    {
    if( !DNSBackgroundWorker.IsBusy )
      {
      DNSBackgroundWorker.RunWorkerAsync( WInfo );
      }
    else
      {
      // ShowStatus( "Dns background process is busy." );
      }

    }
    catch( Exception Except )
      {
      ShowStatus( "Error starting background process for Dns." );
      ShowStatus( Except.Message );
      return;
      }
    }



  private void DNSBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
    BackgroundWorker Worker = (BackgroundWorker)sender;
    DnsWorkerInfo WInfo = (DnsWorkerInfo)(e.Argument);

    try // catch
    {
    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    // System.Net.Dns

    // http://54.201.164.92

    // http://stackoverflow.com/questions/716748/reverse-
    IPAddress addr = IPAddress.Parse( WInfo.IP );
    IPHostEntry Entry = Dns.GetHostEntry( addr );

    string HostName = Utility.CleanAsciiString( Entry.HostName, 300 );
    // Worker.ReportProgress( 0, " " );
    // Worker.ReportProgress( 0, "HostName: " + HostName );

    Worker.ReportProgress( 0, "DNS:\t" + WInfo.IP + "\t" + HostName );

    // IPAddress[] AddressArray = Entry.AddressList;
    // for(int Count = 0; Count < AddressArray.Length; Count++ )
      // Worker.ReportProgress( 0, "Address Array: " + AddressArray[Count].ToString());

    // String[] AliasArray = Entry.Aliases;
    // for(int Count = 0; Count < AliasArray.Length; Count++ )
      // Worker.ReportProgress( 0, "Alias Array: " + AliasArray[Count].ToString());

    // Worker.ReportProgress( 0, " " );

    }
    catch( Exception Except )
      {
      if( !(Except.Message.Contains( "No such host" ) ||
            Except.Message.Contains( "usually a temporary error during" )))
        {
        Worker.ReportProgress( 0, "Error in Dns DoWork process:" );
        Worker.ReportProgress( 0, Except.Message );
        e.Cancel = true;
        }

      return;
      }
    }



  private void DNSBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
    // This runs in the UI thread.
    if( MForm.GetIsClosing())
      return;
  
    if( IsDisposed )
      return;

    string CheckStatus = (string)e.UserState;
    if( CheckStatus == null )
      return;

    if( CheckStatus.Length < 1 )
      return;

    //  "DNS:" + WInfo.IP + ":" + Entry.HostName.Trim() );
    if( CheckStatus.StartsWith( "DNS:" ))
      DnsString = CheckStatus;

    // ShowStatus( CheckStatus );

    // ShowStatus( "User State: " + e.UserState );
    // e.ProgressPercentage
    }




  private void DNSBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
    if( MForm.GetIsClosing())
      return;

    if( IsDisposed )
      return;

    if( e.Cancelled )
      {
      ShowStatus( "DNS Background worker was cancelled." );
      return;
      }

    // ShowStatus( "DNS Background worker is completed." );

    if( DnsString == "" )
      return;

    string[] SplitS = DnsString.Split( new Char[] { '\t' } );
    if( SplitS.Length < 3 )
      return;

    //  "DNS:\t" + WInfo.IP + "\t" + Entry.HostName.Trim() );
    string IP = SplitS[1].Trim();
    string HostName = SplitS[2].Trim();
    MForm.NetStats.UpdateHostName( IP, HostName );

    /*
    if( e.Error != null )
      {
      // HadErrorOrCancel = true;
      ShowStatus( "There was an HTTP error: " + e.Error.Message );
      return;
      }

    // e.Result
    // e.UserState
    */
    }



  }
}

