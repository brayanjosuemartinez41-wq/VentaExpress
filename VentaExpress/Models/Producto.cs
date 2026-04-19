using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaExpress.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        [Range(0, 99999999.99)]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue)]
        public int Cantidad { get; set; }

        // Computed property
        public decimal Subtotal => Precio * Cantidad;
    }
}
