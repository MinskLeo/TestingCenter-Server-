﻿using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
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
        private static string command;
        private static SQLiteConnection database;
        private static SQLiteCommand database_commands;
        private static SQLiteDataReader database_reader;

        static void Main(string[] args)
        {
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
                Console.WriteLine("Server started!"+DateTime.Now);

                //Тут блок связанный с БД
                if (!File.Exists("databases\\Students.db"))
                    throw new SQLiteException ();
                database = new SQLiteConnection("Data Source=databases\\Students.db;Version=3;UTF8Encoding=True;");
                database.Open();//Открываем БД
                database_commands = database.CreateCommand();//Создаем командный объект
                database_reader = database_commands.ExecuteReader();//Создаем ридер, способный считывать данные

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
            catch(SQLiteException ex)//Проблемы с экзепшоном
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
                Console.WriteLine("Port: "+Port);//DEBUG
                //ConsoleUpdatingInformation();//Возможно лучше всего будет выкинуть ненужную хуиту с проекта. Только консоль засоряет 
                //как все воркать будет уберем из консоли все ненужное
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
                    Console.WriteLine("Request: " + message);
                    string[] buf = message.Split('_');
                    string send = null;//Сообщение ответа
                    byte[] response;//Переведенное сообщение ответа
                    string[] TestsList;
                    //Тут будут описаны варианты запросов
                    //buf[0]==login это сообщение от логин скрина
                    switch (buf[0])
                    {
                        case "login":
                            //Поиск по базе данных
                            //До сюда---------------------------------------------------------------

                            send = "login_Сюняков_Андрей_Андреевич";//login_NNN    //DEBUG
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