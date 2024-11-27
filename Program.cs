using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    // Спільний журнал операцій
    static ConcurrentQueue<Operation> operationLog = new ConcurrentQueue<Operation>();

    // Стан ресурсів
    static Dictionary<string, string> resourceState = new Dictionary<string, string>();
    static object resourceLock = new object();

    // Черга конфліктів
    static ConcurrentQueue<Conflict> conflictQueue = new ConcurrentQueue<Conflict>();
    static ManualResetEvent conflictEvent = new ManualResetEvent(false);

    static void Main(string[] args)
    {
        // Ініціалізація потоків
        Thread[] threads = new Thread[5];
        Random rand = new Random();

        for (int i = 0; i < threads.Length; i++)
        {
            int threadId = i;
            threads[i] = new Thread(() => PerformOperations(threadId, rand));
            threads[i].Start();
        }

        // Потік для обробки конфліктів
        Thread conflictResolverThread = new Thread(ResolveConflicts);
        conflictResolverThread.Start();

        foreach (var thread in threads)
            thread.Join();

        // Завершення роботи обробника конфліктів
        conflictEvent.Set();
        conflictResolverThread.Join();

        Console.WriteLine("\nЖурнал операцій:");
        foreach (var operation in operationLog)
            Console.WriteLine(operation);
    }

    static void PerformOperations(int threadId, Random rand)
    {
        for (int i = 0; i < 5; i++) // Кожен потік виконує 5 операцій
        {
            string resourceName = $"Resource{rand.Next(1, 4)}"; // Випадковий ресурс
            string newValue = $"Value{rand.Next(1, 100)}";

            var operation = new Operation
            {
                Timestamp = DateTime.UtcNow,
                ThreadId = threadId,
                Resource = resourceName,
                NewValue = newValue
            };

            lock (resourceLock)
            {
                if (resourceState.TryGetValue(resourceName, out string currentValue) && currentValue != newValue)
                {
                    // Конфлікт
                    Console.WriteLine($"[Конфлікт] Потік {threadId} намагається змінити {resourceName}. Поточне значення: {currentValue}, нове: {newValue}");
                    conflictQueue.Enqueue(new Conflict
                    {
                        Resource = resourceName,
                        Operation1 = new Operation { Timestamp = DateTime.UtcNow, Resource = resourceName, NewValue = currentValue },
                        Operation2 = operation
                    });
                    conflictEvent.Set();
                }
                else
                {
                    // Операція без конфлікту
                    resourceState[resourceName] = newValue;
                    operationLog.Enqueue(operation);
                    Console.WriteLine($"[Успішно] Потік {threadId} змінив {resourceName} на {newValue}");
                }
            }

            Thread.Sleep(rand.Next(100, 500)); // Затримка для моделювання роботи
        }
    }

    static void ResolveConflicts()
    {
        while (true)
        {
            conflictEvent.WaitOne(); // Очікування нових конфліктів

            while (conflictQueue.TryDequeue(out var conflict))
            {
                Console.WriteLine($"[Розв'язання] Конфлікт для {conflict.Resource}:");
                Console.WriteLine($"    Операція 1: {conflict.Operation1}");
                Console.WriteLine($"    Операція 2: {conflict.Operation2}");

                // Проста політика: вибір останньої операції за часовою міткою
                var resolvedOperation = conflict.Operation2.Timestamp > conflict.Operation1.Timestamp
                    ? conflict.Operation2
                    : conflict.Operation1;

                lock (resourceLock)
                {
                    resourceState[conflict.Resource] = resolvedOperation.NewValue;
                    operationLog.Enqueue(resolvedOperation);
                    Console.WriteLine($"[Рішення] {conflict.Resource} встановлено в {resolvedOperation.NewValue}");
                }
            }

            if (conflictQueue.IsEmpty)
                conflictEvent.Reset(); // Повернення у режим очікування
        }
    }
}

// Клас для моделювання операції
class Operation
{
    public DateTime Timestamp { get; set; }
    public int ThreadId { get; set; }
    public string Resource { get; set; }
    public string NewValue { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss.fff}] Потік {ThreadId} змінив {Resource} на {NewValue}";
    }
}

// Клас для моделювання конфлікту
class Conflict
{
    public string Resource { get; set; }
    public Operation Operation1 { get; set; }
    public Operation Operation2 { get; set; }
}
