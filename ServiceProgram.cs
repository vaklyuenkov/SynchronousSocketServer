// using System;
// using System.Net;
// using System.Net.Sockets;
// using System.IO;
// using System.Threading.Tasks;

// // TCP service that computes mean or average of an array

// namespace SocketServerTcpListener
// {
//   class ServiceProgram
//   {
//     static void Main(string[] args)
//     {
//       try
//       {
//         int port = 50000;
//         AsyncService service = new AsyncService(port);
//         service.Run(); // very specific service
//         Console.ReadLine();
//       }
//       catch (Exception ex)
//       {
//         Console.WriteLine(ex.Message);
//         Console.ReadLine();
//       }
//     }
//   } // Program

//   public class AsyncService
//   {
//     private IPAddress ipAddress;
//     private int port;

//     public AsyncService(int port)
//     { 
//       // set up port and determine IP Address
//       this.port = port;
//       string hostName = Dns.GetHostName();
//       IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);

//       //this.ipAddress = IPAddress.Any; // allows requests to any of server's  addresses

//       this.ipAddress = null;
//       for (int i = 0; i < ipHostInfo.AddressList.Length; ++i)
//       {
//         if (ipHostInfo.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
//         {
//           this.ipAddress = ipHostInfo.AddressList[i];
//           break;
//         }
//       }
//       if (this.ipAddress == null)
//         throw new Exception("No IPv4 address for server");
//     } // AsyncService ctor

//     // normally the return would be Task but if so, we'd get a compiler warning in Main
//     // for not using await, but if we prepend await that's an error because Main returns void.
//     // A work-around attempt of making Main return a Task is not allowed. In short, void makes
//     // sense in this situation.

//     //public async Task Run()
//     public async void Run()
//     {
//       TcpListener listener = new TcpListener(this.ipAddress, this.port);
//       listener.Start();
//       Console.WriteLine("\nArray Min and Avg service is now running on port " + this.port);
//       Console.WriteLine("Hit <enter> to stop service\n");
      
//       while (true)
//       {
//         try
//         {
//           //Console.WriteLine("Waiting for a request ..."); 
//           TcpClient tcpClient = await listener.AcceptTcpClientAsync();
//           Task t = Process(tcpClient);
//           await t; // could combine with above
//         }
//         catch (Exception ex)
//         {
//           Console.WriteLine(ex.Message);
//         }
//       }
//     } // Start

//     private async Task Process(TcpClient tcpClient)
//     {
//       string clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
//       //Console.WriteLine("Received connection request from " + clientEndPoint);
//       Console.WriteLine("Received connection request from 123.45.678.999");
//       try
//       {
//         NetworkStream networkStream = tcpClient.GetStream();
//         StreamReader reader = new StreamReader(networkStream);
//         StreamWriter writer = new StreamWriter(networkStream);
//         writer.AutoFlush = true;
//         while (true)
//         {
//           string request = await reader.ReadLineAsync();
//           if (request != null)
//           {
//             Console.WriteLine("Received service request: " + request);
//             string response = Response(request);
//             Console.WriteLine("Computed response is: " + response + "\n");
//             await writer.WriteLineAsync(response);
//           }
//           else
//             break; // client closede connection
//         }
//         tcpClient.Close();
//       }
//       catch (Exception ex)
//       {
//         Console.WriteLine(ex.Message);
//         if (tcpClient.Connected)
//           tcpClient.Close();
//       }
//     } // Process

//     private static string Response(string request)
//     {
//       // assumes request has form like method=average&data=1.1 2.2 3.3&eor
//       // eor stands for end-of-request
//       // dummy delay bssed on the first numeric value
//       string[] pairs = request.Split('&');
//       string methodName = pairs[0].Split('=')[1];
//       string valueString = pairs[1].Split('=')[1];

//       string[] values = valueString.Split(' ');
//       double[] vals = new double[values.Length];
//       for (int i = 0; i < values.Length; ++i)
//         vals[i] = double.Parse(values[i]);

//       string response = "";
//       if (methodName == "average") response += Average(vals);
//       else if (methodName == "minimum") response += Minimum(vals);
//       else response += "BAD methodName: " + methodName;

//       int delay = ((int)vals[0]) * 1000; // dummy delay
//       System.Threading.Thread.Sleep(delay);

//       return response;
//     } // Response

//     private static double Average(double[] vals)
//     {
//       double sum = 0.0;
//       for (int i = 0; i < vals.Length; ++i)
//         sum += vals[i];
//       return sum / vals.Length;
//     }

//     private static double Minimum(double[] vals)
//     {
//       double min = vals[0]; ;
//       for (int i = 0; i < vals.Length; ++i)
//         if (vals[i] < min) min = vals[i];
//       return min;
//     }

//   } // AsyncServer

// } // ns
