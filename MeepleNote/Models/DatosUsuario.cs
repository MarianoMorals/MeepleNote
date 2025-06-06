using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models
{   
    /// <summary>
    /// Clase contenedora de todos los datos asociados a un usuario.
    /// Centraliza las entidades principales de la aplicación para facilitar su gestión.
    /// </summary>
    public class DatosUsuario {
        /// <summary>
        /// Perfil del usuario con información básica (nombre, email, etc.).
        /// Tipo: Modelo <see cref="Usuario"/>.
        /// </summary>
        public Usuario Perfil { get; set; }

        /// <summary>
        /// Lista de todos los juegos registrados por el usuario.
        /// Inicializada como una lista vacía por defecto.
        /// </summary>
        public List<Juego> Juegos { get; set; } = new();

        /// <summary>
        /// Obsoleta, por el uso de la adicion del atributo IDUsuario (firebase) a los propios juegos.
        /// En proximas actualizaciones desaparecera, cuando se asegure que no comprometa el correcto funcionamiento de la app.
        /// </summary>
        public List<Coleccion> Coleccion { get; set; } = new();

        /// <summary>
        /// Historial de partidas jugadas por el usuario.
        /// Contiene detalles como fecha, resultado y juego asociado.
        /// </summary>
        public List<Partida> Partidas { get; set; } = new();

        /// <summary>
        /// Relación de jugadores participantes en cada partida.
        /// Permite registrar múltiples jugadores por partida.
        /// </summary>
        public List<JugadorPartida> JugadoresPartida { get; set; } = new();

        /// <summary>
        /// Partidas publicadas por cualquier usuario.
        /// Visible para otros usuarios para organizar sesiones en comunidad.
        /// </summary>
        public List<PartidaPublica> PartidaPublicas { get; set;} = new();
    }
}
