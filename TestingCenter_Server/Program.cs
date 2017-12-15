using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Text;

namespace TestingCenter_Server
{
    class Commands
    {
        public void Login(string[] buf,ref SQLiteCommand database_commands,ref NetworkStream stream)
        {
            //Поиск по базе данных
            string Command = "SELECT * FROM students WHERE Id=" + buf[1] + ";";//Типо воркает       "SELECT Id FROM students WHERE Id="+buf[1]+";"
            database_commands.CommandText = Command;
            SQLiteDataReader r_login = database_commands.ExecuteReader();
            object id, n, f, m;
            string send= "login_NNN";//[TEST] ""
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
            }
            r_login.Close();

            byte[] response;
            response = Encoding.UTF8.GetBytes(send);
            stream.Write(response, 0, response.Length);
            Console.WriteLine("login: answ:" + send);
        }
        public void Mainscreen(string[] buf,ref NetworkStream stream)
        {
            string Command = "SELECT * FROM marks WHERE ID=" + buf[1] + ";";
            BinaryFormatter formatter = new BinaryFormatter();

            Console.WriteLine("mainscreen: Создаю адаптер");
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(Command, Program.database.ConnectionString);
            DataSet dataSet = new DataSet();
            Console.WriteLine("mainscreen: Создал объект dataset");
            adapter.Fill(dataSet);
            Console.WriteLine("mainscreen: Заполнил объект");
            Console.WriteLine("mainscreen: Сериализирую таблицу");
            formatter.Serialize(stream, dataSet);//Сериализовали целый объект с выборкой по БД
            Console.WriteLine("mainscreen: ГОТОВО!");
        }
        public void Testlist(string[] buf, ref SQLiteCommand database_commands, ref NetworkStream stream)
        {
            List<string> result = new List<string>();
            BinaryFormatter formatter = new BinaryFormatter();

            Console.WriteLine("testslist: Начинаю работу");
            string Command = "SELECT * FROM students WHERE Id=" + buf[1] + ";";
            database_commands.CommandText = Command;
            SQLiteDataReader r_testslist = database_commands.ExecuteReader();
            Console.WriteLine("testslist: Начинаю проход по БД");
            object id = null;
            object term = null;
            object spec = null;
            string send="";
            while (r_testslist.Read())
            {
                id = r_testslist.GetValue(0);
                if (id.ToString().Equals(buf[1]))
                {
                    term = r_testslist.GetValue(4);
                    spec = r_testslist.GetValue(6);
                    break;
                }
                else
                {
                    send = null;
                }
            }
            if (send.Equals(null))
            {
                //Не найден студент. Какой то значит косяк. Отправляем клиенту что ошибка базы данных.
                Console.WriteLine("testslist: No student");//DEBUG
                formatter.Serialize(null, stream);
            }
            else
            {
                if (spec != null && term != null)
                {
                    foreach (var a in Directory.GetFiles("tests"))
                    {
                        Console.WriteLine(a);
                        if (a.Contains(spec.ToString() + "_" + term.ToString()))
                        {
                            result.Add(a);
                        }
                    }
                }

                for (int i = 0; i < result.Count; i++)
                {
                    Console.WriteLine("testslist: Res:" + result[i]);
                }
            }
            r_testslist.Close();
            if (result.Count != 0)
            {
                //Попробую. передать список, а принять как массив
                Console.WriteLine("testslist: Запуск сериализации result");
                formatter.Serialize(stream, result);
                Console.WriteLine("testslist: Операция завершена. Отправлено!");
            }
            else
            {
                //Хз че тут писать. Надо обработать косяк отстутствия файлов с заданной комбинацией
            }
        }
        public void Test(string[] buf, ref NetworkStream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string[] TEST;
            string way = "tests\\" + buf[1] + "_" + buf[2] + "_" + buf[3] + ".txt";
            Console.WriteLine("test: WAY: " + way);
            if (File.Exists(way))
            {
                TEST = File.ReadAllLines(way);
            }
            else
            {
                TEST = new string[0];
            }
            Console.WriteLine("test: SIZE: " + TEST.Length);
            formatter.Serialize(stream, TEST);
            Console.WriteLine("test: Отправка данных окончена");
        }
        public void TestResult(string[] buf, ref SQLiteCommand database_commands)
        {
            string Command = "INSERT INTO marks (ID,Date,Time,Subject,Mark) VALUES('"
                + buf[1] + "' ,'" + DateTime.Today.ToShortDateString() + "', '"
                + buf[4] + "', '" + buf[2] + "', '" + buf[3] + "');";
            Console.WriteLine("testresult: " + Command);
            database_commands.CommandText = Command;
            Console.WriteLine("testresult: Данные добавлены! STATUS: " + database_commands.ExecuteNonQuery());
        }
        public void DateResults(string[] buf, ref NetworkStream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string Command = "SELECT * FROM marks WHERE ID=" + buf[1] + " AND Date BETWEEN " + "'" + buf[2] + "' AND '" + buf[3] + "';";

            Console.WriteLine("dataresult: Создаю адаптер");
            SQLiteDataAdapter adapter1 = new SQLiteDataAdapter(Command, Program.database.ConnectionString);
            DataSet dataSet1 = new DataSet();
            Console.WriteLine("dataresult: Создал объект dataset");
            adapter1.Fill(dataSet1);
            Console.WriteLine("dataresult: Заполнил объект");
            Console.WriteLine("mainscreen: Сериализую таблицу");
            formatter.Serialize(stream, dataSet1);//Сериализовали целый объект с выборкой по БД
            Console.WriteLine("dataresult: ГОТОВО!");
        }
    }
    [Serializable]
    class Program
    {
        [NonSerialized]
        private static int Port = 8888;
        [NonSerialized]
        private static Thread th;//Поток ожидания клиента
        [NonSerialized]
        private static TcpListener tcp;//Объект, с поомщью которого мы прослушивания подключений
        [NonSerialized]
        private static string command;//Для цикла while(true) - пишем вручную в консоль и считываем
        [NonSerialized]
        public static SQLiteConnection database;//Сама база данных
        [NonSerialized]
        private static SQLiteCommand database_commands;//Объект для отдачи команд БД
        [NonSerialized]
        private static string Command;//Строка команды для SQL

        private static void PortScanning()//[TEST] int
        {
            Console.WriteLine("Порт (enter для порта 8888 по умолчанию):");
            while (true)
            {
                string buf = Console.ReadLine();
                int outer = 0;
                bool parsing = int.TryParse(buf, out outer);//Переменная парсера
                bool equal = buf.Equals("");//Значение, указывающее на пустой ввод
                if (buf.Equals(""))
                {
                    Console.WriteLine("Порт установлен по умолчанию");
                    break;
                }
                else if (!equal && !parsing)
                {
                    Console.WriteLine("Некорректный ввод");
                }
                else if (!equal && parsing && outer <= 0)
                {
                    Console.WriteLine("Порт не может быть отрицательным значением ИЛИ равным 0!");
                }
                else
                {
                    Port = outer;
                    break;
                }
            }
        }
        static void Main(string[] args)
        {
            try
            {
                PortScanning();

                th = new Thread(WaitingForClient)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                    Name = "WaitingForClient"
                };
                th.Start();


                //Тут блок связанный с БД
                if (!File.Exists("databases\\Students.db"))
                    throw new SQLiteException();
                database = new SQLiteConnection("Data Source=databases\\Students.db;Version=3;UTF8Encoding=True;");
                database.Open();//Открываем БД
                database_commands = database.CreateCommand();//Создаем командный объект
                Command = "";//Чисто чтобы не вылетал экзепшон
                database_commands.CommandText = Command;//Чисто чтобы не вылетал экзепшон
                //Конец блока с БД
                while (true)
                {
                    //Console.WriteLine("Для получения списка команд введите: help");
                    command = Console.ReadLine().Trim();
                    switch (command)
                    {
                        case "STOP":
                        case "Stop":
                        case "stop":
                            if (th.IsAlive)
                            {
                                tcp.Stop();
                                th.Abort();
                                Console.WriteLine("Работа сервера остановлена.Нажмите любую клавишу для продолжения");
                                Console.ReadKey();
                                return;
                            }
                            break;
                        case "Help":
                        case "HELP":
                        case "help":
                            Console.WriteLine("stop - остановка сервера");
                            Console.WriteLine("cls - очистка консоли");
                            break;
                        case "Cls":
                        case "CLS":
                        case "cls":
                            Console.Clear();
                            break;
                        case "":
                        default:
                            Console.WriteLine("Для получения списка команд введите: help");
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Ошибка ввода [Exception]");
                Main(null);
            }
            catch (SQLiteException ex)//Проблемы с экзепшоном
            {
                //Ловим исключения при работе с БД
                Console.WriteLine("Проблемы с базой данных");
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                tcp.Stop();
                //th.Abort();
                Console.WriteLine("Работа сервера остановлена.Нажмите любую клавишу для продолжения");
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
                //Console.Clear();
                Console.WriteLine("Порт: " + Port);

                while (true)
                {
                    TcpClient client = tcp.AcceptTcpClient();
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
                    Console.WriteLine("Запрос: " + message);
                    string[] buf = message.Split('_');

                    //---
                    //ПЕРЕМЕННЫЕ!
                    Commands CommandCenter = new Commands();
                    //---
                    switch (buf[0])
                    {
                        case "login":
                            CommandCenter.Login(buf, ref database_commands, ref stream);
                            break;
                        case "mainscreen":
                            CommandCenter.Mainscreen(buf, ref stream);
                            break;
                        case "testslist":
                            CommandCenter.Testlist(buf, ref database_commands, ref stream);
                            break;
                        case "test":
                            CommandCenter.Test(buf, ref stream);
                            break;
                        case "testresult":
                            CommandCenter.TestResult(buf, ref database_commands);
                            break;
                        case "dateresult":
                            CommandCenter.DateResults(buf, ref stream);
                            break;
                        default:
                            Console.WriteLine("default: Неопознанный case");
                            break;
                    }
                    //
                    stream.Close();//Закрываем поток, когда закончили работать
                    Console.WriteLine("-------------------------\nПоток закрыт\n-------------------------");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
        }
    }
}