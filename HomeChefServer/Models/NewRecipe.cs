namespace HomeChef.Server.Models
{
    public class NewRecipe
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string SourceUrl { get; set; }
        public int CategoryId { get; set; }
        public int UserId { get; set; }

        public NewRecipe() { }
    }
}
