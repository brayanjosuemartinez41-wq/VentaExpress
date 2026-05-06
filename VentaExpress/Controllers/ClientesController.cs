using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VentaExpress.Data;
using VentaExpress.Models;

namespace VentaExpress.Controllers
{
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(string search, string sortOrder)
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Nombre.Contains(search) || c.Email.Contains(search));
            }

            sortOrder = sortOrder ?? string.Empty;
            query = sortOrder switch
            {
                "nombre_desc" => query.OrderByDescending(c => c.Nombre),
                "email" => query.OrderBy(c => c.Email),
                _ => query.OrderBy(c => c.Nombre),
            };

            var list = await query.ToListAsync();
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            return View(list);
        }

        // GET: Clientes/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["mensaje"] = "Cliente agregado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // POST: Clientes/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                _context.Clientes.Update(cliente);
                await _context.SaveChangesAsync();
                TempData["mensaje"] = "Cliente actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Detalles/5
        public async Task<IActionResult> Detalles(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return View(cliente);
        }

        // GET: Clientes/ConfirmarEliminar/5
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return View("Eliminar", cliente);
        }

        // POST: Clientes/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["mensaje"] = "Cliente eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
