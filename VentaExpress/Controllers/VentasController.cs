
using Microsoft.AspNetCore.Mvc;
using VentaExpress.Models;
using VentaExpress.Data;
using System.Linq;

namespace VentaExpress.Controllers
{
    public class VentasController : Controller
    {
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

        // 🔥 AGREGAR
        [HttpPost]
        public IActionResult Agregar(Producto p)
        {
            if (ModelState.IsValid)
            {
                _context.Productos.Add(p);
                _context.SaveChanges();

                TempData["mensaje"] = "Producto agregado correctamente";
            }
            return RedirectToAction("Index");
        }

        // 🔥 ELIMINAR
        public IActionResult Eliminar(int id)
        {
            var producto = _context.Productos.Find(id);

            if (producto != null)
            {
                _context.Productos.Remove(producto);
                _context.SaveChanges();

                TempData["mensaje"] = "Producto eliminado correctamente";
            }

            return RedirectToAction("Index");
        }

        // 🔥 MOSTRAR EDITAR
        public IActionResult Editar(int id)
        {
            var producto = _context.Productos.Find(id);
            return View(producto);
        }

        // 🔥 GUARDAR EDITAR
        [HttpPost]
        public IActionResult Editar(Producto p)
        {
            if (ModelState.IsValid)
            {
                _context.Productos.Update(p);
                _context.SaveChanges();

                TempData["mensaje"] = "Producto actualizado correctamente";
            }

            return RedirectToAction("Index");
        }
    }
}
