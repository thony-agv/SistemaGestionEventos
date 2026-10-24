using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionEventos.Models
{
    public class Evento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre del evento")]
        public string Nombre { get; set; }

        [StringLength(1000)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El lugar es obligatorio.")]
        [StringLength(200)]
        [Display(Name = "Lugar")]
        public string Lugar { get; set; }

        [Required(ErrorMessage =
            "La fecha de inicio es obligatoria.")]
        [Display(Name = "Fecha de inicio")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaInicio { get; set; }

        [Required(ErrorMessage =
            "La fecha de finalización es obligatoria.")]
        [Display(Name = "Fecha de finalización")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaFin { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "La capacidad debe ser mayor que cero.")]
        [Display(Name = "Capacidad máxima")]
        public int CapacidadMaxima { get; set; }

        [Required]
        [RegularExpression("^(Abierto|Cerrado)$",
            ErrorMessage = "Estado no válido.")]
        public string Estado { get; set; }

        public Evento()
        {
            FechaInicio = DateTime.Now;
            FechaFin = DateTime.Now.AddHours(2);
            Estado = "Abierto";
        }
    }
}
