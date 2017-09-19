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
        private static studentsDataSet database = new studentsDataSet();

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
                        case "database":
                            Console.WriteLine("Database?:");
                            if(Console.ReadLine().Equals("info"))
                            {
                                Console.WriteLine("Rows: "+database.identificators.Rows.Count);//СУКА НЕ ВОРКАЕТ
                                Console.WriteLine("Columns: "+database.identificators.Columns.Count);
                            }
                            break;
                    }
                  /*  if (command == "stop")
                    {
                        if (th.IsAlive)
                        {
                            tcp.Stop();
                            th.Abort();
                            Console.WriteLine("Сервер остановлен. Нажмите любую кнопку для закрытия приложения");
                            Console.ReadKey();
                            return; 
                        }
                    }*/
                }
                //End shit
            }
            catch(FormatException)
            {
                Main(null);
            }
        }
        private static void WaitingForClient()
        {
            try
            {
                //Тут мы запускам прослушку
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                tcp = new TcpListener(ip, Port);
                tcp.Start();
                ConsoleUpdatingInformation();
                while (true)
                {
                    TcpClient client = tcp.AcceptTcpClient();
                    ConsoleUpdatingInformation();//Debug
                    NetworkStream stream = client.GetStream();
                    byte[] data = new byte[256];
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string message = builder.ToString();
                    Console.WriteLine("Запрос: " + message);//Debug: сообщение, которое пришло
                                                            //Тут блок проверки совпадений в базе и отправка ответа

                    //Debug
                    string[] buf = message.Split('_');
                    byte[] response;
                    //Тут будут описаны варианты запросов
                    //buf[0]==login это сообщение от логин скрина
                    //buf[0]==
                    switch (buf[0])
                    {
                        case "login":
                            //Debug. Пробую на ощупь базы данных
                            Console.WriteLine("Rows: "+ database.identificators.Rows.Count);
                            //1.database.identificators.Rows.Count (не прокатило)
                            //2.database.identificators.IDColumn.Table.Rows.Count (еще не проверил)
                            for (int i=0;i<database.identificators.Rows.Count;i++)//Не считывает КОЛИЧЕСТВО СТРОК--------------------------------------------
                            {
                                if(buf[0].CompareTo(database.identificators.IDColumn.Table.Rows[i])==0)
                                {
                                    Console.WriteLine("Найдено совпадение!");
                                }
                                else
                                {
                                    Console.WriteLine("Совпадений нет");
                                }
                            }
                            //
                            string send = "login_Сюняков_Андрей_Андреевич";//login_NNN
                            response = Encoding.UTF8.GetBytes(send);
                            stream.Write(response, 0, response.Length);
                            Console.WriteLine("Ответ: " + send);
                            break;
                        case "testlist":
                            //Список возможных тестов
                            //testlist_специальность_семестр
                            string[] strings;
                            switch(buf[1])
                            {
                                case "POIT":
                                    strings = File.ReadAllLines("testslists\\POIT.txt");
                                    //Хз как тут че писать-----------------------------------------------------
                                    break;
                            }
                            break;
                        case "file":
                            //Получение данных из файла
                            //file_названиеПредмета
                            break;
                    }
                    //
                    
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message+"\n"+e.StackTrace);
            }
        }

        private static void ConsoleUpdatingInformation()
        {
            //Console.Clear();
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