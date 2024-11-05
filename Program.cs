using Microsoft.EntityFrameworkCore;
using MyAPIProject.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text;

namespace MyAPIProject
{
    public class TensorFlowResponse
    {
        public string? message { get; set; }
        public string? computation { get; set; }
        public float[]? result { get; set; }
    }

    public class Program
    {
        private static bool IsCodeSnackIDE => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static int _selectedPort = 5000;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            builder.Services.AddDbContext<PrimaryDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            if (IsCodeSnackIDE)
            {
                for (int port = 5000; port < 5100; port++)
                {
                    try
                    {
                        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), port);
                        listener.Start();
                        listener.Stop();
                        _selectedPort = port;
                        Console.WriteLine($"Found available port: {_selectedPort}");
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                builder.WebHost.UseUrls($"http://127.0.0.1:{_selectedPort}");
            }

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();

            var apiThread = new Thread(() =>
            {
                try
                {
                    if (IsCodeSnackIDE)
                    {
                        app.Run($"http://127.0.0.1:{_selectedPort}");
                    }
                    else
                    {
                        app.Run();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API Error: {ex.Message}");
                }
            });
            apiThread.Start();

            Thread.Sleep(2000);

            RunConsoleInterface().GetAwaiter().GetResult();
        }

        private static void DrawBorder(string title = "")
        {
            int width = Console.WindowWidth - 2;
            string horizontal = new string('═', width);
            string titleBar = String.Empty;

            if (!string.IsNullOrEmpty(title))
            {
                int padding = (width - title.Length) / 2;
                titleBar = '╣' + new string(' ', padding - 1) + title + new string(' ', width - padding - title.Length) + '╠';
            }

            Console.WriteLine("╔" + horizontal + "╗");
            if (!string.IsNullOrEmpty(titleBar))
                Console.WriteLine(titleBar);
        }

        private static void DrawBottomBorder()
        {
            Console.WriteLine("╚" + new string('═', Console.WindowWidth - 2) + "╝");
        }

