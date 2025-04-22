using HomeChef.Server.Models;

namespace HomeChef.Server.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
    }
}
