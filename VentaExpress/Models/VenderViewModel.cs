using System.ComponentModel.DataAnnotations;

namespace VentaExpress.Models
{
    public class VenderViewModel
    {
        [Required]
        public int ProductoId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Categoria { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal Precio { get; set; }

        public int CantidadDisponible { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; } = 1;

        [Required(ErrorMessage = "Seleccione un cliente")]
        public int ClienteId { get; set; }
    }
}
