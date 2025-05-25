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
            _database.CreateTableAsync<PartidaPublica>();

        }

        // === USUARIO ===
        public Task<int> SaveUsuarioAsync(Usuario usuario) => _database.InsertOrReplaceAsync(usuario);
        public Task<List<Usuario>> GetUsuariosAsync() => _database.Table<Usuario>().ToListAsync();

        public async Task<string?> GetEmailUsuarioAsync(string idUsuario) {
            try {
                var usuario = await _database.Table<Usuario>()
                                           .Where(u => u.FirebaseUserId == idUsuario)
                                           .FirstOrDefaultAsync();

                return usuario?.Email ?? string.Empty;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al obtener email: {ex.Message}");
                return string.Empty;
            }
        }
        // Obtener usuario por su ID local (clave primaria autoincremental)
        public Task<Usuario?> GetUsuarioByIdAsync(string id) =>
            _database.Table<Usuario>().Where(u => u.FirebaseUserId == id).FirstOrDefaultAsync();

        // Nuevo método para obtener usuario por su Firebase User ID
        public Task<Usuario?> GetUsuarioByFirebaseIdAsync(string firebaseId) =>
            _database.Table<Usuario>().Where(u => u.FirebaseUserId == firebaseId).FirstOrDefaultAsync();

        //Obtener usuario por FirebaseId
        public async Task<int?> ObtenerIdUsuarioPorFirebaseIdAsync(string firebaseUserId) {
            var usuario = await _database.Table<Usuario>()
                                         .Where(u => u.FirebaseUserId == firebaseUserId)
                                         .FirstOrDefaultAsync();
            return usuario?.IdUsuario;
        }

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

        public async Task AnnadirJuegoExistenteAColeccion(int idJuego, string firebaseId) {
            var juego = await _database.Table<Juego>()
                                       .Where(j => j.IdJuego == idJuego && j.IdUsuario == firebaseId) 
                                       .FirstOrDefaultAsync();
            if (juego != null) {
                juego.EnColeccion = true;
                await _database.UpdateAsync(juego);
            }
        }

        public async Task QuitarJuegoExistenteDeColeccion(int idJuego) {
            var juego = await _database.Table<Juego>().Where(j => j.IdJuego == idJuego).FirstOrDefaultAsync();
            if (juego != null) {
                juego.EnColeccion = false;
                await _database.UpdateAsync(juego);
            }
        }

        public async Task<List<Juego>> GetJuegosAsync(string idUsuario) =>
            await _database.Table<Juego>().Where(j => j.IdUsuario == idUsuario).ToListAsync();
        public async Task<List<Juego>> GetJuegosAsyncEnColeccion(string idUsuario) =>
            await _database.Table<Juego>().Where(j => j.EnColeccion && j.IdUsuario == idUsuario).ToListAsync();
        public Task<int> DeleteJuegoAsync(Juego juego) => _database.DeleteAsync(juego);
        public async Task<bool> JuegoExisteAsync(int idJuego, string idUsuario) =>
            await _database.Table<Juego>()
                   .Where(j => j.IdJuego == idJuego && j.IdUsuario == idUsuario)
                   .FirstOrDefaultAsync() != null;

        public async Task<bool> JuegoEnColeccionAsync(int idJuego, string idUsuario) =>
            await _database.Table<Juego>()
                           .Where(j => j.IdJuego == idJuego && j.IdUsuario == idUsuario && j.EnColeccion)
                           .FirstOrDefaultAsync() != null;
        public Task<Juego> GetJuegoByIdAsync(int idJuego) =>
            _database.Table<Juego>().FirstOrDefaultAsync(j => j.IdJuego == idJuego);
        public async Task MarcarTodosLosJuegosEnColeccionAsync() {
            await _database.ExecuteAsync("UPDATE Juego SET EnColeccion = 1");
        }

        // === PARTIDA ===
        public async Task ReplacePartidasAsync(List<Partida> partidas) {
            await _database.DeleteAllAsync<Partida>();
            await _database.InsertAllAsync(partidas);
        }

        public async Task<List<Partida>> GetPartidasAsync(string idUsuario) => 
            await _database.Table<Partida>().Where(p => p.IdUsuario == idUsuario).ToListAsync();

        public async Task<int> SavePartidaAsync(Partida partida) {
            if (partida.IdPartida != 0)
                return await _database.UpdateAsync(partida);
            else {
                await _database.InsertAsync(partida);

                return partida.IdPartida;
            }
        }

        public Task<List<Partida>> GetPartidasByJuegoAsync(int idJuego) =>
            _database.Table<Partida>().Where(p => p.IdJuego == idJuego).ToListAsync();

        public Task<Partida> GetPartidaByIdAsync(int idPartida) =>
            _database.Table<Partida>().FirstOrDefaultAsync(p => p.IdPartida == idPartida);

        public async Task EliminarPartidaAsync(int idPartida) {
            var partida = await _database.Table<Partida>().Where(p => p.IdPartida == idPartida).FirstOrDefaultAsync();
            if (partida != null) {
                await _database.DeleteAsync(partida);

                // Elimina también los jugadores relacionados si aplica
                var jugadores = await GetJugadoresByPartidaAsync(idPartida);
                foreach (var jugador in jugadores) {
                    await _database.DeleteAsync(jugador);
                }
            }
        }

        public async Task EliminarTodasPartidas() {
            await _database.DeleteAllAsync<JugadorPartida>(); // o como se llame tu clase de relación
            await _database.DeleteAllAsync<Partida>();
        }

        public async Task<bool> JuegoExisteEnColeccionAsync(int idJuego) {
            return await _database.Table<Coleccion>()
                                 .Where(c => c.IdJuego == idJuego)
                                 .CountAsync() > 0;
        }

        // === JUGADOR PARTIDA ===
        public async Task ReplaceJugadoresPartidaAsync(List<JugadorPartida> jugadores) {
            await _database.DeleteAllAsync<JugadorPartida>();
            await _database.InsertAllAsync(jugadores);
        }
        public async Task<int> SaveJugadorPartidaAsync(JugadorPartida jugador) {
            if (jugador.Id == 0)
                return await _database.InsertAsync(jugador);
            else
                return await _database.UpdateAsync(jugador);
        }

        public Task<List<JugadorPartida>> GetJugadoresByPartidaAsync(int idPartida) =>
            _database.Table<JugadorPartida>().Where(j => j.IdPartida == idPartida).ToListAsync();

        public Task<List<JugadorPartida>> GetJugadoresPartidaAsync() => _database.Table<JugadorPartida>().ToListAsync();

        // === PARTIDA PUBLICA ===
        public async Task<List<PartidaPublica>> GetPartidasPublicasAsync(bool soloFuturas = true) {
            var query = _database.Table<PartidaPublica>();

            if (soloFuturas) {
                query = query.Where(p => p.Fecha >= DateTime.Now && !p.Completada);
            }

            return await query.ToListAsync();
        }

        public async Task<int> SavePartidaPublicaAsync(PartidaPublica partida) {
            if (partida.Id == 0) {
                return await _database.InsertAsync(partida);
            }
            else {
                return await _database.UpdateAsync(partida);
            }
        }

        public async Task<int> MarcarPartidaPublicaCompletadaAsync(int id) {
            return await _database.ExecuteAsync(
                "UPDATE PartidaPublica SET Completada = 1 WHERE Id = ?", id);
        }

        // === FECHA DE SINCRONIZACIÓN ===
        public void GuardarFechaUltimaSync(DateTime fecha) =>
            Preferences.Set("UltimaSync", fecha.ToString("O")); // ISO 8601

        public DateTime? ObtenerFechaUltimaSync() {
            var str = Preferences.Get("UltimaSync", null);
            return str != null ? DateTime.Parse(str) : null;
        }

        // === SINCRONIZACIÓN COMPLETA ===
        public async Task<DatosUsuario> ObtenerTodo() {

            var idUsuario = Preferences.Get("UsuarioId", "0");


            var usuario = await GetUsuarioByIdAsync(idUsuario);
            var juegos = await GetJuegosAsync(idUsuario);
            var coleccion = await GetColeccionesAsync();
            var partidas = await GetPartidasAsync(idUsuario);
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

        /*public async Task LimpiarDatosUsuario() {
            try {
                var idUsuario = Preferences.Get("UsuarioId", 0);

                // Eliminar solo los datos del usuario actual
                await _database.ExecuteAsync("DELETE FROM Partida WHERE IdUsuario = ?", idUsuario);
                await _database.ExecuteAsync("DELETE FROM JugadorPartida WHERE IdPartida IN " +
                                           "(SELECT IdPartida FROM Partida WHERE IdUsuario = ?)", idUsuario);
                await _database.ExecuteAsync("DELETE FROM Coleccion WHERE IdUsuario = ?", idUsuario);
                await _database.ExecuteAsync("DELETE FROM Juego WHERE IdUsuario = ?", idUsuario);
                await _database.ExecuteAsync("DELETE FROM PartidaPublica WHERE IdUsuarioOrganizador = ?", idUsuario);

                Console.WriteLine("Datos del usuario limpiados correctamente");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al limpiar datos de usuario: {ex.Message}");
                throw;
            }
        }*/

        public async Task ResetearBaseDatosCompleta() {
            try {
                // Eliminar todas las tablas en orden adecuado (primero las que tienen FK)
                await _database.DeleteAllAsync<JugadorPartida>();
                await _database.DeleteAllAsync<Partida>();
                await _database.DeleteAllAsync<Coleccion>();
                await _database.DeleteAllAsync<Juego>();
                await _database.DeleteAllAsync<Usuario>();
                await _database.DeleteAllAsync<PartidaPublica>(); // Nueva tabla

                // Resetear las preferencias relacionadas
                Preferences.Clear();

                // Recrear las tablas
                await _database.CreateTableAsync<Usuario>();
                await _database.CreateTableAsync<Juego>();
                await _database.CreateTableAsync<Coleccion>();
                await _database.CreateTableAsync<Partida>();
                await _database.CreateTableAsync<JugadorPartida>();
                await _database.CreateTableAsync<PartidaPublica>();

                Console.WriteLine("Base de datos reseteada completamente");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al resetear BD: {ex.Message}");
                throw; // Puedes manejar esto diferente si prefieres
            }
        }
    }
}