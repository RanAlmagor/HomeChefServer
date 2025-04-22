public class User
{
    public int Id { get; set; }                     // Primary Key
    public string Username { get; set; }            // לא ניתן לשינוי
    public string Email { get; set; }               // לא ניתן לשינוי
    public string PasswordHash { get; set; }

    public string? ProfilePictureBase64 { get; set; }  
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }

    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
