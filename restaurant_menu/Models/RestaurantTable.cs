namespace NamRestaurantApi.Models
{
    public class RestaurantTable
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public int Seats { get; set; } // Кількість місць
        public bool IsReserved { get; set; }
    }
}