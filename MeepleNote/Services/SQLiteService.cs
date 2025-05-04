using MeepleNote.Models;
using SQLite;
using System.IO;
using System.Threading.Tasks;

namespace MeepleNote.Services {
    public class SQLiteService {
        private SQLiteAsyncConnection _database;

        public SQLiteService() {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "meeplenote.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            _database.CreateTableAsync<Usuario>();
            _database.CreateTableAsync<Juego>();
            _database.CreateTableAsync<Coleccion>();
            _database.CreateTableAsync<Partida>();
            _database.CreateTableAsync<JugadorPartida>();
        }

        // METODOS
        public Task<int> SaveUsuarioAsync(Usuario usuario) {
            return _database.InsertAsync(usuario);
        }

        public Task<List<Usuario>> GetUsuariosAsync() {
            return _database.Table<Usuario>().ToListAsync();
        }
        public Task<int> SaveJuegoAsync(Juego juego) {
            return _database.InsertAsync(juego);
        }

        public Task<List<Juego>> GetJuegosAsync() {
            return _database.Table<Juego>().ToListAsync();
        }

        public Task<int> DeleteJuegoAsync(Juego juego) {
            return _database.DeleteAsync(juego);
        }
        public async Task<bool> JuegoExisteAsync(int idJuego) {
            var juego = await _database.Table<Juego>()
                .Where(j => j.IdJuego == idJuego)
                .FirstOrDefaultAsync();

            return juego != null;
        }

    }
}
