using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HomeChefServer.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public GeminiService(IConfiguration config)
        {
            _apiKey = config["GeminiApiKey"];
            _apiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> SendPromptAsync(string userPrompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API error: {response.StatusCode} - {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var reply = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .ToString();

            return reply;
        }

        public async Task<string> ExtractFoodKeywordsSmartAsync(string userSentence)
        {
            bool isHebrew = System.Text.RegularExpressions.Regex.IsMatch(userSentence ?? "", @"[א-ת]");
            string prompt;

            if (isHebrew)
            {
                prompt = $"חלץ רק את מילות המפתח הקולינריות מהמשפט הבא: \"{userSentence}\". החזר רק את המילים האלה, מופרדות בפסיקים. לאחר מכן, תרגם את המילים האלה לאנגלית בלבד, מופרדות בפסיקים.";
            }
            else
            {
                prompt = $"Extract only the food-related keywords (ingredients or dish names) from this sentence: \"{userSentence}\". Return keywords only, separated by commas.";
            }

            return await SendPromptAsync(prompt);
        }
    }
}
