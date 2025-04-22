
﻿using HomeChef.Server.Models;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace HomeChef.Server.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IConfiguration _configuration;

        public CategoryService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var categories = new List<Category>();

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetAllCategories", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()
                });
            }

            return categories;
        }
    }
}
