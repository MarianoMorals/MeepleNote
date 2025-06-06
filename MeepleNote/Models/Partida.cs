using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {

    /// <summary>
    /// Clase que representa una partida (jugada) de un juego de mesa.
    /// </summary>
    public class Partida {
        /// <summary>
        /// Clave primaria única para la partida.
        /// No es autoincremental para permitir sincronizaciones sin conflictos, se autoincrementa manualmente.
        /// </summary>
        [PrimaryKey]
        public int IdPartida { get; set; }

        /// <summary>
        /// ID del usuario que creó el registro de la partida.
        /// Relación con la tabla Usuarios.
        /// </summary>
        public string IdUsuario { get; set; }

        /// <summary>
        /// ID del juego al que pertenece la partida.
        /// Relación con la tabla Juegos.
        /// </summary>
        public int IdJuego { get; set; }

        /// <summary>
        /// Fecha en que se jugó la partida.
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Nombre del jugador ganador.
        /// </summary>
        public string Ganador { get; set; }
    }
}