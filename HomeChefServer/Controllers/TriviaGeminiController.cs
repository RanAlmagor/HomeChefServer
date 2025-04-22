using Microsoft.AspNetCore.Mvc;
using HomeChefServer.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.SqlClient;
using HomeChefServer.Models.DTOs;


namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TriviaGeminiController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly IConfiguration _config;

        public TriviaGeminiController(GeminiService geminiService, IConfiguration config)
        {
            _geminiService = geminiService; ; ;
            _config = config;
        }

        

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateTrivia()
        {
            var prompt = @"Create a multiple-choice trivia question about food or cooking. Format it like this:
Question: [question here]
Options: A) [option] B) [option] C) [option] D) [option]
Answer: [correct letter]
Explanation: [optional, 1 sentence explanation]";

            var result = await _geminiService.SendPromptAsync(prompt);

            var question = Regex.Match(result, @"Question:\s*(.*?)\n").Groups[1].Value.Trim();
            var optionsRaw = Regex.Match(result, @"Options:\s*(.*?)\n").Groups[1].Value.Trim();
            var answerRaw = Regex.Match(result, @"Answer:\s*([A-Da-d])").Groups[1].Value.Trim().ToUpper();
            var explanation = Regex.Match(result, @"Explanation:\s*(.*)").Groups[1].Value.Trim();

            var optionsList = optionsRaw.Split(new[] { "A)", "B)", "C)", "D)" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(o => o.Trim())
                                        .ToList();

            var answerIndex = "ABCD".IndexOf(answerRaw);
            var correctText = (answerIndex >= 0 && answerIndex < optionsList.Count)
                                ? optionsList[answerIndex]
                                : "";

            return Ok(new
            {
                question,
                options = optionsList,
                answer = answerRaw,      // e.g. "C"
                correctText,             // e.g. "Shallots"
                explanation
            });
        }

        

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_GetLeaderboard", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await conn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();

            var result = new List<LeaderboardEntryDTO>();
            while (await reader.ReadAsync())
            {
                result.Add(new LeaderboardEntryDTO
                {
                  
                    Username = reader["Username"].ToString(),
                    Score = (int)reader["Score"],
                    CorrectAnswers = (int)reader["CorrectAnswers"],
                    SubmittedAt = (DateTime)reader["SubmittedAt"]
                });
            }

            return Ok(result);
        }

        [HttpPost("submit-score")]
        [Authorize]
        public async Task<IActionResult> SubmitScore([FromBody] TriviaScoreDTO dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("sp_SubmitTriviaScore", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Score", dto.Score);
                cmd.Parameters.AddWithValue("@CorrectAnswers", dto.CorrectAnswers);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                return Ok("✅ Score submitted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error: {ex.Message}");
            }
        }



    }
}
