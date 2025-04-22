using HomeChef.Server.Models.DTOs;

public class UpdateRecipeDTO
{
    public int RecipeId { get; set; }

    public string Title { get; set; }
    public string ImageUrl { get; set; }
    public string SourceUrl { get; set; }

    public int Servings { get; set; }
    public int CookingTime { get; set; }
    public int CategoryId { get; set; }

    public string Cuisine { get; set; }
    public string Summary { get; set; }
    public string InstructionsText { get; set; }

    public bool Vegetarian { get; set; }
    public bool Vegan { get; set; }
    public bool GlutenFree { get; set; }

    public List<IngredientDTO> Ingredients { get; set; }
}
