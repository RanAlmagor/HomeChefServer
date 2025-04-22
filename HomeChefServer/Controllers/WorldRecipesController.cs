using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HomeChefServer.Services;
using System.Text.Json;

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorldRecipesController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly IConfiguration _config;
        private readonly string _pexelsApiKey;

        public WorldRecipesController(GeminiService geminiService, IConfiguration config)
        {
            _geminiService = geminiService;
            _config = config;
            _pexelsApiKey = config["PexelsApiKey"];
        }


        private string ExtractDishFromResponse(string response)
        {
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                if (line.Trim().ToLower().StartsWith("dish:"))
                    return line.Split(':')[1].Trim();
            }

            return null;
        }

        [HttpGet("{country}")]
        public async Task<IActionResult> GetNationalDish(string country)
        {
            try
            {
                var prompt = $"What is the most iconic traditional national dish of {country}? Explain shortly what it is, and then return the dish name only on a separate line starting with 'Dish:'.";
                var geminiResponse = await _geminiService.SendPromptAsync(prompt);

                if (string.IsNullOrWhiteSpace(geminiResponse))
                    return BadRequest("Could not get answer from Gemini.");

                var dish = ExtractDishFromResponse(geminiResponse);
                if (string.IsNullOrWhiteSpace(dish))
                    return BadRequest("Dish name not found.");

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", _pexelsApiKey);

                var queries = new[]
                {
            $"{dish} {country} traditional food plated close up",
            $"{dish} {country} cuisine",
            $"{dish} top down food"
        };

                string imageUrl = null;
                foreach (var query in queries)
                {
                    imageUrl = await GetRelevantImageUrl(httpClient, query, dish);
                    if (!string.IsNullOrWhiteSpace(imageUrl)) break;
                }

                imageUrl ??= await GetFallbackImage(httpClient, $"{dish} food plated");
                imageUrl ??= $"https://source.unsplash.com/800x600/?{Uri.EscapeDataString(dish + ",food," + country)}";


                return Ok(new
                {
                    title = dish,
                    explanation = geminiResponse.Trim(),
                    imageUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        private string CleanGeminiDishName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            var cleaned = raw.ToLowerInvariant()
                .Replace("the most iconic", "")
                .Replace("national dish of", "")
                .Replace("is", "")
                .Replace(":", "")
                .Replace(".", "")
                .Trim();

            var lines = cleaned.Split(new[] { '\n', '.', ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.LastOrDefault()?.Trim().Split(' ').FirstOrDefault();
        }

        private async Task<string> GetRelevantImageUrl(HttpClient httpClient, string query, string dish)
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=10";
            var res = await httpClient.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var photos = doc.RootElement.GetProperty("photos");

                foreach (var photo in photos.EnumerateArray())
                {
                    var alt = photo.GetProperty("alt").GetString()?.ToLowerInvariant() ?? "";
                    var imageUrl = photo.GetProperty("src").GetProperty("large").GetString();

                    var hasDishName = alt.Contains(dish.ToLowerInvariant());
                    var hasFoodKeywords = alt.Contains("food") || alt.Contains("plate") || alt.Contains("dish") || alt.Contains("meal") || alt.Contains("cuisine") || alt.Contains("served") || alt.Contains("plated");

                    var hasBadKeywords = alt.Contains("flag") || alt.Contains("people") || alt.Contains("illustration") || alt.Contains("empty") || alt.Contains("cutlery");

                    if (hasDishName && hasFoodKeywords && !hasBadKeywords)
                    {
                        return imageUrl;
                    }
                }
            }
            catch
            {
                // fallback null
            }

            return null;
        }


        private async Task<string> GetFallbackImage(HttpClient httpClient, string query)
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=5";
            var res = await httpClient.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var photos = doc.RootElement.GetProperty("photos");

                foreach (var photo in photos.EnumerateArray())
                {
                    var alt = photo.GetProperty("alt").GetString()?.ToLowerInvariant() ?? "";
                    var imageUrl = photo.GetProperty("src").GetProperty("large").GetString();

                    var isFood = alt.Contains("food") || alt.Contains("dish") || alt.Contains("meal");

                    if (isFood)
                        return imageUrl;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }


    }
}
