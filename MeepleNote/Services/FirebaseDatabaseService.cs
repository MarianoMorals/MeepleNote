using Firebase.Database;
using Firebase.Database.Query;
using MeepleNote.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Services
{
    public class FirebaseDatabaseService {
        private const string FirebaseUrl = "https://meeplenote-default-rtdb.europe-west1.firebasedatabase.app/";
        private readonly FirebaseClient _firebase;

        public FirebaseDatabaseService(string token) {
            _firebase = new FirebaseClient(FirebaseUrl,
                new FirebaseOptions {
                    AuthTokenAsyncFactory = () => Task.FromResult(token)
                });
        }

        // Subida completa con fecha de sincronización
        public async Task SubirDatosUsuario(
            string usuarioId,
            Usuario usuario,
            List<Coleccion> colecciones,
            List<Juego> juegos,
            List<Partida> partidas,
            List<JugadorPartida> jugadoresPartida,
            DateTime fechaSync) {
            await _firebase.Child("usuarios").Child(usuarioId).Child("perfil").PutAsync(usuario);
            await _firebase.Child("usuarios").Child(usuarioId).Child("coleccion").PutAsync(colecciones);
            await _firebase.Child("usuarios").Child(usuarioId).Child("juegos").PutAsync(juegos);
            await _firebase.Child("usuarios").Child(usuarioId).Child("partidas").PutAsync(partidas);
            await _firebase.Child("usuarios").Child(usuarioId).Child("jugadoresPartida").PutAsync(jugadoresPartida);

            string fechaIso = fechaSync.ToString("o"); // ISO 8601
            await _firebase.Child("usuarios").Child(usuarioId).Child("ultimaSync").PutAsync(fechaIso);
        }

        // Descarga de fecha de sincronización
        public async Task<DateTime?> ObtenerFechaUltimaSync(string usuarioId) {
            var fechaStr = await _firebase
                .Child("usuarios")
                .Child(usuarioId)
                .Child("ultimaSync")
                .OnceSingleAsync<string>();

            if (DateTime.TryParse(fechaStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var fecha))
                return fecha;

            return null;
        }

        // Métodos de descarga de datos
        public async Task<Usuario> DescargarPerfil(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("perfil").OnceSingleAsync<Usuario>();

        public async Task<List<Coleccion>> DescargarColeccion(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("coleccion").OnceSingleAsync<List<Coleccion>>() ?? new();

        public async Task<List<Juego>> DescargarJuegos(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("juegos").OnceSingleAsync<List<Juego>>() ?? new();

        public async Task<List<Partida>> DescargarPartidas(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("partidas").OnceSingleAsync<List<Partida>>() ?? new();

        public async Task<List<JugadorPartida>> DescargarJugadoresPartida(string usuarioId) =>
            await _firebase.Child("usuarios").Child(usuarioId).Child("jugadoresPartida").OnceSingleAsync<List<JugadorPartida>>() ?? new();
        public async Task<DatosUsuario?> DescargarTodo(string usuarioId) {
            try {
                // Descargar todos los datos relevantes del usuario
                var perfil = await DescargarPerfil(usuarioId);
                var coleccion = await DescargarColeccion(usuarioId);
                var juegos = await DescargarJuegos(usuarioId);
                var partidas = await DescargarPartidas(usuarioId);
                var jugadoresPartida = await DescargarJugadoresPartida(usuarioId);

                // Crear un objeto DatosUsuario y asignar los valores descargados
                var datosUsuario = new DatosUsuario {
                    Perfil = perfil,
                    Coleccion = coleccion,
                    Juegos = juegos,
                    Partidas = partidas,
                    JugadoresPartida = jugadoresPartida
                };

                return datosUsuario;
            }
            catch (Exception ex) {
                // Manejo de errores si algo sale mal
                Console.WriteLine($"Error al descargar los datos del usuario {usuarioId}: {ex.Message}");
                return null;
            }
        }


    }
}
