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
        private static int Port = 8888;
        private static Thread th;
        private static TcpListener tcp;
        private static string command;

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Port:");
                Port = Convert.ToInt32(Console.ReadLine());
                th = new Thread(WaitingForClient)
                { 
                    IsBackground=true,
                    Priority=ThreadPriority.Highest,
                    Name="WaitingForClient"
                };
                th.Start();
                //Start shit
                while (true)
                {
                    command = Console.ReadLine();
                    switch(command)
                    {
                        case "stop":
                            if(th.IsAlive)
                            {
                                tcp.Stop();
                                th.Abort();
                                Console.WriteLine("Сервер остановлен. Нажмите любую кнопку для закрытия приложения");
                                Console.ReadKey();
                                return;
                            }
                            break;
                    }
                }
                //End shit
            }
            catch(FormatException e)
            {
                Main(null);
            }
        }
        private static void WaitingForClient()
        {
            //Тут мы запускам прослушку
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            tcp = new TcpListener(ip, Port);
            tcp.Start();
            ConsoleUpdatingInformation();
            while (true)
            {
                TcpClient client = tcp.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] data = new byte[64];
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                string message = builder.ToString();
                Console.WriteLine(message);
            }
        }

        private static void ConsoleUpdatingInformation()
        {
            Console.Clear();
            Console.WriteLine("Port: "+Port);
            Console.WriteLine("IncomingRequests: " + tcp.Pending());
            if (tcp.Pending())
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
