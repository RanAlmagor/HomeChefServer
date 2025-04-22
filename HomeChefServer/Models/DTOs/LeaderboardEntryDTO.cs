namespace HomeChefServer.Models.DTOs
{
    public class LeaderboardEntryDTO
    {
        public string Username { get; set; }
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public DateTime SubmittedAt { get; set; }
    }


}
