// SocketServer
using System;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Configuration;
using System.Collections.Specialized;
using System.Text.RegularExpressions;


class SynchronousSocketServer
{
    private static string Host = ConfigurationManager.AppSettings.Get("Host"); // get parameters from app.config
    private static string DefaultDir = ConfigurationManager.AppSettings.Get("DefaultDir");
    private static string FaviconPath = ConfigurationManager.AppSettings.Get("FaviconPath");
    private static string IndexHtmlPath = ConfigurationManager.AppSettings.Get("IndexHtmlPath");
    private static string JsPath = ConfigurationManager.AppSettings.Get("JsPath");
    private static string CssPath = ConfigurationManager.AppSettings.Get("CssPath");
    private static int Port = Int32.Parse(ConfigurationManager.AppSettings.Get("Port")); 
    private static int ClientLimit = Int32.Parse(ConfigurationManager.AppSettings.Get("ClientLimit")); 
    private static int MessageBytesSize = Int32.Parse(ConfigurationManager.AppSettings.Get("MessageBytesSize"));
    private static string GoToRoot = "<h4><a href='http://" + Host + "?adress=" + DefaultDir + "'>Go to Root</a></h4>"; // for "go to root" button - will use several times
    private static string RawHtml = File.ReadAllText(@IndexHtmlPath);// get html code
    private static string[] HtmlParts = RawHtml.Split(new string[] {"<body>"}, StringSplitOptions.None);// split html to insert content
    private static int ErrorCode = 0;// for tracking errors and get to client corresponding answer
    private static string Logmsg = "";
    private static string BackButton = ""; 
    public static void StartListening()
    { 

        try // To catch all error and write to log
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Host);// Get name and list of adresses of localhost
            IPAddress ipAddr = ipHost.AddressList[0];// Get first ip adress of localhost
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, Port);
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// make lisening socket
            sListener.Bind(ipEndPoint);// bind socket to our endpoint and listen
            sListener.Listen(ClientLimit); //set the maximum number of connections waiting to be processed in the queue. 
            while (true) // listening loop
            {
                Logmsg = "Waiting for connection at " + ipEndPoint.ToString();
                Console.WriteLine(Logmsg);
                LoggingClass.Log(Logmsg);
                Socket handler = sListener.Accept();// waiting for accept from client and then make socket
                try // Now, after connection with client, we can send answer about type of error to client
                {
                    string data = null;
                    byte[] bytes = new byte[MessageBytesSize];
                    int bytesRec = handler.Receive(bytes);// get data
                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    (string address, int Error) = ParseRequest(data); // get parameters from url (address of directory)
                    BackButton = GetBackButton(address);
                    ErrorCode = Error;
                    if (address=="/favicon.ico"|| address=="/style.css"|| address=="/scripts.js" ) { SendLocalResoures(handler, address); } // one need to rewrite this method if we will use more resources
                    else
                    {
                        CheckHtml(); // siple check 
                        (FileInfo[] Files, DirectoryInfo[] Dirs)  = GetFilesAndDirs(address); //get lists of dirs and files
                        SendAnswer(handler, Files, Dirs, BackButton); // make html and sedt to client
                    }
                }    
                catch (System.UnauthorizedAccessException ex)
                {
                    ErrorCode = 1; // Not enough permissions to access the directory
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg);   
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    ErrorCode = 2; // Wrong address, please, check url.
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg); 
                }    
                catch (Exception ex)
                {
                    ErrorCode = 3; // Internal server error
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg);   
                }    
                if (ErrorCode > 0){SendWarnAnswer(handler, ErrorCode, BackButton);} //make and send html with error message
                CloseConnection(handler);
            }
        }
        catch (Exception ex) //TODO: write to log   
        {
                Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                Console.WriteLine(Logmsg);
                LoggingClass.Log(Logmsg);
        }
    }
    public static void SendAnswer(Socket handler, FileInfo[] Files, DirectoryInfo[] Dirs, string BackButton)
    {  
        string FolderList = "<h3>Folders</h3><ul style='list-style-type:circle'>";
        string FilesList = "<h3>Files</h3><ul style='list-style-type:circle'>";
        int i = 0;
        foreach(FileInfo file in Files) // make list of files with popups
        {
            string PrettySize = PrettySizeClass.ToPrettySize(file.Length);
            FilesList += "<li> <div class='popup' id='"+ i + "' onclick='myFunction(this.id)'>" + file.Name +" <span class='popuptext' id='myPopup" + i + "'>" + PrettySize + "</span> </div> </li>";
            i++;
        }
        foreach(DirectoryInfo dir in Dirs) // make list of folders with links
        {
            string url = "http://" + Host + "?adress=" + dir.FullName;
            FolderList += "<li>" + "<a href='" + url + "'>" + dir.Name + "</a>" + "</li>" ;
        }
        FolderList += "</ul>";
        FilesList += "</ul>";
        string html = HtmlParts[0] + "<body>"+ GoToRoot + BackButton + FolderList + FilesList + HtmlParts[1]; //make final html
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); // send reply to client
        Logmsg = "Answer was sent to client"+ "\n\n";
        Console.WriteLine(Logmsg);
        LoggingClass.Log(Logmsg);
    }
    public static void SendWarnAnswer(Socket handler, int ErrorCode, string BackButton)
    {  
        string ErrorMessage = ConfigurationManager.AppSettings.Get(ErrorCode.ToString()); // get error message from error dictionary in app.config 
        string html = HtmlParts[0] + "<body>"+ GoToRoot + BackButton + "<h2 align='center'>" + ErrorMessage +"</h2>" + HtmlParts[1]; //make final html
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); // send reply to client
    }

    public static string GetBackButton(string address)
    {  
        BackButton = "";
        if (address != DefaultDir) 
        {
            BackButton =  "<h4><a href='http://" + Host + "?adress="+ Regex.Replace(@address, @"[^\\]*\\?$", "") + "'>Back</a></h4>"; //make link to do to upper dir 
        }
        return BackButton;
    }
    public static void SendLocalResoures(Socket handler, string address)
    {
        byte[] msg = { 0x20 };
        switch (address)
        {
          case "/favicon.ico":
              msg = System.IO.File.ReadAllBytes(@FaviconPath);
              break;
          case "/style.css":
              msg = System.IO.File.ReadAllBytes(@CssPath);
              break;
          case "/scripts.js":
              msg = System.IO.File.ReadAllBytes(@JsPath);
              break;
        }     
        handler.Send(msg);
        Logmsg = "File "+ address +" was sent to client"+ "\n\n";
        Console.WriteLine(Logmsg);
        LoggingClass.Log(Logmsg);
    }
    public static void CheckHtml()
    {
        if  (HtmlParts.Length != 2)
        {
            Logmsg = "Exeption: Wrong html, something wrong with <body> tag" + "\n\n";
            Console.WriteLine(Logmsg);
            LoggingClass.Log(Logmsg);
            throw new Exception();
        }
    }
    public static (string, int) ParseRequest(string data)
    {  
        Logmsg = "REQUEST: " + data + "\n\n";
        Console.Write(Logmsg);
        LoggingClass.Log(Logmsg);
        string address = DefaultDir;
        int ErrorCode = 0;
        if (data.Length > 0) //not empty request
        {
            string ParamsLine = data.Split('\n')[0]; // get first line
            string ParamsString = ParamsLine.Split(" ")[1]; // get only params

            if (ParamsString != "/") // in case "/" address = DefaultDir
            {
                address = ParamsString.Replace("/?adress=", "");
                address = Uri.UnescapeDataString(address); // decode url params
            } 
            string Param = ParamsString.Replace("/?adress=", ""); // get only address
        }
        Logmsg = "ADDRESS: " + address + "\n\n";
        Console.Write(Logmsg);
        LoggingClass.Log(Logmsg);
        return (address, ErrorCode);
    }
    public static (FileInfo[], DirectoryInfo[]) GetFilesAndDirs(string address)
    {  
        DirectoryInfo d = new DirectoryInfo(@address);
        FileInfo[] Files = d.GetFiles("*"); // Getting all files
        DirectoryInfo[] Dirs = d.GetDirectories(); // Getting all dirs
        return (Files, Dirs);
    }
    public static void CloseConnection(Socket handler)
    {
        Logmsg = "Connection closed.";
        Console.WriteLine(Logmsg);
        LoggingClass.Log(Logmsg);
        handler.Shutdown(SocketShutdown.Both);
        handler.Close();
    }
    public static int Main(String[] args)
    {  
        StartListening();  
        return 0;
    } 
}

