using MeepleNote.Models;
using SQLite;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MeepleNote.Services {

    /// <summary>
    /// Servicio para manejar todas las operaciones de base de datos local SQLite.
    /// Gestiona: Usuarios, Juegos, Colecciones, Partidas y relaciones.
    /// </summary>
    public class SQLiteService {


        private SQLiteAsyncConnection _database;

        /// <summary>
        /// Constructor: Inicializa la conexión a la base de datos y crea las tablas si no existen.
        /// </summary>
        public SQLiteService() {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "meeplenote.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // Creación de tablas (si no existen)
            _database.CreateTableAsync<Usuario>();
            _database.CreateTableAsync<Juego>();
            _database.CreateTableAsync<Coleccion>();
            _database.CreateTableAsync<Partida>();
            _database.CreateTableAsync<JugadorPartida>();
            _database.CreateTableAsync<PartidaPublica>();

        }

        // ================== OPERACIONES DE USUARIO ==================

        /// <summary>
        /// Guarda o actualiza un usuario (upsert).
        /// Usa FirebaseUserId como identificador principal.
        /// </summary>
        public Task<int> SaveUsuarioAsync(Usuario usuario) => _database.InsertOrReplaceAsync(usuario);

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        public Task<List<Usuario>> GetUsuariosAsync() => _database.Table<Usuario>().ToListAsync();

        /// <summary>
        /// Obtiene el email de un usuario por su FirebaseUserId.
        /// </summary>
        public async Task<string?> GetEmailUsuarioAsync(string idUsuario) {
            try {
                var usuario = await _database.Table<Usuario>()
                                           .Where(u => u.FirebaseUserId == idUsuario)
                                           .FirstOrDefaultAsync();

                return usuario.Email;
            }
            catch (Exception ex) {
                Console.WriteLine($"Error al obtener email: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Obtiene un usuario por su FirebaseUserId.
        /// </summary>
        public Task<Usuario?> GetUsuarioByFirebaseIdAsync(string firebaseId) =>
            _database.Table<Usuario>().Where(u => u.FirebaseUserId == firebaseId).FirstOrDefaultAsync();

        /// <summary>
        /// Obtiene el id (local) de un usuario por su FirebaseUserId.
        /// </summary>
        public async Task<int?> ObtenerIdUsuarioPorFirebaseIdAsync(string firebaseUserId) {
            var usuario = await _database.Table<Usuario>()
                                         .Where(u => u.FirebaseUserId == firebaseUserId)
                                         .FirstOrDefaultAsync();
            return usuario?.IdUsuario;
        }

        // ================== OPERACIONES DE COLECCIONES ==================

        /// <summary>
        /// Reemplaza todos los registros de la tabla Coleccion
        /// </summary>
        public async Task ReplaceColeccionesAsync(List<Coleccion> colecciones) {
            await _database.DeleteAllAsync<Coleccion>();
            await _database.InsertAllAsync(colecciones);
        }

        /// <summary>
        /// Obtiene todas las colecciones
        /// </summary>
        public Task<List<Coleccion>> GetColeccionesAsync() => _database.Table<Coleccion>().ToListAsync();

        // ================== OPERACIONES DE JUEGOS ==================

        /// <summary>
        /// Reemplaza completamente la lista de juegos local.
        /// </summary>
        public async Task ReplaceJuegosAsync(List<Juego> juegos) {
            await _database.DeleteAllAsync<Juego>();
            await _database.InsertAllAsync(juegos);
        }

        /// <summary>
        /// Guarda un juego con lógica especial:
        /// - Si existe: Actualiza todos los campos excepto EnColeccion
        /// - Si no existe: Inserta nuevo registro
        /// </summary>
        public async Task<int> SaveJuegoAsync(Juego juego) {
            // Verificar si el juego ya existe en la base de datos
            var juegoExistente = await _database.Table<Juego>()
                                               .Where(j => j.IdJuego == juego.IdJuego && j.IdUsuario == juego.IdUsuario)
                                               .FirstOrDefaultAsync();

            if (juegoExistente != null) {
                // Actualizar los campos necesarios
                //juegoExistente.Id = juego.Id;
                juegoExistente.PuntuacionPersonal = juego.PuntuacionPersonal;
                juegoExistente.Titulo = juego.Titulo;
                juegoExistente.FotoPortada = juego.FotoPortada;
                juegoExistente.Puntuacion = juego.Puntuacion;
                juegoExistente.Descripcion = juego.Descripcion;
                juegoExistente.MinJugadores = juego.MinJugadores;
                juegoExistente.MaxJugadores = juego.MaxJugadores;
                juegoExistente.DuracionEstimada = juego.DuracionEstimada;
                juegoExistente.Edad = juego.Edad;
                juegoExistente.Autor = juego.Autor;
                juegoExistente.Artista = juego.Artista;

                // Mantener el estado de EnColeccion como estaba
                juegoExistente.EnColeccion = juego.EnColeccion;

                return await _database.UpdateAsync(juegoExistente);
            }
            else {
                // Si no existe, insertarlo con el valor de EnColeccion que traiga
                return await _database.InsertAsync(juego);
            }
        }

        /// <summary>
        /// Añade a la coleccion de un usuario un juego que ya existe en la base de datos.
        /// </summary>
        public async Task AnnadirJuegoExistenteAColeccion(int idJuego, string firebaseId) {
            var juego = await _database.Table<Juego>()
                                       .Where(j => j.IdJuego == idJuego && j.IdUsuario == firebaseId) 
                                       .FirstOrDefaultAsync();
            if (juego != null) {
                juego.EnColeccion = true;
                await _database.UpdateAsync(juego);
            }
        }

        /// <summary>
        /// Quita de la coleccion de un usuario un juego sin eliminarlo de la base de datos.
        /// </summary>
        public async Task QuitarJuegoExistenteDeColeccion(int idJuego, string firebaseId) {
            var juego = await _database.Table<Juego>().Where(j => j.IdJuego == idJuego && j.IdUsuario == firebaseId).FirstOrDefaultAsync();
            if (juego != null) {
                juego.EnColeccion = false;
                await _database.UpdateAsync(juego);
            }
        }

        /// <summary>
        /// Obtiene todos los juegos del usuario
        /// </summary>
        public async Task<List<Juego>> GetJuegosAsync(string idUsuario) =>
            await _database.Table<Juego>().Where(j => j.IdUsuario == idUsuario).ToListAsync();

        /// <summary>
        /// Obtiene todos los juegos de la coleccion del usuario
        /// </summary>
        public async Task<List<Juego>> GetJuegosAsyncEnColeccion(string idUsuario) =>
            await _database.Table<Juego>().Where(j => j.EnColeccion && j.IdUsuario == idUsuario).ToListAsync();

        /// <summary>
        /// Elimina un juego del usuario de la base de datos
        /// </summary>
        public Task<int> DeleteJuegoAsync(Juego juego) => _database.DeleteAsync(juego);

        /// <summary>
        /// Comprueba si un juego existe en la base de datos del usuario
        /// </summary>
        public async Task<bool> JuegoExisteAsync(int idJuego, string idUsuario) =>
            await _database.Table<Juego>()
                   .Where(j => j.IdJuego == idJuego && j.IdUsuario == idUsuario)
                   .FirstOrDefaultAsync() != null;

        /// <summary>
        /// Comprueba si un juego existe en la coleccion del usuario
        /// </summary>
        public async Task<bool> JuegoEnColeccionAsync(int idJuego, string idUsuario) =>
            await _database.Table<Juego>()
                           .Where(j => j.IdJuego == idJuego && j.IdUsuario == idUsuario && j.EnColeccion)
                           .FirstOrDefaultAsync() != null;

        /// <summary>
        /// Obtiene un juego por su id (API BGG)
        /// </summary>
        public Task<Juego> GetJuegoByIdAsync(int idJuego) =>
            _database.Table<Juego>().FirstOrDefaultAsync(j => j.IdJuego == idJuego);

        /// <summary>
        /// Pone todos los juegos en la base de datos en la coleccion
        /// </summary>
        public async Task MarcarTodosLosJuegosEnColeccionAsync() {
            await _database.ExecuteAsync("UPDATE Juego SET EnColeccion = 1");
        }

        /// <summary>
        /// Actualiza en un juego determinado la puntuacion personal de un usuario determinado.
        /// </summary>
        public async Task<int> ActualizarPuntuacionJuegoAsync(int idJuego, string idUsuario, int nuevaPuntuacion) {
            var juegoExistente = await _database.Table<Juego>()
                .Where(j => j.IdJuego == idJuego && j.IdUsuario == idUsuario)
                .FirstOrDefaultAsync();

            if (juegoExistente != null) {
                juegoExistente.PuntuacionPersonal = nuevaPuntuacion;
                return await _database.UpdateAsync(juegoExistente);
            }
            return 0;
        }

        /// <summary>
        /// Comprueba si un juego de la base de datos esta en la coleccion.
        /// </summary>
        public async Task<bool> JuegoExisteEnColeccionAsync(int idJuego) {
            return await _database.Table<Juego>()
                                 .Where(c => c.Id == idJuego && c.EnColeccion)
                                 .CountAsync() > 0;
        }

        // ================== OPERACIONES DE PARTIDAS ==================

        /// <summary>
        /// Reemplaza todas las partidas de la base de datos.
        /// </summary>
        public async Task ReplacePartidasAsync(List<Partida> partidas) {
            await _database.DeleteAllAsync<Partida>();
            await _database.InsertAllAsync(partidas);
        }

        /// <summary>
        /// Obtiene todas las partidas de un usuario determinado.
        /// </summary>
        public async Task<List<Partida>> GetPartidasAsync(string idUsuario) {
            return await _database.Table<Partida>()
                                 .Where(p => p.IdUsuario == idUsuario)
                                 .ToListAsync();
        }

        /// <summary>
        /// Guarda una partida generando automáticamente un ID si es nueva.
        /// </summary>
        public async Task<int> SavePartidaAsync(Partida partida) {
            if (partida.IdPartida == 0)  // Si es nuevo (ID no asignado)
            {
                partida.IdPartida = await GetNuevoIdPartidaAsync();
            }

            await _database.InsertOrReplaceAsync(partida);
            return partida.IdPartida;
        }

        /// <summary>
        /// Obtiene todas las partidas de un juego determinado.
        /// </summary>
        public Task<List<Partida>> GetPartidasByJuegoAsync(int idJuego) =>
            _database.Table<Partida>().Where(p => p.IdJuego == idJuego).ToListAsync();

        /// <summary>
        /// Obtiene una partida determinada por su Id.
        /// </summary>
        public Task<Partida> GetPartidaByIdAsync(int idPartida) =>
            _database.Table<Partida>().FirstOrDefaultAsync(p => p.IdPartida == idPartida);

        /// <summary>
        /// Elimina una partida determinada de un usuario determinado.
        /// Tambien elimina los jugadores asociados a la partida.
        /// </summary>
        public async Task EliminarPartidaAsync(int idPartida, string idUsuario) {
            var partida = await _database.Table<Partida>().Where(p => p.IdPartida == idPartida).FirstOrDefaultAsync();
            if (partida != null) {
                await _database.DeleteAsync(partida);

                // Elimina también los jugadores relacionados si aplica
                var jugadores = await GetJugadoresByPartidaAsync(idPartida, idUsuario);
                foreach (var jugador in jugadores) {
                    await _database.DeleteAsync(jugador);
                }
            }
        }

        /// <summary>
        /// Elimina todas las partidas y sus respectivos jugadores de la base de datos
        /// </summary>
        public async Task EliminarTodasPartidas() {
            await _database.DeleteAllAsync<JugadorPartida>();
            await _database.DeleteAllAsync<Partida>();
        }

        /// <summary>
        /// Genera un nuevo ID para partidas (máximo ID existente + 1).
        /// </summary>
        public async Task<int> GetNuevoIdPartidaAsync() {
            // Obtener el máximo ID actual
            var maxId = await _database.Table<Partida>()
                                      .OrderByDescending(p => p.IdPartida)
                                      .FirstOrDefaultAsync();

            return (maxId?.IdPartida ?? 0) + 1;  // Si no hay partidas, empieza en 1
        }


        // ================== OPERACIONES DE JUGADOR PARTIDA ==================

        /// <summary>
        /// Reemplaza todos los jugadores de la base de datos.
        /// </summary>
        public async Task ReplaceJugadoresPartidaAsync(List<JugadorPartida> jugadores) {
            await _database.DeleteAllAsync<JugadorPartida>();
            await _database.InsertAllAsync(jugadores);
        }

        /// <summary>
        /// Guarda un jugador en la base de datos.
        /// Si ya existe, lo reemplaza.
        /// </summary>
        public async Task<int> SaveJugadorPartidaAsync(JugadorPartida jugador) {
            if (jugador.Id == 0)
                return await _database.InsertAsync(jugador);
            else
                return await _database.UpdateAsync(jugador);
        }

        /// <summary>
        /// Obtiene los jugadores pertenecientes a una partida determinada de un usuario determinado.
        /// </summary>
        public Task<List<JugadorPartida>> GetJugadoresByPartidaAsync(int idPartida, string idUsuario) =>
            _database.Table<JugadorPartida>().Where(j => j.IdPartida == idPartida && j.IdUsuario == idUsuario).ToListAsync();

        /// <summary>
        /// Obtiene todos los jugadores de la base de datos.
        /// </summary>
        public Task<List<JugadorPartida>> GetJugadoresPartidaAsync() => _database.Table<JugadorPartida>().ToListAsync();


        // ================== OPERACIONES DE PARTIDAS PÚBLICAS ==================

        /// <summary>
        /// Obtiene todas las partidas publicas que no esten completadas y no hayan expirado (fecha anterior a la actual)
        /// </summary>
        public async Task<List<PartidaPublica>> GetPartidasPublicasAsync() {
            return await _database.Table<PartidaPublica>()
                                 .Where(p => p.Completada == false && p.Fecha > DateTime.Now)
                                 .ToListAsync();
        }

        /// <summary>
        /// Guarda una partida pública usando IdFirebase como clave.
        /// </summary>
        public async Task<int> SavePartidaPublicaAsync(PartidaPublica partida) {
            if (string.IsNullOrEmpty(partida.IdFirebase))
                return 0;

            // Buscar por IdFirebase en lugar de ID local
            var existente = await _database.Table<PartidaPublica>()
                                         .FirstOrDefaultAsync(p => p.IdFirebase == partida.IdFirebase);

            if (existente != null) {
                // Actualizar solo los campos necesarios
                existente.Completada = partida.Completada;
                
                return await _database.UpdateAsync(existente);
            }
            else {
                return await _database.InsertAsync(partida);
            }
        }

        /// <summary>
        /// Marca una partida publica como completada.
        /// </summary>
        public async Task MarcarPartidaPublicaCompletadaAsync(string idFirebase) {
            await _database.ExecuteAsync(
                "UPDATE PartidaPublica SET Completada = 1 WHERE IdFirebase = ?", idFirebase);
        }

        /// <summary>
        /// Reemplaza todas las partidas publicas.
        /// </summary>
        public async Task ReplacePartidasPublicasAsync(List<PartidaPublica> partidas) {
            await _database.RunInTransactionAsync(tx =>
            {
                tx.DeleteAll<PartidaPublica>();
                tx.InsertAll(partidas);
            });
        }

        // ================== SINCRONIZACIÓN ==================

        /// <summary>
        /// Guarda la última fecha de sincronización en Preferences (no en SQLite).
        /// </summary>
        public void GuardarFechaUltimaSync(DateTime fecha) =>
            Preferences.Set("UltimaSync", fecha.ToString("O"));

        /// <summary>
        /// Obtiene la fecha de ultima sincronizacion.
        /// </summary>
        public DateTime? ObtenerFechaUltimaSync() {
            var str = Preferences.Get("UltimaSync", null);
            return str != null ? DateTime.Parse(str) : null;
        }

        /// <summary>
        /// Obtiene toda la informacion de la base de datos.
        /// </summary>
        public async Task<DatosUsuario> ObtenerTodo() {

            var idUsuario = Preferences.Get("UsuarioId", "0");


            var usuario = await GetUsuarioByFirebaseIdAsync(idUsuario);
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

        /// <summary>
        /// Reemplaza todos los datos de usuario.
        /// </summary>
        public async Task GuardarTodoDesdeFirebase(DatosUsuario datos) {
            if (datos.Perfil != null)
                await SaveUsuarioAsync(datos.Perfil);

            await ReplaceJuegosAsync(datos.Juegos);
            await ReplaceColeccionesAsync(datos.Coleccion);
            await ReplacePartidasAsync(datos.Partidas);
            await ReplaceJugadoresPartidaAsync(datos.JugadoresPartida);
        }

        /// <summary>
        /// Resetea completamente la base de datos (para logout o cambios de usuario).
        /// </summary>
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