using HomeChef.Server.Models.DTOs;
using HomeChefServer.Models.DTOs;

namespace HomeChef.Server.Services
{
    public interface IRecipeService
    {
        Task<List<RecipeDTO>> SearchRecipesAsync(string searchTerm);
        Task<RecipeProfileDTO> GetRecipeProfileByIdAsync(int id);



    }
}
