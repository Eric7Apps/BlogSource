// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


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
using System.Globalization;
using System.Diagnostics; // For start program process.
using System.Security.Cryptography;

// CryptoGraphic Services:
// https://msdn.microsoft.com/en-us/library/92f9ye3s%28v=vs.110%29.aspx
// using System.Net.Security; // SslStream
// using System.Security;
// ICryptoTransform


namespace ExampleServer
{
  public partial class MainForm : Form
  {
  internal const string VersionDate = "10/19/2015";
  internal const int VersionNumber = 09; // 0.9
  internal const string MessageBoxTitle = "Eric's Example Server";
  private System.Threading.Mutex SingleInstanceMutex = null;
  private bool IsSingleInstance = false;
  private bool IsClosing = false;
  // private bool FormShownOnce = false;
  internal GlobalProperties GlobalProps;
  private string WebPagesDirectory = "";
  private string DataDirectory = "";
  internal NetIPStatus NetStats;
  // private long CheckTimerCount = 0;
  internal WebListenerForm WebListenForm = null;
  internal TLSListenerForm TLSListenForm = null;
  internal CustomerMessageForm CustomerMsgForm = null;
  internal LaPlataData LaPlataData1;
  internal WebFilesData WebFData;
  internal DomainX509Data X509Data;
  private RNGCryptoServiceProvider CryptoRand;



  public MainForm()
    {
    InitializeComponent();

    SetLocaleEnglish();

    ///////////////////////
    // Keep this at the top:
    SetupDirectories();
    GlobalProps = new GlobalProperties( this );
    ///////////////////////

    CryptoRand = new RNGCryptoServiceProvider();

    NetStats = new NetIPStatus( this );
    NetStats.ReadFromFile();

    LaPlataData1 = new LaPlataData( this );
    WebFData = new WebFilesData( this );
    X509Data = new DomainX509Data( this );


    if( !CheckSingleInstance())
      return;

    IsSingleInstance = true;

    WebListenForm = new WebListenerForm( this );
    TLSListenForm = new TLSListenerForm( this );
    CustomerMsgForm = new CustomerMessageForm( this );

    CheckTimer.Interval = 5 * 60 * 1000;
    CheckTimer.Start();

    StartTimer.Interval = 1000;
    StartTimer.Start();
    }



  internal void ShowStatus( string Status )
    {
    if( IsClosing )
      return;

    if( MainTextBox.Text.Length > (80 * 5000))
      MainTextBox.Text = "";

    MainTextBox.AppendText( Status + "\r\n" ); 
    }



  private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
    string ShowS = "Programming by Eric Chauvin. Version date: " + VersionDate;
    MessageBox.Show( ShowS, MessageBoxTitle, MessageBoxButtons.OK );
    }



  internal string GetWebPagesDirectory()
    {
    return WebPagesDirectory;
    }


  internal string GetDataDirectory()
    {
    return DataDirectory;
    }


  private void SetupDirectories()
    {
    DataDirectory = Application.StartupPath + "\\Data\\";
    WebPagesDirectory = Application.StartupPath + "\\WebPages\\";

    if( !Directory.Exists( WebPagesDirectory ))
      {
      try
      {
      Directory.CreateDirectory( WebPagesDirectory );
      }
      catch( Exception )
        {
        MessageBox.Show( "Error: The WebPages directory could not be created.", MessageBoxTitle, MessageBoxButtons.OK );
        return;
        }
      }

    if( !Directory.Exists( DataDirectory ))
      {
      try
      {
      Directory.CreateDirectory( DataDirectory );
      }
      catch( Exception )
        {
        MessageBox.Show( "Error: The data directory could not be created.", MessageBoxTitle, MessageBoxButtons.OK );
        return;
        }
      }

    }



  internal bool GetIsClosing()
    {
    return IsClosing;
    }


  internal bool CheckEvents()
    {
    if( IsClosing )
      return false;

    Application.DoEvents();
    // if it's cancelled...
    return true;
    }



