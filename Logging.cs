using System;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
public static class LoggingClass 
{
    private static string LogJournalPath = ConfigurationManager.AppSettings.Get("LogJournalPath");

    public static void Log(string logMessage) 
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
