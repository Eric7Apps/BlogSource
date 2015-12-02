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


namespace ExampleServer
{
  internal struct MakeKeysWorkerInfo
    {
    internal CRTCombinSetupRec[] SetupArray;
    internal string PublicKeyModulus;
    internal uint ModMask;
    internal string ProcessName;
    }


  public partial class MakeRSAKeysForm : Form
  {
  private MainForm MForm;
  private bool Cancelled = false;
  private CRTCombinBackground[] CRTCombinBackgArray;


  private MakeRSAKeysForm()
    {
    InitializeComponent();
    }


  internal MakeRSAKeysForm( MainForm UseForm )
    {
    InitializeComponent();

    MForm = UseForm;
    MakeKeysBackgroundWorker.WorkerReportsProgress = true;
    MakeKeysBackgroundWorker.WorkerSupportsCancellation = true;
    }


  internal void FreeEverything()
    {
    if( IsDisposed )
      return;

    try
    {
    if( MakeKeysBackgroundWorker.IsBusy )
      {
      if( !MakeKeysBackgroundWorker.CancellationPending )
        MakeKeysBackgroundWorker.CancelAsync();

      }
    }
    catch( Exception Except )
      {
      ShowStatus( "Error on closing MakeKeysBackgroundWorker:\r\n" + Except.Message );
      }


    if( CRTCombinBackgArray != null )
      {
      try
      {
      for( int Count = 0; Count < CRTCombinBackgArray.Length; Count++ )
        {
        if( CRTCombinBackgArray[Count] == null )
          continue;

        if( CRTCombinBackgArray[Count].IsBusy )
          {
          if( !CRTCombinBackgArray[Count].CancellationPending )
            CRTCombinBackgArray[Count].CancelAsync();

          }

        CRTCombinBackgArray[Count].Dispose();
        CRTCombinBackgArray[Count] = null;
        }
      }
      catch( Exception Except )
        {
        ShowStatus( "Exception for CRTCombinBackgArray[] on closing:\r\n" + Except.Message );
        }
      }
    }



  internal void ShowStatus( string Status )
    {
    if( IsDisposed )
      return;

    if( MainTextBox.Text.Length > (80 * 100000))
      MainTextBox.Text = "";

    MainTextBox.AppendText( Status + "\r\n" ); 
    }



  internal bool CheckEvents()
    {
    Application.DoEvents();

    if( Cancelled ) 
      {
      // ShowStatus( "Cancelled." );
      return false;
      }
    else
      return true;
    
    }


  private void testToolStripMenuItem_Click(object sender, EventArgs e)
    {
    Cancelled = false;
    StartMakingKeys();
    }



  internal void StartMakingKeys()
    {
    MakeKeysWorkerInfo WInfo = new MakeKeysWorkerInfo();
    WInfo.SetupArray = new CRTCombinSetupRec[4];
    WInfo.ProcessName = "Single thread test";
    WInfo.PublicKeyModulus = null; // Make a new random one.

    WInfo.SetupArray[0].Start = 0;
    WInfo.SetupArray[0].End = 5;
    WInfo.ModMask = 0xFFFFFFFF;

    WInfo.SetupArray[1].Start = 6;
    WInfo.SetupArray[1].End = 7;
    WInfo.ModMask = 0xFFFFFFFF;

    WInfo.SetupArray[2].Start = 8;
    WInfo.SetupArray[2].End = 9;
    WInfo.ModMask = 0xFFFFFFFF;

    WInfo.SetupArray[3].Start = 10;
    WInfo.SetupArray[3].End = 11;
    WInfo.ModMask = 0xFFFFFFFF;

    try
    {
    if( !MakeKeysBackgroundWorker.IsBusy )
      {
      ShowStatus( "Starting make keys background process." );
      MakeKeysBackgroundWorker.RunWorkerAsync( WInfo );
      }
    else
      {
      ShowStatus( "The make keys background process is still busy." );
      }

    }
    catch( Exception Except )
      {
      ShowStatus( "Error starting Make Keys background process." );
      ShowStatus( Except.Message );
      return;
      }
    }



