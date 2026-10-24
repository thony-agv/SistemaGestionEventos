using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using SistemaGestionEventos.Data;

namespace SistemaGestionEventos
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(
                GlobalFilters.Filters);

            RouteConfig.RegisterRoutes(
                RouteTable.Routes);

            BundleConfig.RegisterBundles(
                BundleTable.Bundles);

            // Crear la base de datos y sus tablas
            InicializadorBaseDatos.Inicializar();
        }
    }
}
