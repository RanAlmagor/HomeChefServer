namespace HomeChefServer.Models.DTOs
{
    public class IngredientExtractRequest
    {
        public string Summary { get; set; }
        public string Instructions { get; set; }

        public int Servings { get; set; }

    }
}
