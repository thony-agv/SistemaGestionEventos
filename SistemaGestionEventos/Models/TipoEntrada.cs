using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel.DataAnnotations;

namespace SistemaGestionEventos.Models
{
    public class TipoEntrada
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un evento.")]
        [Display(Name = "Evento")]
        public int EventoId { get; set; }

        [Required(ErrorMessage =
            "El nombre del tipo de entrada es obligatorio.")]
        [StringLength(100,
            ErrorMessage = "El nombre no puede superar 100 caracteres.")]
        [Display(Name = "Tipo de entrada")]
        public string Nombre { get; set; }

        [StringLength(500,
            ErrorMessage = "La descripción no puede superar 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
    }
}
