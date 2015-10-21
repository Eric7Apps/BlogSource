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



namespace ExampleServer
{

  public struct BackWorkerInfo
    {
    public string ServerIPOrDomainName;
    }


  public partial class CustomerMessageForm : Form
  {
  private MainForm MForm;


  private CustomerMessageForm()
    {
    InitializeComponent();
    }


  internal CustomerMessageForm( MainForm UseForm )
    {
    InitializeComponent();

    MForm = UseForm;

    GetX509BackgroundWorker.WorkerReportsProgress = true;
    GetX509BackgroundWorker.WorkerSupportsCancellation = true;
    }


  private void testToolStripMenuItem_Click(object sender, EventArgs e)
    {
    StartSendingForX509();
    }


  internal void ShowStatus( string Status )
    {
    if( IsDisposed )
      return;

    if( MForm.GetIsClosing() )
      return;

    if( MainTextBox.Text.Length > (80 * 5000))
      MainTextBox.Text = "";

    MainTextBox.AppendText( Status + "\r\n" ); 
    }



  internal void FreeEverything()
    {
    if( IsDisposed )
      return;

    try
    {
    if( GetX509BackgroundWorker.IsBusy )
      {
      if( !GetX509BackgroundWorker.CancellationPending )
        GetX509BackgroundWorker.CancelAsync();

      }
    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in FreeEverything():" );
      ShowStatus( Except.Message );
      }
    }



  private void CustomerMessageForm_FormClosing(object sender, FormClosingEventArgs e)
    {
    e.Cancel = true;
    Hide();
    }



  internal void StartSendingForX509()
    {
    if( IsDisposed )
      return;

    ShowStatus( "Starting to send for X.509 data." );
    BackWorkerInfo WInfo = new BackWorkerInfo();
    WInfo.ServerIPOrDomainName = MForm.X509Data.GetRandomDomainName();

    try
    {
    if( !GetX509BackgroundWorker.IsBusy )
      {
      GetX509BackgroundWorker.RunWorkerAsync( WInfo );
      }
    else
      {
      ShowStatus( "Background process is busy for X.509 data." );
      }
    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in StartSendingForX509():" );
      ShowStatus( Except.Message );
      return;
      }
    }



  private void GetX509BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
    BackgroundWorker Worker = (BackgroundWorker)sender;
    BackWorkerInfo WInfo = (BackWorkerInfo)(e.Argument);
    
    try // catch
    {
    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }
    
    SendCustomerTLSHandshake SendMessage = new SendCustomerTLSHandshake( Worker, WInfo );
    try // finally
    {

    if( !SendMessage.Connect())
      return;

    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    Worker.ReportProgress( 0, "Before SendMessage.ExchangeMessages()." );
    if( SendMessage.ExchangeMessages())
      {
      // Do something.
      }
    }
    finally
      {
      Worker.ReportProgress( 0, "Closing the connection." );
      SendMessage.FreeEverything();
      }
    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in DoWork process:" );
      Worker.ReportProgress( 0, Except.Message );
      e.Cancel = true;
      return;
      }
    }



  private void GetX509BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
    // This runs in the UI thread.
    if( IsDisposed )
      return;

    string CheckStatus = (string)e.UserState;
    if( CheckStatus == null )
      return;

    if( CheckStatus.Length < 1 )
      return;

    ShowStatus( CheckStatus );
    }



  private void GetX509BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
    if( IsDisposed )
      return;

    if( MForm.GetIsClosing())
      return;

    if( e.Cancelled )
      {
      ShowStatus( "Background worker was cancelled." );
      return;
      }

    ShowStatus( "Background worker is completed." );

    // e.Error
    // e.Result
    // e.UserState
    }




  }
}
