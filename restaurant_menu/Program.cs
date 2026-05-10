using Microsoft.EntityFrameworkCore;
using NamRestaurantApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conn = ctx.Database.GetDbConnection();
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('MenuItems');";
            using var reader = cmd.ExecuteReader();
            var hasImage = false;
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (name == "ImageUrl")
                {
                    hasImage = true;
                    break;
                }
            }

            if (!hasImage)
            {
                using var addCmd = conn.CreateCommand();
                addCmd.CommandText = "ALTER TABLE MenuItems ADD COLUMN ImageUrl TEXT;";
                addCmd.ExecuteNonQuery();
            }

            var needed = new[]{ "Calories","Protein","Fat","Carbs","Allergens" };
            var existing = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cmd.CommandText = "PRAGMA table_info('MenuItems');";
            using var reader2 = cmd.ExecuteReader();
            while (reader2.Read()) existing.Add(reader2.GetString(1));
            foreach(var col in needed){
                if(!existing.Contains(col)){
                    using var c = conn.CreateCommand();
                    var type = col=="Calories" ? "INTEGER" : (col=="Allergens"?"TEXT":"REAL");
                    c.CommandText = $"ALTER TABLE MenuItems ADD COLUMN {col} {type};";
                    try{ c.ExecuteNonQuery(); }catch{};
                }
            }

            using (var cmd2 = conn.CreateCommand())
            {
                cmd2.CommandText = "PRAGMA table_info('Categories');";
                using var r2 = cmd2.ExecuteReader();
                var hasCatImage = false;
                while (r2.Read())
                {
                    var name = r2.GetString(1);
                    if (name == "ImageUrl") { hasCatImage = true; break; }
                }
                if (!hasCatImage)
                {
                    using var add = conn.CreateCommand();
                    add.CommandText = "ALTER TABLE Categories ADD COLUMN ImageUrl TEXT;";
                    add.ExecuteNonQuery();
                }
            }
        }
        conn.Close();
    }
    catch
    {
    }
}

app.Run();
