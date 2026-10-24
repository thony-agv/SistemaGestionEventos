using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Services
{
    public class ParticipanteService : IParticipanteService
    {
        private readonly IParticipanteRepository _participanteRepository;

        // CONSTRUCTOR
        public ParticipanteService(
            IParticipanteRepository participanteRepository)
        {
            _participanteRepository = participanteRepository ??
                throw new ArgumentNullException(
                    nameof(participanteRepository));
        }

        // 1. CONSULTAR TODOS LOS PARTICIPANTES
        public List<Participante> ObtenerTodos()
        {
            return _participanteRepository.ObtenerTodos();
        }

        // 2. BUSCAR PARTICIPANTE POR ID
        public Participante ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del participante no es válido.");
            }

            return _participanteRepository.ObtenerPorId(id);
        }

        // 3. REGISTRAR PARTICIPANTE
        public void Crear(Participante participante)
        {
            ValidarParticipante(participante);

            // Verificar documento duplicado
            if (_participanteRepository.ExisteDocumento(
                participante.Documento))
            {
                throw new InvalidOperationException(
                    "Ya existe un participante registrado " +
                    "con ese documento de identificación.");
            }

            // Guardar participante
            _participanteRepository.Crear(participante);
        }

        // 4. ACTUALIZAR PARTICIPANTE
        public void Actualizar(Participante participante)
        {
            if (participante == null)
            {
                throw new ArgumentNullException(
                    nameof(participante));
            }

            if (participante.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del participante no es válido.");
            }

            // Comprobar que exista
            Participante actual =
                _participanteRepository.ObtenerPorId(
                    participante.Id);

            if (actual == null)
            {
                throw new InvalidOperationException(
                    "El participante seleccionado no existe.");
            }

            // Validar los nuevos datos
            ValidarParticipante(participante);

            // Comprobar que el documento no pertenezca
            // a otro participante
            if (_participanteRepository.ExisteDocumento(
                participante.Documento, participante.Id))
            {
                throw new InvalidOperationException(
                    "El documento de identificación " +
                    "ya pertenece a otro participante.");
            }

            // Actualizar datos
            _participanteRepository.Actualizar(participante);
        }

        // 5. ELIMINAR PARTICIPANTE
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del participante no es válido.");
            }

            Participante participante =
                _participanteRepository.ObtenerPorId(id);

            if (participante == null)
            {
                throw new InvalidOperationException(
                    "El participante no existe.");
            }

            // Proteger el historial de inscripciones
            if (_participanteRepository.TieneInscripciones(id))
            {
                throw new InvalidOperationException(
                    "No se puede eliminar el participante " +
                    "porque tiene inscripciones registradas.");
            }

            _participanteRepository.Eliminar(id);
        }

        // 6. VALIDACIONES GENERALES
        private void ValidarParticipante(Participante participante)
        {
            if (participante == null)
            {
                throw new ArgumentNullException(
                    nameof(participante));
            }

            // Eliminar espacios innecesarios
            participante.Documento =
                participante.Documento?.Trim();

            participante.NombreCompleto =
                participante.NombreCompleto?.Trim();

            participante.Correo =
                participante.Correo?.Trim();

            participante.Telefono =
                participante.Telefono?.Trim();

            // Normalizar campos opcionales
            if (string.IsNullOrWhiteSpace(participante.Correo))
            {
                participante.Correo = null;
            }

            if (string.IsNullOrWhiteSpace(participante.Telefono))
            {
                participante.Telefono = null;
            }

            // Validar atributos del modelo
            var contexto = new ValidationContext(participante);

            var resultados = new List<ValidationResult>();

            bool esValido = Validator.TryValidateObject(
                participante,
                contexto,
                resultados,
                true);

            if (!esValido)
            {
                throw new ValidationException(
                    resultados[0].ErrorMessage);
            }

            // Evitar valores compuestos solo por espacios
            if (string.IsNullOrWhiteSpace(
                participante.Documento))
            {
                throw new ValidationException(
                    "El documento de identificación es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(
                participante.NombreCompleto))
            {
                throw new ValidationException(
                    "El nombre completo es obligatorio.");
            }
        }
    }
}
