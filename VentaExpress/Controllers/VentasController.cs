
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

            // cargar clientes para el dropdown
            var clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.Clientes = clientes;

            return View(producto);
        }

        // 🔥 PROCESAR VENTA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vender(int id, int cantidad)
        {
            var producto = await _context.Productos.FindAsync(id);
            var clienteIdValue = Request.Form["clienteId"].FirstOrDefault();
            int clienteId = 0;
            int.TryParse(clienteIdValue, out clienteId);

            if (clienteId == 0)
            {
                TempData["mensaje"] = "Seleccione un cliente para la venta.";
                return RedirectToAction("Vender", new { id });
            }

            if (producto == null) return NotFound();

            if (cantidad <= 0 || cantidad > producto.Cantidad)
            {
                TempData["mensaje"] = "Cantidad inválida para la venta.";
                return RedirectToAction("Vender", new { id });
            }

            // realizar venta: crear Venta y DetalleVenta en transacción
            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var venta = new VentaExpress.Models.Venta
                    {
                        Fecha = System.DateTime.UtcNow,
                        ClienteId = clienteId,
                        Total = producto.Precio * cantidad
                    };

                    _context.Ventas.Add(venta);
                    await _context.SaveChangesAsync();

                    var detalle = new VentaExpress.Models.DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        Precio = producto.Precio
                    };

                    _context.DetalleVentas.Add(detalle);

                    producto.Cantidad -= cantidad;
                    _context.Productos.Update(producto);

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    TempData["mensaje"] = $"Venta realizada (ID {venta.Id}). Se vendieron {cantidad} unidad(es) de {producto.Nombre}.";
                    return RedirectToAction("Index");
                }
                catch
                {
                    await tx.RollbackAsync();
                    TempData["mensaje"] = "Ocurrió un error al procesar la venta.";
                    return RedirectToAction("Vender", new { id });
                }
            }
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
                try
                {
                    await _productoService.AgregarAsync(p);
                    TempData["mensaje"] = "El producto ha sido agregado correctamente";
                    return RedirectToAction("Index");
                }
                catch (ValidationException vex)
                {
                    ModelState.AddModelError(string.Empty, vex.Message);
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el producto.");
                }
            }

            return View(p);
        }

        // 🔥 ELIMINAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                await _productoService.EliminarAsync(id);
                TempData["mensaje"] = "El producto ha sido eliminado correctamente";
            }
            catch (ValidationException vex)
            {
                TempData["mensaje"] = vex.Message;
            }
            catch
            {
                TempData["mensaje"] = "Ocurrió un error al eliminar el producto.";
            }

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
