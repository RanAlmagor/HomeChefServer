using HomeChef.Server.Models;
using HomeChef.Server.Models.DTOs;
using HomeChefServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;


namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyRecipesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public MyRecipesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet("my-recipes")]
        public async Task<ActionResult<IEnumerable<RecipeDTO>>> GetMyRecipes()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
                return Unauthorized("User ID not found in token.");

            int userId = int.Parse(userIdClaim.Value);
            var recipes = new List<RecipeDTO>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetMyRecipes", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
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

            return Ok(recipes);
        }

        /* -----------------------------------------------------------
 *  Controller action  (put inside MyRecipesController)
 * -----------------------------------------------------------*/
        [HttpGet("{id:int}")]
        public async Task<ActionResult<RecipeCreateDto>> GetRecipeById(int id)
        {
            // 1) authenticate / authorize
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            await using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // 2) load recipe header + category name
            const string headerSql = @"
        SELECT r.*, c.Name AS CategoryName
          FROM NewRecipes r
          JOIN Categories  c ON c.Id = r.CategoryId
         WHERE r.Id = @Rid";
            await using var headCmd = new SqlCommand(headerSql, conn);
            headCmd.Parameters.AddWithValue("@Rid", id);

            await using var headReader = await headCmd.ExecuteReaderAsync();
            if (!await headReader.ReadAsync())
                return NotFound("Recipe not found");

            // ownership check
            var createdBy = headReader.GetInt32(headReader.GetOrdinal("CreatedByUserId"));
            if (createdBy != userId)
                return Forbid("Not your recipe");

            // now read each column safely:
            int ix(string name) => headReader.GetOrdinal(name);
            bool has(string name) => !headReader.IsDBNull(ix(name));

            var dto = new RecipeCreateDto
            {
                RecipeId = id,
                Title = headReader.GetString(ix("Title")),
                ImageUrl = has("ImageUrl") ? headReader.GetString(ix("ImageUrl")) : null,
                SourceUrl = has("SourceUrl") ? headReader.GetString(ix("SourceUrl")) : null,
                Servings = headReader.GetInt32(ix("Servings")),
                CookingTime = headReader.GetInt32(ix("CookingTime")),
                CategoryId = headReader.GetInt32(ix("CategoryId")),
                CategoryName = headReader.GetString(ix("CategoryName")),
                InstructionsText = has("InstructionsText")
                                  ? headReader.GetString(ix("InstructionsText"))
                                  : null,
                Summary = has("Summary") ? headReader.GetString(ix("Summary")) : null,
                Cuisine = has("Cuisine") ? headReader.GetString(ix("Cuisine")) : null,
                Vegetarian = has("Vegetarian") && headReader.GetBoolean(ix("Vegetarian")),
                Vegan = has("Vegan") && headReader.GetBoolean(ix("Vegan")),
                GlutenFree = has("GlutenFree") && headReader.GetBoolean(ix("GlutenFree")),
            };
            headReader.Close();

            return Ok(dto);
        }


        // POST api/myrecipes/add
        [HttpPost("add")]
        public async Task<IActionResult> AddRecipe([FromBody] RecipeCreateDto recipe)
        {
            /* 0) ─── current user ───────────────────────────────────────── */
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            /* 1) ─── basic validation ──────────────────────────────────── */
            if (recipe.CategoryId <= 0)
                return BadRequest("Category is required.");

            /* 2) ─── open connection ------------------------------------ */
            await using var conn =
                new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            /* 2a) does the category actually exist?                      */
            var catChk = new SqlCommand("SELECT 1 FROM Categories WHERE Id = @Id", conn);
            catChk.Parameters.AddWithValue("@Id", recipe.CategoryId);
            if (await catChk.ExecuteScalarAsync() is null)
                return BadRequest($"Category {recipe.CategoryId} does not exist.");

            /* 3) ─── start a transaction -------------------------------- */
            await using var trx = await conn.BeginTransactionAsync();


            try
            {
                string publisher;
                await using (var getUserCmd = new SqlCommand("SELECT Username FROM Users WHERE Id = @Id", conn, (SqlTransaction)trx))
                {
                    getUserCmd.Parameters.AddWithValue("@Id", userId);
                    var result = await getUserCmd.ExecuteScalarAsync();
                    publisher = result?.ToString() ?? "Unknown";
                }


                /* 3b) insert recipe ------------------------------------ */
                var addRecipe = new SqlCommand(@"
INSERT INTO NewRecipes
      (Title, ImageUrl, SourceUrl, Servings, CookingTime, CategoryId,
       CreatedAt, CreatedByUserId, InstructionsText, Summary, Cuisine,
       Vegetarian, Vegan, GlutenFree, Publisher)
OUTPUT INSERTED.Id
VALUES (@Title,@Img,@Src,@Srv,@Cook,@Cat,
        GETUTCDATE(),@User,@Instr,@Sum,@Cui,
        @Veg,@Vegan,@GF,@Publisher);",
                    conn, (SqlTransaction)trx);

                addRecipe.Parameters.AddWithValue("@Title", recipe.Title);
                addRecipe.Parameters.AddWithValue("@Img", (object?)recipe.ImageUrl ?? DBNull.Value);
                addRecipe.Parameters.AddWithValue("@Src", (object?)recipe.SourceUrl ?? DBNull.Value);
                addRecipe.Parameters.AddWithValue("@Srv", recipe.Servings);
                addRecipe.Parameters.AddWithValue("@Cook", recipe.CookingTime);
                addRecipe.Parameters.AddWithValue("@Cat", recipe.CategoryId);
                addRecipe.Parameters.AddWithValue("@User", userId);
                addRecipe.Parameters.AddWithValue("@Instr", (object?)recipe.InstructionsText ?? DBNull.Value);
                addRecipe.Parameters.AddWithValue("@Sum", (object?)recipe.Summary ?? DBNull.Value);
                addRecipe.Parameters.AddWithValue("@Cui", (object?)recipe.Cuisine ?? DBNull.Value);
                addRecipe.Parameters.AddWithValue("@Veg", recipe.Vegetarian);
                addRecipe.Parameters.AddWithValue("@Vegan", recipe.Vegan);
                addRecipe.Parameters.AddWithValue("@GF", recipe.GlutenFree);
                addRecipe.Parameters.AddWithValue("@Publisher", publisher);

                int recipeId = (int)await addRecipe.ExecuteScalarAsync();

                await trx.CommitAsync();
                return Ok(new { Id = recipeId });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return StatusCode(500, $"Failed to create recipe: {ex.Message}");
            }
        }




        [HttpPut("update")]
        public async Task<IActionResult> UpdateRecipe([FromBody] UpdateRecipeDTO recipe)
        {
            // 1. Get user ID from token
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid or missing User ID.");

            // 2. Open connection + begin transaction
            await using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();

            try
            {
                // 3. Verify ownership
                await using (var checkCmd = new SqlCommand(
                    "SELECT CreatedByUserId FROM NewRecipes WHERE Id = @RecipeId", conn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@RecipeId", recipe.RecipeId);
                    var creatorObj = await checkCmd.ExecuteScalarAsync();
                    if (creatorObj is null)
                        return NotFound("Recipe not found.");

                    if ((int)creatorObj != userId)
                        return Forbid("You are not allowed to edit this recipe.");
                }

                // 4. Update NewRecipes row
                const string updateSql = @"
            UPDATE NewRecipes
            SET 
                Title            = @Title,
                ImageUrl         = @ImageUrl,
                SourceUrl        = @SourceUrl,
                Servings         = @Servings,
                CookingTime      = @CookingTime,
                CategoryId       = @CategoryId,
                Cuisine          = @Cuisine,
                Summary          = @Summary,
                InstructionsText = @InstructionsText,
                Vegetarian       = @Vegetarian,
                Vegan            = @Vegan,
                GlutenFree       = @GlutenFree
            WHERE Id = @RecipeId;
        ";

                await using (var upd = new SqlCommand(updateSql, conn, tx))
                {
                    upd.Parameters.AddWithValue("@RecipeId", recipe.RecipeId);
                    upd.Parameters.AddWithValue("@Title", recipe.Title);
                    upd.Parameters.AddWithValue("@ImageUrl", (object)recipe.ImageUrl ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@SourceUrl", (object)recipe.SourceUrl ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Servings", recipe.Servings);
                    upd.Parameters.AddWithValue("@CookingTime", recipe.CookingTime);
                    upd.Parameters.AddWithValue("@CategoryId", recipe.CategoryId);
                    upd.Parameters.AddWithValue("@Cuisine", (object)recipe.Cuisine ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Summary", (object)recipe.Summary ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@InstructionsText", (object)recipe.InstructionsText ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@Vegetarian", recipe.Vegetarian);
                    upd.Parameters.AddWithValue("@Vegan", recipe.Vegan);
                    upd.Parameters.AddWithValue("@GlutenFree", recipe.GlutenFree);
                    upd.Parameters.AddWithValue("@UserId", userId);
                    await upd.ExecuteNonQueryAsync();
                }

                // 7. Commit transaction
                await tx.CommitAsync();
                return Ok(new { Message = $"Recipe {recipe.RecipeId} updated successfully." });
            }
            catch
            {
                // 8. Rollback on any error
                await tx.RollbackAsync();
                return StatusCode(500, "An error occurred while updating the recipe.");
            }
        }




        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var checkCmd = new SqlCommand("SELECT CreatedByUserId FROM NewRecipes WHERE Id = @RecipeId", conn);
            checkCmd.Parameters.AddWithValue("@RecipeId", id);

            var creatorIdObj = await checkCmd.ExecuteScalarAsync();
            if (creatorIdObj == null) return NotFound("Recipe not found.");
            if ((int)creatorIdObj != userId) return Forbid("You cannot delete this recipe.");

            using var cmd = new SqlCommand("sp_DeleteRecipe", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@RecipeId", id);

            await cmd.ExecuteNonQueryAsync();
            return Ok(new { Message = $"Recipe {id} deleted successfully." });
        }
    }
}
