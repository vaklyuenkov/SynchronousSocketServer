// SocketServer
using System;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;  
using System.Text.RegularExpressions;
class SynchronousSocketServer
{
    private static string Host = "localhost"; 
    private static int Port = 80; 
    private static string DefaultDir = "C:\\";
    private static string FaviconPath = "img\\favicon.ico";
    private static string IndexHtmlPath = "html\\index.html";
    private static string JsPath = "js\\script.js";
    private static string CssPath = "css\\style.css";
    private static string BackButtonRegexPattern = "[^\\\\]*\\\\?$";
    private static string ServerAddress = Host+":"+Port;
    private static int ClientLimit = 10; 
    private static int BufferSize = 1024;
    private static string GoToRoot = "<h4><a href='http://" + ServerAddress + "?adress=" + DefaultDir + "'>Go to Root</a></h4>"; // for "go to root" button - will use several times
    private static string RawHtml = File.ReadAllText(@IndexHtmlPath);
    private static string[] HtmlParts = RawHtml.Split(new string[] {"<body>"}, StringSplitOptions.None);// split html to insert content later 
    private static int ErrorCode = 0;// for tracking errors and send to client corresponding answer
    private static string Logmsg = "";
    private static string BackButton = ""; 
    private static string StatusCode200 = "HTTP/1.1 200 OK \n\n";
    private static string EndOfRequestPattern = "\n+"; 
    public static Dictionary <int, string> ErrCodes = new Dictionary<int, string>(3)
    {
        {1, "Not enough permissions to access the directory"},
        {2, "Wrong address, please, check url."},
        {3, "Internal server error"}
    };
 
    public static void StartListening()
    { 
        try 
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, Port);
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sListener.Bind(ipEndPoint);
            sListener.Listen(ClientLimit); 
            while (true) 
            {
                Logmsg = "Waiting for connection at " + ipEndPoint.ToString();
                Console.WriteLine(Logmsg);
                LoggingClass.Log(Logmsg);
                Socket handler = sListener.Accept();
                try // Now, after connection with client, we can catch errors and send answer about specific type of errors to client
                {
                    string data = null;
                    byte[] bytes = new byte[BufferSize];
                    while (true) 
                    {  
                        Console.WriteLine("reading");
                        int bytesRec = handler.Receive(bytes);  
                        data += Encoding.ASCII.GetString(bytes,0,bytesRec);  
                        if (Regex.IsMatch(@data, @EndOfRequestPattern)) 
                        {  
                            break;  
                        }
                    } 
                    (string address, int Error) = ParseRequest(data); 
                    BackButton = GetBackButton(address);
                    ErrorCode = Error;
                    if (address=="/favicon.ico"|| address=="/style.css"|| address=="/scripts.js" ) { SendLocalResoures(handler, address); } 
                    else
                    {
                        CheckHtml(); 
                        (FileInfo[] Files, DirectoryInfo[] Dirs)  = GetFilesAndDirs(address); 
                        SendAnswer(handler, Files, Dirs, BackButton); 
                    }
                }    
                catch (System.UnauthorizedAccessException ex)
                {
                    ErrorCode = 1; 
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg);   
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    ErrorCode = 2; 
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg); 
                }    
                catch (Exception ex)
                {
                    ErrorCode = 3; //there we can also just answer with 500 code. 
                    Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                    Console.WriteLine(Logmsg);
                    LoggingClass.Log(Logmsg);   
                }    
                if (ErrorCode > 0){SendWarnAnswer(handler, ErrorCode, BackButton);} //make and send html with error message to client
                CloseConnection(handler);
            }
        }
        catch (Exception ex) 
        {
                Logmsg = "Exeption: " + ex.ToString() + "\n\n";
                Console.WriteLine(Logmsg);
                LoggingClass.Log(Logmsg);
        }
    }
    public static void SendAnswer(Socket handler, FileInfo[] Files, DirectoryInfo[] Dirs, string BackButton)
    {  
        string html = StatusCode200;
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
            string url = "http://" + ServerAddress + "?adress=" + Uri.EscapeDataString(dir.FullName);
            FolderList += "<li>" + "<a href='" + url + "'>" + dir.Name + "</a>" + "</li>" ;
        }
        FolderList += "</ul>";
        FilesList += "</ul>";
        html += HtmlParts[0] + "<body>"+ GoToRoot + BackButton + FolderList + FilesList + HtmlParts[1]; //make final html
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); 
        Logmsg = "Answer was sent to client"+ "\n\n";
        Console.WriteLine(Logmsg);
        LoggingClass.Log(Logmsg);
    }
    public static void SendWarnAnswer(Socket handler, int ErrorCode, string BackButton)
    {  
        string ErrorMessage = ErrCodes[ErrorCode]; // get error message from error dictionary
        string html = StatusCode200 + HtmlParts[0] + "<body>"+ GoToRoot + BackButton + "<h2 align='center'>" + ErrorMessage +"</h2>" + HtmlParts[1]; //make final html
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); 
    }

    public static string GetBackButton(string address)
    {  
        BackButton = "";
        if (address != DefaultDir) 
        {
            BackButton =  "<h4><a href='http://" + ServerAddress + "?adress="+ Regex.Replace(@address, @BackButtonRegexPattern, "") + "'>Back</a></h4>"; //make link to do to upper dir 
        }
        return BackButton;
    }
    public static void SendLocalResoures(Socket handler, string address)
    {
        byte[] msg_bytes = { 0x20 };
        string msg_text = StatusCode200;
        switch (address)
        {
          case "/favicon.ico":
              msg_bytes = System.IO.File.ReadAllBytes(@FaviconPath);
              break;
          case "/style.css":
              msg_text += File.ReadAllText(@CssPath);
              msg_bytes = Encoding.UTF8.GetBytes(msg_text);
              break;
          case "/scripts.js":
              msg_text += File.ReadAllText(@JsPath);
              msg_bytes =  Encoding.UTF8.GetBytes(msg_text);
              break;
        }     
        handler.Send(msg_bytes);
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
        if (data.Length > 0) 
        {
            string ParamsLine = data.Split('\n')[0]; 
            string ParamsString = ParamsLine.Split(" ")[1];

            if (ParamsString != "/") // in case "/" address = DefaultDir
            {
                address = ParamsString.Replace("/?adress=", "");
                address = Uri.UnescapeDataString(address); 
            } 
        }
        Logmsg = "ADDRESS: " + address + "\n\n";
        Console.Write(Logmsg);
        LoggingClass.Log(Logmsg);
        return (address, ErrorCode);
    }
    public static (FileInfo[], DirectoryInfo[]) GetFilesAndDirs(string address)
    {  
        DirectoryInfo d = new DirectoryInfo(@address);
        FileInfo[] Files = d.GetFiles("*"); 
        DirectoryInfo[] Dirs = d.GetDirectories(); 
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