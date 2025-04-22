namespace HomeChef.Server.Models
{
    public class NewFavorite
    {
        public int UserId { get; set; }
        public int RecipeId { get; set; }

        public NewFavorite() { }
    }
}