  // This has to be added in the Program.cs file.
  internal static void UIThreadException( object sender, ThreadExceptionEventArgs t )
    {
    string ErrorString = t.Exception.Message;

    try
      {
      string ShowString = "There was an unexpected error:\r\n\r\n" +
             "The program will close now.\r\n\r\n" +
             ErrorString;

      MessageBox.Show( ShowString, "Program Error", MessageBoxButtons.OK, MessageBoxIcon.Stop );
      }
    finally
      {
      Application.Exit();
      }
    }



  private bool CheckSingleInstance()
    {
    bool InitialOwner = false; // Owner for single instance check.

    string ShowS = "Another instance of the Example Server is already running." +
      " This instance will close.";

    try
    {
    SingleInstanceMutex = new System.Threading.Mutex( true, "Example Server Single Instance", out InitialOwner );
    }
    catch
      {
      MessageBox.Show( ShowS, MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop );
      // mutex.Close();
      // mutex = null;
      
      // Can't do this here:
      // Application.Exit();
      SingleInstanceTimer.Interval = 50;
      SingleInstanceTimer.Start();
      return false;
      }
      
    if( !InitialOwner )
      {
      MessageBox.Show( ShowS, MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop );
      // Application.Exit();
      SingleInstanceTimer.Interval = 50;
      SingleInstanceTimer.Start();
      return false;
      }

    return true;
    }



  private void SetLocaleEnglish()
    {
    // Make sure you know what you're dealing with when converting money, numbers,
    // delimiters, etc.
    RegionInfo Region = new RegionInfo( Application.CurrentCulture.Name );
    
    string CName = Application.CurrentCulture.Name;
    string CultureName = Application.CurrentCulture.EnglishName;

    if( CName != "en-US" )
      {
      try
      {
      CultureInfo American = new CultureInfo( "en-US" );
      Application.CurrentCulture = American;
      }
      catch
        {
        MessageBox.Show( "This program may not work correctly in this culture. Could not set the culture to English (United States). Culture name is: " + CultureName, MessageBoxTitle, MessageBoxButtons.OK );
        }
      }
    }



  private void SingleInstanceTimer_Tick(object sender, EventArgs e)
    {
    SingleInstanceTimer.Stop();
    Application.Exit();
    }



  private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
    if( IsSingleInstance )
      {
      if( DialogResult.Yes != MessageBox.Show( "Close the program?", MessageBoxTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question ))
        {
        e.Cancel = true;
        return;
        }
      }

    NetStats.SaveToFile();

    IsClosing = true;
    CheckTimer.Stop();

    if( WebListenForm != null )
      {
      if( !WebListenForm.IsDisposed )
        {
        WebListenForm.Hide();
        
        // This could take a while since it's closing connections:
        WebListenForm.FreeEverythingAndStopServer();
        WebListenForm.Dispose();
        }

      WebListenForm = null;
      }

    if( TLSListenForm != null )
      {
      if( !TLSListenForm.IsDisposed )
        {
        TLSListenForm.Hide();
        
        // This could take a while since it's closing connections:
        TLSListenForm.FreeEverythingAndStopServer();
        TLSListenForm.Dispose();
        }

      TLSListenForm = null;
      }

    if( CustomerMsgForm != null )
      {
      if( !CustomerMsgForm.IsDisposed )
        {
        CustomerMsgForm.Hide();
        
        CustomerMsgForm.FreeEverything();
        CustomerMsgForm.Dispose();
        }

      CustomerMsgForm = null;
      }

