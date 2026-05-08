using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VentaExpress.Data;
using VentaExpress.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configuración de cultura (opcional, pero tú ya lo tienes)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 🔹 Conexión a la base de datos (soporta SqlServer o Sqlite según configuración)
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider")?.ToLowerInvariant() ?? "sqlserver";
Console.WriteLine($"Selected DatabaseProvider: {dbProvider}");
if (dbProvider == "sqlite")
{
    // Use an absolute path inside the project content root so the .db file is easy to find
    var sqliteRelative = builder.Configuration.GetValue<string>("SqliteConnection") ?? "Data Source=ventaexpress.db";
    // sqliteRelative might be like "Data Source=ventaexpress.db"; extract filename if present
    var filename = sqliteRelative.Replace("Data Source=", "").Trim();
    var sqlitePath = System.IO.Path.Combine(builder.Environment.ContentRootPath, filename);
    var sqliteConn = $"Data Source={sqlitePath}";
    Console.WriteLine($"Sqlite connection: {sqliteConn}");
    Console.WriteLine($"Sqlite DB exists: {System.IO.File.Exists(sqlitePath)} (path: {sqlitePath})");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(sqliteConn));
}
else
{
    Console.WriteLine($"SqlServer connection: {builder.Configuration.GetConnectionString("DefaultConnection")}");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure()
        ));
}

// 🔹 Servicios MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProductoService, ProductoService>();

var app = builder.Build();

// 🔹 Configuración del pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 🔹 Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Ventas}/{action=Index}/{id?}");

// Aplicar migraciones pendientes al iniciar para asegurarnos que la BD y tablas existan
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        // Si ocurre un error al aplicar migraciones, escribir en consola para diagnóstico
        Console.WriteLine($"Error applying migrations: {ex}");
        // Si usamos Sqlite, intentar EnsureCreated como fallback (útil en dev cuando las migrations
        // fueron generadas originalmente para SQL Server y fallan en Sqlite)
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            if (dbProvider == "sqlite")
            {
                context.Database.EnsureCreated();
                Console.WriteLine("Database created with EnsureCreated fallback for Sqlite.");
            }
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"EnsureCreated also failed: {ex2}");
        }
    }
}

app.Run();
