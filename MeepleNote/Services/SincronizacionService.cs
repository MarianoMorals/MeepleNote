using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Services
{
    public class SincronizacionService
    {
        private readonly SQLiteService _sqlite;
        private readonly FirebaseDatabaseService _firebase;
        private readonly string _usuarioId;

        public SincronizacionService(SQLiteService sqliteService, FirebaseDatabaseService firebaseService) {
            _sqlite = sqliteService;
            _firebase = firebaseService;
            _usuarioId = Preferences.Get("UsuarioId", null);
        }

        public async Task SincronizarConFirebase() {
            if (string.IsNullOrEmpty(_usuarioId))
                return;

            // Obtener datos locales
            var usuario = await _sqlite.GetUsuarioByIdAsync(int.Parse(_usuarioId));
            var juegos = await _sqlite.GetJuegosAsync();
            var colecciones = await _sqlite.GetColeccionesAsync();
            var partidas = await _sqlite.GetPartidasAsync();
            var jugadores = await _sqlite.GetJugadoresPartidaAsync();

            var fechaSync = DateTime.UtcNow;

            // Subir datos a Firebase
            await _firebase.SubirDatosUsuario(_usuarioId, usuario, colecciones, juegos, partidas, jugadores, fechaSync);

            // Guardar fecha localmente
            _sqlite.GuardarFechaUltimaSync(fechaSync);
        }
        public async Task SincronizarDesdeFirebaseSiNecesario() {
            if (string.IsNullOrEmpty(_usuarioId))
                return;

            var fechaLocal = _sqlite.ObtenerFechaUltimaSync();
            var fechaFirebase = await _firebase.ObtenerFechaUltimaSync(_usuarioId);

            if (fechaFirebase.HasValue && (!fechaLocal.HasValue || fechaFirebase > fechaLocal)) {
                var usuario = await _firebase.DescargarPerfil(_usuarioId);
                var juegos = await _firebase.DescargarJuegos(_usuarioId);
                var colecciones = await _firebase.DescargarColeccion(_usuarioId);
                var partidas = await _firebase.DescargarPartidas(_usuarioId);
                var jugadores = await _firebase.DescargarJugadoresPartida(_usuarioId);

                await _sqlite.ReplaceJuegosAsync(juegos);
                await _sqlite.ReplaceColeccionesAsync(colecciones);
                await _sqlite.ReplacePartidasAsync(partidas);
                await _sqlite.ReplaceJugadoresPartidaAsync(jugadores);

                if (usuario != null)
                    await _sqlite.SaveUsuarioAsync(usuario);

                _sqlite.GuardarFechaUltimaSync(fechaFirebase.Value);
            }
        }

    }
}
