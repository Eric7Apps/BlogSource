// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // BackgroundWorker
using System.Net;
using System.Threading;
using System.IO;


namespace ExampleServer
{
  class SendCustomerTLSHandshake
  {
  private BackgroundWorker Worker;
  private BackWorkerInfo WInfo;
  private CustomerTLSClient MsgClient;
  private ECTime StartTime;


  private SendCustomerTLSHandshake()
    {

    }



  internal SendCustomerTLSHandshake( BackgroundWorker UseWorker, BackWorkerInfo UseWInfo )
    {
    Worker = UseWorker;
    WInfo = UseWInfo;
    StartTime = new ECTime();
    StartTime.SetToNow();
    MsgClient = new CustomerTLSClient();
    }



  internal void FreeEverything()
    {
    if( MsgClient != null )
      {
      MsgClient.FreeEverything();
      MsgClient = null;
      }
    }



  internal bool Connect()
    {
    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    // Worker.ReportProgress( 0, "Time: " + RightNow.ToLocalTimeString() );

    if( !MsgClient.Connect( WInfo.ServerIPOrDomainName, 443 ))
      {
      string ErrorS = "Can't connect to the server for:\r\n" +
        WInfo.ServerIPOrDomainName + "\r\n" +
        MsgClient.GetStatusString() + "\r\n";

      Worker.ReportProgress( 0, ErrorS );
      Thread.Sleep( 1000 );
      MsgClient.FreeEverything();
      return false;
      }

    Worker.ReportProgress( 0, "Connected to the server at: " + WInfo.ServerIPOrDomainName );
    return true;
    }



  internal bool ExchangeMessages()
    {
    try
    {
    if( MsgClient.IsShutDown())
      {
      Worker.ReportProgress( 0, "The client has been disconnected." );
      return false;
      }

    if( Worker.CancellationPending )
      return false;

    if( !MsgClient.SendCrudeClientHello())
      {
      Worker.ReportProgress( 0, "Could not do SendCrudeClientHello()." );
      Worker.ReportProgress( 0, MsgClient.GetStatusString() );
      return false;
      }

    Worker.ReportProgress( 0, "Sent ClientHello." );

    if( Worker.CancellationPending )
      return false;

    while( true )
      {
      if( Worker.CancellationPending )
        {
        Worker.ReportProgress( 0, "Process was cancelled in the while loop." );
        return false;
        }

      if( MsgClient.IsShutDown() )
        {
        Worker.ReportProgress( 0, "Got disconnected from the server." );
        return false;
        }

      MsgClient.ProcessOuterMessages();
      string ShowS = MsgClient.GetStatusString();
      if( ShowS != "" )
        {
        Worker.ReportProgress( 0, ShowS );
        }

      double HowLong = MsgClient.GetLastReadWriteTimeSecondsToNow();

      if( HowLong > 10 )
        {
        Worker.ReportProgress( 0, "Timed out waiting for the server." );
        Worker.ReportProgress( 0, "Delay time is: " + HowLong.ToString( "N1" ) + " seconds." );
        Thread.Sleep( 1000 );
        return false;
        }

      if( Worker.CancellationPending )
        return false;

      // Give up the time slice.
      Thread.Sleep( 100 );
      }

    // return true;
    }
    catch( Exception )
      {
      Worker.ReportProgress( 0, "Error in ExchangeMessages()." );
      return false;
      }
    }




  }
}



