// Programming by Eric Chauvin.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;



namespace ExampleServer
{
  public struct IPStatusRec
    {
    public ulong TimeIndex;
    public int HackerCount;
    public int TimedOutCount;
    public int BadWebPageCount;
    public string SentText;
    public long Port80Count;
    public long Port443Count;
    public int GoodWebRequestCount;
    public int TotalGoodWebRequestCount;
    public string Referer;
    public string UserAgent;
    public string HostName;
    public ulong LastHostNameUpdate;
    public int GetPublicKeyCount;
    public int TotalGetPublicKeyCount;
    public int ConfirmBillingCount;
    public int TotalConfirmBillingCount;
    }


  class NetIPStatus
  {
  private MainForm MForm;
  private SortedDictionary<string, IPStatusRec> IPsDictionary;
  private SortedDictionary<string, int> RefererDictionary;
  private SortedDictionary<string, int> UserAgentDictionary;


  private NetIPStatus()
    {

    }


  internal NetIPStatus( MainForm UseForm )
    {
    MForm = UseForm;

    IPsDictionary = new SortedDictionary<string, IPStatusRec>();
    RefererDictionary = new SortedDictionary<string, int>();
    UserAgentDictionary = new SortedDictionary<string, int>();
    }


  // This is unnecessary in managed code if this is all it does, since it will
  // be set to zeros.
  private IPStatusRec MakeNewStatusRec()
    {
    IPStatusRec StatusRec = new IPStatusRec();
    StatusRec.TimeIndex = 0;
    StatusRec.Port80Count = 0;
    StatusRec.Port443Count = 0;
    StatusRec.SentText = "";
    StatusRec.HackerCount = 0;
    StatusRec.TimedOutCount = 0;
    StatusRec.BadWebPageCount = 0;
    StatusRec.GoodWebRequestCount = 0;
    StatusRec.Referer = "";
    StatusRec.UserAgent = "";
    StatusRec.HostName = "";
    StatusRec.LastHostNameUpdate = 0;
    StatusRec.GetPublicKeyCount = 0;
    StatusRec.TotalGetPublicKeyCount = 0;
    StatusRec.ConfirmBillingCount = 0;
    StatusRec.TotalConfirmBillingCount = 0;

    return StatusRec;
    }



  internal bool IsBlockedAddress( string IP )
    {
    // There might be lots of users all coming from the same IP address, like from
    // behind a corporate firewall, but this is used to put some reasonable limits
    // on what can happen from a specific IP address.  So you can pick what you
    // think reasonable limits would be.

    try
    {
    if( IsBadIP( IP ))
      {
      return true;
      }

    if( !IPsDictionary.ContainsKey( IP ))
      return false; // It's an unknown address so it can't be blocked.

    IPStatusRec StatusRec = IPsDictionary[IP];

    if( StatusRec.ConfirmBillingCount > 100 )
      return true;

    if( StatusRec.GetPublicKeyCount > 1000 )
      return true;

    if( StatusRec.TimedOutCount > 2000 )
      return true;

    // Are they just randomly creating file names to see what's on there?
    if( StatusRec.BadWebPageCount > 100 )
      return true;

    if( StatusRec.HackerCount > 100 )
      return true;

    // 10 web pages per minute for a whole day?
    if( StatusRec.GoodWebRequestCount > (60 * 24 * 10) )
      return true;


    return false;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in IsBlockedAddress():" );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  private bool IsBadIP( string IP )
    {
    // This is hardcoded here, but you can keep a list of IP addresses you 
    // want to block, like from known scam-service-providers.
    // if( IP.StartsWith( "1.2.3." ))
      // return true;

    return false;
    }




  internal void AddToPort80Count( string IP )
    {
    if( IsBadIP( IP ))
      return;

    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( IP ))
      StatusRec = IPsDictionary[IP];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.Port80Count++;

    IPsDictionary[IP] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToPort80Count():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToPort443Count( string IP )
    {
    if( IsBadIP( IP ))
      return;

    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( IP ))
      StatusRec = IPsDictionary[IP];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.Port443Count++;

    IPsDictionary[IP] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToPort80Count():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToConfirmBillingCount( string Address )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.ConfirmBillingCount++;
    StatusRec.TotalConfirmBillingCount++;

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToConfirmBillingCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToGetPublicKeyCount( string Address )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.GetPublicKeyCount++;
    StatusRec.TotalGetPublicKeyCount++;

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToGetPublicKeyCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToGoodWebRequestCount( string Address, string SentText )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.GoodWebRequestCount++;
    StatusRec.TotalGoodWebRequestCount++;

    StatusRec.SentText = Utility.CleanAsciiString( SentText, 2048 );

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToGoodWebRequestCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToTimedOutCount( string Address, string SentText )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.TimedOutCount++;
    StatusRec.SentText = Utility.CleanAsciiString( SentText, 2048 );

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToTimedOutCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToBadWebPageCount( string Address, string SentText )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.BadWebPageCount++;
    StatusRec.SentText = Utility.CleanAsciiString( SentText, 2048 );

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToBadWebPageCountCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToHackerCount( string Address, string SentText )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.HackerCount++;
    StatusRec.SentText = Utility.CleanAsciiString( SentText, 2048 );

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToHackerCount():" );
      MForm.ShowStatus( Except.Message );
      }
    }




