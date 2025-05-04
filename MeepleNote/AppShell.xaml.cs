using MeepleNote.Views;
using System;
namespace MeepleNote;

public partial class AppShell : Shell {
    public AppShell() {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes() {
        // Registrar rutas de autenticación
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));

        // Registrar rutas de las páginas principales
        Routing.RegisterRoute("PerfilPage", typeof(PerfilPage));
        Routing.RegisterRoute("ColeccionPage", typeof(ColeccionPage));
        Routing.RegisterRoute("PartidasPage", typeof(PartidasPage));
        Routing.RegisterRoute("ExplorarPage", typeof(ExplorarPage));
        //Routing.RegisterRoute("UtilidadesPage", typeof(UtilidadesPage));
    }
}
