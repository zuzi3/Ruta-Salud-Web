namespace CarnetSaludWeb.Pages
{
    public class ExamenResultadoDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string ArchivoPdf { get; set; } = string.Empty;
    }
}