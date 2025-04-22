using HomeChef.Server.Models.DTOs;
using HomeChefServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class RatingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public RatingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

   [HttpPost]
public async Task<ActionResult> AddRating([FromBody] RatingDTO ratingDto)
{
    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
    if (userIdClaim == null)
    {
        return Unauthorized("User not logged in.");
    }

    int userId = int.Parse(userIdClaim.Value);
    ratingDto.UserId = userId;

    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await conn.OpenAsync();

    using var cmd = new SqlCommand("sp_AddRecipeRating", conn)
    {
        CommandType = CommandType.StoredProcedure
    };
    cmd.Parameters.AddWithValue("@RecipeId", ratingDto.RecipeId);
    cmd.Parameters.AddWithValue("@UserId", ratingDto.UserId);
    cmd.Parameters.AddWithValue("@Rating", ratingDto.Rating);

    await cmd.ExecuteNonQueryAsync();

    return Ok();
}


    [HttpPut]
public async Task<ActionResult> UpdateRating([FromBody] RatingDTO ratingDto)
{
    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
    if (userIdClaim == null)
    {
        return Unauthorized("User not logged in.");
    }

    int userId = int.Parse(userIdClaim.Value);
    ratingDto.UserId = userId;

    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await conn.OpenAsync();

    using var cmd = new SqlCommand("sp_UpdateRecipeRating", conn)
    {
        CommandType = CommandType.StoredProcedure
    };
    cmd.Parameters.AddWithValue("@RecipeId", ratingDto.RecipeId);
    cmd.Parameters.AddWithValue("@UserId", ratingDto.UserId);
    cmd.Parameters.AddWithValue("@Rating", ratingDto.Rating);

    await cmd.ExecuteNonQueryAsync();

    return Ok();
}


    // מחיקת דירוג למתכון
    [HttpDelete]
    public async Task<ActionResult> DeleteRating([FromBody] RatingDTO ratingDto)
    {
        // בדיקה אם המשתמש מחובר
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
        {
            return Unauthorized("User not logged in.");
        }

        // קבלת מזהה המשתמש מה-Claim
        int userId = int.Parse(userIdClaim.Value);
        ratingDto.UserId = userId;

        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        using var cmd = new SqlCommand("sp_DeleteRecipeRating", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@RecipeId", ratingDto.RecipeId);
        cmd.Parameters.AddWithValue("@UserId", ratingDto.UserId);

        await cmd.ExecuteNonQueryAsync();

        // חישוב ממוצע הדירוגים והעדכון בטבלת המתכונים
        await UpdateRecipeRating(ratingDto.RecipeId);

        return Ok();
    }

    // שליפת דירוג ממוצע ומספר הדירוגים
    [HttpGet("{recipeId}")]
    public async Task<ActionResult<RatingDTO>> GetRatingDetails(int recipeId)
    {
        var rating = new RatingDTO();

        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        using var cmd = new SqlCommand("sp_GetRecipeRatingDetails", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@RecipeId", recipeId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            rating.AverageRating = reader["AverageRating"] != DBNull.Value
                                   ? Convert.ToDouble(reader["AverageRating"])
                                   : null;

            rating.RatingCount = reader["RatingCount"] != DBNull.Value
                                   ? Convert.ToInt32(reader["RatingCount"])
                                   : 0;

        }

        return Ok(rating);
    }

    // פונקציה לעדכון דירוג ממוצע ומספר הדירוגים בטבלת המתכון
    private async Task UpdateRecipeRating(int recipeId)
    {
        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        // חישוב ממוצע הדירוגים
        using var cmd = new SqlCommand("sp_UpdateRecipeRating", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@RecipeId", recipeId);

        await cmd.ExecuteNonQueryAsync();
    }

    // GET /api/ratings/{recipeId}/me
    [HttpGet("{recipeId}/me")]
    public async Task<ActionResult<object>> GetMyRating(int recipeId)
    {
        // 🔒 require login
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null)
            return Unauthorized("User not logged in.");

        int userId = int.Parse(userIdClaim.Value);

        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        // single‑scalar query: does this user have a rating for this recipe?
        using var cmd = new SqlCommand(
            "SELECT Rating FROM dbo.RecipeRatings WHERE RecipeId = @RecipeId AND UserId = @UserId",
            conn);
        cmd.Parameters.AddWithValue("@RecipeId", recipeId);
        cmd.Parameters.AddWithValue("@UserId", userId);

        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
            return NoContent();  // HTTP 204


        return Ok(new { rating = Convert.ToInt32(result) }); // { "rating": 4 }
    }

}
