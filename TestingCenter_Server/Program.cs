using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestingCenter_Server
{
    class Program
    {
        private static string IP_adr;
        private static int Port;
        static void Main(string[] args)
        {
            try
            {
                //типо при запуске сервера он будет конектить 1 клиента и то в ручную
                //параша нужно передетать
                Console.WriteLine("IP адрес для прослушивания: (напр. 127.0.0.1)");
                IP_adr = Console.ReadLine();
                Console.WriteLine("Порт для прослушивания:");
                Port = Convert.ToInt32(Console.ReadLine());
                //Создаем листенер
                IPAddress ip = IPAddress.Parse(IP_adr);
                TcpListener tcp = new TcpListener(ip, Port);
                tcp.Start(); //просто забил тот ip и в порт 1111 и все уже ошибка 

                ConsoleUpdatingInformation(ref tcp);//Отображение инфы  листенере
                while(true)
                {

                }
                //
            }
            catch(FormatException e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                Main(null);
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
