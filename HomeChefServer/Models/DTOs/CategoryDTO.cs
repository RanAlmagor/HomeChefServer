namespace HomeChef.Server.Models.DTOs
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int RecipeCount { get; set; }  
    }
}
