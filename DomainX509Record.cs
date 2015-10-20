// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://eric7apps.blogspot.com/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExampleServer
{
  class DomainX509Record
  {
  private MainForm MForm;
  // internal int Rank = 0; // Where it is ranked in popularity.  Google is 1.
  internal string DomainName = "";
  private ECTime ModifyTime;
     // This is the most recent certificate list
     // from the Server Certificate Message.
  private byte[] X509CertificateList;



  private DomainX509Record()
    {
    }



  internal DomainX509Record( MainForm UseForm )
    {
    MForm = UseForm;
    ModifyTime = new ECTime();
    }


  internal ulong GetModifyTimeIndex()
    {
    return ModifyTime.GetIndex();
    }


  internal bool ImportOriginalStringToObject( string Line )
    {
    try
    {
    if( Line.Length < 2 )
      return false;

    string[] SplitS = Line.Split( new Char[] { ',' } );
    if( SplitS.Length < 2 )
      return false;

    // int Field = 0;
    // Rank = Int32.Parse( SplitS[0] );
    DomainName = SplitS[1].Trim().ToLower();

    return true;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus("Exception in DomainX509Record.StringToObject()." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  }
}
