namespace HomeChefServer.Models.DTOs
{
    public class UpdateProfileDto
    {
        public string? Bio { get; set; }
        public string? ProfilePictureBase64 { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
    }

}
