using Firebase.Database;
using Firebase.Database.Query;
using MeepleNote.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Services {
    /// <summary>
    /// Servicio para interactuar con Firebase Realtime Database.
    /// Maneja sincronización de datos de usuario, partidas públicas y colecciones.
    /// </summary>
    public class FirebaseDatabaseService {
        // URL de la base de datos Firebase (Realtime Database)
        private const string FirebaseUrl = "https://meeplenote-default-rtdb.europe-west1.firebasedatabase.app/";

        // Cliente Firebase configurado
        private readonly FirebaseClient _firebase;

        /// <summary>
        /// Constructor: inicializa el cliente Firebase con el token de autenticación.
        /// </summary>
        /// <param name="token">Token obtenido de Firebase Authentication</param>
        public FirebaseDatabaseService(string token) {
            _firebase = new FirebaseClient(FirebaseUrl,
                new FirebaseOptions {
                    AuthTokenAsyncFactory = () => Task.FromResult(token) // Inyección del token
                });
        }

        // ================== MÉTODOS DE SINCRONIZACIÓN ==================

        /// <summary>
        /// Sube todos los datos del usuario a Firebase en una operación atómica.
        /// </summary>
        public async Task SubirDatosUsuario(
            string usuarioId,
            Usuario usuario,
            List<Coleccion> colecciones,
            List<Juego> juegos,
            List<Partida> partidas,
            List<JugadorPartida> jugadoresPartida,
            DateTime fechaSync) {
            // Asegura que el FirebaseUserId coincida
            usuario.FirebaseUserId = usuarioId;

            // Subida paralela secuencial
            await _firebase.Child("usuarios").Child(usuarioId).Child("perfil").PutAsync(usuario);
            await _firebase.Child("usuarios").Child(usuarioId).Child("coleccion").PutAsync(colecciones);
            await _firebase.Child("usuarios").Child(usuarioId).Child("juegos").PutAsync(juegos);
            await _firebase.Child("usuarios").Child(usuarioId).Child("partidas").PutAsync(partidas);
            await _firebase.Child("usuarios").Child(usuarioId).Child("jugadoresPartida").PutAsync(jugadoresPartida);

            // Registra timestamp de sincronización
            string fechaIso = fechaSync.ToString("o");
            await _firebase.Child("usuarios").Child(usuarioId).Child("ultimaSync").PutAsync(fechaIso);
        }

        /// <summary>
        /// Obtiene la última fecha de sincronización del usuario desde Firebase.
        /// </summary>
        public async Task<DateTime?> ObtenerFechaUltimaSync(string usuarioId) {
            var fechaStr = await _firebase
                .Child("usuarios")
                .Child(usuarioId)
                .Child("ultimaSync")
                .OnceSingleAsync<string>();

            // Parseo con formato Roundtrip 
            if (DateTime.TryParse(fechaStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var fecha))
                return fecha;

            return null;
        }

        // ================== MÉTODOS DE DESCARGA ==================

        /// <summary>
        /// Descarga el perfil básico del usuario.
        /// </summary>
        public async Task<Usuario> DescargarPerfil(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("perfil").OnceSingleAsync<Usuario>();

        /// <summary>
        /// OBSOLETO, se eliminara en futuras actualizaciones.
        /// Descarga la colección personal del usuario.
        /// </summary>
        public async Task<List<Coleccion>> DescargarColeccion(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("coleccion").OnceSingleAsync<List<Coleccion>>() ?? new();

        /// <summary>
        /// Descarga los juegos del usuario.
        /// </summary>
        public async Task<List<Juego>> DescargarJuegos(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("juegos").OnceSingleAsync<List<Juego>>() ?? new();

        /// <summary>
        /// Descarga el historial de partidas del usuario.
        /// </summary>
        public async Task<List<Partida>> DescargarPartidas(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("partidas").OnceSingleAsync<List<Partida>>() ?? new();

        /// <summary>
        /// Descarga los jugadores asociados a partidas.
        /// </summary>
        public async Task<List<JugadorPartida>> DescargarJugadoresPartida(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("jugadoresPartida").OnceSingleAsync<List<JugadorPartida>>() ?? new();

        // ================== PARTIDAS PÚBLICAS ==================

        /// <summary>
        /// Sube una lista de partidas públicas al nodo global (no asociado a un usuario).
        /// </summary>
        public async Task SubirPartidasPublicas(List<PartidaPublica> partidas) {
            await _firebase.Child("partidasPublicasGlobales").PutAsync(partidas);
        }

        /// <summary>
        /// Descarga todas las partidas públicas disponibles.
        /// Asigna automáticamente el IdFirebase (clave generada por Firebase).
        /// </summary>
        public async Task<List<PartidaPublica>> DescargarPartidasPublicas() {
            try {
                var firebaseObjects = await _firebase
                    .Child("partidasPublicasGlobales")
                    .OnceAsync<PartidaPublica>();

                return firebaseObjects?
                    .Select(item => {
                        item.Object.IdFirebase = item.Key; // Asigna el ID generado por Firebase
                        return item.Object;
                    })
                    .ToList() ?? new List<PartidaPublica>();
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al descargar partidas: {ex.Message}");
                return new List<PartidaPublica>();
            }
        }

        // ================== OPERACIONES COMPUESTAS ==================

        /// <summary>
        /// Descarga todos los datos del usuario en un objeto unificado DatosUsuario.
        /// </summary>
        public async Task<DatosUsuario?> DescargarTodo(string usuarioId) {
            try {
                return new DatosUsuario {
                    Perfil = await DescargarPerfil(usuarioId),
                    Coleccion = await DescargarColeccion(usuarioId),
                    Juegos = await DescargarJuegos(usuarioId),
                    Partidas = await DescargarPartidas(usuarioId),
                    JugadoresPartida = await DescargarJugadoresPartida(usuarioId)
                };
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al descargar los datos del usuario {usuarioId}: {ex.Message}");
                return null;
            }
        }

        // ================== OPERACIONES CRUD PARA PARTIDAS PÚBLICAS ==================

        /// <summary>
        /// Crea una nueva partida pública en Firebase y devuelve su ID generado.
        /// </summary>
        public async Task<string> CrearPartidaPublicaEnFirebase(PartidaPublica partida) {
            try {
                partida.IdFirebase = null; // Asegura que sea Firebase el que genere un nuevo ID

                var nuevaPartidaRef = await _firebase
                    .Child("partidasPublicasGlobales")
                    .PostAsync(partida);

                return nuevaPartidaRef.Key; // Devuelve el ID único generado
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error al crear partida pública: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una partida pública existente en Firebase usando su ID.
        /// </summary>
        public async Task ActualizarPartidaPublicaEnFirebase(PartidaPublica partida) {
            try {
                await _firebase
                    .Child("partidasPublicasGlobales")
                    .Child(partida.IdFirebase.ToString())
                    .PutAsync(partida);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al actualizar partida: {ex.Message}");
                throw;
            }
        }
    }
}