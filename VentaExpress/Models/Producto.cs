using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaExpress.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede tener más de 250 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [DataType(DataType.Currency)]
        [Range(0, 99999999.99, ErrorMessage = "El precio debe ser un monto no negativo")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
        public int Cantidad { get; set; }

        // Computed property
        public decimal Subtotal => Precio * Cantidad;
    }
}
