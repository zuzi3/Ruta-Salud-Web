namespace CarnetSaludWeb.Models
{
    public class PacienteFullInfo
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rut { get; set; } = string.Empty;
        public int Edad { get; set; }
        public string Prevision { get; set; } = string.Empty;
        public bool TieneDiscapacidad { get; set; }
        public string TipoDiscapacidad { get; set; } = string.Empty;
        public List<string> Alergias { get; set; } = new();
        public string SaludMentalInfo { get; set; } = string.Empty;
        public List<string> AntecedentesFamiliares { get; set; } = new();
        public List<CitaItem> Citas { get; set; } = new();
        public List<ExamenItem> Examenes { get; set; } = new();
    }

    public class CitaItem
    {
        public int Id { get; set; }
        public string Doctor { get; set; } = string.Empty;
        public string Especialidad { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class ExamenItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Doctor { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
    }
}