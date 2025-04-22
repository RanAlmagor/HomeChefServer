using System.Net.Http.Headers;
using System.Text.Json;

namespace HomeChefServer.Services
{
    public class PexelsService
    {
        private readonly string _apiKey;
        private readonly HttpClient _http;

        public PexelsService(IConfiguration config)
        {
            _apiKey = config["PexelsApiKey"];
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> FetchImageUrl(string query)
        {
            try
            {
                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=1";
                var response = await _http.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var photoUrl = doc.RootElement
                    .GetProperty("photos")[0]
                    .GetProperty("src")
                    .GetProperty("large")
                    .GetString();

                return photoUrl;
            }
            catch
            {
                return "https://source.unsplash.com/600x400/?food"; // fallback
            }
        }
    }
}
