using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IParticipanteRepository
    {
        // Consultar todos los participantes
        List<Participante> ObtenerTodos();

        // Buscar participante por ID
        Participante ObtenerPorId(int id);

        // Registrar un participante
        void Crear(Participante participante);

        // Actualizar participante
        void Actualizar(Participante participante);

        // Eliminar participante
        void Eliminar(int id);

        // Verificar documento duplicado
        bool ExisteDocumento(
            string documento, int excluirId = 0);

        // Verificar inscripciones relacionadas
        bool TieneInscripciones(int participanteId);
    }
}
