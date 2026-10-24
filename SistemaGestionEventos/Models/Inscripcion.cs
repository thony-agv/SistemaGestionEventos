using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionEventos.Models
{
    public class Inscripcion
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un evento.")]
        [Display(Name = "Evento")]
        public int EventoId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un participante.")]
        [Display(Name = "Participante")]
        public int ParticipanteId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un tipo de entrada.")]
        [Display(Name = "Tipo de entrada")]
        public int TipoEntradaId { get; set; }

        [Display(Name = "Fecha de inscripción")]
        public DateTime FechaInscripcion { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        // Datos adicionales para mostrar en las vistas
        public string NombreEvento { get; set; }

        public string NombreParticipante { get; set; }

        public string NombreTipoEntrada { get; set; }

        public Inscripcion()
        {
            FechaInscripcion = DateTime.Now;
            Estado = "Activa";
        }
    }
}
