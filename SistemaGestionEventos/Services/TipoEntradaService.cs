using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Services
{
    public class TipoEntradaService : ITipoEntradaService
    {
        private readonly ITipoEntradaRepository _tipoRepository;
        private readonly IEventoRepository _eventoRepository;

        // CONSTRUCTOR
        public TipoEntradaService(
            ITipoEntradaRepository tipoRepository,
            IEventoRepository eventoRepository)
        {
            _tipoRepository = tipoRepository ??
                throw new ArgumentNullException(
                    nameof(tipoRepository));

            _eventoRepository = eventoRepository ??
                throw new ArgumentNullException(
                    nameof(eventoRepository));
        }

        // 1. CONSULTAR TODOS LOS TIPOS DE ENTRADA
        public List<TipoEntrada> ObtenerTodos()
        {
            return _tipoRepository.ObtenerTodos();
        }

        // 2. CONSULTAR TIPOS POR EVENTO
        public List<TipoEntrada> ObtenerPorEvento(int eventoId)
        {
            if (eventoId <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es válido.");
            }

            return _tipoRepository.ObtenerPorEvento(eventoId);
        }

        // 3. BUSCAR TIPO DE ENTRADA POR ID
        public TipoEntrada ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del tipo de entrada no es válido.");
            }

            return _tipoRepository.ObtenerPorId(id);
        }

        // 4. REGISTRAR TIPO DE ENTRADA
        public void Crear(TipoEntrada tipoEntrada)
        {
            ValidarTipoEntrada(tipoEntrada);

            // Verificar que el evento exista y esté abierto
            ValidarEventoAbierto(tipoEntrada.EventoId);

            // Evitar nombres duplicados en el mismo evento
            if (_tipoRepository.ExisteNombre(
                tipoEntrada.EventoId,
                tipoEntrada.Nombre))
            {
                throw new InvalidOperationException(
                    "Ya existe un tipo de entrada con " +
                    "ese nombre para el evento seleccionado.");
            }

            _tipoRepository.Crear(tipoEntrada);
        }

        // 5. ACTUALIZAR TIPO DE ENTRADA
        public void Actualizar(TipoEntrada tipoEntrada)
        {
            if (tipoEntrada == null)
            {
                throw new ArgumentNullException(
                    nameof(tipoEntrada));
            }

            if (tipoEntrada.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del tipo de entrada no es válido.");
            }

            // Buscar registro actual
            TipoEntrada actual =
                _tipoRepository.ObtenerPorId(tipoEntrada.Id);

            if (actual == null)
            {
                throw new InvalidOperationException(
                    "El tipo de entrada seleccionado no existe.");
            }

            // No permitir cambiar el evento relacionado
            if (tipoEntrada.EventoId != actual.EventoId)
            {
                throw new InvalidOperationException(
                    "No se puede cambiar el evento " +
                    "asociado al tipo de entrada.");
            }

            // Verificar evento abierto
            ValidarEventoAbierto(actual.EventoId);

            // Validar los nuevos datos
            ValidarTipoEntrada(tipoEntrada);

            // Evitar nombres duplicados
            if (_tipoRepository.ExisteNombre(
                tipoEntrada.EventoId,
                tipoEntrada.Nombre,
                tipoEntrada.Id))
            {
                throw new InvalidOperationException(
                    "Ya existe otro tipo de entrada " +
                    "con ese nombre en el evento.");
            }

            _tipoRepository.Actualizar(tipoEntrada);
        }

        // 6. ELIMINAR TIPO DE ENTRADA
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del tipo de entrada no es válido.");
            }

            TipoEntrada tipoEntrada =
                _tipoRepository.ObtenerPorId(id);

            if (tipoEntrada == null)
            {
                throw new InvalidOperationException(
                    "El tipo de entrada no existe.");
            }

            // Verificar que el evento siga abierto
            ValidarEventoAbierto(tipoEntrada.EventoId);

            // Proteger inscripciones relacionadas
            if (_tipoRepository.TieneInscripciones(id))
            {
                throw new InvalidOperationException(
                    "No se puede eliminar el tipo de entrada " +
                    "porque tiene inscripciones registradas.");
            }

            _tipoRepository.Eliminar(id);
        }

        // METODO AUXILIAR: VALIDAR EVENTO
        private void ValidarEventoAbierto(int eventoId)
        {
            Evento evento =
                _eventoRepository.ObtenerPorId(eventoId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento seleccionado no existe.");
            }

            if (evento.Estado != "Abierto")
            {
                throw new InvalidOperationException(
                    "No se pueden realizar cambios " +
                    "en un evento cerrado.");
            }
        }

        // METODO AUXILIAR: VALIDAR TIPO DE ENTRADA
        private void ValidarTipoEntrada(TipoEntrada tipoEntrada)
        {
            if (tipoEntrada == null)
            {
                throw new ArgumentNullException(
                    nameof(tipoEntrada));
            }

            // Eliminar espacios innecesarios
            tipoEntrada.Nombre =
                tipoEntrada.Nombre?.Trim();

            tipoEntrada.Descripcion =
                tipoEntrada.Descripcion?.Trim();

            // Normalizar descripción opcional
            if (string.IsNullOrWhiteSpace(
                tipoEntrada.Descripcion))
            {
                tipoEntrada.Descripcion = null;
            }

            // Validar atributos del modelo
            var contexto =
                new ValidationContext(tipoEntrada);

            var resultados =
                new List<ValidationResult>();

            bool valido = Validator.TryValidateObject(
                tipoEntrada,
                contexto,
                resultados,
                true);

            if (!valido)
            {
                throw new ValidationException(
                    resultados[0].ErrorMessage);
            }

            // Verificar nombre obligatorio
            if (string.IsNullOrWhiteSpace(
                tipoEntrada.Nombre))
            {
                throw new ValidationException(
                    "El nombre del tipo de entrada " +
                    "es obligatorio.");
            }

            // Verificar evento seleccionado
            if (tipoEntrada.EventoId <= 0)
            {
                throw new ValidationException(
                    "Debe seleccionar un evento válido.");
            }
        }
    }
}
