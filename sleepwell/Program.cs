using SleepWellApp;

namespace SleepWellApp
{
    class Program
    {
        private static bool _isSyncing = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("DB will be created at: " + Path.GetFullPath("sleepWell.db"));
            
            if (AppConfig.ClientId.Contains("YOUR_CLIENT_ID"))
            {
                Console.WriteLine("ERROR: You must update the AppConfig class with your real Client ID and Secret.");
                return;
            }

            Database.Initialize();

            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            // 1. Dashboard Page
            app.MapGet("/", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(DashboardView.GetHtml());
            });

            // 2. API Endpoint
            app.MapGet("/api/sleepdata", async (HttpContext context) =>
            {
                if (!int.TryParse(context.Request.Query["days"], out int days) || days <= 0) days = 7;
                var data = Database.GetSleepDataSummary(days);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(data);
            });

            // 3. Sync Trigger
            app.MapGet("/sync/start", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html";

                if (_isSyncing)
                {
                    await context.Response.WriteAsync("<h1>Sync is already in progress!</h1><p>Please check your terminal.</p>");
                    return;
                }

                _isSyncing = true;
                await context.Response.WriteAsync("<h1>Sync Started!</h1><p>Check your application console/terminal to see progress.</p><p>You can close this tab.</p>");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine("\n=== Starting Manual Sync ===");
                        
                        await FitbitAuthService.Authenticate();

                        int durationInt = 60;
                        DateTime today = DateTime.Today;
                        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "sleepOut.txt");

                        for (int i = 0; i < durationInt; i++)
                        {
                            DateTime targetDate = today.AddDays(-i);
                            string dateString = targetDate.ToString("yyyy-MM-dd");
                            Console.WriteLine($"Fetching data for {dateString}...");

                            await FitbitDataService.GetSleepData(dateString, filePath);

                            await Task.Delay(1500);
                        }
                        Console.WriteLine("\n=== Sync Complete! You may now refresh the dashboard. ===");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sync Error: {ex.Message}");
                    }
                    finally
                    {
                        _isSyncing = false;
                    }
                });
            });

            Console.WriteLine("\n Web Server Running. Go to http://localhost:5000");
            await app.RunAsync();
        }
    }
}