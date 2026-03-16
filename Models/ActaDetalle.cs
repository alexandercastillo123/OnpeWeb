namespace OnpeWeb.Models
{
    public class ActaDetalle
    {
        public bool Existe { get; set; }
        public string Departamento { get; set; } = "";
        public string Provincia { get; set; } = "";
        public string Distrito { get; set; } = "";
        public string LocalVotacion { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string MesaNro { get; set; } = "";
        public string Copia { get; set; } = "";
        public string EstadoActa { get; set; } = "";
        public int ElectoresHabiles { get; set; }
        public int TotalVotantes { get; set; }
        public int P1 { get; set; }
        public int P2 { get; set; }
        public int Blancos { get; set; }
        public int Nulos { get; set; }
        public int Impugnados { get; set; }
    }
}