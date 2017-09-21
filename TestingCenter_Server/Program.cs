using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestingCenter_Server
{
    [Serializable]
    class Program
    {
        private static int Port = 8888;
        private static Thread th;
        private static TcpListener tcp;
        private static string command;
        private static studentsDataSet database = new studentsDataSet();
        public static DateTime Today { get; }

        static void Main(string[] args)
        {

            DateTime thisDay = DateTime.Today;
            Console.WriteLine(thisDay.ToString("D"));
            Console.WriteLine(System.DateTime.Now.ToLongTimeString());

            try
            {
                Console.WriteLine("Port:");
                Port = Convert.ToInt32(Console.ReadLine());
                th = new Thread(WaitingForClient)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "WaitingForClient"
                };
                th.Start();
                //Start shit
                while (true)
                {
                    command = Console.ReadLine();
                    switch (command)
                    {
                        case "stop":
                            if (th.IsAlive)
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
                            if (Console.ReadLine().Equals("info"))
                            {
                                Console.WriteLine("Rows: " + database.identificators.Rows.Count);//СУКА НЕ ВОРКАЕТ
                                Console.WriteLine("Columns: " + database.identificators.Columns.Count);
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
            catch (FormatException)
            {
                Main(null);
            }
        }
        private static void WaitingForClient()
        {
            try
            {
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                tcp = new TcpListener(ip, Port);
                tcp.Start();
                Console.Clear();//DEBUG
                Console.WriteLine("Port: "+Port);//DEBUG
                //ConsoleUpdatingInformation();//Возможно лучше всего будет выкинуть ненужную хуиту с проекта. Только консоль засоряет 
                //как все воркать будет уберем из консоли все ненужное
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
                    string send = null;//Сообщение ответа
                    byte[] response;//Переведенное сообщение ответа
                    string[] TestsList;
                    //Тут будут описаны варианты запросов
                    //buf[0]==login это сообщение от логин скрина
                    switch (buf[0])
                    {
                        case "login":
                            //Debug. Пробую на ощупь базы данных
                            Console.WriteLine("Rows: " + database.identificators.Rows.Count);
                            //1.database.identificators.Rows.Count (не прокатило)
                            //2.database.identificators.IDColumn.Table.Rows.Count (еще не проверил)
                            for (int i = 0; i < database.identificators.Rows.Count; i++)//Не считывает КОЛИЧЕСТВО СТРОК--------------------------------------------
                            {
                                if (buf[0].CompareTo(database.identificators.IDColumn.Table.Rows[i]) == 0)
                                {
                                    Console.WriteLine("Найдено совпадение!");
                                }
                                else
                                {
                                    Console.WriteLine("Совпадений нет");
                                }
                            }
                            //
                            send = "login_Сюняков_Андрей_Андреевич";//login_NNN
                            response = Encoding.UTF8.GetBytes(send);
                            stream.Write(response, 0, response.Length);
                            Console.WriteLine("Ответ: " + send);
                            break;
                        case "testslist":
                            //Список возможных тестов
                            //testlist_id (немножко передумал вариант)
                            //тут надо подключиться к базе данных, и по ID проверить, какая у человека специальность и семестестр (ЗАПОлНИТЬ С БАЗЫ ДАННЫХ speciality)------------
                            string specialty = "POIT_2";//DEBUG
                            string file = "testslist\\" + specialty + ".txt";//Файл с которого будет считывать вопросы и т.п.
                            if (!File.Exists(file))
                            {
                                Console.WriteLine("Файл списка тестов " + file + " не найден");
                                send = "testslist_NNN";
                                response = Encoding.UTF8.GetBytes(send);
                                stream.Write(response, 0, response.Length);
                                break;
                            }
                            else
                            {
                                TestsList = File.ReadAllLines(file);
                                //Сериализировать и отправить сей массив-список тестов
                                //Траблы с атрибутом, надо куда то выкинуть массив, где можно будет накинуть атрибут
                                //Гы, теперь класс Програм может быть сериализован)
                                BinaryFormatter formatter = new BinaryFormatter();
                                formatter.Serialize(stream, TestsList);
                                Console.WriteLine("Запуск сериализации:");
                                for(int i=0;i<TestsList.Length;i++)
                                {
                                    Console.WriteLine(TestsList[i]);
                                }
                                Console.WriteLine("Список тестов сериализован!");//DEBUG
                                //Я погуглил оно короче в юникоде все пересылает попробуй стринг билдер с UTF8 на UNICODE перевести мб пофиксится тот трабл с текстом
                                //Но это неточно 
                            }

                            break;
                        case "file":
                            //Получение данных из файла
                            //file_названиеПредмета
                            break;
                    }
                    //
                    stream.Close();//Закрываем поток, когда закончили работать
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
        }

        private static void ConsoleUpdatingInformation()
        {
            //Console.Clear();
            //Console.WriteLine("Port: " + Port);
            //Console.WriteLine("IncomingRequests: " + tcp.Pending());
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

    [Serializable]//Сериализация
    class Question
    {
        private string question;
        private string q_count; 
        private string[][] answers; // зачем 2х мерный ? решилтипо 1-вопрос 2-ответ
        // тогда нах q_count если можно .length сделать по данному массиву 
        //P.S. мб я чего не понял 
    }
}