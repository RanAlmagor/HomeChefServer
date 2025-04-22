namespace HomeChefServer.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
