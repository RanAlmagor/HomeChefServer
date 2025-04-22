using Microsoft.AspNetCore.Mvc;
using HomeChefServer.Models.DTOs;
using HomeChef.Server.Services;
using HomeChef.Server.Models;
using System.Data;
using Dapper;
using System.Data.SqlClient;
using Dapper;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace HomeChefServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            var users = await conn.QueryAsync<User>(
                "sp_GetAllUsers",
                commandType: CommandType.StoredProcedure
            );
            return Ok(users);

        }

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteRecipe(int id)
{
    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    await conn.OpenAsync();

    using var tx = conn.BeginTransaction();

    try
    {
        // 1. Delete related ratings
        using (var deleteRatings = new SqlCommand("DELETE FROM RecipeRatings WHERE RecipeId = @Id", conn, tx))
        {
            deleteRatings.Parameters.AddWithValue("@Id", id);
            await deleteRatings.ExecuteNonQueryAsync();
        }

        // 2. Delete the recipe itself
        using (var deleteRecipe = new SqlCommand("DELETE FROM NewRecipes WHERE Id = @Id", conn, tx))
        {
            deleteRecipe.Parameters.AddWithValue("@Id", id);
            var rowsAffected = await deleteRecipe.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                await tx.RollbackAsync();
                return NotFound();
            }
        }

        await tx.CommitAsync();
        return NoContent(); // 204
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.Error.WriteLine("❌ Error deleting recipe: " + ex.Message);
        return StatusCode(500, "Failed to delete recipe");
    }
}




    }
}
