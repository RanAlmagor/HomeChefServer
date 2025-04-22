using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace HomeChefServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientsController : ControllerBase
{
    private readonly string _cs;
    public IngredientsController(IConfiguration cfg) =>
        _cs = cfg.GetConnectionString("DefaultConnection");

    /* GET api/ingredients/search?query=tom */
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Ok(Array.Empty<object>());
        var list = new List<object>();

        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();
        const string sql = @"SELECT TOP 10 Id, Name
                             FROM NewIngredients
                             WHERE Name LIKE @Q + '%'
                             ORDER BY Name";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Q", query);
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            list.Add(new { id = (int)rd["Id"], name = rd["Name"].ToString() });

        return Ok(list);
    }

    /* POST api/ingredients  { "name":"Papaya" } */
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IngredientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name required");

        await using var cn = new SqlConnection(_cs);
        await cn.OpenAsync();

        /* already exists? */
        var check = new SqlCommand("SELECT Id FROM NewIngredients WHERE Name=@N", cn);
        check.Parameters.AddWithValue("@N", dto.Name);
        var idObj = await check.ExecuteScalarAsync();
        if (idObj != null)
            return Ok(new { id = (int)idObj, name = dto.Name });

        /* insert */
        var ins = new SqlCommand("INSERT INTO NewIngredients(Name) OUTPUT INSERTED.Id VALUES(@N)", cn);
        ins.Parameters.AddWithValue("@N", dto.Name);
        var newId = (int)(await ins.ExecuteScalarAsync()!);
        return Ok(new { id = newId, name = dto.Name });
    }

    public record IngredientDto(string Name);
}
