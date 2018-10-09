﻿// SocketServer
using System;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Configuration;
using System.Collections.Specialized;

//TODO: undestand about how close sockets

//TODO: more exeptions to erros, for example binding errors and so on
//TODO: 2) ecxeptions to client and dont fall
//TODO: 3) socket optimisation
//TODO: 3.1) check methods types
//TODO: 4) git commit

// about socket server https://professorweb.ru/my/csharp/web/level3/3_1.php 
class SynchronousSocketServer
{
    private static string Host = ConfigurationManager.AppSettings.Get("Host");
    private static string DefaultDir = ConfigurationManager.AppSettings.Get("DefaultDir");
    private static string FaviconPath = ConfigurationManager.AppSettings.Get("FaviconPath");
    private static string IndexHtmlPath = ConfigurationManager.AppSettings.Get("IndexHtmlPath");
    private static int Port = Int32.Parse(ConfigurationManager.AppSettings.Get("Port")); 
    private static string LogJournalPath = ConfigurationManager.AppSettings.Get("LogJournalPath");
    private static string GoToRoot = "<h4><a href='http://" + Host + "?adress=" + DefaultDir + "'>Go to Root</a></h4>";//for "go to root" button
    private static string RawHtml = File.ReadAllText(@IndexHtmlPath);
    private static string[] HtmlParts = RawHtml.Split(new string[] {"<body>"}, StringSplitOptions.None);
    private static int ErrorCode = 0;
    public static void StartListening()
    { 

        try // To catch all error and write to log
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Host);// Get name and list of adresses of localhost
            IPAddress ipAddr = ipHost.AddressList[0];// Get first ip adress of localhost
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, Port);
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);// make lisening socket
            sListener.Bind(ipEndPoint);// bind socket to our endpoint and listen
            sListener.Listen(10);
            while (true) // listening loop
            {
                Console.WriteLine("Waiting for connection at {0}", ipEndPoint);
                Log("Waiting for connection at " + ipEndPoint.ToString());
                Socket handler = sListener.Accept();// waiting for accept from client and then make socket
                try // Now, after connection with client, we can send answer about type of error to client
                {
                    string data = null;
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);// get data
                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    (string address, int Error) = ParseRequest(data);
                    ErrorCode = Error;
                    if (address=="/favicon.ico") { SendFavicon(handler); } // there we can send system files like images, css, js if we need
                    else
                    {
                        CheckHtml();
                        (FileInfo[] Files, DirectoryInfo[] Dirs)  = GetFilesAndDicts(address);
                        SendAnswer(handler, Files, Dirs);
                    }
                }    
                catch (System.UnauthorizedAccessException ex)
                {
                    ErrorCode = 1;
                    Console.WriteLine(ex.ToString());
                    Log("Exeption: " + ex.ToString() + "\n\n");   
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    ErrorCode = 2;
                    Console.WriteLine(ex.ToString());  
                    Log("Exeption: " + ex.ToString() + "\n\n");  
                }    
                catch (Exception ex)
                {
                    ErrorCode = 3;
                    Console.WriteLine(ex.ToString()); 
                    Log("Exeption: " + ex.ToString() + "\n\n");   
                }    
                    //how to catch another exeptions?
                if (ErrorCode > 0){SendWarnAnswer(handler, ErrorCode);}
                CloseConnection(handler);
            }
        }
        catch (Exception ex) //TODO: write to log   
        {
            Console.WriteLine(ex.ToString());
            Log("Exeption: " + ex.ToString() + "\n\n");
        }
    }
    public static void SendAnswer(Socket handler, FileInfo[] Files, DirectoryInfo[] Dirs)
    {  
        string FolderList = "<h3>Folders</h3><ul style='list-style-type:circle'>";
        string FilesList = "<h3>Files</h3><ul style='list-style-type:circle'>";
        int i = 0;
        foreach(FileInfo file in Files)
        {
            string PrettySize = PrettySizeClass.ToPrettySize(file.Length);
            FilesList += "<li> <div class='popup' id='"+ i + "' onclick='myFunction(this.id)'>" + file.Name +" <span class='popuptext' id='myPopup" + i + "'>" + PrettySize + "</span> </div> </li>";
            i++;
        }
        foreach(DirectoryInfo dir in Dirs)
        {
            string url = "http://" + Host + "?adress=" + dir.FullName;
            FolderList += "<li>" + "<a href='" + url + "'>" + dir.Name + "</a>" + "</li>" ;
        }
        FolderList += "</ul>";
        FilesList += "</ul>";
        string html = HtmlParts[0] + "<body>"+ GoToRoot + FolderList + FilesList + HtmlParts[1];
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); // send reply to client
    }
    public static void SendWarnAnswer(Socket handler, int ErrorCode)
    {  
        string ErrorMessage = ConfigurationManager.AppSettings.Get(ErrorCode.ToString());
        string html = HtmlParts[0] + "<body>"+ GoToRoot + "<h2 align='center'>" + ErrorMessage +"</h2>" + HtmlParts[1];
        byte[] msg = Encoding.UTF8.GetBytes(html);
        handler.Send(msg); // send reply to client
    }
    public static void SendFavicon(Socket handler)
    {
        byte[] msg = System.IO.File.ReadAllBytes(@FaviconPath);
        handler.Send(msg);
    }
        public static void CheckHtml()
    {
        if  (HtmlParts.Length != 2)
        {
            Console.WriteLine("Wrong html, something wrong with <body> tag");
            Log("Exeption: Wrong html, something wrong with <body> tag" + "\n\n");
            throw new Exception();
        }
    }
    public static (string, int) ParseRequest(string data)
    {  
        Console.Write("REQUEST: " + data + "\n\n");
        Log("Request: " + data + "\n\n");
        string address = DefaultDir;
        int ErrorCode = 0;
        if (data.Length > 0)
        {
            string ParamsLine = data.Split('\n')[0];
            string ParamsString = ParamsLine.Split(" ")[1];

            if (ParamsString != "/")
            {
                address = ParamsString.Replace("/?adress=", "");
                address = Uri.UnescapeDataString(address); 
            } 
            string Param = ParamsString.Replace("/?adress=", "");
        }
        Console.Write("ADDRESS: " + address + "\n\n");
        Log("Parsed address: " + address + "\n\n");
        return (address, ErrorCode);
    }
    public static (FileInfo[], DirectoryInfo[]) GetFilesAndDicts(string address)
    {  
        DirectoryInfo d = new DirectoryInfo(@address);
        FileInfo[] Files = d.GetFiles("*"); //Getting Text files
        DirectoryInfo[] Dirs = d.GetDirectories(); //Getting Text files
        return (Files, Dirs);
    }
    public static void CloseConnection(Socket handler)
    {
        Console.WriteLine("Connection closed.");
        Log("Connection closed.");
        handler.Shutdown(SocketShutdown.Both);
        handler.Close();
    }
    public static void Log(string logMessage) // to optimise we can hold opend tw while server work
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

    public static int Main(String[] args)
    {  
        StartListening();  
        return 0;
    } 
}
public static class PrettySizeClass
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
