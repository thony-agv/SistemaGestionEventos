using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SQLite;
using System.Diagnostics;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;
using SistemaGestionEventos.Repositories;
using SistemaGestionEventos.Services;

namespace SistemaGestionEventos.Controllers
{
    public class RegistrosEntradaController : Controller
    {
        private readonly IRegistroEntradaService _registroService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IEventoService _eventoService;

        // CONSTRUCTOR PRINCIPAL
        public RegistrosEntradaController()
            : this(
                new RegistroEntradaService(
                    new RegistroEntradaRepository(),
                    new InscripcionRepository(),
                    new EventoRepository()),

                new InscripcionService(
                    new InscripcionRepository(),
                    new EventoRepository(),
                    new ParticipanteRepository(),
                    new TipoEntradaRepository()),

                new EventoService(
                    new EventoRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFACES
        public RegistrosEntradaController(
            IRegistroEntradaService registroService,
            IInscripcionService inscripcionService,
            IEventoService eventoService)
        {
            _registroService = registroService ??
                throw new ArgumentNullException(
                    nameof(registroService));

            _inscripcionService = inscripcionService ??
                throw new ArgumentNullException(
                    nameof(inscripcionService));

            _eventoService = eventoService ??
                throw new ArgumentNullException(
                    nameof(eventoService));
        }

        // 1. LISTADO DE INGRESOS
        [HttpGet]
        public ActionResult Index(int? eventoId)
        {
            try
            {
                var eventos = _eventoService.ObtenerTodos();

                ViewBag.EventosFiltro = new SelectList(
                    eventos, "Id", "Nombre", eventoId);

                var registros = eventoId.GetValueOrDefault() > 0
                    ? _registroService.ObtenerPorEvento(
                        eventoId.Value)
                    : _registroService.ObtenerTodos();

                return View(registros);
            }
            catch (ArgumentException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudieron cargar los registros de entrada.";
            }

            ViewBag.EventosFiltro = new SelectList(
                new List<Evento>(), "Id", "Nombre");

            return View(new List<RegistroEntrada>());
        }

        // 2. DETALLES DEL INGRESO
        [HttpGet]
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var registro =
                    _registroService.ObtenerPorId(id.Value);

                if (registro == null)
                    return HttpNotFound();

                return View(registro);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el registro de entrada.";

                return RedirectToAction("Index");
            }
        }

        // 3. MOSTRAR FORMULARIO DE INGRESO
        [HttpGet]
        public ActionResult Crear(int? eventoId)
        {
            try
            {
                CargarCombos(eventoId);

                return View(new RegistroEntrada
                {
                    EventoId = eventoId.GetValueOrDefault()
                });
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo cargar el formulario de ingreso.";

                return RedirectToAction("Index");
            }
        }

        // 4. GUARDAR REGISTRO DE ENTRADA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include = "InscripcionId,EventoId")]
            RegistroEntrada registro)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _registroService.Registrar(
                        registro.InscripcionId);

                    var inscripcion =
                        _inscripcionService.ObtenerPorId(
                            registro.InscripcionId);

                    TempData["MensajeExito"] =
                        "Ingreso registrado correctamente.";

                    return RedirectToAction(
                        "Index",
                        new
                        {
                            eventoId = inscripcion != null
                                ? (int?)inscripcion.EventoId
                                : null
                        });
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (SQLiteException ex)
                {
                    Trace.TraceError(ex.ToString());

                    ModelState.AddModelError("",
                        "No se pudo registrar el ingreso.");
                }
            }

            try
            {
                CargarCombos(
                    registro.EventoId,
                    registro.InscripcionId);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudieron cargar los datos del formulario.";

                return RedirectToAction("Index");
            }

            return View(registro);
        }

        // 5. OBTENER INSCRIPCIONES DISPONIBLES POR EVENTO
        [HttpGet]
        public ActionResult InscripcionesDisponibles(int eventoId)
        {
            if (eventoId <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var evento = _eventoService.ObtenerPorId(eventoId);

                if (evento == null || evento.Estado != "Abierto")
                {
                    return Json(
                        new object[0],
                        JsonRequestBehavior.AllowGet);
                }

                var opciones = ObtenerOpcionesDisponibles(eventoId)
                    .Select(x => new
                    {
                        id = x.Value,
                        nombre = x.Text
                    })
                    .ToList();

                return Json(
                    opciones,
                    JsonRequestBehavior.AllowGet);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                return new HttpStatusCodeResult(
                    500, "Error al consultar las inscripciones.");
            }
        }

        // 6. CARGAR LISTAS DEL FORMULARIO
        private void CargarCombos(
            int? eventoId = null,
            int? inscripcionId = null)
        {
            var eventos = _eventoService
                .ObtenerTodos()
                .Where(e => e.Estado == "Abierto")
                .OrderBy(e => e.Nombre)
                .ToList();

            ViewBag.Eventos = new SelectList(
                eventos,
                "Id",
                "Nombre",
                eventoId);

            var opciones = new List<SelectListItem>();

            if (eventoId.GetValueOrDefault() > 0 &&
                eventos.Any(e => e.Id == eventoId.Value))
            {
                opciones = ObtenerOpcionesDisponibles(
                    eventoId.Value);
            }

            foreach (var opcion in opciones)
            {
                opcion.Selected =
                    opcion.Value == inscripcionId.ToString();
            }

            ViewBag.Inscripciones = opciones;
        }

        // 7. OBTENER INSCRIPCIONES SIN INGRESO
        private List<SelectListItem> ObtenerOpcionesDisponibles(
            int eventoId)
        {
            return _inscripcionService
                .ObtenerPorEvento(eventoId)
                .Where(i => i.Estado == "Activa")
                .Where(i => !_registroService.TieneEntrada(i.Id))
                .OrderBy(i => i.NombreParticipante)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = i.NombreParticipante +
                        " - " + i.NombreTipoEntrada
                })
                .ToList();
        }
    }
}
