using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestingCenter_Server
{
    class Program
    {
        /*private static string IP_adr;
        private static int Port;*/
        private const string IP_adr = "localhost";
        private const int Port = 8888;
        private static Thread th;
        static void Main(string[] args)
        {
            try
            {
                /*Console.WriteLine("IP адрес для прослушивания: (напр. 127.0.0.1)");
                IP_adr = Console.ReadLine();
                Console.WriteLine("Порт для прослушивания:");
                Port = Convert.ToInt32(Console.ReadLine());*/


                th = new Thread(WaitingForClient)
                {
                    IsBackground=true,
                    Priority=ThreadPriority.Highest,
                    Name="WaitingForClient"
                };
                th.Start();
                Thread.Sleep(Timeout.Infinite);
                //
            }
            catch(FormatException e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                Main(null);
            }
        }
        private static void WaitingForClient()
        {
            IPAddress ip = IPAddress.Parse(IP_adr);
            TcpListener tcp = new TcpListener(ip, Port);
            tcp.Start();
            ConsoleUpdatingInformation(ref tcp);
            while (true)
            {
                TcpClient client = tcp.AcceptTcpClient();
                Console.WriteLine("GetStream");
                NetworkStream stream = client.GetStream();
                Console.WriteLine("CompRes");
                string result = "Accepted";
                byte[] byte_result = Encoding.UTF8.GetBytes(result);

                stream.Write(byte_result, 0, byte_result.Length);
                Console.WriteLine("Sended");
            }
        }

        private static void ConsoleUpdatingInformation(ref TcpListener tcp)
        {
            Console.Clear();
            Console.WriteLine("IP: "+IP_adr);
            Console.WriteLine("Port: "+Port);
            Console.WriteLine("IncomingRequests: "+tcp.Pending());
            if(tcp.Pending())
            {
                Console.WriteLine("Отвечаем на запрос...");
            }
            else
            {
                Console.WriteLine("Сервер запущен. Ожидание запросов...");
            }
        }
    }
}
//Нужен метод для проверки корректоности ввода порта и айпишника. Иначе Демедович развалит кабину
