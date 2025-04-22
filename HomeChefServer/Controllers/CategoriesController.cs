using HomeChef.Server.Models.DTOs;
using HomeChefServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HomeChefServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CategoriesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<RecipeDTO>>> GetAllRecipes()
        {
            var recipes = new List<RecipeDTO>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetAllRecipes", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recipes.Add(new RecipeDTO
                {
                    RecipeId = (int)reader["Id"],
                    Title = reader["Title"].ToString(),
                    ImageUrl = reader["ImageUrl"].ToString(),
                    SourceUrl = reader["SourceUrl"].ToString(),
                    Servings = reader["Servings"] != DBNull.Value ? (int)reader["Servings"] : 0,
                    CookingTime = reader["CookingTime"] != DBNull.Value ? (int)reader["CookingTime"] : 0,
                    
                });
            }

            return Ok(recipes);
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategories()
        {
            try
            {
                var categories = new List<CategoryDTO>();

                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_GetAllCategories", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new CategoryDTO
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"].ToString(),
                        ImageUrl = reader["ImageUrl"].ToString(),
                        RecipeCount = (int)reader["RecipeCount"]  // אם זה הוספת מספר המתכונים
                    });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                // הדפסת שגיאה ל-logs
                Console.Error.WriteLine($"Error occurred while fetching categories: {ex.Message}");
                return StatusCode(500, "Internal server error. Please try again later.");
            }
        }

        [HttpGet("{id}/recipes")]
        public async Task<IActionResult> GetRecipesByCategory(
    int id,
    int pageNumber = 1,
    int pageSize = 20)
        {
            var recipes = new List<RecipeDTO>();
            int totalCount = 0;

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetRecipesByCategoryId", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CategoryId", id);
            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            // 🔵 שלב 1: לקרוא את totalCount
            if (await reader.ReadAsync())
            {
                totalCount = (int)reader["TotalCount"];
            }

            // 🔵 שלב 2: לעבור לתוצאה השנייה (המתכונים)
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    recipes.Add(new RecipeDTO
                    {
                        RecipeId = (int)reader["RecipeId"],
                        Title = reader["Title"].ToString(),
                        ImageUrl = reader["ImageUrl"].ToString(),
                        SourceUrl = reader["SourceUrl"].ToString(),
                        CategoryName = reader["CategoryName"].ToString(),
                        CookingTime = reader["CookingTime"] != DBNull.Value ? (int)reader["CookingTime"] : 0,
                        Servings = reader["Servings"] != DBNull.Value ? (int)reader["Servings"] : 0
                    });
                }
            }

            return Ok(new
            {
                recipes,
                totalCount
            });
        }




        [HttpGet("{categoryId}/favorites")]
        public async Task<ActionResult<IEnumerable<RecipeDTO>>> GetFavoritesByCategory(int categoryId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized("User ID not found in token.");

            int userId = int.Parse(userIdClaim.Value);

            var recipes = new List<RecipeDTO>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetFavoritesByUserIdAndCategory", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@CategoryId", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recipes.Add(new RecipeDTO
                {
                    RecipeId = (int)reader["RecipeId"],
                    Title = reader["Title"].ToString(),
                    ImageUrl = reader["ImageUrl"].ToString(),
                    SourceUrl = reader["SourceUrl"].ToString(),
                    Servings = reader["Servings"] != DBNull.Value ? (int)reader["Servings"] : 0,
                    CookingTime = reader["CookingTime"] != DBNull.Value ? (int)reader["CookingTime"] : 0,
                    CategoryName = reader["CategoryName"].ToString()
                });
            }

            return Ok(recipes);
        }
    }
}
