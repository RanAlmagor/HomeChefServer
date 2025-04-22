using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HomeChefServer.Models.DTOs;

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public UserProfileController(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId");
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserIdFromToken();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_GetUserProfile", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    Bio = reader["Bio"]?.ToString(),
                    ProfilePictureBase64 = reader["ProfilePictureBase64"]?.ToString(),
                    Gender = reader["Gender"]?.ToString(),
                    BirthDate = reader["BirthDate"] != DBNull.Value ? ((DateTime)reader["BirthDate"]).ToString("yyyy-MM-dd") : null
                };

                return Ok(new { data = result });
            }

            return NotFound("User not found.");
        }


        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserIdFromToken();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateUserProfile", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Bio", (object?)dto.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePictureBase64", (object?)dto.ProfilePictureBase64 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Gender", (object?)dto.Gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BirthDate", (object?)dto.BirthDate ?? DBNull.Value);


            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok("Profile updated successfully.");
        }

        [HttpPut("update-picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserIdFromToken();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_UpdateUserProfilePicture", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@ProfilePictureBase64", dto.ProfilePictureBase64 ?? (object)DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return Ok("✅ Profile picture updated.");
        }


        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserIdFromToken();

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest("New password is too weak. Use at least 6 characters.");
            }

            var oldHash = HashPassword(dto.OldPassword);
            var newHash = HashPassword(dto.NewPassword);

            if (oldHash == newHash)
            {
                return BadRequest("New password must be different from the current one.");
            }

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_ChangeUserPassword", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@OldPasswordHash", oldHash);
            cmd.Parameters.AddWithValue("@NewPasswordHash", newHash);

            await conn.OpenAsync();

            try
            {
                await cmd.ExecuteNonQueryAsync();
                return Ok("Password changed successfully.");
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                return BadRequest("Old password is incorrect.");
            }
        }

    }
}
