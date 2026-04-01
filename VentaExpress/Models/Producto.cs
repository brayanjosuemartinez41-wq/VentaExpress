namespace VentaExpress.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public double Precio { get; set; }
        public int Cantidad { get; set; }

        public double Subtotal
        {
            get { return Precio * Cantidad; }
        }
    }
}