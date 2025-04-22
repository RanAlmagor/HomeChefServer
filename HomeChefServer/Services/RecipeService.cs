using HomeChef.Server.Models.DTOs;
using HomeChefServer.Models.DTOs;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;


namespace HomeChef.Server.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IConfiguration _configuration;

        public RecipeService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<RecipeDTO>> SearchRecipesAsync(string searchTerm)
        {
            var results = new List<RecipeDTO>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_SearchRecipes", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new RecipeDTO
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

            return results;
        }
        public async Task<RecipeProfileDTO> GetRecipeProfileByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))

            {
                var parameters = new { Id = id };
                var result = await connection.QueryFirstOrDefaultAsync<RecipeProfileDTO>(
                    "sp_GetRecipeProfileById",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
        }


    }
}
