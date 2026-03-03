using Newtonsoft.Json.Linq;

namespace SleepWellApp
{
    public static class FitbitDataService
    {
        public static async Task<JObject> GetSleepData(string date, string filePath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {FitbitAuthService.AccessToken}");
                var response = await client.GetAsync($"https://api.fitbit.com/1.2/user/-/sleep/date/{date}.json");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error fetching {date}: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var sleepData = JObject.Parse(content);
                var sleepArray = sleepData["sleep"] as JArray;

                JToken mainSleep = null;
                int medianHeartRate = 0;

                if (sleepArray != null && sleepArray.Any())
                {
                    var sleepEnumerable = sleepArray.Cast<JToken>();
                    mainSleep = sleepEnumerable.FirstOrDefault(s => s["isMainSleep"]?.Value<bool>() == true) ?? sleepArray[0];
                }

                if (mainSleep != null)
                {
                    string startTime = mainSleep["startTime"]?.ToString();
                    string endTime = mainSleep["endTime"]?.ToString();
                    var avgHR = mainSleep["averageHeartRate"];

                    if (avgHR != null) medianHeartRate = avgHR.Value<int>();

                    if (medianHeartRate == 0 && startTime != null && endTime != null)
                    {
                        medianHeartRate = await GetMedianHeartRateDuringSleep(date, startTime, endTime);
                    }
                }

                try { Database.SaveSleepSummary(date, sleepData, medianHeartRate); }
                catch (Exception ex) { Console.WriteLine($"DB Error: {ex.Message}"); }

                return sleepData;
            }
        }

        private static async Task<int> GetMedianHeartRateDuringSleep(string date, string startTime, string endTime)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {FitbitAuthService.AccessToken}");
                DateTime start = DateTime.Parse(startTime);
                DateTime end = DateTime.Parse(endTime);
                string url = $"https://api.fitbit.com/1/user/-/activities/heart/date/{start:yyyy-MM-dd}/{end:yyyy-MM-dd}/1min/time/{start:HH:mm}/{end:HH:mm}.json";
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return 0;
                
                return 0;
            }
        }
    }
}