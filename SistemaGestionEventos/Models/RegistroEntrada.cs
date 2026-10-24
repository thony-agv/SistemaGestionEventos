using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionEventos.Models
{
    public class RegistroEntrada
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar una inscripción válida.")]
        [Display(Name = "Inscripción")]
        public int InscripcionId { get; set; }

        [Display(Name = "Fecha y hora de ingreso")]
        public DateTime FechaIngreso { get; set; }

        // DATOS ADICIONALES PARA LAS VISTAS
        public int EventoId { get; set; }

        public string NombreEvento { get; set; }

        public string NombreParticipante { get; set; }

        public string NombreTipoEntrada { get; set; }

        public RegistroEntrada()
        {
            FechaIngreso = DateTime.Now;
        }
    }
}
