using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NamRestaurantApi.Data;
using NamRestaurantApi.Models;

namespace NamRestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest("Назва категорії не може бути порожньою! Введіть нормальну назву (наприклад, 'Десерти').");
            }

            if (category.Name.Length < 3)
            {
                return BadRequest("Назва категорії занадто коротка. Мінімум 3 символи.");
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }
    }
}