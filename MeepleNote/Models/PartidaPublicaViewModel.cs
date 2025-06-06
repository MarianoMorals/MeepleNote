using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {

    /// <summary>
    /// ViewModel para mostrar partidas públicas en la interfaz de usuario.
    /// Contiene datos combinados de PartidaPublica y Juego para visualización.
    /// </summary>
    public class PartidaPublicaViewModel {

        /// <summary>
        /// Identificador único de la partida pública (IdFirebase).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Título del juego asociado a la partida.
        /// </summary>
        public string TituloJuego { get; set; }

        /// <summary>
        /// URL o ruta local de la imagen de portada del juego.
        /// </summary>
        public string FotoPortada { get; set; }

        /// <summary>
        /// Ciudad donde se organiza la partida.
        /// </summary>
        public string Ciudad { get; set; }

        /// <summary>
        /// Fecha y hora programada para la partida.
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Número total de jugadores necesarios.
        /// </summary>
        public int JugadoresRequeridos { get; set; }

        /// <summary>
        /// Nombre público del usuario organizador.
        /// </summary>
        public string Organizador { get; set; }

        /// <summary>
        /// Email de contacto para unirse a la partida.
        /// </summary>
        public string EmailContacto { get; set; }

        /// <summary>
        /// Indica si el usuario actual es el organizador de esta partida.
        /// Usado para mostrar/u ocultar controles de administración (boton "Completada").
        /// </summary>
        public bool EsOrganizador { get; set; }
    }
}