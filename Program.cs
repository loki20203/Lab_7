using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    static DistributedSystem distributedSystem = new DistributedSystem();

    static void Main(string[] args)
    {
        // Додавання вузлів у систему
        distributedSystem.AddNode("Node1");
        distributedSystem.AddNode("Node2");
        distributedSystem.AddNode("Node3");

        // Реєстрація обробників подій
        distributedSystem.Subscribe("Node1", evt => Console.WriteLine($"Node1 отримав подію: {evt}"));
        distributedSystem.Subscribe("Node2", evt => Console.WriteLine($"Node2 отримав подію: {evt}"));
        distributedSystem.Subscribe("Node3", evt => Console.WriteLine($"Node3 отримав подію: {evt}"));

        // Створення потоків для реєстрації подій
        var threads = new List<Thread>
        {
            new Thread(() => distributedSystem.TriggerEvent("Node1", "Подія A")),
            new Thread(() => distributedSystem.TriggerEvent("Node2", "Подія B")),
            new Thread(() => distributedSystem.TriggerEvent("Node3", "Подія C")),
        };

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Динамічне додавання вузла
        distributedSystem.AddNode("Node4");
        distributedSystem.Subscribe("Node4", evt => Console.WriteLine($"Node4 отримав подію: {evt}"));
        distributedSystem.TriggerEvent("Node4", "Подія D");

        // Динамічне видалення вузла
        distributedSystem.RemoveNode("Node2");

        // Реєстрація подій після видалення
        distributedSystem.TriggerEvent("Node1", "Подія E");
        distributedSystem.TriggerEvent("Node3", "Подія F");

        Console.WriteLine("\nЛог подій (впорядкований):");
        foreach (var evt in distributedSystem.GetEventLog())
        {
            Console.WriteLine(evt);
        }
    }
}

// Клас для моделювання події
class Event
{
    public string SourceNode { get; set; }
    public string EventDescription { get; set; }
    public int Timestamp { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp}] {SourceNode}: {EventDescription}";
    }
}

// Клас для вузла розподіленої системи
class Node
{
    public string NodeName { get; }
    public int LogicalClock { get; private set; }
    public Action<Event> EventHandler { get; private set; }

    public Node(string nodeName)
    {
        NodeName = nodeName;
        LogicalClock = 0;
    }

    public void SetEventHandler(Action<Event> handler)
    {
        EventHandler = handler;
    }

    public void TriggerEvent(string description, ConcurrentQueue<Event> globalEventLog)
    {
        lock (this)
        {
            LogicalClock++;
            var evt = new Event
            {
                SourceNode = NodeName,
                EventDescription = description,
                Timestamp = LogicalClock
            };

            globalEventLog.Enqueue(evt);
            EventHandler?.Invoke(evt);
        }
    }

    public void ReceiveEvent(Event evt)
    {
        lock (this)
        {
            LogicalClock = Math.Max(LogicalClock, evt.Timestamp) + 1;
            EventHandler?.Invoke(evt);
        }
    }
}

// Клас для розподіленої системи
class DistributedSystem
{
    private readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();
    private readonly ConcurrentQueue<Event> globalEventLog = new ConcurrentQueue<Event>();
    private readonly object nodeLock = new object();

    public void AddNode(string nodeName)
    {
        lock (nodeLock)
        {
            if (!nodes.ContainsKey(nodeName))
            {
                var node = new Node(nodeName);
                nodes[nodeName] = node;
                Console.WriteLine($"Вузол {nodeName} додано до системи.");
            }
        }
    }

    public void RemoveNode(string nodeName)
    {
        lock (nodeLock)
        {
            if (nodes.Remove(nodeName))
            {
                Console.WriteLine($"Вузол {nodeName} видалено з системи.");
            }
        }
    }

    public void Subscribe(string nodeName, Action<Event> handler)
    {
        lock (nodeLock)
        {
            if (nodes.TryGetValue(nodeName, out var node))
            {
                node.SetEventHandler(handler);
                Console.WriteLine($"Вузол {nodeName} підписано на події.");
            }
        }
    }

    public void TriggerEvent(string nodeName, string description)
    {
        lock (nodeLock)
        {
            if (nodes.TryGetValue(nodeName, out var node))
            {
                node.TriggerEvent(description, globalEventLog);

                // Передача події всім іншим вузлам
                foreach (var otherNode in nodes.Values.Where(n => n.NodeName != nodeName))
                {
                    foreach (var evt in globalEventLog)
                    {
                        otherNode.ReceiveEvent(evt);
                    }
                }
            }
        }
    }

    public List<Event> GetEventLog()
    {
        return globalEventLog.OrderBy(e => e.Timestamp).ToList();
    }
}