  internal void ShowIPStatus()
    {
    ECTime RecTime = new ECTime();
    MForm.ShowStatus( " " );

    string ShowS = "IP Address\t" +
                   "Server Time\t" +
                   "Server Date\t" +
                   "Hacker Count\t" +
                   "Timed Out Count\t" +
                   "Bad Web Page Count\t" +
                   "Good Web Request Count\t" +
                   "Total Good Web Request Count\t" +
                   "Port 80 Count\t" +
                   "GetPublicKeyCount\t" +
                   "TotalGetPublicKeyCount\t" +
                   "ConfirmBillingCount\t" +
                   "TotalConfirmBillingCount\t" +
                   "Referrer\t" +
                   "User Agent\t" +
                   "Host Name\t" +
                   "Sent Text";



    MForm.ShowStatus( ShowS );

    int HowMany = 0;
    foreach( KeyValuePair<string, IPStatusRec> Kvp in IPsDictionary )
      {
      // Don't show too many.  This is only meant as a quick way to show
      // some records.
      HowMany++;
      if( HowMany > 1000 )
        {
        MForm.ShowStatus( " " );
        MForm.ShowStatus( "Can't show more than 1000 IPs." );
        MForm.ShowStatus( " " );
        return;
        }

      RecTime.SetFromIndex( Kvp.Value.TimeIndex );

      ShowS = Kvp.Key + "\t" +
         RecTime.ToLocalTimeString() + "\t" +
         RecTime.ToLocalDateString() + "\t" +
         Kvp.Value.HackerCount.ToString( "N0" ) + "\t" +
         Kvp.Value.TimedOutCount.ToString( "N0" ) + "\t" +
         Kvp.Value.BadWebPageCount.ToString( "N0" ) + "\t" +
         Kvp.Value.GoodWebRequestCount.ToString( "N0" ) + "\t" +
         Kvp.Value.TotalGoodWebRequestCount.ToString( "N0" ) + "\t" +
         Kvp.Value.Port80Count.ToString( "N0" ) + "\t" +
         Kvp.Value.Port443Count.ToString( "N0" ) + "\t" +
         Kvp.Value.GetPublicKeyCount.ToString( "N0" ) + "\t" +
         Kvp.Value.TotalGetPublicKeyCount.ToString( "N0" ) + "\t" +
         Kvp.Value.ConfirmBillingCount.ToString( "N0" ) + "\t" +
         Kvp.Value.TotalConfirmBillingCount.ToString( "N0" ) + "\t" +
         Utility.CleanAsciiString( Kvp.Value.Referer, 300 ) + "\t" +
         Utility.CleanAsciiString( Kvp.Value.UserAgent, 300 ) + "\t" +
         Utility.CleanAsciiString( Kvp.Value.HostName, 300 ) + "\t" +
         Utility.CleanAsciiString( Kvp.Value.SentText, 300 );

      MForm.ShowStatus( ShowS );
      }

    MForm.ShowStatus( " " );
    }




