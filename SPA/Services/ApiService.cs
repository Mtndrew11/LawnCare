using System.Text.Json;
using SPA.Models;

namespace SPA.Services
{
    // ApiService.cs
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherData> GetWeatherForecastAsync()
        {
            var response = await _httpClient.GetAsync("https://weatherapp20240729133707.azurewebsites.net/api/GetWeatherData?code=ziHD04RuERk25VSlAG1ZQVl272-TRIxCtRDxt44JPnwOAzFuUUvxnQ%3D%3D");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<WeatherData>(responseBody);
        }
    }
}
