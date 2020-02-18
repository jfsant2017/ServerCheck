using System;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Reflection;

namespace ServerCheck
{
    class Program
    {
        private static Timer checkTimer;
        private static Uri resource = null;
        private static TypeCheck executionType = TypeCheck.None;

        private enum TypeCheck
        {
            None = 0,
            Server,
            Service
        }

        public static string helpMessage = "ServerCheck " + Assembly.GetExecutingAssembly().GetName().Version +
                "\nUsage: ServerCheck [options]\n\n" +
                "Options:\n\t-h\t\t\tDisplays this help\n" +
                "\t-s service_address\tInform the service address to be checked\n" +
                "\t-w server_address\tInform the server address to be checked\n" +
                "\t-t 1-60\t\t\tEnable check periodicaly in minutes\n";


        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h")
            {
                Console.WriteLine(helpMessage);
                Environment.Exit(0);
            }
            else
            {
                if (args[0] == "-w")
                {
                    executionType = TypeCheck.Server;
                }
                else if (args[0] == "-s")
                {
                    executionType = TypeCheck.Service;
                }
                else
                {
                    Console.WriteLine(helpMessage);
                    Environment.Exit(0);
                }

                if (args.Length >= 2 && !Uri.TryCreate(args[1], UriKind.Absolute, out resource))
                {
                    Console.WriteLine("Invalid address");
                }

                if (args.Length > 3 && args[2] == "-t")
                {
                    checkTimer = new Timer();
                    if (int.TryParse(args[3], out int periodicity))
                    {
                        checkTimer.Interval = 1000 * 60 * periodicity;
                    }
                    else
                    {
                        checkTimer.Interval = 1000 * 60 * 1;
                    }

                    checkTimer.Elapsed += CheckTimer_Elapsed;
                    checkTimer.AutoReset = true;

                    checkTimer.Enabled = true;

                    Console.WriteLine("{0} - Check {1} started. Press ENTER or CTRL + C to finish."
                        , DateTime.UtcNow, resource);
                    Console.ReadLine();
                    Console.WriteLine("Check finished.");
                }
                else
                {
                    Console.WriteLine("Elapsed {0} - {1}", DateTime.UtcNow, RunCheck());
                }
            }

            Environment.Exit(0);
        }

        private static string RunCheck()
        {
            if (executionType == TypeCheck.Server)
                return CheckServer();
            else if (executionType == TypeCheck.Service)
                return CheckService();
            else
                return "Invalid parameter";
        }

        private static void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Elapsed {0} - {1}", e.SignalTime.ToUniversalTime(), RunCheck());
        }

        private static string CheckServer()
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(resource.Host, resource.Port);

                return "Server online";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string CheckService()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(resource.AbsoluteUri);
                var resp = (HttpWebResponse)req.GetResponse();

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    return "Service available";
                }
                else
                {
                    return string.Format("Service unavailable {0}", resp.StatusDescription);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
