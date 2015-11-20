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
  public struct MakeKeysWorkerInfo
    {
    public string Test;
    }


  public partial class MakeRSAKeysForm : Form
  {
  private MainForm MForm;
  private bool Cancelled = false;


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
    StartMakingKeys();
    }



  internal void StartMakingKeys()
    {
    MakeKeysWorkerInfo WInfo = new MakeKeysWorkerInfo();
    WInfo.Test = "Nada";

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


  }
}
