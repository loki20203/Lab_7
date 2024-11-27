public class Program
{
    public static async Task Main()
    {
        var nodeA = new DistributedSystemNode("A");
        var nodeB = new DistributedSystemNode("B");
        var nodeC = new DistributedSystemNode("C");

        nodeA.AddNode(nodeB);
        nodeA.AddNode(nodeC);
        nodeB.AddNode(nodeA);
        nodeB.AddNode(nodeC);
        nodeC.AddNode(nodeA);
        nodeC.AddNode(nodeB);

        // Старт обробки подій
        var processTasks = new List<Task>
        {
            nodeA.ProcessEventsAsync(),
            nodeB.ProcessEventsAsync(),
            nodeC.ProcessEventsAsync()
        };

        // Відправлення повідомлень
        await nodeA.SendMessageAsync("B", "Hello from A to B");
        await nodeB.SendMessageAsync("C", "Hello from B to C");
        await nodeC.SendMessageAsync("A", "Hello from C to A");

        // Зміна статусу
        nodeA.SetStatus(false);
        nodeB.SetStatus(true);

        // Очікування завершення обробки подій (для демонстраційних цілей)
        await Task.Delay(5000);

        // Зупинка обробки подій
        nodeA.SetStatus(false);
        nodeB.SetStatus(false);
        nodeC.SetStatus(false);

        await Task.WhenAll(processTasks);
    }
}