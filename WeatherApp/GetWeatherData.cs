using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public class RainfallData
    {
        public string time { get; set; }
        public string rain { get; set; }
    }


    public class SoilData
    {
        public string time { get; set; }
        public string temp { get; set; }
    }

    public static class GetWeatherData
    {
        // Start Date (previous 7 days)
        // End Dat (today)
        // API to get soil temperatures

        private static readonly HttpClient httpClient = new HttpClient();
        //private static readonly string apiUrl1 = "https://api.open-meteo.com/v1/forecast?latitude=38.611&longitude=-77.3397&hourly=precipitation&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";
        //private static readonly string rainfallHistory_URL = "https://archive-api.open-meteo.com/v1/archive?latitude=52.52&longitude=13.41&start_date=2024-07-13&end_date=2024-07-27&hourly=rain&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";

        [FunctionName("GetWeatherData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string endDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string startDate = DateTime.Now.AddDays(-8).ToString("yyyy-MM-dd");
            double RainfallPast7Days = 0;
            log.LogInformation($"startDate: {startDate}");
            log.LogInformation($"endDate: {endDate}");

            string rainfallHistory_URL = $"https://archive-api.open-meteo.com/v1/archive?latitude=52.52&longitude=13.41&start_date={startDate}&end_date={endDate}&hourly=rain&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch";
            string precipitationForcast3days_URL = $"https://api.open-meteo.com/v1/forecast?latitude=38.6&longitude=77.34&daily=precipitation_sum&temperature_unit=fahrenheit&precipitation_unit=inch&timezone=America%2FNew_York&forecast_days=3";
            string soiltempHistory_api = $"https://api.open-meteo.com/v1/forecast?latitude=38.6&longitude=77.34&hourly=soil_temperature_6cm&daily=precipitation_sum,rain_sum&temperature_unit=fahrenheit&precipitation_unit=inch&timezone=America%2FNew_York";

            // Rainfall History
            var response = await httpClient.GetAsync(rainfallHistory_URL);
            var content = await response.Content.ReadAsStringAsync();

            dynamic jsonData = JsonConvert.DeserializeObject(content);

            List<RainfallData> rainfallDataList = new List<RainfallData>();

            if (jsonData != null && jsonData.hourly != null && jsonData.hourly.time != null && jsonData.hourly.rain != null)
            {
                log.LogInformation("Starting Rainfall loop");

                for (int i = 0; i < jsonData.hourly.time.Count; i++)
                {
                    //Console.WriteLine($"hourly time object type: {jsonData.hourly.time.GetType().ToString()}");
                    //rainAmount = jsonData.hourly.rain[i] ?? new JValue((object)null);

                    RainfallPast7Days = jsonData.hourly.rain[i] != null ? RainfallPast7Days + jsonData.hourly.rain[i].ToObject<double>() : RainfallPast7Days + 0;

                    rainfallDataList.Add(new RainfallData
                    {
                        time = jsonData.hourly.time[i],
                        rain = jsonData.hourly.rain[i] != null ? jsonData.hourly.rain[i].ToObject<double>().ToString() : "No Data Available"
                    });
                }
            }

            else
            {
                Console.WriteLine($"json data null: {jsonData.hourly.time}");
            }

            log.LogInformation("The type of rainfallDataList is: " + rainfallDataList.GetType().ToString()); // Log the type of content


            // Rainfall Forecast
            var response2 = await httpClient.GetAsync(precipitationForcast3days_URL);
            var content2 = await response2.Content.ReadAsStringAsync();
            dynamic jsonData2 = JsonConvert.DeserializeObject(content2);
            double precipitationSum = 0;

            if (jsonData2 != null && jsonData2.daily != null && jsonData2.daily.time != null && jsonData2.daily.precipitation_sum != null)
            {
                log.LogInformation("Averaging forecasted precipitation for next 3 days");

                for (int i = 0; i < jsonData2.daily.time.Count; i++)
                {
                    precipitationSum = jsonData2.daily.precipitation_sum[i] != null ? precipitationSum + jsonData2.daily.precipitation_sum[i].ToObject<double>() : precipitationSum + 0;
                }

                log.LogInformation("Total precipitation forecasted for next 3 days: " + precipitationSum);
            }

            else
            {
                log.LogWarning("API data was null!");
            }


            // Soil Temps
            var response3 = await httpClient.GetAsync(soiltempHistory_api);
            var content3 = await response3.Content.ReadAsStringAsync();
            double soilTempSum = 0;
            double soilTempAvg = 0;

            dynamic jsonData3 = JsonConvert.DeserializeObject(content3);
            //Console.WriteLine("jsonData3:"); // Log the type of content
            //Console.WriteLine(jsonData3);

            List<SoilData> soilTempsList = new List<SoilData>();

            if (jsonData3 != null && jsonData3.hourly != null && jsonData3.hourly.time != null && jsonData3.hourly.soil_temperature_6cm != null)
            {
                log.LogInformation("Starting Soil temp loop");

                for (int i = 0; i < jsonData3.hourly.time.Count; i++)
                {
                    soilTempSum = jsonData3.hourly.soil_temperature_6cm[i] != null ? soilTempSum + jsonData3.hourly.soil_temperature_6cm[i].ToObject<double>() : soilTempSum + 0;

                    soilTempsList.Add(new SoilData
                    {
                        time = jsonData3.hourly.time[i],
                        temp = jsonData3.hourly.soil_temperature_6cm[i] != null ? jsonData3.hourly.soil_temperature_6cm[i].ToObject<double>().ToString() : "No Data Available"
                    });
                }

                soilTempAvg = soilTempSum / soilTempsList.Count;
            }

            else
            {
                Console.WriteLine($"json data null: {jsonData3.hourly.soil_temperature_6cm}");
            }

            // Output
            String output = $@"
            Date                                         {DateTime.Now}
            Rainfall amount last 7 days:                 {Math.Truncate(RainfallPast7Days * 100) / 100}""
            Expected rainfall in the next 3 days:        {Math.Truncate(precipitationSum * 100) / 100}
            Soil Temperatures Averages the last 3 days:  {Math.Truncate(soilTempAvg * 100) / 100}
            ";

            Console.WriteLine(output);

            return new OkObjectResult(output);

        }
    }
}