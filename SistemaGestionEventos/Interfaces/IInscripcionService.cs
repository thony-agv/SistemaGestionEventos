using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IInscripcionService
    {
        // Consultar todas las inscripciones
        List<Inscripcion> ObtenerTodos();

        // Consultar inscripciones de un evento
        List<Inscripcion> ObtenerPorEvento(int eventoId);

        // Buscar inscripción por ID
        Inscripcion ObtenerPorId(int id);

        // Registrar participante en un evento
        void Registrar(Inscripcion inscripcion);

        // Cancelar inscripción
        void Cancelar(int id);
    }
}
