using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    // Кількість ресурсів
    static int availableCPU = 3;
    static int availableRAM = 4;
    static int availableDisk = 2;

    // Об'єкти для синхронізації
    static SemaphoreSlim cpuSemaphore = new SemaphoreSlim(availableCPU);
    static SemaphoreSlim ramSemaphore = new SemaphoreSlim(availableRAM);
    static SemaphoreSlim diskSemaphore = new SemaphoreSlim(availableDisk);
    static object priorityLock = new object();

    // Список потоків з чергою за пріоритетами
    static SortedList<int, Queue<Action>> priorityQueues = new SortedList<int, Queue<Action>>();

    static void Main(string[] args)
    {
        // Ініціалізація потоків
        Thread[] threads = new Thread[10];
        Random rand = new Random();

        for (int i = 0; i < threads.Length; i++)
        {
            int priority = rand.Next(1, 4); // Пріоритет: 1 - високий, 3 - низький
            threads[i] = new Thread(() => SimulateWork(priority, i));
            threads[i].Start();
        }

        foreach (var thread in threads)
            thread.Join();

        Console.WriteLine("Усі потоки завершені.");
    }

    static void SimulateWork(int priority, int threadId)
    {
        lock (priorityLock)
        {
            if (!priorityQueues.ContainsKey(priority))
                priorityQueues[priority] = new Queue<Action>();

            priorityQueues[priority].Enqueue(() =>
            {
                Console.WriteLine($"Потік {threadId} із пріоритетом {priority} очікує ресурси.");

                // Отримання ресурсів
                cpuSemaphore.Wait();
                ramSemaphore.Wait();
                diskSemaphore.Wait();

                Console.WriteLine($"Потік {threadId} із пріоритетом {priority} отримав ресурси. Виконується...");
                Thread.Sleep(2000); // Симуляція роботи

                // Звільнення ресурсів
                cpuSemaphore.Release();
                ramSemaphore.Release();
                diskSemaphore.Release();

                Console.WriteLine($"Потік {threadId} із пріоритетом {priority} завершив роботу.");
            });
        }

        ProcessQueues();
    }

    static void ProcessQueues()
    {
        lock (priorityLock)
        {
            foreach (var queue in priorityQueues.OrderBy(p => p.Key)) // Вищий пріоритет - нижче число
            {
                while (queue.Value.Any())
                {
                    var action = queue.Value.Dequeue();
                    action.Invoke();
                }
            }
        }
    }
}
