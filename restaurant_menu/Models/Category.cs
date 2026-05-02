namespace NamRestaurantApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<MenuItem> MenuItems { get; set; } = new();
    }
}