//  ──────────────────────────────────────────────────────────────
//  File:  Models/DTOs/RecipeDtos.cs
//  ──────────────────────────────────────────────────────────────
namespace HomeChef.Server.Models.DTOs;

/// <summary>A single ingredient line inside a recipe</summary>
public class IngredientLineDto
{
    /// <summary>ID in NewIngredients; null/0 means “create new”</summary>
    public int? IngredientId { get; set; }

    public string Name { get; set; } = string.Empty;   // ⬅ default = ""
    public decimal Quantity { get; set; }                  // ⬅ use decimal for food units
    public string Unit { get; set; } = string.Empty;   // ⬅ default = ""
}

// ------------------------------------------------------------------
//  DTO used by POST /api/MyRecipes/add  and  PUT /api/MyRecipes/update
// ------------------------------------------------------------------
public class RecipeCreateDto
{
    /* ---------- server‑generated for GET /{id} ---------- */
    public int RecipeId { get; set; }
    public string? CategoryName { get; set; }

    /* ---------- required fields ---------- */
    public string Title { get; set; } = string.Empty;
    public int Servings { get; set; }
    public int CookingTime { get; set; }
    public int CategoryId { get; set; }

    /* ---------- optional strings ---------- */
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public string? InstructionsText { get; set; }
    public string? Summary { get; set; }
    public string? Cuisine { get; set; }

    /* ---------- flags ---------- */
    public bool Vegetarian { get; set; }
    public bool Vegan { get; set; }
    public bool GlutenFree { get; set; }

    /* ---------- ingredients (never null) ---------- */

}

// ------------------------------------------------------------------
//  Legacy DTO kept for old code paths (unchanged except decimal + defaults)
// ------------------------------------------------------------------
public class CreateRecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int Servings { get; set; }
    public int CookingTime { get; set; }
    public int CategoryId { get; set; }

}
