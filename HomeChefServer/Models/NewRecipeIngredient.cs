namespace HomeChef.Server.Models
{
    public class NewRecipeIngredient
    {
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public float Quantity { get; set; }
        public string Unit { get; set; }

        public NewRecipeIngredient() { }
    }

}
