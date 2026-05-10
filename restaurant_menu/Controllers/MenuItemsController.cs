using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NamRestaurantApi.Data;
using NamRestaurantApi.Models;

namespace NamRestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly string _adminPass;

        public MenuItemsController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _adminPass = config.GetValue<string>("Admin:Password") ?? "nam2026";
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] string? q, [FromQuery] int? categoryId)
        {
            var query = _context.MenuItems.Include(mi => mi.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var low = q.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(low) || (i.Description != null && i.Description.ToLower().Contains(low)));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            var items = await query.ToListAsync();
            var dto = items.Select(i => new {
                i.Id, i.Name, i.Description, i.Price, i.ImageUrl, i.CategoryId, CategoryName = i.Category?.Name,
                Calories = i.Calories, Protein = i.Protein, Fat = i.Fat, Carbs = i.Carbs, Allergens = i.Allergens
            });
            return Ok(dto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MenuItem>> Get(int id)
        {
        var item = await _context.MenuItems.Include(m=>m.Category).FirstOrDefaultAsync(m=>m.Id==id);
        if (item == null) return NotFound();
        return Ok(new {
            item.Id, item.Name, item.Description, item.Price, item.ImageUrl, item.CategoryId, CategoryName = item.Category?.Name,
            Calories = item.Calories, Protein = item.Protein, Fat = item.Fat, Carbs = item.Carbs, Allergens = item.Allergens
        });
        }

        [HttpPost]
        public async Task<ActionResult<MenuItem>> Create([FromHeader(Name = "Admin-Pass")] string pass, MenuItem dto)
        {
            if (pass != _adminPass) return Unauthorized("невірний пароль");
            if (dto == null) return BadRequest("Невірні дані");

            dto.Name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Назва не може бути порожньою");

            if (dto.Price < 0) return BadRequest("Ціна не може бути від'ємною");
            if (dto.Calories.HasValue && dto.Calories.Value < 0) return BadRequest("Калорії не можуть бути від'ємними");
            if (dto.Protein.HasValue && dto.Protein.Value < 0) return BadRequest("Білки не можуть бути від'ємними");
            if (dto.Fat.HasValue && dto.Fat.Value < 0) return BadRequest("Жири не можуть бути від'ємними");
            if (dto.Carbs.HasValue && dto.Carbs.Value < 0) return BadRequest("Вуглеводи не можуть бути від'ємними");

            _context.MenuItems.Add(dto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromHeader(Name = "Admin-Pass")] string pass, MenuItem dto)
    {
        if (pass != _adminPass) return Unauthorized("невірний пароль");
        if (id != dto.Id) return BadRequest();

        var item = await _context.MenuItems.FindAsync(id);
        if (item == null) return NotFound();
        if (dto.Price < 0) return BadRequest("Ціна не може бути від'ємною");
        if (dto.Calories.HasValue && dto.Calories.Value < 0) return BadRequest("Калорії не можуть бути від'ємними");
        if (dto.Protein.HasValue && dto.Protein.Value < 0) return BadRequest("Білки не можуть бути від'ємними");
        if (dto.Fat.HasValue && dto.Fat.Value < 0) return BadRequest("Жири не можуть бути від'ємними");
        if (dto.Carbs.HasValue && dto.Carbs.Value < 0) return BadRequest("Вуглеводи не можуть бути від'ємними");

        item.Name = dto.Name?.Trim() ?? item.Name;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.ImageUrl = dto.ImageUrl;
        item.CategoryId = dto.CategoryId;
        item.Calories = dto.Calories;
        item.Protein = dto.Protein;
        item.Fat = dto.Fat;
        item.Carbs = dto.Carbs;
        item.Allergens = dto.Allergens;

        await _context.SaveChangesAsync();
        return NoContent();
    }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromHeader(Name = "Admin-Pass")] string pass)
        {
            if (pass != _adminPass) return Unauthorized("невірний пароль");
            var item = await _context.MenuItems.FindAsync(id);
            if (item == null) return NotFound();
            _context.MenuItems.Remove(item); 
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("upload")]
        public async Task<ActionResult> Upload([FromHeader(Name = "Admin-Pass")] string pass, [FromBody] ImageUploadDto dto)
        {
            if (pass != _adminPass) return Unauthorized("невірний пароль");
            if (dto == null || string.IsNullOrWhiteSpace(dto.ImageBase64)) return BadRequest("No image");

            try
            {
                var bytes = Convert.FromBase64String(dto.ImageBase64);
                var name = $"img_{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var filePath = Path.Combine(path, name);
                await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                var url = $"/uploads/{name}";
                return Ok(new { url });
            }
            catch
            {
                return BadRequest("Не вдалося зберегти зображення");
            }
        }

        public record ImageUploadDto(string ImageBase64);
    }
}
