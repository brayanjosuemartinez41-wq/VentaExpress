
using Microsoft.AspNetCore.Mvc;
using VentaExpress.Models;
using VentaExpress.Data;
using System.Linq;

namespace VentaExpress.Controllers
{
    public class VentasController : Controller
    {
        // 🔥 Conexión a la base de datos
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // 🔥 MOSTRAR PRODUCTOS
        public IActionResult Index()
        {
            var lista = _context.Productos.ToList();
            return View(lista);
        }

        // 🔥 AGREGAR PRODUCTO
        [HttpPost]
        public IActionResult Agregar(Producto p)
        {
            if (ModelState.IsValid)
            {
                _context.Productos.Add(p);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 🔥 ELIMINAR PRODUCTO
        public IActionResult Eliminar(int id)
        {
            var producto = _context.Productos.Find(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
