namespace NamRestaurantApi.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}