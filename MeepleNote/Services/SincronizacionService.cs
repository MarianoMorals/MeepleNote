using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage; // Para Preferences

namespace MeepleNote.Services {
    public class SincronizacionService {
        private readonly SQLiteService _sqlite;
        private readonly FirebaseDatabaseService _firebase;
        private readonly string _firebaseUsuarioId; // Usaremos el Firebase User ID

        public SincronizacionService(SQLiteService sqliteService, FirebaseDatabaseService firebaseService) {
            _sqlite = sqliteService;
            _firebase = firebaseService;
            _firebaseUsuarioId = Preferences.Get("UsuarioId", null); // Obtener el Firebase User ID al instanciar
        }

        public async Task SincronizarConFirebase() {
            if (string.IsNullOrEmpty(_firebaseUsuarioId))
                return;

            // Obtener datos locales buscando el usuario por su Firebase User ID
            var usuario = await _sqlite.GetUsuarioByFirebaseIdAsync(_firebaseUsuarioId);
            var juegos = await _sqlite.GetJuegosAsync();
            var colecciones = await _sqlite.GetColeccionesAsync();
            var partidas = await _sqlite.GetPartidasAsync();
            var jugadores = await _sqlite.GetJugadoresPartidaAsync();

            var fechaSync = DateTime.UtcNow;

            // Subir datos a Firebase, utilizando el Firebase User ID como identificador del usuario
            await _firebase.SubirDatosUsuario(_firebaseUsuarioId, usuario, colecciones, juegos, partidas, jugadores, fechaSync);

            // Guardar la fecha de la última sincronización localmente
            _sqlite.GuardarFechaUltimaSync(fechaSync);
        }

        public async Task SincronizarDesdeFirebaseSiNecesario() {
            if (string.IsNullOrEmpty(_firebaseUsuarioId))
                return;

            var fechaLocal = _sqlite.ObtenerFechaUltimaSync();
            var fechaFirebase = await _firebase.ObtenerFechaUltimaSync(_firebaseUsuarioId);

            // Si hay una fecha en Firebase y es más reciente que la local, descargar los datos
            if (fechaFirebase.HasValue && (!fechaLocal.HasValue || fechaFirebase > fechaLocal)) {
                var usuario = await _firebase.DescargarPerfil(_firebaseUsuarioId);
                var juegos = await _firebase.DescargarJuegos(_firebaseUsuarioId);
                var colecciones = await _firebase.DescargarColeccion(_firebaseUsuarioId);
                var partidas = await _firebase.DescargarPartidas(_firebaseUsuarioId);
                var jugadores = await _firebase.DescargarJugadoresPartida(_firebaseUsuarioId);

                // Reemplazar los datos locales con los descargados de Firebase
                await _sqlite.ReplaceJuegosAsync(juegos);
                await _sqlite.ReplaceColeccionesAsync(colecciones);
                await _sqlite.ReplacePartidasAsync(partidas);
                await _sqlite.ReplaceJugadoresPartidaAsync(jugadores);

                // Guardar el perfil del usuario si se descargó
                if (usuario != null)
                    await _sqlite.SaveUsuarioAsync(usuario);

                // Guardar la fecha de la última sincronización desde Firebase
                _sqlite.GuardarFechaUltimaSync(fechaFirebase.Value);
            }
        }
    }
}