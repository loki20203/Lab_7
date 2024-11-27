using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

public class DistributedSystemNode
{
    private readonly string nodeId;
    private readonly Dictionary<string, DistributedSystemNode> nodes;
    private readonly List<string> messages;
    private bool isActive;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    public DistributedSystemNode(string id)
    {
        nodeId = id;
        nodes = new Dictionary<string, DistributedSystemNode>();
        messages = new List<string>();
        isActive = true;
    }

    public string NodeId => nodeId;

    // Метод для додавання вузла
    public void AddNode(DistributedSystemNode node)
    {
        nodes[node.NodeId] = node;
    }

    // Метод для видалення вузла
    public void RemoveNode(string id)
    {
        nodes.Remove(id);
    }

    // Метод для відправлення повідомлення
    public async Task SendMessageAsync(string receiverId, string message)
    {
        if (nodes.ContainsKey(receiverId))
        {
            await nodes[receiverId].ReceiveMessageAsync(message);
        }
        else
        {
            Console.WriteLine($"Node {receiverId} not found.");
        }
    }

    // Метод для отримання повідомлення
    public async Task ReceiveMessageAsync(string message)
    {
        await semaphore.WaitAsync();
        try
        {
            messages.Add(message);
            Console.WriteLine($"{nodeId} received message: {message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Метод для перевірки статусу вузла
    public void SetStatus(bool active)
    {
        isActive = active;
        NotifyStatusChange();
    }

    // Метод для оповіщення про зміну статусу
    private void NotifyStatusChange()
    {
        string status = isActive ? "active" : "inactive";
        foreach (var node in nodes.Values)
        {
            node.ReceiveStatusNotificationAsync(nodeId, status).Wait();
        }
    }

    // Метод для отримання сповіщення про зміну статусу
    public async Task ReceiveStatusNotificationAsync(string senderId, string status)
    {
        await semaphore.WaitAsync();
        try
        {
            Console.WriteLine($"{nodeId} received status update from {senderId}: {status}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Метод для асинхронної обробки подій
    public async Task ProcessEventsAsync()
    {
        while (isActive)
        {
            await Task.Delay(1000); // Імітація обробки подій
            Console.WriteLine($"{nodeId} is processing events.");
        }
    }
}