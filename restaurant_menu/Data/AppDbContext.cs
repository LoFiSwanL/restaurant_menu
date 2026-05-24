using Microsoft.EntityFrameworkCore;
using NamRestaurantApi.Models;

namespace NamRestaurantApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<RestaurantTable> Tables { get; set; }
        public DbSet<SpecialOffer> SpecialOffers { get; set; }
    }
}