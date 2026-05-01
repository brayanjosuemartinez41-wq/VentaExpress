using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VentaExpress.Data;
using VentaExpress.Services;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configuración de cultura (opcional, pero tú ya lo tienes)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 🔹 Conexión a SQL Server (LO IMPORTANTE)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

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

app.Run();