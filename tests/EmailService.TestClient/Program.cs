using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace EmailService.TestClient;

class Program
{
    private static readonly HttpClient _httpClient = new();
    private const string BaseUrl = "http://localhost:52563";
    private const string ApiKey = "test-api-key-12345"; // Replace with actual tenant API key

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Email Service Test Client ===\n");

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. Send single email");
            Console.WriteLine("2. Send template email");
            Console.WriteLine("3. Send bulk emails");
            Console.WriteLine("4. Send bulk template emails");
            Console.WriteLine("5. Get email status");
            Console.WriteLine("6. Exit");
            Console.Write("\nYour choice: ");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await SendSingleEmail();
                        break;
                    case "2":
                        await SendTemplateEmail();
                        break;
                    case "3":
                        await SendBulkEmails();
                        break;
                    case "4":
                        await SendBulkTemplateEmails();
                        break;
                    case "5":
                        await GetEmailStatus();
                        break;
                    case "6":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine("\nGoodbye!");
    }

    static async Task SendSingleEmail()
    {
        Console.WriteLine("\n--- Send Single Email ---");

        Console.Write("To Email: ");
        var toEmail = Console.ReadLine();

        Console.Write("Subject: ");
        var subject = Console.ReadLine();

        Console.Write("Body: ");
        var body = Console.ReadLine();

        Console.Write("Send Immediately? (y/n): ");
        var sendImmediately = Console.ReadLine()?.ToLower() == "y";

        var request = new
        {
            toEmail,
            subject,
            body,
            isHtml = true,
            priority = 1
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/email/send?sendImmediately={sendImmediately}", content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"\nResponse ({response.StatusCode}):");
        Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented));
    }

    static async Task SendTemplateEmail()
    {
        Console.WriteLine("\n--- Send Template Email ---");

        Console.Write("Template ID (GUID): ");
        var templateId = Console.ReadLine();

        Console.Write("To Email: ");
        var toEmail = Console.ReadLine();

        Console.Write("Send Immediately? (y/n): ");
        var sendImmediately = Console.ReadLine()?.ToLower() == "y";

        var request = new
        {
            templateId,
            toEmail,
            templateData = new Dictionary<string, object>
            {
                { "firstName", "John" },
                { "lastName", "Doe" },
                { "verificationCode", "123456" }
            }
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/email/send-template?sendImmediately={sendImmediately}", content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"\nResponse ({response.StatusCode}):");
        Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented));
    }

    static async Task SendBulkEmails()
    {
        Console.WriteLine("\n--- Send Bulk Emails ---");

        Console.Write("Number of emails to send: ");
        var count = int.Parse(Console.ReadLine() ?? "1");

        Console.Write("Send Immediately? (y/n): ");
        var sendImmediately = Console.ReadLine()?.ToLower() == "y";

        var emails = new List<object>();
        for (int i = 1; i <= count; i++)
        {
            emails.Add(new
            {
                toEmail = $"test{i}@example.com",
                subject = $"Test Email {i}",
                body = $"<h1>This is test email #{i}</h1><p>Sent via bulk endpoint</p>",
                isHtml = true,
                priority = 1
            });
        }

        var request = new { emails };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/email/send-bulk?sendImmediately={sendImmediately}", content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"\nResponse ({response.StatusCode}):");
        Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented));
    }

    static async Task SendBulkTemplateEmails()
    {
        Console.WriteLine("\n--- Send Bulk Template Emails ---");

        Console.Write("Template ID (GUID): ");
        var templateId = Console.ReadLine();

        Console.Write("Number of recipients: ");
        var count = int.Parse(Console.ReadLine() ?? "1");

        Console.Write("Send Immediately? (y/n): ");
        var sendImmediately = Console.ReadLine()?.ToLower() == "y";

        var recipients = new List<object>();
        for (int i = 1; i <= count; i++)
        {
            recipients.Add(new
            {
                toEmail = $"test{i}@example.com",
                toName = $"Test User {i}",
                templateData = new Dictionary<string, object>
                {
                    { "firstName", $"Test{i}" },
                    { "code", $"{i}23456" }
                }
            });
        }

        var request = new
        {
            templateId,
            recipients
        };

        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/email/send-bulk-template?sendImmediately={sendImmediately}", content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"\nResponse ({response.StatusCode}):");
        Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented));
    }

    static async Task GetEmailStatus()
    {
        Console.WriteLine("\n--- Get Email Status ---");

        Console.Write("Email ID (GUID): ");
        var emailId = Console.ReadLine();

        var response = await _httpClient.GetAsync($"/api/email/{emailId}");
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"\nResponse ({response.StatusCode}):");
        Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(result), Formatting.Indented));
    }
}
