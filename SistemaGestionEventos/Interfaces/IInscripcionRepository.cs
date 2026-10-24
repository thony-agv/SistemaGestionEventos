using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IInscripcionRepository
    {
        // Consultar todas las inscripciones
        List<Inscripcion> ObtenerTodos();

        // Consultar inscripciones de un evento
        List<Inscripcion> ObtenerPorEvento(int eventoId);

        // Buscar inscripción por ID
        Inscripcion ObtenerPorId(int id);

        // Registrar o reactivar una inscripción cancelada
        void Registrar(Inscripcion inscripcion);

        // Cancelar una inscripción activa
        void Cancelar(int id);

        // Verificar si ya se registró una entrada
        bool TieneRegistroEntrada(int inscripcionId);
    }
}
