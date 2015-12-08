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

  internal struct QuadResWorkerInfo
    {
    // internal CRTCombinSetupRec[] SetupArray;
    internal string PublicKeyModulus;
    internal uint ModMask;
    internal string ProcessName;
    }


  public partial class QuadResSearchForm : Form
  {
  private MainForm MForm;
  private bool Cancelled = false;
  private QuadResCombinBackground[] QuadResCombinBackgArray;



  private QuadResSearchForm()
    {
    InitializeComponent();
    }


  internal QuadResSearchForm( MainForm UseForm )
    {
    InitializeComponent();

    MForm = UseForm;
    }



  private void testToolStripMenuItem_Click( object sender, EventArgs e )
    {
    Cancelled = false;

    try
    {
    // Some test numbers:
    // string PubKey =                        "429,606,691,379";
    // string PubKey =                      "9,073,276,189,777";
    // string PubKey =                "243,047,686,576,334,411";
    // string PubKey =         "15,654,675,243,543,711,381,029";
    // string PubKey =     "28,569,209,393,580,650,250,552,443";
    // string PubKey =  "1,646,567,279,211,844,205,225,474,329";
     string PubKey =  "2,171,587,791,847,501,194,011,797,973";
    // string PubKey =  "9,642,014,564,948,464,387,250,299,211";
    // string PubKey = "56,486,207,148,903,090,249,668,503,717";

    // RSA-100
    // string PubKey = "15226050279225333605356183781326374297180681149613" +
    //                 "80688657908494580122963258952897654000350692006139";


    // RSA-2048
    /*
    string PubKey = "2519590847565789349402718324004839857142928212620403202777713783604366202070" +
           "7595556264018525880784406918290641249515082189298559149176184502808489120072" +
           "8449926873928072877767359714183472702618963750149718246911650776133798590957" +
           "0009733045974880842840179742910064245869181719511874612151517265463228221686" +
           "9987549182422433637259085141865462043576798423387184774447920739934236584823" +
           "8242811981638150106748104516603773060562016196762561338441436038339044149526" +
           "3443219011465754445417842402092461651572335077870774981712577246796292638635" +
           "6373289912154831438167899885040445364023527381951378636564391212010397122822" +
           "120720357";
      */

    // Four threads:
    QuadResCombinBackgArray = new QuadResCombinBackground[4];

    // Some primes:
    // 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15
    // 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53,

    // 16  17  18  19  20  21  22  23  24   25   26   27   28   29
    // 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113,

    //  30   31   32   33   34   35   36   37   38   39   40   41
    // 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181,

    //  42   43   44   45   46   47   48   49   50   51   52   53
    // 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251,

    //  54   55   56   57   58   59   60   61   62   63   64   65
    // 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317,

    //  66   67   68   69   70   71   72   73   74   75   76   77
    // 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397,


    QuadResWorkerInfo WInfo = new QuadResWorkerInfo();
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 0;
    WInfo.ProcessName = "Process 0";
    QuadResCombinBackgArray[0] = new QuadResCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 0" );
    QuadResCombinBackgArray[0].RunWorkerAsync( WInfo );


    ////////////////
    WInfo = new QuadResWorkerInfo();
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 1;
    WInfo.ProcessName = "Process 1";

    QuadResCombinBackgArray[1] = new QuadResCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 1" );
    QuadResCombinBackgArray[1].RunWorkerAsync( WInfo );


    ////////
    WInfo = new QuadResWorkerInfo();
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 2;
    WInfo.ProcessName = "Process 2";

    QuadResCombinBackgArray[2] = new QuadResCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 2" );
    QuadResCombinBackgArray[2].RunWorkerAsync( WInfo );


    ////////
    WInfo = new QuadResWorkerInfo();
    WInfo.PublicKeyModulus = PubKey;
    WInfo.ModMask = 3;
    WInfo.ProcessName = "Process 3";

    QuadResCombinBackgArray[3] = new QuadResCombinBackground( MForm, WInfo );
    ShowStatus( "Starting background process at: 3" );
    QuadResCombinBackgArray[3].RunWorkerAsync( WInfo );

    }
    catch( Exception Except )
      {
      ShowStatus( "Exception in testToolStripMenuItem_Click():\r\n" + Except.Message );
      }
    }



  internal void FreeEverything()
    {
    if( IsDisposed )
      return;

    if( QuadResCombinBackgArray != null )
      {
      try
      {
      for( int Count = 0; Count < QuadResCombinBackgArray.Length; Count++ )
        {
        if( QuadResCombinBackgArray[Count] == null )
          continue;

        if( QuadResCombinBackgArray[Count].IsBusy )
          {
          if( !QuadResCombinBackgArray[Count].CancellationPending )
            QuadResCombinBackgArray[Count].CancelAsync();

          }

        QuadResCombinBackgArray[Count].Dispose();
        QuadResCombinBackgArray[Count] = null;
        }
      }
      catch( Exception Except )
        {
        ShowStatus( "Exception for QuadResCombinBackgArray[] on closing:\r\n" + Except.Message );
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



  private void QuadResSearchForm_KeyDown( object sender, KeyEventArgs e )
    {
    if( e.KeyCode == Keys.Escape ) //  && (e.Alt || e.Control || e.Shift))
      {
      ShowStatus( "Cancelled." );
      Cancelled = true;
      FreeEverything(); // Stop background process.
      }
    }



  private void QuadResSearchForm_FormClosing( object sender, FormClosingEventArgs e )
    {
    e.Cancel = true;
    Hide();
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

    if( QuadResCombinBackgArray == null )
      return;

    for( int Count = 0; Count < QuadResCombinBackgArray.Length; Count++ )
      {
      try
      {
      if( QuadResCombinBackgArray[Count] == null )
        continue;

      if( QuadResCombinBackgArray[Count].IsBusy )
        {
        if( !QuadResCombinBackgArray[Count].CancellationPending )
          QuadResCombinBackgArray[Count].CancelAsync();

        }

      QuadResCombinBackgArray[Count].Dispose();
      QuadResCombinBackgArray[Count] = null;
      }
      catch( Exception Except )
        {
        ShowStatus( "Exception for FoundSolutionShutDown():\r\n" + Except.Message );
        }
      }
    }


  }
}