        private static void DisplayStatusMessage(string message, bool isError = false)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = isError ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"\nSTATUS: {message}");
            Console.ForegroundColor = originalColor;
        }

        private static async Task RunConsoleInterface()
        {
            var baseUrl = IsCodeSnackIDE
                ? $"http://127.0.0.1:{_selectedPort}/api/Products"
                : "http://localhost:5000/api/Products";

            var client = new HttpClient();
            bool firstRun = true;

            while (true)
            {
                Console.Clear();
                DrawBorder("Products API Console Interface");
                Console.WriteLine($"║ Environment: {(IsCodeSnackIDE ? "CodeSnack IDE" : "Windows")}");
                Console.WriteLine($"║ API URL: {baseUrl}");
                Console.WriteLine($"║ Status: Active");
                Console.WriteLine($"║ Time: {DateTime.Now}");
                DrawBorder();

                if (firstRun)
                {
                    await GetAllProducts(client, baseUrl);
                    firstRun = false;
                }

                Console.WriteLine("║ Menu Options:");
                Console.WriteLine("║ 1. ├── Refresh Products List");
                Console.WriteLine("║ 2. ├── Get Product by ID");
                Console.WriteLine("║ 3. ├── Run TensorFlow Demo");
                Console.WriteLine("║ 4. ├── Run Custom Logic");
                Console.WriteLine("║ 5. └── Exit");
                Console.Write("\n║ Enter your choice (1-5): ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await GetAllProducts(client, baseUrl);
                            break;
                        case "2":
                            await GetProductById(client, baseUrl);
                            break;
                        case "3":
                            await RunTensorFlowDemo(client, baseUrl);
                            break;
                        case "4":
                            await RunCustomLogic(client, baseUrl);
                            break;
                        case "5":
                            DisplayStatusMessage("Shutting down...");
                            Thread.Sleep(1000);
                            Environment.Exit(0);
                            return;
                        default:
                            DisplayStatusMessage("Invalid choice. Please try again.", true);
                            break;
                    }
                }
                catch (HttpRequestException ex)
                {
                    DisplayStatusMessage($"API Error: {ex.Message}", true);
                    DisplayStatusMessage("Make sure the API is running and the URL is correct.", true);
                }
                catch (Exception ex)
                {
                    DisplayStatusMessage($"Error: {ex.Message}", true);
                }

                DrawBottomBorder();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static async Task GetAllProducts(HttpClient client, string baseUrl)
        {
            DisplayStatusMessage("Fetching products...");
            var products = await client.GetFromJsonAsync<List<Product>>(baseUrl);

            if (products != null && products.Any())
            {
                DrawBorder("Products List");
                foreach (var product in products)
                {
                    Console.WriteLine("║ ┌──────────────────────────");
                    Console.WriteLine($"║ │ Product ID: {product.IdProduct}");
                    var json = JsonSerializer.Serialize(product, new JsonSerializerOptions { WriteIndented = true });
                    foreach (var line in json.Split('\n'))
                    {
                        Console.WriteLine($"║ │ {line}");
                    }
                    Console.WriteLine("║ └──────────────────────────");
                }
                DisplayStatusMessage($"Found {products.Count} products");
            }
            else
            {
                DisplayStatusMessage("No products found", true);
            }
        }

        private static async Task GetProductById(HttpClient client, string baseUrl)
        {
            Console.Write("║ Enter product ID: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                DisplayStatusMessage($"Fetching product {id}...");
                var response = await client.GetAsync($"{baseUrl}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var product = await response.Content.ReadFromJsonAsync<Product>();
                    DrawBorder($"Product {id} Details");
                    var json = JsonSerializer.Serialize(product, new JsonSerializerOptions { WriteIndented = true });
                    foreach (var line in json.Split('\n'))
                    {
                        Console.WriteLine($"║ {line}");
                    }
                    DisplayStatusMessage("Product found successfully");
                }
                else
                {
                    DisplayStatusMessage($"Product with ID {id} not found", true);
                }
            }
            else
            {
                DisplayStatusMessage("Invalid ID format", true);
            }
        }

        private static async Task RunTensorFlowDemo(HttpClient client, string baseUrl)
        {
            DisplayStatusMessage("Running TensorFlow demo...");
            var response = await client.GetAsync($"{baseUrl}/tensorflow-hello");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TensorFlowResponse>(content);

                    DrawBorder("TensorFlow Results");
                    if (result != null)
                    {
                        Console.WriteLine($"║ Message: {result.message}");
                        Console.WriteLine($"║ Computation: {result.computation}");
                        Console.WriteLine($"║ Result: [{string.Join(", ", result.result ?? Array.Empty<float>())}]");
                        DisplayStatusMessage("TensorFlow demo completed successfully");
                    }
                    else
                    {
                        DisplayStatusMessage("Failed to parse TensorFlow response", true);
                    }
                }
                catch (JsonException ex)
                {
                    DisplayStatusMessage($"JSON Parsing Error: {ex.Message}", true);
                    var rawContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"║ Raw Response: {rawContent}");
                }
            }
            else
            {
                DisplayStatusMessage("Failed to run TensorFlow demo", true);
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"║ Error Response: {errorContent}");
            }
        }

        private static async Task RunCustomLogic(HttpClient client, string baseUrl)
        {
            Console.Write("║ Enter your string input: ");
            var input = Console.ReadLine() ?? string.Empty;

            DisplayStatusMessage("Running custom logic...");

            var content = new StringContent($"\"{input}\"", Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/custom-logic", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                DrawBorder("Custom Logic Results");
                Console.WriteLine($"║ Response: {result}");
                DisplayStatusMessage("Custom logic executed successfully");
            }
            else
            {
                DisplayStatusMessage("Failed to execute custom logic", true);
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"║ Error Response: {errorContent}");
            }
        }
    }
}