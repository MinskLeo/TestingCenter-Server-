using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Data;
using System.Data.SQLite;
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
        private static string command;//Для цикла while(true) - пишем вручную в консоль и считываем
        private static SQLiteConnection database;//Сама база данных
        private static SQLiteCommand database_commands;//Объект для отдачи команд БД
        private static string Command;//Строка команды для SQL

        static void Main(string[] args)
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Port:");
                    Port = Convert.ToInt32(Console.ReadLine());

                    if (Port <= 0)
                    {
                        Console.WriteLine("Error!!! Port can't be <= 0");
                    }
                    else break;

                }

                th = new Thread(WaitingForClient)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "WaitingForClient"
                };
                th.Start();
                Console.WriteLine("Server started!" + DateTime.Now);

                //Тут блок связанный с БД
                if (!File.Exists("databases\\Students.db"))
                    throw new SQLiteException();
                database = new SQLiteConnection("Data Source=databases\\Students.db;Version=3;UTF8Encoding=True;");
                database.Open();//Открываем БД
                database_commands = database.CreateCommand();//Создаем командный объект
                Command = "";//Чисто чтобы не вылетал экзепшон
                database_commands.CommandText = Command;//Чисто чтобы не вылетал экзепшон
                Console.WriteLine("State: " + database.State);

                //Конец блока с БД
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
                                Console.WriteLine("The server is stopped. Press any button to close the application");
                                Console.ReadKey();
                                return;
                            }
                            break;
                        case "database":
                            Console.WriteLine("Database?:");
                            if (Console.ReadLine().Equals("info"))
                            {
                                Command = "SELECT count(*) FROM students";
                                database_commands.CommandText = Command;
                                Console.WriteLine("Rows: " + database_commands.ExecuteScalar());
                                //-------------------------------------------------------------------Тут было кол во строк\столбцев
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
            }
            catch (FormatException)
            {
                Main(null);
            }
            catch (SQLiteException ex)//Проблемы с экзепшоном
            {
                //Ловим траблы с БД
                Console.WriteLine("Troubles with database:");
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                tcp.Stop();
                //th.Abort();
                Console.WriteLine("The server is stopped. Press any button to close the application");
                Console.ReadKey();
                return;
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
                Console.WriteLine("Port: " + Port);//DEBUG

                while (true)
                {
                    TcpClient client = tcp.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    //Тестим норм потоки. //DEBUG----------------------------------- Вплоть до ПРОВЕРИТЬ
                    //StreamReader NetReader = new StreamReader(stream);
                    //StreamWriter NetWriter = new StreamWriter(stream);
                    //string message=NetReader.ReadLine();
                    //NetReader.Close();
                    //

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
                    Console.WriteLine("Request: " + message);
                    string[] buf = message.Split('_');
                    string send = null;//Сообщение ответа
                    byte[] response;//Переведенное сообщение ответа
                    string[] TestsList;
                    //---
                    //ПЕРЕМЕННЫЕ!
                    object id, n, f, m, term, spec;
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    //---
                    switch (buf[0])
                    {
                        case "login":
                            //Поиск по базе данных
                            Command = "SELECT * FROM students WHERE Id="+buf[1]+";";//Типо воркает       "SELECT Id FROM students WHERE Id="+buf[1]+";"
                            database_commands.CommandText = Command;
                            SQLiteDataReader r_login = database_commands.ExecuteReader();
                            while (r_login.Read())
                            {
                                id = r_login.GetValue(0);//Id
                                n = r_login.GetValue(1);//Name
                                f = r_login.GetValue(2);//Famil
                                m = r_login.GetValue(3);//Otchestvo
                                if (id.ToString().Equals(buf[1]))
                                {
                                    send = "login_" + n + "_" + f + "_" + m;
                                    r_login.Close();
                                    break;
                                }
                                else
                                {
                                    send = "login_NNN";
                                }
                            }
                            r_login.Close();

                            Console.WriteLine("Ended");//DEBUG
                                                       //send = "login_Сюняков_Андрей_Андреевич";//login_NNN    //DEBUG
                                                       //Очень важный тест!----------------------------------------------------------------------------------//DEBUG
                                                       //NetWriter.WriteLine(send);
                                                       //NetWriter.Flush();
                                                       //NetWriter.Close();
                                                       //NetReader.Close();
                            response = Encoding.UTF8.GetBytes(send);
                            stream.Write(response, 0, response.Length);
                            Console.WriteLine("Ответ: " + send);
                            break;
                        case "mainscreen":
                            Command = "SELECT * FROM marks WHERE ID=" + buf[1] + ";";
                            database_commands.CommandText= "SELECT count(*) FROM marks WHERE ID=" + buf[1] + ";";//DEBUG
                            Console.WriteLine("Колчиество строк в ответе: "+database_commands.ExecuteScalar());//DEBUG
                            Console.WriteLine("mainscreen: " + Command);

                            Console.WriteLine("создаю адаптер");
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(Command, database.ConnectionString);
                            DataSet dataSet = new DataSet();
                            Console.WriteLine("создал объект dataset");
                            adapter.Fill(dataSet);
                            Console.WriteLine("заполнил объект");
                            //Тут был binaryFormatter
                            Console.WriteLine("сериализую");
                            binaryFormatter.Serialize(stream, dataSet);//Сериализовали целый объект с выборкой по БД
                            Console.WriteLine("ГОТОВО!");
                            break;
                        case "testslist":
                            //Список возможных тестов
                            //testlist_id (немножко передумал вариант)
                            //тут надо подключиться к базе данных, и по ID проверить, какая у человека специальность и семестестр (ЗАПОлНИТЬ С БАЗЫ ДАННЫХ speciality)------------
                            Console.WriteLine("Начинаю работу с TESTSLITS");//DEBUG
                            Command = "SELECT * FROM students;";
                            database_commands.CommandText = Command;
                            SQLiteDataReader r_testslist = database_commands.ExecuteReader();
                            Console.WriteLine("Начинаю проход по БД");//DEBUG
                            while (r_testslist.Read())
                            {
                                id = r_testslist.GetValue(0);
                                if (id.ToString().Equals(buf[1]))
                                {
                                    //Нам нужно узнать специальность и семестр
                                    term = r_testslist.GetValue(4);//Столбик с семестром
                                    spec = r_testslist.GetValue(6);//Столбик со специальностью
                                    //spec_term
                                    send = "testslist\\" + spec.ToString() + "_" + term.ToString();//попадет в if\else и выберет нужный файлик для отправки клиенту
                                    break;
                                }
                                else
                                {
                                    send = "testslist_NNN";
                                }
                            }
                            if (send.Equals("testslist_NNN"))
                            {
                                //Не найден студент. Какой то значит косяк. Отправляем клиенту что ошибка базы данных.
                                Console.WriteLine("No student");//DEBUG
                                //Тут надо это дописать------------------------------------------------
                                break;
                            }
                            else
                            {
                                send += ".txt";
                                Console.WriteLine("FILE: " + send);//DEBUG
                            }
                            r_testslist.Close();
                            //END
                            //string specialty = "POIT_2";//DEBUG----------------------------------------------------------------------
                            //string file = "testslist\\" + specialty + ".txt";//Файл с которого будет считывать вопросы и т.п.
                            if (!File.Exists(send))
                            {
                                Console.WriteLine("Файл списка тестов " + send + " не найден");
                                send = "testslist_NNN";
                                response = Encoding.UTF8.GetBytes(send);
                                stream.Write(response, 0, response.Length);
                                break;
                            }
                            else
                            {
                                TestsList = File.ReadAllLines(send);
                                //Сериализировать и отправить сей массив-список тестов
                                //Траблы с атрибутом, надо куда то выкинуть массив, где можно будет накинуть атрибут
                                //Гы, теперь класс Програм может быть сериализован)
                                BinaryFormatter formatter = new BinaryFormatter();
                                formatter.Serialize(stream, TestsList);
                                Console.WriteLine("Запуск сериализации:");
                                for (int i = 0; i < TestsList.Length; i++)
                                {
                                    Console.WriteLine(TestsList[i]);
                                }
                                Console.WriteLine("Список тестов сериализован!");//DEBUG
                                //Я погуглил оно короче в юникоде все пересылает попробуй стринг билдер с UTF8 на UNICODE перевести мб пофиксится тот трабл с текстом
                                //Но это неточно
                            }
                            break;
                        case "test":
                            //test_Math_3
                            string[] TEST;
                            string way= "tests\\" + buf[1]+ "_" + buf[2] + ".txt";
                            Console.WriteLine("WAY: "+way);
                            if(File.Exists(way))
                            {
                                TEST = File.ReadAllLines(way);
                            }
                            else
                            {
                                TEST = new string[0];
                            }
                            Console.WriteLine("SIZE: "+TEST.Length);
                            binaryFormatter.Serialize(stream, TEST);
                            Console.WriteLine("ОКОНЧИЛ ОТПРАВКУ ФАЙЛА ТЕСТА");
                            break;
                        default:
                            Console.WriteLine("Вышел в дефаулт");
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
    }

    /*[Serializable]//Сериализация
    class Question
    {
        private string question;
        private string q_count; 
        private string[][] answers;
    }*/
}