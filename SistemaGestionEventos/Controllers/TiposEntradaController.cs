using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data.SQLite;
using System.Diagnostics;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;
using SistemaGestionEventos.Repositories;
using SistemaGestionEventos.Services;

namespace SistemaGestionEventos.Controllers
{
    public class TiposEntradaController : Controller
    {
        private readonly ITipoEntradaService _tipoService;
        private readonly IEventoService _eventoService;

        // CONSTRUCTOR PRINCIPAL
        public TiposEntradaController()
            : this(
                new TipoEntradaService(
                    new TipoEntradaRepository(),
                    new EventoRepository()),
                new EventoService(
                    new EventoRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFACES
        public TiposEntradaController(
            ITipoEntradaService tipoService,
            IEventoService eventoService)
        {
            _tipoService = tipoService ??
                throw new ArgumentNullException(
                    nameof(tipoService));

            _eventoService = eventoService ??
                throw new ArgumentNullException(
                    nameof(eventoService));
        }

        // 1. LISTADO DE TIPOS DE ENTRADA
        public ActionResult Index()
        {
            try
            {
                var tipos = _tipoService.ObtenerTodos();

                var nombresEventos = _eventoService
                    .ObtenerTodos()
                    .ToDictionary(e => e.Id, e => e.Nombre);

                ViewBag.NombresEventos = nombresEventos;

                return View(tipos);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                ViewBag.MensajeError =
                    "No se pudieron cargar los tipos de entrada.";

                ViewBag.NombresEventos =
                    new Dictionary<int, string>();

                return View(new List<TipoEntrada>());
            }
        }

        // 2. DETALLES DEL TIPO DE ENTRADA
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var tipo = _tipoService.ObtenerPorId(id.Value);

                if (tipo == null)
                    return HttpNotFound();

                CargarNombreEvento(tipo.EventoId);

                return View(tipo);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el tipo de entrada.";

                return RedirectToAction("Index");
            }
        }

        // 3. MOSTRAR FORMULARIO DE REGISTRO
        [HttpGet]
        public ActionResult Crear(int? eventoId)
        {
            CargarEventosAbiertos(eventoId);

            return View(new TipoEntrada
            {
                EventoId = eventoId ?? 0
            });
        }

        // 4. GUARDAR TIPO DE ENTRADA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include =
                "EventoId,Nombre,Descripcion")]
            TipoEntrada tipoEntrada)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _tipoService.Crear(tipoEntrada);

                    TempData["MensajeExito"] =
                        "Tipo de entrada registrado correctamente.";

                    return RedirectToAction("Index");
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
                        "No se pudo registrar el tipo de entrada.");
                }
            }

            CargarEventosAbiertos(tipoEntrada.EventoId);

            return View(tipoEntrada);
        }

        // 5. MOSTRAR FORMULARIO DE EDICION
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var tipo = _tipoService.ObtenerPorId(id.Value);

                if (tipo == null)
                    return HttpNotFound();

                var evento = _eventoService.ObtenerPorId(
                    tipo.EventoId);

                if (evento == null)
                    return HttpNotFound();

                if (evento.Estado == "Cerrado")
                {
                    TempData["MensajeError"] =
                        "No se pueden editar categorías " +
                        "de un evento cerrado.";

                    return RedirectToAction("Index");
                }

                ViewBag.NombreEvento = evento.Nombre;

                return View(tipo);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el tipo de entrada.";

                return RedirectToAction("Index");
            }
        }

        // 6. ACTUALIZAR TIPO DE ENTRADA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include =
                "Id,EventoId,Nombre,Descripcion")]
            TipoEntrada tipoEntrada)
        {
            if (tipoEntrada == null || tipoEntrada.Id <= 0)
                return new HttpStatusCodeResult(400);

            if (ModelState.IsValid)
            {
                try
                {
                    _tipoService.Actualizar(tipoEntrada);

                    TempData["MensajeExito"] =
                        "Tipo de entrada actualizado correctamente.";

                    return RedirectToAction("Index");
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
                        "No se pudo actualizar el tipo de entrada.");
                }
            }

            // Recuperar el nombre del evento para la vista
            try
            {
                CargarNombreEvento(tipoEntrada.EventoId);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                ModelState.AddModelError("",
                    "No se pudo consultar el evento relacionado.");

                ViewBag.NombreEvento = "No disponible";
            }

            return View(tipoEntrada);
        }

        // 7. MOSTRAR CONFIRMACION DE ELIMINACION
        [HttpGet]
        public ActionResult Eliminar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var tipo = _tipoService.ObtenerPorId(id.Value);

                if (tipo == null)
                    return HttpNotFound();

                CargarNombreEvento(tipo.EventoId);

                return View(tipo);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el tipo de entrada.";

                return RedirectToAction("Index");
            }
        }

        // 8. CONFIRMAR ELIMINACION
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminar(int id)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                _tipoService.Eliminar(id);

                TempData["MensajeExito"] =
                    "Tipo de entrada eliminado correctamente.";
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
                    "No se pudo eliminar el tipo de entrada.";
            }

            return RedirectToAction("Index");
        }

        // METODO AUXILIAR: CARGAR EVENTOS ABIERTOS
        private void CargarEventosAbiertos(int? seleccionado = null)
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
                seleccionado);
        }

        // METODO AUXILIAR: MOSTRAR NOMBRE DEL EVENTO
        private void CargarNombreEvento(int eventoId)
        {
            var evento = _eventoService.ObtenerPorId(eventoId);

            ViewBag.NombreEvento = evento != null
                ? evento.Nombre
                : "Evento no encontrado";
        }
    }
}
