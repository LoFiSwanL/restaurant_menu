using System.ComponentModel.DataAnnotations;

namespace NamRestaurantApi.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public List<MenuItem> MenuItems { get; set; } = new();
    }
}
