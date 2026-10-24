using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Services
{
    public class RegistroEntradaService : IRegistroEntradaService
    {
        private readonly IRegistroEntradaRepository _registroRepository;
        private readonly IInscripcionRepository _inscripcionRepository;
        private readonly IEventoRepository _eventoRepository;

        // CONSTRUCTOR
        public RegistroEntradaService(
            IRegistroEntradaRepository registroRepository,
            IInscripcionRepository inscripcionRepository,
            IEventoRepository eventoRepository)
        {
            _registroRepository = registroRepository ??
                throw new ArgumentNullException(
                    nameof(registroRepository));

            _inscripcionRepository = inscripcionRepository ??
                throw new ArgumentNullException(
                    nameof(inscripcionRepository));

            _eventoRepository = eventoRepository ??
                throw new ArgumentNullException(
                    nameof(eventoRepository));
        }

        // 1. CONSULTAR TODOS LOS INGRESOS
        public List<RegistroEntrada> ObtenerTodos()
        {
            return _registroRepository.ObtenerTodos();
        }

        // 2. CONSULTAR INGRESOS POR EVENTO
        public List<RegistroEntrada> ObtenerPorEvento(int eventoId)
        {
            if (eventoId <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            Evento evento =
                _eventoRepository.ObtenerPorId(eventoId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento seleccionado no existe.");
            }

            return _registroRepository.ObtenerPorEvento(eventoId);
        }

        // 3. BUSCAR REGISTRO POR ID
        public RegistroEntrada ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del registro no es valido.");
            }

            return _registroRepository.ObtenerPorId(id);
        }

        // 4. REGISTRAR INGRESO
        public void Registrar(int inscripcionId)
        {
            if (inscripcionId <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar una inscripcion valida.");
            }

            // VERIFICAR INSCRIPCION
            Inscripcion inscripcion =
                _inscripcionRepository.ObtenerPorId(
                    inscripcionId);

            if (inscripcion == null)
            {
                throw new InvalidOperationException(
                    "La inscripcion seleccionada no existe.");
            }

            // VERIFICAR QUE ESTE ACTIVA
            if (inscripcion.Estado != "Activa")
            {
                throw new InvalidOperationException(
                    "No se puede registrar el ingreso " +
                    "porque la inscripcion esta cancelada.");
            }

            // VERIFICAR EVENTO
            Evento evento =
                _eventoRepository.ObtenerPorId(
                    inscripcion.EventoId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento relacionado no existe.");
            }

            // IMPEDIR INGRESOS EN EVENTOS CERRADOS
            if (evento.Estado != "Abierto")
            {
                throw new InvalidOperationException(
                    "No se permiten ingresos " +
                    "en eventos cerrados.");
            }

            // VERIFICAR SI YA REGISTRO ENTRADA
            if (_registroRepository.TieneEntrada(
                inscripcionId))
            {
                throw new InvalidOperationException(
                    "Este participante ya tiene " +
                    "su ingreso registrado en el evento.");
            }

            // REGISTRAR ENTRADA EN SQLITE
            _registroRepository.Registrar(inscripcionId);
        }

        // 5. VERIFICAR REGISTRO DE ENTRADA
        public bool TieneEntrada(int inscripcionId)
        {
            if (inscripcionId <= 0)
            {
                throw new ArgumentException(
                    "El ID de la inscripcion no es valido.");
            }

            return _registroRepository.TieneEntrada(
                inscripcionId);
        }
    }
}