  private void MakeRSAKeysForm_KeyDown(object sender, KeyEventArgs e)
    {
    if( e.KeyCode == Keys.Escape ) //  && (e.Alt || e.Control || e.Shift))
      {
      ShowStatus( "Cancelled." );
      Cancelled = true;
      FreeEverything(); // Stop background process.
      }
    }



  private void MakeRSAKeysForm_FormClosing(object sender, FormClosingEventArgs e)
    {
    e.Cancel = true;
    Hide();
    }



  private void MakeKeysBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
    BackgroundWorker Worker = (BackgroundWorker)sender;
    MakeKeysWorkerInfo WInfo = (MakeKeysWorkerInfo)(e.Argument);
    
    try // catch
    {
    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }
    
    // MakeKeysBackground MakeKeys = new MakeKeysBackground( Worker, WInfo );
    // MakeKeys.MakeRSAKeys();
    // MakeKeys.FreeEverything();

    FindFactorsBackground FindFac = new FindFactorsBackground( Worker, WInfo );
    FindFac.FindFactors();
    FindFac.FreeEverything();

    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in MakeRSAKeysForm DoWork process:" );
      Worker.ReportProgress( 0, Except.Message );
      e.Cancel = true;
      }
    }



  private void MakeKeysBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
    // This runs in the UI thread.
    if( IsDisposed )
      return;

    if( Cancelled )
      return;

    if( MForm.GetIsClosing())
      return;

    string CheckStatus = (string)e.UserState;
    if( CheckStatus == null )
      return;

    if( CheckStatus.Length < 1 )
      return;

    if( e.ProgressPercentage > 0 )
      {
      string[] SplitS = CheckStatus.Split( new Char[] { ':' } );
      if( SplitS.Length >= 2 )
        {
        string KeyWord = SplitS[0].Trim();
        string Value = SplitS[1].Trim();
        
        if( KeyWord == "Prime1" )
          MForm.GlobalProps.SetRSAPrime1( Value );

        if( KeyWord == "Prime2" )
          MForm.GlobalProps.SetRSAPrime2( Value );

        if( KeyWord == "PubKeyN" )
          MForm.GlobalProps.SetRSAPubKeyN( Value );

        if( KeyWord == "PrivKInverseExponent" )
          MForm.GlobalProps.SetRSAPrivKInverseExponent( Value );

        }
      }

    ShowStatus( CheckStatus );
    }



  private void MakeKeysBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
    if( IsDisposed )
      return;

    if( MForm.GetIsClosing())
      return;

    if( e.Cancelled )
      {
      ShowStatus( "Cancelled." );
      return;
      }

    ShowStatus( " " );
    ShowStatus( " " );
    ShowStatus( " " );
    ShowStatus( "Finished making keys." );
    ShowStatus( " " );
    }



  private void startProcessesToolStripMenuItem_Click( object sender, EventArgs e )
    {
    try
    {
    // Some test numbers:
    // string PubKey =         "15,654,675,243,543,711,381,029";
    string PubKey =     "28,569,209,393,580,650,250,552,443";
    // string PubKey = "56,486,207,148,903,090,249,668,503,717";

    CRTCombinBackgArray = new CRTCombinBackground[4];

    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107

    // In this case I'm setting up each thread the same, except for the 
    // ModMask, but you might want to set some threads with smaller
    // combinations (SetupArray would be smaller and with smaller start
    // and end values) because if the solutions P and Q were close together
    // they'd be more likely to be found sooner with smaller combinations.

    int Start0 = 0;
    int End0 = 6;

    int Start1 = 7;
    int End1 = 8;

    int Start2 = 9;
    int End2 = 10;

    int Start3 = 11;
    int End3 = 12;

    MakeKeysWorkerInfo WInfo = new MakeKeysWorkerInfo();
    WInfo.SetupArray = new CRTCombinSetupRec[4];
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 0;
    WInfo.ProcessName = "Process 0";

    WInfo.SetupArray[0].Start = Start0;
    WInfo.SetupArray[0].End = End0;

    WInfo.SetupArray[1].Start = Start1;
    WInfo.SetupArray[1].End = End1;

    WInfo.SetupArray[2].Start = Start2;
    WInfo.SetupArray[2].End = End2;

    WInfo.SetupArray[3].Start = Start3;
    WInfo.SetupArray[3].End = End3;

    CRTCombinBackgArray[0] = new CRTCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 0" );
    CRTCombinBackgArray[0].RunWorkerAsync( WInfo );


    ////////////////
    WInfo = new MakeKeysWorkerInfo();
    WInfo.SetupArray = new CRTCombinSetupRec[4];
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 1;
    WInfo.ProcessName = "Process 1";

    WInfo.SetupArray[0].Start = Start0;
    WInfo.SetupArray[0].End = End0;

    WInfo.SetupArray[1].Start = Start1;
    WInfo.SetupArray[1].End = End1;

    WInfo.SetupArray[2].Start = Start2;
    WInfo.SetupArray[2].End = End2;

    WInfo.SetupArray[3].Start = Start3;
    WInfo.SetupArray[3].End = End3;

    CRTCombinBackgArray[1] = new CRTCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 1" );
    CRTCombinBackgArray[1].RunWorkerAsync( WInfo );


    ////////
    WInfo = new MakeKeysWorkerInfo();
    WInfo.SetupArray = new CRTCombinSetupRec[4];
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 2;
    WInfo.ProcessName = "Process 2";

    WInfo.SetupArray[0].Start = Start0;
    WInfo.SetupArray[0].End = End0;

    WInfo.SetupArray[1].Start = Start1;
    WInfo.SetupArray[1].End = End1;

    WInfo.SetupArray[2].Start = Start2;
    WInfo.SetupArray[2].End = End2;

    WInfo.SetupArray[3].Start = Start3;
    WInfo.SetupArray[3].End = End3;

    CRTCombinBackgArray[2] = new CRTCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 2" );
    CRTCombinBackgArray[2].RunWorkerAsync( WInfo );


    ////////
    WInfo = new MakeKeysWorkerInfo();
    WInfo.SetupArray = new CRTCombinSetupRec[4];
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 3;
    WInfo.ProcessName = "Process 3";

    WInfo.SetupArray[0].Start = Start0;
    WInfo.SetupArray[0].End = End0;

    WInfo.SetupArray[1].Start = Start1;
    WInfo.SetupArray[1].End = End1;

    WInfo.SetupArray[2].Start = Start2;
    WInfo.SetupArray[2].End = End2;

    WInfo.SetupArray[3].Start = Start3;
    WInfo.SetupArray[3].End = End3;

    CRTCombinBackgArray[3] = new CRTCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 3" );
    CRTCombinBackgArray[3].RunWorkerAsync( WInfo );


    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in loop for starting CRTCombinBackgArray:\r\n" + Except.Message );
      }
    }



  internal void FoundSolutionShutDown( string ProcessName,
                                       string SolutionP,
                                       string SolutionQ )
    {
    if( IsDisposed )
      return;

    ShowStatus( "Found the solution in: " + ProcessName );
    ShowStatus( "Stopping processes." );
    ShowStatus( "SolutionP: " + SolutionP );
    ShowStatus( "SolutionQ: " + SolutionQ );

    if( CRTCombinBackgArray == null )
      return;

    for( int Count = 0; Count < CRTCombinBackgArray.Length; Count++ )
      {
      try
      {
      if( CRTCombinBackgArray[Count] == null )
        continue;

      if( CRTCombinBackgArray[Count].IsBusy )
        {
        if( !CRTCombinBackgArray[Count].CancellationPending )
          CRTCombinBackgArray[Count].CancelAsync();

        }

      CRTCombinBackgArray[Count].Dispose();
      CRTCombinBackgArray[Count] = null;
      }
      catch( Exception Except )
        {
        ShowStatus( "Exception for FoundSolutionShutDown():\r\n" + Except.Message );
        }
      }
    }



  }
}
