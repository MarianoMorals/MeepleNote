using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MeepleNote.Services {

    /// <summary>
    /// Servicio para sincronizar datos entre SQLite (local) y Firebase (nube).
    /// Implementa estrategia de sincronización bidireccional.
    /// </summary>
    public class SincronizacionService {
        private readonly SQLiteService _sqlite;
        private readonly FirebaseDatabaseService _firebase;
        private readonly string _firebaseUsuarioId; // Almacena el ID de Firebase Auth

        /// <summary>
        /// Constructor principal que obtiene el ID de usuario desde Preferences.
        /// </summary>
        public SincronizacionService(SQLiteService sqliteService, FirebaseDatabaseService firebaseService) {
            _sqlite = sqliteService;
            _firebase = firebaseService;
            _firebaseUsuarioId = Preferences.Get("UsuarioId", null); // Recupera ID guardado
        }

        /// <summary>
        /// Constructor alternativo que permite inyectar el ID de Firebase directamente.
        /// Útil escenarios especiales.
        /// </summary>
        public SincronizacionService(SQLiteService sqliteService, FirebaseDatabaseService firebaseService, string fireBaseId) {
            _sqlite = sqliteService;
            _firebase = firebaseService;
            _firebaseUsuarioId = fireBaseId; // Usa ID proporcionado
        }

        /// <summary>
        /// Sincronización hacia Firebase (upload):
        /// 1. Recoge todos los datos locales
        /// 2. Sube a Firebase en una operación atómica
        /// 3. Actualiza fecha de sincronización
        /// </summary>
        public async Task SincronizarConFirebase() {
            if (string.IsNullOrEmpty(_firebaseUsuarioId))
                return;

            // Paso 1: Obtener datos locales
            var idUsuario = Preferences.Get("UsuarioId", "0");
            var usuario = await _sqlite.GetUsuarioByFirebaseIdAsync(_firebaseUsuarioId);
            var juegos = await _sqlite.GetJuegosAsync(idUsuario);
            var colecciones = await _sqlite.GetColeccionesAsync();
            var partidas = await _sqlite.GetPartidasAsync(idUsuario);
            var jugadores = await _sqlite.GetJugadoresPartidaAsync();
            var partidasPublicas = await _sqlite.GetPartidasPublicasAsync();

            var fechaSync = DateTime.UtcNow;

            // Paso 2: Subir a Firebase (excepto partidas públicas)
            await _firebase.SubirDatosUsuario(
                _firebaseUsuarioId,
                usuario,
                colecciones,
                juegos,
                partidas,
                jugadores,
                fechaSync
            );

            // Paso 3: Guardar fecha local
            _sqlite.GuardarFechaUltimaSync(fechaSync);
        }

        /// <summary>
        /// Sincronización desde Firebase (download):
        /// 1. Compara fechas de modificación (lógica actualmente deshabilitada)
        /// 2. Descarga datos completos
        /// 3. Reemplaza base de datos local
        /// </summary>
        public async Task SincronizarDesdeFirebaseSiNecesario() {
            if (string.IsNullOrEmpty(_firebaseUsuarioId))
                return;

            // Obtener fechas (comparación deshabilitada temporalmente)
            var fechaLocal = _sqlite.ObtenerFechaUltimaSync();
            var fechaFirebase = await _firebase.ObtenerFechaUltimaSync(_firebaseUsuarioId);

            // Descarga masiva desde Firebase
            var usuario = await _firebase.DescargarPerfil(_firebaseUsuarioId);
            var juegos = await _firebase.DescargarJuegos(_firebaseUsuarioId);
            var colecciones = await _firebase.DescargarColeccion(_firebaseUsuarioId);
            var partidas = await _firebase.DescargarPartidas(_firebaseUsuarioId);
            var jugadores = await _firebase.DescargarJugadoresPartida(_firebaseUsuarioId);

            // Reemplazo completo de datos locales
            await _sqlite.ReplaceJuegosAsync(juegos);
            await _sqlite.ReplaceColeccionesAsync(colecciones);
            await _sqlite.ReplacePartidasAsync(partidas);
            await _sqlite.ReplaceJugadoresPartidaAsync(jugadores);

            // Guardar perfil si existe
            if (usuario != null)
                await _sqlite.SaveUsuarioAsync(usuario);

            // Actualizar fecha de sincronización
            if (fechaFirebase.HasValue)
                _sqlite.GuardarFechaUltimaSync(fechaFirebase.Value);
        }
    }
}