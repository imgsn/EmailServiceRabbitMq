using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;

namespace EmailService.RabbitMQTestClient;

class Program
{
    private static IConnection? _connection;
    private static IModel? _channel;
    private const string QueueName = "email-queue";

    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     RabbitMQ Test Client - Email Service              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Configuration
        var config = new RabbitMQConfig
        {
            HostName = "38.242.135.48",
            Port = 5672,
            UserName = "admin",
            Password = "admin123",
            VirtualHost = "/"
        };

        // Display configuration
        Console.WriteLine("📋 Configuration:");
        Console.WriteLine($"   Host: {config.HostName}:{config.Port}");
        Console.WriteLine($"   VirtualHost: {config.VirtualHost}");
        Console.WriteLine($"   Queue: {QueueName}");
        Console.WriteLine();

        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n═══════════════════════════════════════");
            Console.WriteLine("Select an option:");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("1. Test Connection");
            Console.WriteLine("2. Publish Single Email Message");
            Console.WriteLine("3. Publish Bulk Email Messages");
            Console.WriteLine("4. Listen for Messages (Consumer)");
            Console.WriteLine("5. Check Queue Stats");
            Console.WriteLine("6. Purge Queue (Delete all messages)");
            Console.WriteLine("7. View RabbitMQ Management UI Info");
            Console.WriteLine("8. Exit");
            Console.WriteLine("═══════════════════════════════════════");
            Console.Write("\nYour choice: ");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await TestConnection(config);
                        break;
                    case "2":
                        await PublishSingleMessage(config);
                        break;
                    case "3":
                        await PublishBulkMessages(config);
                        break;
                    case "4":
                        await ListenForMessages(config);
                        break;
                    case "5":
                        await CheckQueueStats(config);
                        break;
                    case "6":
                        await PurgeQueue(config);
                        break;
                    case "7":
                        ShowManagementInfo();
                        break;
                    case "8":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("❌ Invalid option");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }

            if (!exit)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        // Cleanup
        _channel?.Close();
        _connection?.Close();
        Console.WriteLine("\n👋 Goodbye!");
    }

    static async Task TestConnection(RabbitMQConfig config)
    {
        Console.WriteLine("\n🔌 Testing RabbitMQ Connection...");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
                VirtualHost = config.VirtualHost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            Console.WriteLine("✅ Connection successful!");
            Console.WriteLine($"   Connection: {connection.Endpoint}");
            Console.WriteLine($"   IsOpen: {connection.IsOpen}");

            // Declare queue to ensure it exists
            var queueDeclareOk = channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"✅ Queue '{QueueName}' verified");
            Console.WriteLine($"   Messages: {queueDeclareOk.MessageCount}");
            Console.WriteLine($"   Consumers: {queueDeclareOk.ConsumerCount}");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            throw;
        }
    }

    static async Task PublishSingleMessage(RabbitMQConfig config)
    {
        Console.WriteLine("\n📤 Publishing Single Message...");

        Console.Write("Enter Email Queue ID (or press Enter for random GUID): ");
        var input = Console.ReadLine();
        var emailQueueId = string.IsNullOrWhiteSpace(input) ? Guid.NewGuid() : Guid.Parse(input);

        var message = new EmailQueueMessage
        {
            EmailQueueId = emailQueueId
        };

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.DeliveryMode = 2;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: QueueName,
            basicProperties: properties,
            body: body);

        Console.WriteLine($"✅ Message published successfully!");
        Console.WriteLine($"   Queue: {QueueName}");
        Console.WriteLine($"   EmailQueueId: {emailQueueId}");
        Console.WriteLine($"   Message: {json}");

        await Task.CompletedTask;
    }

    static async Task PublishBulkMessages(RabbitMQConfig config)
    {
        Console.WriteLine("\n📤 Publishing Bulk Messages...");

        Console.Write("How many messages to publish? ");
        if (!int.TryParse(Console.ReadLine(), out int count) || count <= 0)
        {
            Console.WriteLine("❌ Invalid number");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.DeliveryMode = 2;

        Console.WriteLine($"\n📨 Publishing {count} messages...");
        var startTime = DateTime.Now;

        for (int i = 1; i <= count; i++)
        {
            var message = new EmailQueueMessage
            {
                EmailQueueId = Guid.NewGuid()
            };

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: QueueName,
                basicProperties: properties,
                body: body);

            if (i % 100 == 0 || i == count)
            {
                Console.Write($"\r   Progress: {i}/{count} ({(i * 100.0 / count):F1}%)");
            }
        }

        var elapsed = DateTime.Now - startTime;
        Console.WriteLine($"\n✅ Published {count} messages successfully!");
        Console.WriteLine($"   Time taken: {elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"   Rate: {count / elapsed.TotalSeconds:F0} messages/second");

        await Task.CompletedTask;
    }

    static async Task ListenForMessages(RabbitMQConfig config)
    {
        Console.WriteLine("\n👂 Starting Message Listener...");
        Console.WriteLine("   Press Ctrl+C to stop listening");
        Console.WriteLine();

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        int messageCount = 0;

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            messageCount++;

            Console.WriteLine($"📨 Message #{messageCount} Received:");
            Console.WriteLine($"   Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   Message: {message}");
            Console.WriteLine($"   Delivery Tag: {ea.DeliveryTag}");
            Console.WriteLine();

            // Acknowledge the message
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            Console.WriteLine($"   ✅ Acknowledged");
            Console.WriteLine("   ─────────────────────────────────────");
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        Console.WriteLine("✅ Listener started. Waiting for messages...");
        Console.WriteLine("   Press Enter to stop");

        // Wait for user to press Enter
        await Task.Run(() => Console.ReadLine());

        Console.WriteLine($"\n📊 Statistics:");
        Console.WriteLine($"   Total messages received: {messageCount}");
    }

    static async Task CheckQueueStats(RabbitMQConfig config)
    {
        Console.WriteLine("\n📊 Checking Queue Statistics...");

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var queueDeclareOk = channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        Console.WriteLine($"\n📈 Queue: {QueueName}");
        Console.WriteLine($"   ├─ Messages Ready: {queueDeclareOk.MessageCount}");
        Console.WriteLine($"   ├─ Active Consumers: {queueDeclareOk.ConsumerCount}");
        Console.WriteLine($"   └─ Queue Name: {queueDeclareOk.QueueName}");

        await Task.CompletedTask;
    }

    static async Task PurgeQueue(RabbitMQConfig config)
    {
        Console.WriteLine("\n⚠️  WARNING: This will delete ALL messages in the queue!");
        Console.Write("Are you sure? (yes/no): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("❌ Operation cancelled");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var purgedCount = channel.QueuePurge(QueueName);

        Console.WriteLine($"✅ Queue purged successfully!");
        Console.WriteLine($"   Messages deleted: {purgedCount}");

        await Task.CompletedTask;
    }

    static void ShowManagementInfo()
    {
        Console.WriteLine("\n🌐 RabbitMQ Management UI Information:");
        Console.WriteLine("   ═══════════════════════════════════════");
        Console.WriteLine("   URL: http://localhost:15672");
        Console.WriteLine("   Default Username: guest");
        Console.WriteLine("   Default Password: guest");
        Console.WriteLine("   ═══════════════════════════════════════");
        Console.WriteLine("\n   From the Management UI you can:");
        Console.WriteLine("   • View queue statistics");
        Console.WriteLine("   • Monitor message rates");
        Console.WriteLine("   • See active connections");
        Console.WriteLine("   • Manage exchanges and bindings");
        Console.WriteLine("   • View detailed metrics");
    }

    class EmailQueueMessage
    {
        public Guid EmailQueueId { get; set; }
    }

    class RabbitMQConfig
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
}