// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;



namespace ExampleServer
{
  class TLSClient
  {
  private MainForm MForm;
  private TLSTCPClient TLSTCPClient1;
  private string RemoteAddress = "";



  private TLSClient()
    {
    }


  internal TLSClient( MainForm UseForm, TcpClient UseClient, string UseAddress )
    {
    MForm = UseForm;
    RemoteAddress = UseAddress;

    TLSTCPClient1 = new TLSTCPClient( MForm, UseClient, RemoteAddress );
    }



  internal void FreeEverything()
    {
    if( TLSTCPClient1 != null )
      {
      TLSTCPClient1.FreeEverything();
      }
    }



  internal bool IsProcessingInBackground()
    {
    if( TLSTCPClient1 == null )
      return false;

    return TLSTCPClient1.IsProcessingInBackground();
    }


  internal bool ProcessOuterMessages()
    {
    if( TLSTCPClient1 == null )
      return false;

    return TLSTCPClient1.ProcessOuterMessages();
    }


  internal ulong GetLastTransactTimeIndex()
    {
    if( TLSTCPClient1 == null )
      return 0;

    return TLSTCPClient1.GetLastTransactTimeIndex();
    }




  internal bool IsShutDown()
    {
    if( TLSTCPClient1 == null )
      return true;

    return TLSTCPClient1.IsShutDown();
    }



  internal string GetRemoteAddress()
    {
    return RemoteAddress;
    }



  }
}

