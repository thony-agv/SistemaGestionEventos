using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IEventoService
    {
        // Consultar eventos
        List<Evento> ObtenerTodos();

        // Buscar un evento
        Evento ObtenerPorId(int id);

        // Crear evento
        void Crear(Evento evento);

        // Editar evento
        void Actualizar(Evento evento);

        // Eliminar evento
        void Eliminar(int id);

        // Cerrar evento
        void Cerrar(int id);

        // Consultar inscripciones activas
        int ObtenerInscritosActivos(int eventoId);

        // Consultar espacios disponibles
        int ObtenerCuposDisponibles(int eventoId);

        // Consultar entradas registradas
        int ObtenerIngresos(int eventoId);
    }
}
