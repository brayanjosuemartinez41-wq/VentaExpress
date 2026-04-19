
using Microsoft.AspNetCore.Mvc;
using VentaExpress.Models;
using VentaExpress.Data;
using System.Linq;
using System.Text;

namespace VentaExpress.Controllers
{
    public class VentasController : Controller
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 MOSTRAR PRODUCTOS con búsqueda, filtro por categoría y orden
        public IActionResult Index(string search, string category, string sortOrder)
        {
            var query = _context.Productos.AsQueryable();

            // filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Nombre.Contains(search) || p.Descripcion.Contains(search));
            }

            // filtro por categoría
            var categorias = _context.Productos.Select(p => p.Categoria).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Categoria == category);
            }

            // ordenamiento simple
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
        public IActionResult ExportCsv()
        {
            var productos = _context.Productos.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Nombre,Categoria,Precio,Cantidad,Subtotal");
            foreach (var p in productos)
            {
                // escapar comillas básicas
                var nombre = p.Nombre?.Replace("\"", "\"\"") ?? string.Empty;
                var categoria = p.Categoria?.Replace("\"", "\"\"") ?? string.Empty;
                sb.AppendLine($"{p.Id},\"{nombre}\",\"{categoria}\",{p.Precio},{p.Cantidad},{p.Subtotal}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "productos.csv");
        }

        // 🔥 MOSTRAR FORMULARIO VENDER
        public IActionResult Vender(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 PROCESAR VENTA (reduce cantidad)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vender(int id, int cantidad)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();

            if (cantidad <= 0 || cantidad > producto.Cantidad)
            {
                TempData["mensaje"] = "Cantidad inválida para la venta.";
                return RedirectToAction("Vender", new { id });
            }

            producto.Cantidad -= cantidad;
            _context.Productos.Update(producto);
            _context.SaveChanges();

            TempData["mensaje"] = $"Venta realizada. Se vendieron {cantidad} unidad(es) de {producto.Nombre}.";
            return RedirectToAction("Index");
        }

        // 🔥 CREAR - mostrar formulario
        public IActionResult Crear()
        {
            return View();
        }

        // 🔥 CREAR - guardar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(Producto p)
        {
            if (ModelState.IsValid)
            {
                _context.Productos.Add(p);
                _context.SaveChanges();

                TempData["mensaje"] = "El producto ha sido agregado correctamente";
                return RedirectToAction("Index");
            }

            return View(p);
        }

        // 🔥 ELIMINAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var producto = _context.Productos.Find(id);

            if (producto != null)
            {
                _context.Productos.Remove(producto);
                _context.SaveChanges();

                TempData["mensaje"] = "El Producto ha sido eliminado correctamente";
            }

            return RedirectToAction("Index");
        }

        // 🔥 MOSTRAR EDITAR
        public IActionResult Editar(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 MOSTRAR DETALLES
        public IActionResult Detalles(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        // 🔥 MOSTRAR CONFIRMACION ELIMINAR
        public IActionResult ConfirmarEliminar(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto == null) return NotFound();
            return View("Eliminar", producto);
        }

        // 🔥 GUARDAR EDITAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Producto p)
        {
            if (ModelState.IsValid)
            {
                _context.Productos.Update(p);
                _context.SaveChanges();

                TempData["mensaje"] = "El producto se actualizado correctamente";
                return RedirectToAction("Index");
            }

            return View(p);
        }
    }
}
