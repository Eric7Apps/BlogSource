// Programming by Eric Chauvin.
// Notes on this source code are at:
// ericbreakingrsa.blogspot.com



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;



namespace ExampleServer
{
  public partial class ImageDrawForm : Form
  {
  private MainForm MForm;
  private Bitmap ImageBitmap;
  private MultiplyBits MultBits;



  private ImageDrawForm()
    {
    InitializeComponent();
    }


  internal ImageDrawForm( MainForm UseForm )
    {
    InitializeComponent();
    MForm = UseForm;
    MultBits = new MultiplyBits( MForm );
    ImageBitmap = new Bitmap( MForm.GetMainScreenWidth(),
                              MForm.GetMainScreenHeight() );
                              // PixelFormat.Canonical ); // Default 24 bit color.

    DrawTimer.Interval = 200;
    DrawTimer.Start();
    }



  private void testToolStripMenuItem_Click(object sender, EventArgs e)
    {
    // Do profiling and time-testing without the 
    // servers interfering.
    MForm.StopServers();

    }



  private void DrawToBitmap()
    {
    if( ImageBitmap == null )
      return;

    try
    {
    Size Sz = ImagePanel.Size;
    if( Sz.Width < 10 )
      return;

    if( Sz.Height < 10 )
      return;

    using( Graphics BitGraph = Graphics.FromImage( ImageBitmap ))
      {
      if( BitGraph == null )
        {
        MForm.ShowStatus( "BitGraph is null." );
        return;
        }

      BitGraph.Clear( Color.Black );
      MultBits.Draw( BitGraph, Sz.Height, Sz.Width );
      }

    MainPictureBox.Image = ImageBitmap;
    }
    catch( Exception Except )
      {
      // DrawTimer.Stop();
      MForm.ShowStatus( "Exception in DrawToBitmap()." );
      MForm.ShowStatus( Except.Message );
      }
    }



  private void DrawTimer_Tick(object sender, EventArgs e)
    {
    if( MForm.GetCancelled())
      {
      DrawTimer.Stop();
      return;
      }

    try
    {
    DrawTimer.Stop();
    MultBits.TestValues();
    DrawToBitmap();
    DrawTimer.Start();

    }
    catch( Exception Except )
      {
      DrawTimer.Stop();
      MForm.ShowStatus( "Exception in DrawTimer_Tick()." );
      MForm.ShowStatus( Except.Message );
      }
    }



  private void showUnknownsToolStripMenuItem_Click(object sender, EventArgs e)
    {
    MultBits.MakeUnknownsDictionary();
    }



  private void testPrimeToolStripMenuItem_Click(object sender, EventArgs e)
    {
    // 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73,
    // 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131,
    // 137, 139, 149, 151, 157, 163, 167, 173, 179, 181,
    // 191, 193, 197, 199, 211, 223, 227, 229, 233, 239,
    // 241, 251, 257, 263, 269, 271, 277, 281, 283, 293,
    // 307, 311, 313, 317, 331, 337, 347, 349, 353, 359,
    // 367, 373, 379, 383, 389, 397, 401, 409, 419, 421,
    // 431, 433, 439, 443, 449, 457, 461, 463, 467, 479,
    // 487, 491, 499, 503, 509, 521, 523, 541, 

    // Test a composite number too.
    MultBits.SetupFermatLittle( 63 ); // 127
    DrawTimer.Interval = 200;
    DrawTimer.Start();
    }



  private void showInputOutputToolStripMenuItem_Click(object sender, EventArgs e)
    {
    MultBits.ShowInputOutputDictionary();
    }



  private void productToolStripMenuItem_Click(object sender, EventArgs e)
    {
    }



  private void ImageDrawForm_Resize(object sender, EventArgs e)
    {
    DrawToBitmap();
    }


  }
}

