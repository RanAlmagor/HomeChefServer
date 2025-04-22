public class RatingDTO
{
    public int RecipeId { get; set; }  // מזהה המתכון
    public int UserId { get; set; }    // מזהה המשתמש
    public int Rating { get; set; }    // הדירוג שהמשתמש נתן
    public DateTime CreatedAt { get; set; } // תאריך יצירת הדירוג

    // שדות נוספים לצורך הצגת דירוגים בצד הלקוח
    public double? AverageRating { get; set; }  // דירוג ממוצע של המתכון
    public int RatingCount { get; set; }  // מספר הדירוגים שהתקבלו
}
