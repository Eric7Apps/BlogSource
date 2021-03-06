/*
Obsolete and not used anymore.


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
  class CRTCombinBackground : BackgroundWorker
  {
  private MainForm MForm;
  private string SolutionP = "";
  private string SolutionQ = "";
  private string ProcessName = "No Name";


  private CRTCombinBackground()
    {
    }


  internal CRTCombinBackground( MainForm UseForm, MakeKeysWorkerInfo WInfo )
    {
    MForm = UseForm;

    DoWork += new DoWorkEventHandler( CRTCombinBackground_DoWork );
    ProgressChanged += new ProgressChangedEventHandler( CRTCombinBackground_ProgressChanged );
    RunWorkerCompleted += new RunWorkerCompletedEventHandler( CRTCombinBackground_RunWorkerCompleted );

    WorkerReportsProgress = true;
    WorkerSupportsCancellation = true;

    ProcessName = WInfo.ProcessName;
    }



  private void CRTCombinBackground_DoWork(object sender, DoWorkEventArgs e)
    {
    if( CancellationPending )
      return;

    if( MForm.GetIsClosing())
      return;

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

    SolutionP = FindFac.GetSolutionPString();
    SolutionQ = FindFac.GetSolutionQString();

    FindFac.FreeEverything();

    if( Worker.CancellationPending )
      {
      e.Cancel = true;
      return;
      }

    }
    catch( Exception Except )
      {
      Worker.ReportProgress( 0, "Error in CRTCombinBackground DoWork process:" );
      Worker.ReportProgress( 0, Except.Message );
      e.Cancel = true;
      }
    }



  private void CRTCombinBackground_ProgressChanged( object sender, ProgressChangedEventArgs e )
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
      MForm.ShowMakeKeysFormStatus( CheckStatus );
    else
      MForm.ShowMakeKeysFormStatus( ProcessName + ") " + CheckStatus );

    }



  private void CRTCombinBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
    if( CancellationPending )
      return;

    if( MForm.GetIsClosing())
      return;

    if( e.Cancelled )
      {
      MForm.ShowMakeKeysFormStatus( "Cancelled." );
      return;
      }

    MForm.ShowMakeKeysFormStatus( " " );
    MForm.ShowMakeKeysFormStatus( "Finished Background process." );
    MForm.ShowMakeKeysFormStatus( "SolutionP: " + SolutionP );

    // The string is either "", or it's "0", or it's a real solution.
    if( SolutionP.Length > 1 )
      {
      MForm.FoundSolutionShutDown( ProcessName, SolutionP, SolutionQ );
      }
    }



  }
}
*/