  internal void ShowUserAgents()
    {
    ECTime RecTime = new ECTime();
    MForm.ShowStatus( " " );

    MForm.ShowStatus( "User Agents:" );

    foreach( KeyValuePair<string, int> Kvp in UserAgentDictionary )
      {
      string ShowS = Kvp.Key + "\t" +
         Kvp.Value.ToString();

      MForm.ShowStatus( ShowS );
      }

    MForm.ShowStatus( " " );
    }



  internal void ShowReferers()
    {
    ECTime RecTime = new ECTime();
    MForm.ShowStatus( " " );

    MForm.ShowStatus( "Referers:" );

    foreach( KeyValuePair<string, int> Kvp in RefererDictionary )
      {
      string ShowS = Kvp.Key + "\t" +
         Kvp.Value.ToString();

      MForm.ShowStatus( ShowS );
      }

    MForm.ShowStatus( " " );
    }



  internal void ClearIPStatus()
    {
    try
    {
    IPsDictionary.Clear();
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in IPsDictionary.Clear():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal void AddToUserAgentAndReferer( string Address, string Referer, string UserAgent )
    {
    try
    {
    Referer = Utility.CleanAsciiString( Referer, 1000 );
    UserAgent = Utility.CleanAsciiString( UserAgent, 1000 );

    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( Address ))
      StatusRec = IPsDictionary[Address];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.Referer = Utility.CleanAsciiString( Referer, 2048 );
    StatusRec.UserAgent = Utility.CleanAsciiString( UserAgent, 2048 );

    if( RefererDictionary.ContainsKey( Referer ))
      RefererDictionary[Referer] = RefererDictionary[Referer] + 1;
    else
      RefererDictionary[Referer] = 1;

    if( UserAgentDictionary.ContainsKey( UserAgent ))
      UserAgentDictionary[UserAgent] = UserAgentDictionary[UserAgent] + 1;
    else
      UserAgentDictionary[UserAgent] = 1;

    IPsDictionary[Address] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in AddToUserAgentAndReferer():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal bool SaveToFile()
    {
    string FileName = MForm.GetDataDirectory() + "NetStats.txt";

    ECTime RecTime = new ECTime();
    // OldTime.SetToYear1999();
    // ulong OldIndex = OldTime.GetIndex();

    try
    {
    using( StreamWriter SWriter = new StreamWriter( FileName  )) 
      {
      foreach( KeyValuePair<string, IPStatusRec> Kvp in IPsDictionary )
        {
        RecTime.SetFromIndex( Kvp.Value.TimeIndex );
        
        string Line = Kvp.Key + "\t" +
          RecTime.ToLocalTimeString() + "\t" +
          RecTime.ToLocalDateString() + "\t" +
          Kvp.Value.TimeIndex.ToString() + "\t" +
          Kvp.Value.HackerCount.ToString() + "\t" +
          Kvp.Value.TimedOutCount.ToString() + "\t" +
          Kvp.Value.GoodWebRequestCount.ToString() + "\t" +
          Kvp.Value.TotalGoodWebRequestCount.ToString() + "\t" +
          Kvp.Value.Port80Count.ToString() + "\t" +
          Kvp.Value.Port443Count.ToString() + "\t" +
          Kvp.Value.Referer + "\t" +
          Kvp.Value.UserAgent + "\t" +
          Kvp.Value.HostName + "\t" +
          Kvp.Value.LastHostNameUpdate.ToString() + "\t" +
          Kvp.Value.BadWebPageCount.ToString(); //  + "\t" +
          // Kvp.Value.SentText;

        SWriter.WriteLine( Line );
        }

      SWriter.WriteLine( " " );
      }
    
    return true;
    
    }
    catch( Exception ) //  Except )
      {
      // "Error: Could not write
      return false;
      }
    }




  internal bool ReadFromFile()
    {
    IPsDictionary.Clear();
    RefererDictionary.Clear();
    UserAgentDictionary.Clear();

    string FileName = MForm.GetDataDirectory() + "NetStats.txt";
    // ECTime RecTime = new ECTime();

    try
    {
    string Line;
    using( StreamReader SReader = new StreamReader( FileName  )) 
      {
      while( SReader.Peek() >= 0 ) 
        {
        Line = SReader.ReadLine();
        if( Line == null )
          continue;
          
        // Keep the tabs here.
        // Line = Line.Trim();
        if( Line.Length < 3 )
          continue;
        
        if( !Line.Contains( "\t" ))
          continue;

        string[] SplitS = Line.Split( new Char[] { '\t' } );
        if( SplitS.Length < 23 )
          continue;

        string Key = SplitS[0].Trim();
        if( Key.Length < 7 ) // 1.2.3.4
          continue;

        IPStatusRec StatusRec = new IPStatusRec();
        try
        {
        int Field = 3;
        StatusRec.TimeIndex = (ulong)Int64.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.HackerCount = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.TimedOutCount = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.GoodWebRequestCount = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.TotalGoodWebRequestCount = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.Port80Count = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.Port443Count = Int32.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.Referer = SplitS[Field].Trim();
        Field++;
        StatusRec.UserAgent = SplitS[Field].Trim();
        Field++;
        StatusRec.HostName = SplitS[Field].Trim();
        Field++;
        StatusRec.LastHostNameUpdate = (ulong)Int64.Parse( SplitS[Field].Trim() );
        Field++;
        StatusRec.BadWebPageCount = Int32.Parse( SplitS[Field].Trim() );
        // Field++;

        }
        catch( Exception )
          {
          continue;
          }

        IPsDictionary[Key] = StatusRec;
        }
      }

    return true;

    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Could not read the NetStats file." + "\r\n" + Except.Message );
      return false;
      }
    }



  internal ulong GetLastHostNameUpdate( string IP )
    {
    if( !IPsDictionary.ContainsKey( IP ))
      return 0;

    return IPsDictionary[IP].LastHostNameUpdate;
    }



  internal void UpdateHostNameCheckTime( string IP )
    {
    try
    {
    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( IP ))
      StatusRec = IPsDictionary[IP];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.LastHostNameUpdate = RightNow.GetIndex();
    IPsDictionary[IP] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in NetStatus.UpdateHostNameCheckTime():" );
      MForm.ShowStatus( Except.Message );
      }
    }




  internal void UpdateHostName( string IP, string HostName )
    {
    try
    {
    HostName = Utility.CleanAsciiString( HostName, 1000 );

    IPStatusRec StatusRec;
    if( IPsDictionary.ContainsKey( IP ))
      StatusRec = IPsDictionary[IP];
    else
      StatusRec = MakeNewStatusRec();

    ECTime RightNow = new ECTime();
    RightNow.SetToNow();
    StatusRec.TimeIndex = RightNow.GetIndex();
    StatusRec.HostName = HostName;
    StatusRec.LastHostNameUpdate = RightNow.GetIndex();
    IPsDictionary[IP] = StatusRec;
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in NetStatus.UpdateHostName():" );
      MForm.ShowStatus( Except.Message );
      }
    }



  internal bool ClearMidnightValues()
    {
    try
    {
    // The IP stuff is also kept in the server log.
    ECTime RecTime = new ECTime();
    ECTime OldTime = new ECTime();
    OldTime.SetToNow();
    OldTime.AddMinutes( -(60 * 24 * 30)); // 30 days.
    ulong OldIndex = OldTime.GetIndex();

    SortedDictionary<string, IPStatusRec> TempIPsDictionary = new SortedDictionary<string, IPStatusRec>();

    foreach( KeyValuePair<string, IPStatusRec> Kvp in IPsDictionary )
      {
      RecTime.SetFromIndex( Kvp.Value.TimeIndex );
      if( RecTime.GetIndex() < OldIndex )
        continue;

      // Keep totals on things like bad reg key count.
      IPStatusRec Rec = Kvp.Value;
      Rec.TimedOutCount = 0;
      Rec.GoodWebRequestCount = 0;
      // TotalGoodWebRequestCount
      // BadWebPageCount
      Rec.GetPublicKeyCount = 0;
      // Rec.TotalGetPublicKeyCount
      Rec.ConfirmBillingCount = 0;
      // Rec.TotalConfirmBillingCount

      TempIPsDictionary[Kvp.Key] = Rec;
      }

    IPsDictionary = TempIPsDictionary;
    return true;
    
    }
    catch( Exception Except )
      {
      MForm.ShowStatus( "Exception in ClearMidnightValues():" );
      MForm.ShowStatus( Except.Message );
      return false;
      }
    }



  }
}



