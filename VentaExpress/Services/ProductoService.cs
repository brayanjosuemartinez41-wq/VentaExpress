using VentaExpress.Data;
using VentaExpress.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace VentaExpress.Services
{
    public class ProductoService : IProductoService
    {
        private readonly AppDbContext _context;

        public ProductoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> ObtenerTodosAsync()
        {
            return await _context.Productos.ToListAsync();
        }

        public async Task<Producto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Productos.FindAsync(id);
        }

        public async Task AgregarAsync(Producto producto)
        {
            if (producto == null) throw new System.ArgumentNullException(nameof(producto));

            // Reglas de validación
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                throw new ValidationException("El nombre del producto es obligatorio.");

            if (producto.Precio < 0)
                throw new ValidationException("El precio no puede ser negativo.");

            if (producto.Cantidad < 0)
                throw new ValidationException("La cantidad no puede ser negativa.");

            // Evitar duplicados por nombre (case-insensitive)
            var existe = await _context.Productos.AnyAsync(p => p.Nombre.ToLower() == producto.Nombre.ToLower());
            if (existe)
                throw new ValidationException("Ya existe un producto con el mismo nombre.");

            await _context.Productos.AddAsync(producto);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Producto producto)
        {
            if (producto == null) throw new System.ArgumentNullException(nameof(producto));

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                throw new ValidationException("El nombre del producto es obligatorio.");

            if (producto.Precio < 0)
                throw new ValidationException("El precio no puede ser negativo.");

            if (producto.Cantidad < 0)
                throw new ValidationException("La cantidad no puede ser negativa.");

            // Evitar duplicados por nombre en otro registro
            var existe = await _context.Productos.AnyAsync(p => p.Id != producto.Id && p.Nombre.ToLower() == producto.Nombre.ToLower());
            if (existe)
                throw new ValidationException("Ya existe otro producto con el mismo nombre.");

            _context.Productos.Update(producto);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
            }
        }
    }
}