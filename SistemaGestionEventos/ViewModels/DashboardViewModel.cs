using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SistemaGestionEventos.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEventos { get; set; }

        public int EventosAbiertos { get; set; }

        public int TotalParticipantes { get; set; }

        public int InscripcionesActivas { get; set; }

        public int TotalIngresos { get; set; }

        public int CuposDisponibles { get; set; }

        public List<ResumenEventoViewModel> EventosVigentes
        { get; set; }

        public DashboardViewModel()
        {
            EventosVigentes =
                new List<ResumenEventoViewModel>();
        }
    }

    public class ResumenEventoViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        public string Lugar { get; set; }

        public DateTime? FechaInicio { get; set; }

        public int CapacidadMaxima { get; set; }

        public int Inscritos { get; set; }

        public int CuposDisponibles { get; set; }
    }
}
