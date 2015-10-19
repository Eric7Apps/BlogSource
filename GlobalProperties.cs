// Programming by Eric Chauvin.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;


namespace ExampleServer
{
  class GlobalProperties
  {
  private MainForm MForm;
  private ConfigureFile Config;
  private string RSAPrime1 = "";
  private string RSAPrime2 = "";
  private string RSAPubKeyN = "";
  private string RSAPrivKInverseExponent = "";



  internal GlobalProperties( MainForm UseForm )
    {
    MForm = UseForm;

    Config = new ConfigureFile( MForm, Application.StartupPath + "\\Config.txt" );
    ReadAllPropertiesFromConfig();
    }




  internal void ReadAllPropertiesFromConfig()
    {
    RSAPrime1 = Config.GetString( "RSAPrime1" );
    RSAPrime2 = Config.GetString( "RSAPrime2" );
    RSAPubKeyN = Config.GetString( "RSAPubKeyN" );
    RSAPrivKInverseExponent = Config.GetString( "RSAPrivKInverseExponent" );
    }



  internal string GetRSAPrime1()
    {
    return RSAPrime1;
    }


  internal string GetRSAPrime2()
    {
    return RSAPrime2;
    }


  internal string GetRSAPubKeyN()
    {
    return RSAPubKeyN;
    }


  internal string GetRSAPrivKInverseExponent()
    {
    return RSAPrivKInverseExponent;
    }



  internal void SetRSAPrime1( string SetTo )
    {
    RSAPrime1 = SetTo;
    Config.SetString( "RSAPrime1", SetTo );
    Config.WriteToTextFile();
    }


  internal void SetRSAPrime2( string SetTo )
    {
    RSAPrime2 = SetTo;
    Config.SetString( "RSAPrime2", SetTo );
    Config.WriteToTextFile();
    }


  internal void SetRSAPubKeyN( string SetTo )
    {
    RSAPubKeyN = SetTo;
    Config.SetString( "RSAPubKeyN", SetTo );
    Config.WriteToTextFile();
    }


  internal void SetRSAPrivKInverseExponent( string SetTo )
    {
    RSAPrivKInverseExponent = SetTo;
    Config.SetString( "RSAPrivKInverseExponent", SetTo );
    Config.WriteToTextFile();
    }



  }
}


