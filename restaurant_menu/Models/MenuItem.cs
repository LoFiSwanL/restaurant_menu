using System.ComponentModel.DataAnnotations;

namespace NamRestaurantApi.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, 100000)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public int? Calories { get; set; }
        public decimal? Protein { get; set; }
        public decimal? Fat { get; set; }
        public decimal? Carbs { get; set; }

        public string? Allergens { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
