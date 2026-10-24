using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionEventos.Models
{
    public class Participante
    {
        public int Id { get; set; }

        [Required(ErrorMessage =
            "El documento de identificación es obligatorio.")]
        [StringLength(30)]
        [Display(Name = "Documento de identificación")]
        public string Documento { get; set; }

        [Required(ErrorMessage =
            "El nombre completo es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; }

        [EmailAddress(ErrorMessage =
            "Ingrese un correo electrónico válido.")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }

        [StringLength(25)]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }
    }
}
