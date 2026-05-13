
using VentaExpress.Services;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<VentasController> _logger;

        public VentasController(AppDbContext context, IProductoService productoService, ILogger<VentasController> logger)
        {
            _context = context;
            _productoService = productoService;
            _logger = logger;
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

        // ExportCsv removed

        // 🔥 MOSTRAR FORMULARIO VENDER
        public async Task<IActionResult> Vender(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            // cargar clientes para el dropdown
            var clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.Clientes = clientes;

            var vm = new VentaExpress.Models.VenderViewModel
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                Categoria = producto.Categoria,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CantidadDisponible = producto.Cantidad,
                Cantidad = 1
            };

            return View(vm);
        }

        // 🔥 PROCESAR VENTA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vender(VentaExpress.Models.VenderViewModel vm)
        {
            _logger?.LogInformation("Vender POST called with vm: {@Vm}", vm);

            // Si el modelo no es válido, devolver la vista con errores
            if (!ModelState.IsValid)
            {
                ViewBag.Clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
                return View(vm);
            }

            var producto = await _context.Productos.FindAsync(vm.ProductoId);
            if (producto == null) return NotFound();

            if (vm.Cantidad <= 0 || vm.Cantidad > producto.Cantidad)
            {
                ModelState.AddModelError("Cantidad", "Cantidad inválida para la venta.");
                ViewBag.Clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
                return View(vm);
            }

            // Use the EF Core execution strategy when retries are enabled (EnableRetryOnFailure)
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                // All operations in the transaction must be executed inside the strategy
                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var venta = new Venta
                        {
                            Fecha = System.DateTime.UtcNow,
                            ClienteId = vm.ClienteId,
                            Total = producto.Precio * vm.Cantidad
                        };

                        _context.Ventas.Add(venta);
                        await _context.SaveChangesAsync();

                        var detalle = new DetalleVenta
                        {
                            VentaId = venta.Id,
                            ProductoId = producto.Id,
                            Cantidad = vm.Cantidad,
                            Precio = producto.Precio
                        };

                        _context.DetalleVentas.Add(detalle);

                        producto.Cantidad -= vm.Cantidad;
                        _context.Productos.Update(producto);

                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();

                        TempData["mensaje"] = $"Venta realizada (ID {venta.Id}). Se vendieron {vm.Cantidad} unidad(es) de {producto.Nombre}.";
                        return RedirectToAction("Index");
                    }
                    catch (System.Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger?.LogError(ex, "Error processing sale for product {ProductId} and client {ClientId}", vm.ProductoId, vm.ClienteId);
                        var errMsg = ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : string.Empty);
                        Console.WriteLine($"Error processing sale: {ex}");
                        TempData["mensaje"] = "Ocurrió un error al procesar la venta. " + errMsg;
                        ViewBag.Clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
                        return View(vm);
                    }
                }
            });
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
