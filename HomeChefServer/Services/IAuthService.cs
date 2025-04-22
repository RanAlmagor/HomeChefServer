using HomeChef.Server.Models;

namespace HomeChef.Server.Services
{
    public interface IAuthService
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(int id);
        string GenerateJwtToken(User user);
    }
}
