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

    DrawTimer.Interval = 500;
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

    // Font MainFont = new Font( FontFamily.GenericSansSerif,
    //                          14.0F, 
    //                          FontStyle.Regular,
    //                          GraphicsUnit.Pixel );

      MultBits.Draw( BitGraph, Sz.Height, Sz.Width );
      }

    MainPictureBox.Image = ImageBitmap;
    }
    catch( Exception ) // Except )
      {
      return;
      }
    }



  private void DrawTimer_Tick(object sender, EventArgs e)
    {
    if( MForm.GetCancelled())
      {
      DrawTimer.Stop();
      return;
      }

    DrawToBitmap();
    }



  }
}