    /*
    if( MakeKeysForm != null )
      {
      if( !MakeKeysForm.IsDisposed )
        {
        MakeKeysForm.Hide();
        
        // This could take a while:
        MakeKeysForm.FreeEverything();
        MakeKeysForm.Dispose();
        }

      MakeKeysForm = null;
      }
      */

    }



  private void SaveAllMidnightFiles()
    {
    /*
    if( WebListenForm != null )
      {
      if( !WebListenForm.IsDisposed )
        {
        WebListenForm.ClearDailyHackCount();
        }
      }
      */

    NetStats.ClearMidnightValues();
    }



  private void CheckTimer_Tick(object sender, EventArgs e)
    {
    CheckTimer.Stop();
    try
    {
    // CheckTimerCount++;
    // int CountMod = (int)(CheckTimerCount % 5);
    // if( CountMod == 0 )

    NetStats.SaveToFile();

    // ECTime ShowTime = new ECTime();
    // ShowTime.SetToNow();
    // ShowStatus( "Saved data files at: " + ShowTime.ToLocalTimeString() );

    }
    finally
      {
      CheckTimer.Start();
      }
    }





  // Be careful about what you execute from the server.
  internal bool StartProgramOrFile( string FileName )
    {
    if( !File.Exists( FileName ))
      return false;

    Process ProgProcess = new Process();

    try
    {
    ProgProcess.StartInfo.FileName = FileName;
    ProgProcess.StartInfo.Verb = ""; // "Print";
    ProgProcess.StartInfo.CreateNoWindow = false;
    ProgProcess.StartInfo.ErrorDialog = false;
    ProgProcess.Start();
    }
    catch( Exception Except )
      {
      MessageBox.Show( "Could not start the file: \r\n" + FileName + "\r\n\r\nThe error was:\r\n" + Except.Message, MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop );
      return false;
      }

    return true;
    }



  internal void ShowWebListenerFormStatus( string Status )
    {
    if( IsClosing )
      return;

    if( WebListenForm == null )
      return;
      
    if( WebListenForm.IsDisposed )
      return;

    WebListenForm.ShowStatus( Status ); 
    }



  internal void ShowCustomerMsgFormStatus( string Status )
    {
    if( IsClosing )
      return;

    if( CustomerMsgForm == null )
      return;

    if( CustomerMsgForm.IsDisposed )
      return;

    CustomerMsgForm.ShowStatus( Status ); 
    }


  internal void ShowTLSListenerFormStatus( string Status )
    {
    if( IsClosing )
      return;

    if( TLSListenForm == null )
      return;
      
    if( TLSListenForm.IsDisposed )
      return;

    TLSListenForm.ShowStatus( Status ); 
    }




  private void showBasicWebServerToolStripMenuItem_Click(object sender, EventArgs e)
    {
    if( WebListenForm == null )
      return;

    if( WebListenForm.IsDisposed )
      return;

    WebListenForm.Show();
    WebListenForm.WindowState = FormWindowState.Normal;
    WebListenForm.BringToFront();
    }




  private void StartTimer_Tick(object sender, EventArgs e)
    {
    ShowStatus( "StartTimer was called." );
    StartTimer.Stop();

    LaPlataData1.ReadFromFile();
    ReadWebFileData();
    X509Data.ImportFromOriginalListFile();
    ShowStatus( "Reading data is finished." );

    WebListenForm.StartServer();
    TLSListenForm.StartServer();
    }



  private void showTLSServerToolStripMenuItem_Click(object sender, EventArgs e)
    {
    if( TLSListenForm == null )
      return;

    if( TLSListenForm.IsDisposed )
      return;

    TLSListenForm.Show();
    TLSListenForm.WindowState = FormWindowState.Normal;
    TLSListenForm.BringToFront();
    }



  internal void ReadWebFileData()
    {
    ShowStatus( "Starting to read web file data." );
    WebFData.SearchWebPagesDirectory();
    ShowStatus( "Finished reading web file data." );
    }



  internal int GetRandomNumber()
    {
    byte[] TheBytes = new byte[4];

    CryptoRand.GetBytes( TheBytes );
    uint Result = (uint)(TheBytes[0]) & 0x7F;
    Result <<= 8;

    Result |= TheBytes[1];
    Result <<= 8;

    Result |= TheBytes[2];
    Result <<= 8;

    Result |= TheBytes[3];

    return (int)Result;
    }



  private void showTLSCustomerToolStripMenuItem_Click(object sender, EventArgs e)
    {
    if( CustomerMsgForm == null )
      return;

    if( CustomerMsgForm.IsDisposed )
      return;

    CustomerMsgForm.Show();
    CustomerMsgForm.WindowState = FormWindowState.Normal;
    CustomerMsgForm.BringToFront();
    }


  }
}


