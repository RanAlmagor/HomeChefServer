using HomeChefServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using HomeChefServer.Models.DTOs;

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly string _pexelsApiKey;

        public GeminiController(GeminiService geminiService, IConfiguration config)
        {
            _geminiService = geminiService;
            _pexelsApiKey = config["PexelsApiKey"];
        }



        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] string userMessage)
        {
            try
            {
                var reply = await _geminiService.SendPromptAsync(userMessage);
                return Ok(new { content = reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gemini error: {ex.Message}");
            }
        }



        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            try
            {
                // נחלץ מילות מפתח קולינריות עם Gemini (NLP)
                var keywords = await _geminiService.ExtractFoodKeywordsSmartAsync(query);
                var cleaned = keywords?.Trim();

                // אם לא הצליח לחלץ או קיבלנו טקסט קצר מדי – נ fallback לתמונה כללית
                if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 2)
                {
                    return Ok(new { imageUrl = "https://source.unsplash.com/600x400/?chef,robot" });
                }

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", _pexelsApiKey);


                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(cleaned)}&per_page=1";
                var response = await client.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var photoUrl = doc.RootElement
                    .GetProperty("photos")[0]
                    .GetProperty("src")
                    .GetProperty("large")
                    .GetString();

                return Ok(new { imageUrl = photoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching image from Pexels: {ex.Message}");
            }
        }

        [HttpPost("extract-ingredients")]
        public async Task<IActionResult> ExtractIngredients([FromBody] IngredientExtractRequest request)
        {
            try
            {
                var prompt = $"You are an expert recipe assistant. Extract a complete, accurate list of ingredients from the following recipe. " +
             $"Include **estimated measurements** and **units** for all ingredients, even if the original is unclear. If needed, make a smart guess. " +
             $"Group ingredients by sections such as *brownie base*, *mint filling*, and *chocolate topping* if applicable. " +
             $"Each line should contain the ingredient, quantity, and unit. " +
             $"Scale the ingredients for exactly {request.Servings} servings.\n\n" +
             $"Summary:\n{request.Summary}\n\n" +
             $"Instructions:\n{request.Instructions}\n\n" +
             $"Format the result like this:\n\n" +
             $"Brownie Base:\n- 2 tbsp flaxseed meal\n- 1/3 cup applesauce\n...\n\n" +
             $"Mint Filling:\n- 1/2 avocado\n- 1/4 cup coconut milk\n...\n\n" +
             $"Chocolate Topping:\n- 1/2 cup dark chocolate\n- 1 tbsp oil\n\n" +
             $"Only return the list of ingredients. Do not include steps or extra text.";



                var reply = await _geminiService.SendPromptAsync(prompt);
                return Ok(new { ingredients = reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Gemini error: {ex.Message}");
            }
        }




    }
}