public static class LoggingClass // All logging methods
{
    private static string LogJournalPath = ConfigurationManager.AppSettings.Get("LogJournalPath");

    public static void Log(string logMessage) // write messages to log; to optimise we can hold opend tw while server work
    {
        string path = LogJournalPath + "\\" + DateTime.Now.ToString("dd-M-yyyy") +"-Log" + ".txt";
        TextWriter tw = new StreamWriter(path, true);
        tw.Write("\r\nLog Entry : ");
        tw.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
        DateTime.Now.ToLongDateString());
        tw.WriteLine("  :");
        tw.WriteLine("  :{0}", logMessage);
        tw.WriteLine ("-------------------------------");
        tw.Close();
    }
}

public static class PrettySizeClass // To show pretty sizes of files
{
    private const long OneKb = 1024;
    private const long OneMb = OneKb * 1024;
    private const long OneGb = OneMb * 1024;
    private const long OneTb = OneGb * 1024;

    public static string ToPrettySize(this int value, int decimalPlaces = 0)
    {
        return ((long)value).ToPrettySize(decimalPlaces);
    }

    public static string ToPrettySize(this long value, int decimalPlaces = 0)
    {
        var asTb = Math.Round((double)value / OneTb, decimalPlaces);
        var asGb = Math.Round((double)value / OneGb, decimalPlaces);
        var asMb = Math.Round((double)value / OneMb, decimalPlaces);
        var asKb = Math.Round((double)value / OneKb, decimalPlaces);
        string chosenValue = asTb > 1 ? string.Format("{0}Tb",asTb)
            : asGb > 1 ? string.Format("{0}Gb",asGb)
            : asMb > 1 ? string.Format("{0}Mb",asMb)
            : asKb > 1 ? string.Format("{0}Kb",asKb)
            : string.Format("{0}B", Math.Round((double)value, decimalPlaces));
        return chosenValue;
    }
}
