// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace ExampleServer
{
  class QuadResCombinBackground : BackgroundWorker
  {
  private MainForm MForm;
  private string SolutionP = "";
  private string SolutionQ = "";
  private string ProcessName = "No Name";


  private QuadResCombinBackground()
    {
    }


  internal QuadResCombinBackground( MainForm UseForm, QuadResWorkerInfo WInfo )
    {
    MForm = UseForm;

    DoWork += new DoWorkEventHandler( QuadResCombinBackground_DoWork );
    ProgressChanged += new ProgressChangedEventHandler( QuadResCombinBackground_ProgressChanged );
    RunWorkerCompleted += new RunWorkerCompletedEventHandler( QuadResCombinBackground_RunWorkerCompleted );

    WorkerReportsProgress = true;
    WorkerSupportsCancellation = true;

    ProcessName = WInfo.ProcessName;
    }



  private void QuadResCombinBackground_DoWork(object sender, DoWorkEventArgs e)
    {
    if( CancellationPending )
      return;

    if( MForm.GetIsClosing())
      return;

    BackgroundWorker Worker = (BackgroundWorker)sender;
    QuadResWorkerInfo WInfo = (QuadResWorkerInfo)(e.Argument);

    try // catch
    {
    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    QuadResCombinatorics QuadResCombin = new QuadResCombinatorics( WInfo, Worker );
    QuadResCombin.FindTheFactors();

    SolutionP = QuadResCombin.GetSolutionPString();
    SolutionQ = QuadResCombin.GetSolutionQString();

    QuadResCombin.FreeEverything();

    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in QuadResCombinBackground DoWork process:" );
      Worker.ReportProgress( 0, Except.Message );
      e.Cancel = true;
      }
    }



  private void QuadResCombinBackground_ProgressChanged( object sender, ProgressChangedEventArgs e )
    {
    // This runs in the UI thread.
    if( CancellationPending )
      return;

    if( MForm.GetIsClosing())
      return;

    string CheckStatus = (string)e.UserState;
    if( CheckStatus == null )
      return;

    if( CheckStatus.Length < 1 )
      return;

    // if( e.ProgressPercentage > 0 )

    if( CheckStatus.Trim().Length < 1 )
      MForm.ShowQuadResFormStatus( CheckStatus );
    else
      MForm.ShowQuadResFormStatus( ProcessName + ") " + CheckStatus );

    }



  private void QuadResCombinBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
    if( CancellationPending )
      return;

    if( MForm.GetIsClosing())
      return;

    if( e.Cancelled )
      {
      MForm.ShowQuadResFormStatus( "Cancelled." );
      return;
      }

    // MForm.ShowQuadResFormStatus( " " );
    MForm.ShowQuadResFormStatus( "Finished Background process." );
    // MForm.ShowQuadResFormStatus( "SolutionP: " + SolutionP );

    // The string is either "", or it's "0", or it's a real solution.
    if( SolutionP.Length > 1 )
      {
      MForm.FoundSolutionShutDownQuadRes( ProcessName, SolutionP, SolutionQ );
      }
    }



  }
}

