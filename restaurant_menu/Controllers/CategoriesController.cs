using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
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
        private readonly string _adminPass;

        public CategoriesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _adminPass = config.GetValue<string>("Admin:Password") ?? "nam2026";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            try
            {
                return await _context.Categories
                    .Include(c => c.MenuItems)
                    .ToListAsync();
            }
            catch (SqliteException ex) when (ex.Message?.Contains("no such column") == true)
            {
                await EnsureMenuItemColumnsAsync();
                return await _context.Categories
                    .Include(c => c.MenuItems)
                    .ToListAsync();
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound();

                return category;
            }
            catch (SqliteException ex) when (ex.Message?.Contains("no such column") == true)
            {
                await EnsureMenuItemColumnsAsync();
                var category = await _context.Categories
                    .Include(c => c.MenuItems)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound();

                return category;
            }
        }

        private async Task EnsureMenuItemColumnsAsync()
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('MenuItems');";
                    using var reader = await cmd.ExecuteReaderAsync();
                    var existing = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                    while (await reader.ReadAsync())
                    {
                        existing.Add(reader.GetString(1));
                    }

                    var toAdd = new System.Collections.Generic.List<string>();
                    if (!existing.Contains("ImageUrl")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN ImageUrl TEXT;");
                    if (!existing.Contains("Calories")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN Calories INTEGER;");
                    if (!existing.Contains("Protein")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN Protein REAL;");
                    if (!existing.Contains("Fat")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN Fat REAL;");
                    if (!existing.Contains("Carbs")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN Carbs REAL;");
                    if (!existing.Contains("Allergens")) toAdd.Add("ALTER TABLE MenuItems ADD COLUMN Allergens TEXT;");

                    foreach (var sql in toAdd)
                    {
                        using var c = conn.CreateCommand();
                        c.CommandText = sql;
                        try { await c.ExecuteNonQueryAsync(); } catch { }
                    }
                }
                await conn.CloseAsync();
            }
            catch
            {
            }
        }

        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory([FromHeader(Name = "Admin-Pass")] string pass, Category category)
        {
            if (pass != _adminPass)
            {
                return Unauthorized("невірний пароль");
            }

            if (category == null)
                return BadRequest("Невірні дані категорії.");

            category.Name = category.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest("Назва категорії не може бути порожньою.");
            }

            if (category.Name.Length < 3)
            {
                return BadRequest("Назва категорії занадто коротка. Мінімум 3 символи.");
            }

            if (category.Name.Length > 100)
            {
                return BadRequest("Назва категорії занадто довга.");
            }

            var exists = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
            if (exists != null)
            {
                return Conflict(new { message = "Категорія з такою назвою вже існує.", existingId = exists.Id });
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromHeader(Name = "Admin-Pass")] string pass, Category dto)
        {
            if (pass != _adminPass) return Unauthorized("невірний пароль");
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            dto.Name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 3) return BadRequest("Неправильна назва");

            var other = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id);
            if (other != null) return Conflict(new { message = "Інша категорія з такою назвою вже існує.", existingId = other.Id });

            cat.Name = dto.Name;
            cat.ImageUrl = dto.ImageUrl;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, [FromHeader(Name = "Admin-Pass")] string pass)
        {
            if (pass != _adminPass)
                return Unauthorized("невірний пароль");

            var cat = await _context.Categories.Include(c => c.MenuItems).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();

            _context.Categories.Remove(cat);
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
                var name = $"cat_{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
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

        [HttpPost("login")]
        public ActionResult Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Pass) || dto.Pass != _adminPass)
            {
                return Unauthorized("невірний пароль");
            }

            return Ok();
        }

        public record LoginDto(string Pass);
        public record ImageUploadDto(string ImageBase64);
        public record MergeDto(int ExistingId, bool ReplaceExisting, string? NewName, string? NewImageUrl);

        [HttpPost("merge")]
        public async Task<IActionResult> Merge([FromHeader(Name = "Admin-Pass")] string pass, MergeDto dto)
        {
            if (pass != _adminPass) return Unauthorized("невірний пароль");
            if (dto == null) return BadRequest("Невірні дані");

            var existing = await _context.Categories.Include(c => c.MenuItems).FirstOrDefaultAsync(c => c.Id == dto.ExistingId);
            if (existing == null) return NotFound("Існуюча категорія не знайдена");

            if (dto.ReplaceExisting)
            {
                existing.Name = dto.NewName?.Trim() ?? existing.Name;
                existing.ImageUrl = dto.NewImageUrl ?? existing.ImageUrl;
                await _context.SaveChangesAsync();
                return Ok(new { message = "updated", id = existing.Id });
            }

            var newCat = new Category
            {
                Name = dto.NewName?.Trim() ?? existing.Name,
                ImageUrl = dto.NewImageUrl
            };
            _context.Categories.Add(newCat);
            await _context.SaveChangesAsync();

            var items = existing.MenuItems.ToList();
            foreach (var it in items)
            {
                it.CategoryId = newCat.Id;
            }
            await _context.SaveChangesAsync();
            _context.Categories.Remove(existing);
            _context.Categories.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "merged", newId = newCat.Id });
        }
    }
}