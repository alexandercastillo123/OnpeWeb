using System.Collections.Generic;

namespace OnpeWeb.Models
{
    public class ParticipacionTotalViewModel
    {
        public string Ambito { get; set; } = "Nacional";
        public int ElectoresHabiles { get; set; }
        public int TotalAsistentes { get; set; }
        public string PorcAsistentes { get; set; } = "0.000 %";
        public int TotalAusentes { get; set; }
        public string PorcAusentes { get; set; } = "0.000 %";
        public List<DatoResultado> ListaDepartamentos { get; set; } = new();
        public string? DepartamentoSeleccionado { get; set; }
        public string? ProvinciaSeleccionada { get; set; }
        public string? DistritoSeleccionado { get; set; }
    }
}