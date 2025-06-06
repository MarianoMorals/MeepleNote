using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {

    /// <summary>
    /// Clase que representa la participación de un jugador en una partida específica.
    /// </summary>
    public class JugadorPartida {
        /// <summary>
        /// Clave primaria autoincremental para la tabla local.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// ID de Firebase del Usuario
        /// </summary>
        public string IdUsuario { get; set; }

        /// <summary>
        /// ID de la partida asociada (clave foránea).
        /// Relación con la tabla Partidas.
        /// </summary>
        public int IdPartida { get; set; }

        /// <summary>
        /// Nombre visible del jugador en el contexto de la partida.
        /// </summary>
        public string NombreJugador { get; set; }
    }
}