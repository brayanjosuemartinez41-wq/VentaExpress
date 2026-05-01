
using VentaExpress.Services;
using Microsoft.AspNetCore.Mvc;
using VentaExpress.Models;
using VentaExpress.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace VentaExpress.Controllers
{
    public class VentasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IProductoService _productoService;

        public VentasController(AppDbContext context, IProductoService productoService)
        {
            _context = context;
            _productoService = productoService;
        }

        // 🔥 MOSTRAR PRODUCTOS con búsqueda, filtro y orden
        public async Task<IActionResult> Index(string search, string category, string sortOrder)
        {
            var listaAll = await _productoService.ObtenerTodosAsync();
            var query = listaAll.AsQueryable();

            // filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Nombre.Contains(search) || p.Descripcion.Contains(search));
            }

            // filtro por categoría
            var categorias = listaAll.Select(p => p.Categoria).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Categoria == category);
            }

            // ordenamiento
            sortOrder = sortOrder ?? string.Empty;
            query = sortOrder switch
            {
                "precio_asc" => query.OrderBy(p => p.Precio),
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                "cantidad_asc" => query.OrderBy(p => p.Cantidad),
                "cantidad_desc" => query.OrderByDescending(p => p.Cantidad),
                _ => query.OrderBy(p => p.Nombre),
            };

            ViewBag.Categorias = categorias;
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.SortOrder = sortOrder;

            var lista = query.ToList();
            return View(lista);
        }

        // 🔥 EXPORTAR CSV
        public async Task<IActionResult> ExportCsv()
        {
            var productos = await _context.Productos.ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Nombre,Categoria,Precio,Cantidad,Subtotal");

            foreach (var p in productos)
            {
                var nombre = p.Nombre?.Replace("\"", "\"\"") ?? string.Empty;
                var categoria = p.Categoria?.Replace("\"", "\"\"") ?? string.Empty;
                sb.AppendLine($"{p.Id},\"{nombre}\",\"{categoria}\",{p.Precio},{p.Cantidad},{p.Subtotal}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "productos.csv");
        }

        // 🔥 MOSTRAR FORMULARIO VENDER
        public async Task<IActionResult> Vender(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 PROCESAR VENTA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vender(int id, int cantidad)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            if (cantidad <= 0 || cantidad > producto.Cantidad)
            {
                TempData["mensaje"] = "Cantidad inválida para la venta.";
                return RedirectToAction("Vender", new { id });
            }

            producto.Cantidad -= cantidad;
            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();

            TempData["mensaje"] = $"Venta realizada. Se vendieron {cantidad} unidad(es) de {producto.Nombre}.";
            return RedirectToAction("Index");
        }

        // 🔥 CREAR (GET)
        public IActionResult Crear()
        {
            return View();
        }

        // 🔥 CREAR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto p)
        {
            if (ModelState.IsValid)
            {
                await _productoService.AgregarAsync(p);
                TempData["mensaje"] = "El producto ha sido agregado correctamente";
                return RedirectToAction("Index");
            }

            return View(p);
        }

        // 🔥 ELIMINAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _productoService.EliminarAsync(id);
            TempData["mensaje"] = "El producto ha sido eliminado correctamente";
            return RedirectToAction("Index");
        }

        // 🔥 EDITAR (GET)
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 DETALLES
        public async Task<IActionResult> Detalles(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 CONFIRMAR ELIMINAR
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View("Eliminar", producto);
        }

        // 🔥 EDITAR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Producto p)
        {
            if (ModelState.IsValid)
            {
                await _productoService.ActualizarAsync(p);

                TempData["mensaje"] = "El producto se actualizó correctamente";
                return RedirectToAction("Index");
            }

            return View(p);
        }
    }
}
