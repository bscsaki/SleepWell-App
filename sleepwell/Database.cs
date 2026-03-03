using Microsoft.Data.Sqlite;
using System;
using Newtonsoft.Json.Linq; 
using System.Linq; 
using System.Collections.Generic; // Added for KeyValuePair fix if needed

namespace SleepWellApp
{
    public static class Database
    {
        private const string ConnectionString = "Data Source=sleepWell.db";

        public static void Initialize()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var createSleepSummary = @"
                CREATE TABLE IF NOT EXISTS SleepSummary (
                    date TEXT PRIMARY KEY, 
                    start_time TEXT,
                    end_time TEXT,
                    duration_minutes INTEGER,
                    deep_minutes INTEGER,
                    light_minutes INTEGER,
                    rem_minutes INTEGER,
                    wake_minutes INTEGER,
                    efficiency INTEGER,
                    avg_hr INTEGER
                );
            ";

            using var cmd = new SqliteCommand(createSleepSummary, connection);
            cmd.ExecuteNonQuery();

            Console.WriteLine("✔ Database initialized and SleepSummary table created.");
        }
        
        // **CRITICAL FIX: Accept the date string explicitly**
        public static void SaveSleepSummary(string date, dynamic json, int medianHeartRate)
        {
            // Find the main sleep entry or null if none exists
            JToken mainSleep = null;
            var sleepArray = json?["sleep"] as JArray;

            if (sleepArray != null && sleepArray.Any())
            {
                // Find main sleep, using the safe JToken casting
                mainSleep = sleepArray.Cast<JToken>()
                    .FirstOrDefault(s => s["isMainSleep"]?.Value<bool>() == true) ?? sleepArray[0];
            }
            
            // --- Initialize Variables (Null/Placeholders for 'No Data') ---
            string startTime = null;
            string endTime = null;
            int? durationMinutes = null; 
            int? efficiency = null;
            int? deep = null;
            int? light = null;
            int? rem = null;
            int? wake = null;
            int? avgHr = null; 

            // --- Extract Data Only if Main Sleep Exists ---
            if (mainSleep != null)
            {
                startTime = mainSleep["startTime"]?.ToString();
                endTime = mainSleep["endTime"]?.ToString();
                
                durationMinutes = mainSleep["duration"]?.Value<int>() / 60000;
                efficiency = mainSleep["efficiency"]?.Value<int>();

                var summary = mainSleep["levels"]?["summary"];

                if (mainSleep["type"]?.ToString() == "stages" && summary != null)
                {
                    deep = summary["deep"]?["minutes"]?.Value<int>();
                    light = summary["light"]?["minutes"]?.Value<int>();
                    rem = summary["rem"]?["minutes"]?.Value<int>();
                    wake = summary["wake"]?["minutes"]?.Value<int>();
                }
                else if (mainSleep["type"]?.ToString() == "classic" && summary != null)
                {
                    // For classic, only capture awake time
                    wake = summary["awake"]?["minutes"]?.Value<int>();
                }
            }

            // Always set avg_hr from the value calculated in Program.cs
            if (medianHeartRate > 0)
            {
                avgHr = medianHeartRate;
            }

            // --- Database Operation: Insert or Replace ---
            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO SleepSummary 
                    (date, start_time, end_time, duration_minutes, deep_minutes, light_minutes, rem_minutes, wake_minutes, efficiency, avg_hr)
                    VALUES ($date, $start, $end, $duration, $deep, $light, $rem, $wake, $efficiency, $avgHr)
                ";
                
                // Use the explicitly passed date
                cmd.Parameters.AddWithValue("$date", date); 
                cmd.Parameters.AddWithValue("$start", (object?)startTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$end", (object?)endTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$duration", (object?)durationMinutes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$deep", (object?)deep ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$light", (object?)light ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$rem", (object?)rem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$wake", (object?)wake ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$efficiency", (object?)efficiency ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$avgHr", (object?)avgHr ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                Console.WriteLine($"✔ Sleep summary saved to database for {date} ({(mainSleep == null ? "No Sleep Data" : "Data Available")})");
            }
        }

        public static List<Dictionary<string, object>> GetSleepDataSummary(int days)
{
    var dataList = new List<Dictionary<string, object>>();

    // Query to select the last 'days' amount of entries.
    // ORDER BY date DESC ensures we get the most recent dates.
    string query = $@"
        SELECT 
            date, 
            start_time, 
            end_time, 
            duration_minutes, 
            deep_minutes, 
            light_minutes, 
            rem_minutes, 
            wake_minutes, 
            efficiency, 
            avg_hr
        FROM SleepSummary
        ORDER BY date DESC
        LIMIT {days};";

    using (var conn = new SqliteConnection(ConnectionString))
    {
        conn.Open();
        using var cmd = new SqliteCommand(query, conn);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                // Safely handle DBNull values, as "no data" entries contain them
                string columnName = reader.GetName(i);
                object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row.Add(columnName, value);
            }
            dataList.Add(row);
        }
    }
    
    // The data is returned in reverse order (newest first) due to ORDER BY DESC.
    // The web client can reverse this for graphing, or we can reverse it here.
    dataList.Reverse(); // Reverse to return chronologically (oldest first)
    return dataList;
}

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }
}