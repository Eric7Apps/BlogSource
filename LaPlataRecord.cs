// Programming by Eric Chauvin.
// Notes on this source code are at:
// http://ericsoftwarenotes.blogspot.com/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



namespace ExampleServer
{
  class LaPlataRecord
  {
  private MainForm MForm;
  internal string APN = "";             // 0
  internal string OWNER = "";           // 1
  internal string PREVIOUS_A = "";      // 2
  internal string LAST3 = "";           // 3
  internal string ACRES_CALC = "";      // 4
  internal string DeadCardId = "";      // 5
  internal string YellowCard = "";      // 6
  internal string Shape_STAr = "";      // 7
  internal string Shape_STLe = "";      // 8
  internal string APN1 = "";            // 9
  internal string ACCOUNT_NO = "";      // 10
  internal string SITE_ADDR = "";       // 11
  internal string TAX_DIST = "";        // 12
  internal string NAME = "";            // 13
  internal string ADDR_1 = "";          // 14
  internal string ADDR_2 = "";          // 15
  internal string CITY = "";            // 16
  internal string STATE = "";           // 17
  internal string ZIP = "";             // 18
  internal string MAPNO = "";           // 19
  internal string STREET_NO = "";       // 20
  internal string STREET_DIR = "";      // 21
  internal string STREET_NM = "";       // 22
  internal string STREETSUF = "";       // 23
  internal string UNIT_NO = "";         // 24
  internal string SITE_CITY = "";       // 25
  internal string SITE_ZIP = "";        // 26
  internal string LEGALT = "";          // 27
  internal string APRDIST = "";         // 28
  internal string SUBCODE = "";         // 29
  internal string SUBNAME = "";         // 30
  internal string BLOCK = "";           // 31
  internal string LOT = "";             // 32
  internal string CONDOCODE = "";       // 33
  internal string CONDONAME = "";       // 34
  internal string CONDOUNIT = "";       // 35
  internal string RECEPTION = "";       // 36
  internal string SALEDT = "";          // 37
  internal string DEEDTYPE = "";        // 38
  internal string DOCFEE = "";          // 39
  internal string SALEP = "";           // 40
  internal string ACCTTYPE = "";        // 41
  internal string LAND_IMP = "";        // 42
  internal string LANDACT = "";         // 43
  internal string IMPACT = "";          // 44
  internal string LANDASD = "";         // 45
  internal string IMPASD = "";          // 46
  internal string ACRES = "";           // 47
  internal string LANDSQFT = "";        // 48
  internal string IMPSQFT = "";         // 49
  internal string ARCH_STYLE = "";      // 50
  internal string YR_BLT = "";          // 51
  internal string USE_CODE = "";        // 52
  internal string INTERNET = "";        // 53
  internal string LAST3_1 = "";         // 54
  internal string Shape_Leng = "";      // 55
  internal string Shape_Area = "";      // 56



  private LaPlataRecord()
    {
    }


  internal LaPlataRecord( MainForm UseForm )
    {
    MForm = UseForm;
    }


