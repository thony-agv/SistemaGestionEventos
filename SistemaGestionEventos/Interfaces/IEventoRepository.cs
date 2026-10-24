using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IEventoRepository
    {
        // Consultar todos los eventos
        List<Evento> ObtenerTodos();

        // Buscar evento por ID
        Evento ObtenerPorId(int id);

        // Registrar un evento
        void Crear(Evento evento);

        // Modificar un evento
        void Actualizar(Evento evento);

        // Eliminar un evento sin registros relacionados
        void Eliminar(int id);

        // Cerrar un evento
        void Cerrar(int id);

        // Contar inscripciones activas
        int ContarInscripcionesActivas(int eventoId);

        // Contar ingresos registrados
        int ContarIngresos(int eventoId);

        // Comprobar si tiene inscripciones
        bool TieneInscripciones(int eventoId);
    }
}
