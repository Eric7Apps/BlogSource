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
  public struct LowExponentWorkerInfo
    {
    public string Test;
    }


  public partial class LowExponentSecurityForm : Form
  {
  private MainForm MForm;
  private bool Cancelled = false;



  private LowExponentSecurityForm()
    {
    InitializeComponent();
    }


  internal LowExponentSecurityForm( MainForm UseForm )
    {
    InitializeComponent();

    MForm = UseForm;
    LowExponentBackgroundWorker.WorkerReportsProgress = true;
    LowExponentBackgroundWorker.WorkerSupportsCancellation = true;
    }



  private void testToolStripMenuItem_Click( object sender, EventArgs e )
    {
    StartTesting();
    }



  internal void StartTesting()
    {
    LowExponentWorkerInfo WInfo = new LowExponentWorkerInfo();
    WInfo.Test = "Nada";

    try
    {
    if( !LowExponentBackgroundWorker.IsBusy )
      {
      ShowStatus( "Starting the low exponent background process." );
      LowExponentBackgroundWorker.RunWorkerAsync( WInfo );
      }
    else
      {
      ShowStatus( "The low exponent background process is still busy." );
      }

    }
    catch( Exception Except )
      {
      ShowStatus( "Error starting low exponent background process." );
      ShowStatus( Except.Message );
      return;
      }
    }



  internal void FreeEverything()
    {
    if( IsDisposed )
      return;

    try
    {
    if( LowExponentBackgroundWorker.IsBusy )
      {
      if( !LowExponentBackgroundWorker.CancellationPending )
        LowExponentBackgroundWorker.CancelAsync();

      }
    }
    catch( Exception Except )
      {
      ShowStatus( "Error on closing LowExponentBackgroundWorker:\r\n" + Except.Message );
      }
    }



  internal void ShowStatus( string Status )
    {
    if( IsDisposed )
      return;

    if( MainTextBox.Text.Length > (80 * 10000))
      MainTextBox.Text = "";

    MainTextBox.AppendText( Status + "\r\n" ); 
    }



  internal bool CheckEvents()
    {
    Application.DoEvents();

    if( Cancelled ) 
      {
      ShowStatus( "Cancelled." );
      return false;
      }
    else
      return true;

    }



  private void LowExponentSecurityForm_KeyDown( object sender, KeyEventArgs e )
    {
    if( e.KeyCode == Keys.Escape ) //  && (e.Alt || e.Control || e.Shift))
      {
      ShowStatus( "Cancelled." );
      Cancelled = true;
      FreeEverything(); // Stop background process.
      }
    }



  private void LowExponentSecurityForm_FormClosing( object sender, FormClosingEventArgs e )
    {
    e.Cancel = true;
    Hide();
    }



  private void LowExponentBackgroundWorker_DoWork( object sender, DoWorkEventArgs e )
    {
    BackgroundWorker Worker = (BackgroundWorker)sender;
    LowExponentWorkerInfo WInfo = (LowExponentWorkerInfo)(e.Argument);
    
    try // catch
    {
    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }
    
    LowExponentBackground LowExponent = new LowExponentBackground( Worker, WInfo );
    LowExponent.StartTest();
    LowExponent.FreeEverything();

    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in LowExponentSecurityForm DoWork process:" );
      Worker.ReportProgress( 0, Except.Message );
      e.Cancel = true;
      }
    }




  private void LowExponentBackgroundWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
    {
    // This runs in the UI thread.
    if( IsDisposed )
      return;

    if( MForm.GetIsClosing())
      return;

    string CheckStatus = (string)e.UserState;
    if( CheckStatus == null )
      return;

    if( CheckStatus.Length < 1 )
      return;

    ShowStatus( CheckStatus );
    }




  private void LowExponentBackgroundWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
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
    ShowStatus( "Finished low exponent test." );
    ShowStatus( " " );
    }



  }
}

