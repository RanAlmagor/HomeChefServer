using Dapper;
using HomeChef.Server.Models.DTOs;
using HomeChef.Server.Services;
using HomeChefServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RecipesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("paged")]
        public async Task<ActionResult> GetRecipesPaged(
        int pageNumber = 1,
        int pageSize = 20)
        {
            List<RecipeDTO> recipes = new List<RecipeDTO>();
            int totalCount = 0;

            using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand("sp_GetRecipesPaged", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            // שלב 1: קריאת totalCount
            if (await reader.ReadAsync())
            {
                totalCount = (int)reader["TotalCount"];
            }

            // שלב 2: לעבור לתוצאה הבאה (המתכונים)
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    recipes.Add(new RecipeDTO
                    {
                        RecipeId = (int)reader["Id"],
                        Title = reader["Title"].ToString(),
                        ImageUrl = reader["ImageUrl"].ToString(),
                        SourceUrl = reader["SourceUrl"].ToString(),
                        Servings = (int)reader["Servings"],
                        CookingTime = (int)reader["CookingTime"],
                        CategoryName = reader["CategoryName"].ToString()
                    });
                }
            }

            return Ok(new
            {
                recipes,
                totalCount
            });
        }



        [HttpGet("search")]
        public async Task<ActionResult> SearchRecipes(
    string term,
    int pageNumber = 1,
    int pageSize = 20)
        {
            var recipes = new List<RecipeDTO>();
            int totalCount = 0;

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var cmd = new SqlCommand("sp_SearchRecipes", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@SearchTerm", term);
            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();

            // שלב 1: שליפת TotalCount
            if (await reader.ReadAsync())
            {
                totalCount = (int)reader["TotalCount"];
            }

            // שלב 2: מעבר לתוצאה הבאה (ResultSet)
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



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_DeleteRecipe", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@RecipeId", id);

            await cmd.ExecuteNonQueryAsync();
            return Ok($"Recipe {id} deleted.");
        }
       
        [HttpGet("{id}")]
        public async Task<ActionResult<FullRecipeDTO>> GetRecipeById(int id)
        {
            FullRecipeDTO recipe = null;
            var ingredients = new List<IngredientDTO>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetRecipeById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@RecipeId", id);

            using var reader = await cmd.ExecuteReaderAsync();

            // קריאת פרטי המתכון
            if (await reader.ReadAsync())
            {
                recipe = new FullRecipeDTO
                {
                    Id = (int)reader["Id"],
                    Title = reader["Title"].ToString(),
                    ImageUrl = reader["ImageUrl"].ToString(),
                    SourceUrl = reader["SourceUrl"].ToString(),
                    Servings = (int)reader["Servings"],
                    CookingTime = (int)reader["CookingTime"],
                    CategoryId = (int)reader["CategoryId"],
                    CategoryName = reader["CategoryName"].ToString(),
                    Ingredients = new List<IngredientDTO>()
                };
            }

            // קריאת המרכיבים
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    ingredients.Add(new IngredientDTO
                    {
                        IngredientId = (int)reader["IngredientId"],
                        Quantity = decimal.Parse(reader["Quantity"].ToString()),
                        Unit = reader["Unit"].ToString()
                    });
                }
            }

            if (recipe == null)
                return NotFound();

            recipe.Ingredients = ingredients;
            return Ok(recipe);
        }

        [HttpGet("profile/{id}")]
        public async Task<IActionResult> GetRecipeProfile(int id)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            var parameters = new { Id = id };

            var recipe = await conn.QueryFirstOrDefaultAsync<RecipeProfileDTO>(
                "sp_GetRecipeProfileById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (recipe == null)
                return NotFound();

            return Ok(recipe);
        }






    }
}
