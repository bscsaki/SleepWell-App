using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SleepWellApp
{
    public static class FitbitAuthService
    {
        public static string AccessToken { get; private set; } = "";
        public static string RefreshToken { get; private set; } = "";

        public static async Task Authenticate()
        {
            var authUrl = $"https://www.fitbit.com/oauth2/authorize?response_type=code&client_id={AppConfig.ClientId}&redirect_uri={Uri.EscapeDataString(AppConfig.RedirectUri)}&scope={AppConfig.Scope}";
            
            Console.WriteLine("Opening browser for authentication...");
            OpenBrowser(authUrl);

            var authCode = await ListenForAuthorizationCode();
            await ExchangeCodeForToken(authCode);
        }

        private static async Task<string> ListenForAuthorizationCode()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(AppConfig.RedirectUri + "/"); 
            listener.Start();

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            var response = context.Response;
            string responseString = "<html><body><h1>Authorization Successful!</h1><p>You can close this tab.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            listener.Stop();
            return code;
        }

        private static async Task ExchangeCodeForToken(string authCode)
        {
            using (var client = new HttpClient())
            {
                var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AppConfig.ClientId}:{AppConfig.ClientSecret}"));
                
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", AppConfig.ClientId),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", AppConfig.RedirectUri),
                    new KeyValuePair<string, string>("code", authCode)
                });
                var response = await client.PostAsync("https://api.fitbit.com/oauth2/token", formData);
                var content = await response.Content.ReadAsStringAsync();
                
                if(!response.IsSuccessStatusCode)
                {
                    throw new Exception("Auth failed: " + content);
                }

                var json = JObject.Parse(content);
                AccessToken = json["access_token"].ToString();
                RefreshToken = json["refresh_token"].ToString();
            }
        }

        private static void OpenBrowser(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { Console.WriteLine($"Open URL manually: {url}"); }
        }
    }
}