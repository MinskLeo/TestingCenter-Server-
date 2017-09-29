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
                    Console.WriteLine("Порт (8888 по умолчанию):");
                    Port = Convert.ToInt32(Console.ReadLine().Trim());
                    if (Port <= 0)
                    {
                        Console.WriteLine("Ошибка!!! Порт не может быть меньше или равен нулю");
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


                //Тут блок связанный с БД
                if (!File.Exists("databases\\Students.db"))
                    throw new SQLiteException();
                database = new SQLiteConnection("Data Source=databases\\Students.db;Version=3;UTF8Encoding=True;");
                database.Open();//Открываем БД
                database_commands = database.CreateCommand();//Создаем командный объект
                Command = "";//Чисто чтобы не вылетал экзепшон
                database_commands.CommandText = Command;//Чисто чтобы не вылетал экзепшон
                Console.WriteLine("Статус : " + database.State);
                //Конец блока с БД
                while (true)
                {
                    command = Console.ReadLine().Trim();
                    switch (command)
                    {
                        case "/stop":
                            if (th.IsAlive)
                            {
                                tcp.Stop();
                                th.Abort();
                                Console.WriteLine("Работа сервера остановлена.Нажмите любую клавишу для продолжения");
                                Console.ReadKey();
                                return;
                            }
                            break;
                        case "/database":
                            Console.WriteLine("Database?:");
                            if (Console.ReadLine().Equals("info"))
                            {
                                Command = "SELECT count(*) FROM students";
                                database_commands.CommandText = Command;
                                Console.WriteLine("Rows: " + database_commands.ExecuteScalar());
                                //-------------------------------------------------------------------Тут было кол во строк\столбцев
                            }
                            break;

                        case "/help":
                            Console.WriteLine("/stop - остановка сервер \n/database - просмотр базы данных\n/clear_screen - очистка консоли");
                            break;
                        case "/clear_screen":
                            Console.Clear();
                            break;
                        default:
                            Console.WriteLine("Ошибка!!!\nДля получения списка команд введите /help");
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                Main(null);
            }
            catch (SQLiteException ex)//Проблемы с экзепшоном
            {
                //Ловим траблы с БД
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
                Console.Clear();//DEBUG
                Console.WriteLine("Порт: " + Port);//DEBUG

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
                    string send = "0";//Сообщение ответа. P.S. почему 0? Потому что null не хочет принимать в блоке списка тестов
                    byte[] response;//Переведенное сообщение ответа


                    //---
                    //ПЕРЕМЕННЫЕ!
                    object id, n, f, m, term=null, spec=null;//Поля для БД
                    BinaryFormatter binaryFormatter = new BinaryFormatter();//Сериализатор
                    List<string> result = new List<string>();//Список тестов
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
                                                     

                            response = Encoding.UTF8.GetBytes(send);
                            stream.Write(response, 0, response.Length);
                            Console.WriteLine("login: answ:" + send);
                            break;
                        case "mainscreen":
                            Command = "SELECT * FROM marks WHERE ID=" + buf[1] + ";";
                            //database_commands.CommandText= "SELECT count(*) FROM marks WHERE ID=" + buf[1] + ";";//DEBUG
                            //Console.WriteLine("Колчиество строк в ответе: "+database_commands.ExecuteScalar());//DEBUG
                            //Console.WriteLine("mainscreen: " + Command);

                            Console.WriteLine("mainscreen: Создаю адаптер");
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(Command, database.ConnectionString);
                            DataSet dataSet = new DataSet();
                            Console.WriteLine("mainscreen: Создал объект dataset");
                            adapter.Fill(dataSet);
                            Console.WriteLine("mainscreen: Заполнил объект");
                            //Тут был binaryFormatter
                            Console.WriteLine("mainscreen: Сериализую таблицу");
                            binaryFormatter.Serialize(stream, dataSet);//Сериализовали целый объект с выборкой по БД
                            Console.WriteLine("mainscreen: ГОТОВО!");
                            break;
                        case "testslist":
                            //Список возможных тестов
                            //testlist_id (немножко передумал вариант)
                            //тут надо подключиться к базе данных, и по ID проверить, какая у человека специальность и семестестр (ЗАПОлНИТЬ С БАЗЫ ДАННЫХ speciality)------------
                            Console.WriteLine("testslist: Начинаю работу");//DEBUG
                            Command = "SELECT * FROM students WHERE Id="+buf[1]+";";
                            //Console.WriteLine("testslist: КОМАНДА НА СПИСОК ТЕСТОВ: " + Command);
                            database_commands.CommandText = Command;
                            SQLiteDataReader r_testslist = database_commands.ExecuteReader();
                            Console.WriteLine("testslist: Начинаю проход по БД");//DEBUG
                            while (r_testslist.Read())
                            {
                                id = r_testslist.GetValue(0);
                                if (id.ToString().Equals(buf[1]))
                                {
                                    //Нам нужно узнать специальность и семестр
                                    term = r_testslist.GetValue(4);//Столбик с семестром
                                    spec = r_testslist.GetValue(6);//Столбик со специальностью
                                    //Console.WriteLine("TERM: "+term.ToString()+"\nSPEC: "+spec.ToString());
                                    //send = "testslist\\" + spec.ToString() + "_" + term.ToString();//попадет в if\else и выберет нужный файлик для отправки клиенту
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
                                //Тут надо это дописать------------------------------------------------
                                break;
                            }
                            else
                            {
                                if (spec != null && term != null)
                                {
                                    foreach (var a in Directory.GetFiles("tests"))
                                    {
                                        Console.WriteLine(a);
                                        if (a.Contains(spec.ToString() +"_"+ term.ToString()))
                                        {
                                            result.Add(a);//Добавляем в список возможных тестов тот что подходит по spec+term (POIT_3)
                                        }
                                    }
                                }
                                //send += ".txt";
                                //Console.WriteLine("FILE: " + send);//DEBUG
                                for(int i=0;i<result.Count;i++)
                                {
                                    Console.WriteLine("testslist: Res:" + result[i]);
                                }
                            }
                            r_testslist.Close();
                            //END
                            //string specialty = "POIT_2";//DEBUG----------------------------------------------------------------------
                            //string file = "testslist\\" + specialty + ".txt";//Файл с которого будет считывать вопросы и т.п.
                            if(result.Count!=0)
                            {
                                //Попробую. передать список, а принять как массив
                                Console.WriteLine("testslist: Запуск сериализации result");
                                binaryFormatter.Serialize(stream, result);
                                Console.WriteLine("testslist: Операция завершена. Отправлено!");
                            }
                            else
                            {
                                //Хз че тут писать. Надо обработать косяк отстутствия файлов с заданной комбинацией
                            }
                           
                            break;
                        case "test":
                            //test_Math_3
                            //test_POIT_3_Math
                            string[] TEST;
                            string way= "tests\\"+buf[1]+ "_" + buf[2] +"_"+buf[3]+".txt";//Пока что убрал tests\\ в начале строки, т.к. нам сразу строки с путями передаются. Потом фиксанем
                            Console.WriteLine("test: WAY: "+way);
                            if(File.Exists(way))
                            {
                                TEST = File.ReadAllLines(way);
                            }
                            else
                            {
                                TEST = new string[0];
                            }
                            Console.WriteLine("test: SIZE: " + TEST.Length);
                            binaryFormatter.Serialize(stream, TEST);
                            Console.WriteLine("test: Отправка данных окончена");
                            break;
                        case "testresult":
                            //Надо добавить данные в таблицу
                            //testresult_ID_Subject_Mark_Time
                            Command = "INSERT INTO marks (ID,Date,Time,Subject,Mark) VALUES('"
                                + buf[1] + "' ,'" + DateTime.Today.ToShortDateString() + "', '"
                                + buf[4] + "', '" + buf[2] + "', '" + buf[3] + "');";
                            Console.WriteLine("testresult: "+Command);
                            database_commands.CommandText = Command;
                            Console.WriteLine("testresult: Данные добавлены! STATUS: "+ database_commands.ExecuteNonQuery());
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