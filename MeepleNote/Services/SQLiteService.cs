using MeepleNote.Models;
using SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage; // Para Preferences

namespace MeepleNote.Services {
    public class SQLiteService {
        private SQLiteAsyncConnection _database;

        public SQLiteService() {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "meeplenote.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Crear las tablas si no existen
            _database.CreateTableAsync<Usuario>();
            _database.CreateTableAsync<Juego>();
            _database.CreateTableAsync<Coleccion>();
            _database.CreateTableAsync<Partida>();
            _database.CreateTableAsync<JugadorPartida>();
        }

        // === USUARIO ===
        public Task<int> SaveUsuarioAsync(Usuario usuario) => _database.InsertOrReplaceAsync(usuario);
        public Task<List<Usuario>> GetUsuariosAsync() => _database.Table<Usuario>().ToListAsync();

        // Obtener usuario por su ID local (clave primaria autoincremental)
        public Task<Usuario?> GetUsuarioByIdAsync(int id) =>
            _database.Table<Usuario>().Where(u => u.IdUsuario == id).FirstOrDefaultAsync();

        // Nuevo método para obtener usuario por su Firebase User ID
        public Task<Usuario?> GetUsuarioByFirebaseIdAsync(string firebaseId) =>
            _database.Table<Usuario>().Where(u => u.FirebaseUserId == firebaseId).FirstOrDefaultAsync();

        // === COLECCION ===
        public async Task ReplaceColeccionesAsync(List<Coleccion> colecciones) {
            await _database.DeleteAllAsync<Coleccion>();
            await _database.InsertAllAsync(colecciones);
        }

        public Task<List<Coleccion>> GetColeccionesAsync() => _database.Table<Coleccion>().ToListAsync();

        // === JUEGO ===
        public async Task ReplaceJuegosAsync(List<Juego> juegos) {
            await _database.DeleteAllAsync<Juego>();
            await _database.InsertAllAsync(juegos);
        }

        public Task<int> SaveJuegoAsync(Juego juego) => _database.InsertAsync(juego);
        public Task<List<Juego>> GetJuegosAsync() => _database.Table<Juego>().ToListAsync();
        public Task<int> DeleteJuegoAsync(Juego juego) => _database.DeleteAsync(juego);
        public async Task<bool> JuegoExisteAsync(int idJuego) =>
            await _database.Table<Juego>().Where(j => j.IdJuego == idJuego).FirstOrDefaultAsync() != null;

        // === PARTIDA ===
        public async Task ReplacePartidasAsync(List<Partida> partidas) {
            await _database.DeleteAllAsync<Partida>();
            await _database.InsertAllAsync(partidas);
        }

        public Task<List<Partida>> GetPartidasAsync() => _database.Table<Partida>().ToListAsync();

        // === JUGADOR PARTIDA ===
        public async Task ReplaceJugadoresPartidaAsync(List<JugadorPartida> jugadores) {
            await _database.DeleteAllAsync<JugadorPartida>();
            await _database.InsertAllAsync(jugadores);
        }

        public Task<List<JugadorPartida>> GetJugadoresPartidaAsync() => _database.Table<JugadorPartida>().ToListAsync();

        // === FECHA DE SINCRONIZACIÓN ===
        public void GuardarFechaUltimaSync(DateTime fecha) =>
            Preferences.Set("UltimaSync", fecha.ToString("O")); // ISO 8601

        public DateTime? ObtenerFechaUltimaSync() {
            var str = Preferences.Get("UltimaSync", null);
            return str != null ? DateTime.Parse(str) : null;
        }

        // === SINCRONIZACIÓN COMPLETA ===
        public async Task<DatosUsuario> ObtenerTodo() {
            // Asumimos que el perfil del usuario logueado tiene el ID local 1 (esto puede necesitar ajuste)
            var usuario = await GetUsuarioByIdAsync(1);
            var juegos = await GetJuegosAsync();
            var coleccion = await GetColeccionesAsync();
            var partidas = await GetPartidasAsync();
            var jugadoresPartida = await GetJugadoresPartidaAsync();

            return new DatosUsuario {
                Perfil = usuario,
                Juegos = juegos,
                Coleccion = coleccion,
                Partidas = partidas,
                JugadoresPartida = jugadoresPartida
            };
        }

        public async Task GuardarTodoDesdeFirebase(DatosUsuario datos) {
            if (datos.Perfil != null)
                await SaveUsuarioAsync(datos.Perfil);

            await ReplaceJuegosAsync(datos.Juegos);
            await ReplaceColeccionesAsync(datos.Coleccion);
            await ReplacePartidasAsync(datos.Partidas);
            await ReplaceJugadoresPartidaAsync(datos.JugadoresPartida);
        }
    }
}