using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    /// <summary>
    /// Clase que representa una partida pública organizada por usuarios.
    /// Combina almacenamiento local (SQLite) y sincronización con Firebase.
    /// </summary>
    public class PartidaPublica {

        /// <summary>
        /// Clave primaria autoincremental para la base de datos local (SQLite).
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int IdLocal { get; set; }

        /// <summary>
        /// ID único de Firebase para sincronización en la nube.
        /// </summary>
        public string IdFirebase { get; set; }

        /// <summary>
        /// ID del juego asociado (relación con la tabla Juegos).
        /// </summary>
        public int IdJuego { get; set; }

        /// <summary>
        /// ID del usuario organizador (relación con la tabla Usuarios).
        /// </summary>
        public string IdUsuarioOrganizador { get; set; }

        /// <summary>
        /// Nombre público del organizador.
        /// </summary>
        public string NombreOrganizador { get; set; }

        /// <summary>
        /// Email de contacto para unirse a la partida.
        /// </summary>
        public string EmailContacto { get; set; }

        /// <summary>
        /// Fecha y hora programada para la partida.
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Ciudad donde se organiza la partida.
        /// </summary>
        public string Ciudad { get; set; }

        /// <summary>
        /// Número total de jugadores necesarios.
        /// </summary>
        public int JugadoresRequeridos { get; set; }

        /// <summary>
        /// Indica si la partida ya se completó (true) o sigue abierta (false).
        /// </summary>
        public bool Completada { get; set; }

    }
}