  internal bool StringToObject( string Line )
    {
    try
    {
    if( Line.Length < 2 )
      return false;

    string[] SplitS = Line.Split( new Char[] { '\t' } );
    if( SplitS.Length < 57 )
      return false;

    int Field = 0;
    APN = SplitS[Field].Trim();
    Field++;
    OWNER = SplitS[Field].Trim();           // 1
    Field++;
    PREVIOUS_A = SplitS[Field].Trim();      // 2
    Field++;
    LAST3 = SplitS[Field].Trim();           // 3
    Field++;
    ACRES_CALC = SplitS[Field].Trim();      // 4
    Field++;
    DeadCardId = SplitS[Field].Trim();      // 5
    Field++;
    YellowCard = SplitS[Field].Trim();      // 6
    Field++;
    Shape_STAr = SplitS[Field].Trim();      // 7
    Field++;
    Shape_STLe = SplitS[Field].Trim();      // 8
    Field++;
    APN1 = SplitS[Field].Trim();            // 9
    Field++;
    ACCOUNT_NO = SplitS[Field].Trim();      // 10
    Field++;
    SITE_ADDR = SplitS[Field].Trim();       // 11
    Field++;
    TAX_DIST = SplitS[Field].Trim();        // 12
    Field++;
    NAME = SplitS[Field].Trim();            // 13
    Field++;
    ADDR_1 = SplitS[Field].Trim();          // 14
    Field++;
    ADDR_2 = SplitS[Field].Trim();          // 15
    Field++;
    CITY = SplitS[Field].Trim();            // 16
    Field++;
    STATE = SplitS[Field].Trim();           // 17
    Field++;
    ZIP = SplitS[Field].Trim();             // 18
    Field++;
    MAPNO = SplitS[Field].Trim();           // 19
    Field++;
    STREET_NO = SplitS[Field].Trim();       // 20
    Field++;
    STREET_DIR = SplitS[Field].Trim();      // 21
    Field++;
    STREET_NM = SplitS[Field].Trim();       // 22
    Field++;
    STREETSUF = SplitS[Field].Trim();       // 23
    Field++;
    UNIT_NO = SplitS[Field].Trim();         // 24
    Field++;
    SITE_CITY = SplitS[Field].Trim();       // 25
    Field++;
    SITE_ZIP = SplitS[Field].Trim();        // 26
    Field++;
    LEGALT = SplitS[Field].Trim();          // 27
    Field++;
    APRDIST = SplitS[Field].Trim();         // 28
    Field++;
    SUBCODE = SplitS[Field].Trim();         // 29
    Field++;
    SUBNAME = SplitS[Field].Trim();         // 30
    Field++;
    BLOCK = SplitS[Field].Trim();           // 31
    Field++;
    LOT = SplitS[Field].Trim();             // 32
    Field++;
    CONDOCODE = SplitS[Field].Trim();       // 33
    Field++;
    CONDONAME = SplitS[Field].Trim();       // 34
    Field++;
    CONDOUNIT = SplitS[Field].Trim();       // 35
    Field++;
    RECEPTION = SplitS[Field].Trim();       // 36
    Field++;
    SALEDT = SplitS[Field].Trim();          // 37
    Field++;
    DEEDTYPE = SplitS[Field].Trim();        // 38
    Field++;
    DOCFEE = SplitS[Field].Trim();          // 39
    Field++;
    SALEP = SplitS[Field].Trim();           // 40
    Field++;
    ACCTTYPE = SplitS[Field].Trim();        // 41
    Field++;
    LAND_IMP = SplitS[Field].Trim();        // 42
    Field++;
    LANDACT = SplitS[Field].Trim();         // 43
    Field++;
    IMPACT = SplitS[Field].Trim();          // 44
    Field++;
    LANDASD = SplitS[Field].Trim();         // 45
    Field++;
    IMPASD = SplitS[Field].Trim();          // 46
    Field++;
    ACRES = SplitS[Field].Trim();           // 47
    Field++;
    LANDSQFT = SplitS[Field].Trim();        // 48
    Field++;
    IMPSQFT = SplitS[Field].Trim();         // 49
    Field++;
    ARCH_STYLE = SplitS[Field].Trim();      // 50
    Field++;
    YR_BLT = SplitS[Field].Trim();          // 51
    Field++;
    USE_CODE = SplitS[Field].Trim();        // 52
    Field++;
    INTERNET = SplitS[Field].Trim();        // 53
    Field++;
    LAST3_1 = SplitS[Field].Trim();         // 54
    Field++;
    Shape_Leng = SplitS[Field].Trim();      // 55
    Field++;
    Shape_Area = SplitS[Field].Trim();      // 56
    return true;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus("Exception in LaPlataRecord.StringToObject()." );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }


  }
}


