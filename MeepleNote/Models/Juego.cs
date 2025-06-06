using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {

    /// <summary>
    /// Clase que representa un juego de mesa en el sistema.
    /// Mapea la tabla 'Juego' en la base de datos SQLite y contiene
    /// propiedades para gestión en la UI.
    /// </summary>
    public class Juego {

        /// <summary>
        /// Clave primaria autoincremental (para la tabla local).
        /// No confundir con IdJuego (ID de la API de BoardGameGeek).
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador único del juego (ID de la API de BoardGameGeek).
        /// </summary>
        public int IdJuego { get; set; }

        /// <summary>
        /// ID del usuario dueño de este registro (para multi-usuario), se corresponde con el Id de FireBase.
        /// </summary>
        public string IdUsuario { get; set; }

        /// <summary>
        /// Ruta o URL de la imagen de portada del juego.
        /// </summary>
        public string FotoPortada { get; set; }

        /// <summary>
        /// Nombre del juego (ej: "Catan", "Ticket to Ride").
        /// </summary>
        public string Titulo { get; set; }

        /// <summary>
        /// Puntuación global del juego (ej: 7.5 de 10).
        /// </summary>
        public double Puntuacion { get; set; }

        /// <summary>
        /// OBSOLETO, se eliminara en futuras actualizaciones. 
        /// Es sustituido por el rango de jugadores, aporta mas informacion.
        /// </summary>
        public int NumeroJugadores { get; set; }

        /// <summary>
        /// Duración estimada en formato legible (ej: "60 mins").
        /// </summary>
        public string DuracionEstimada { get; set; }

        /// <summary>
        /// Edad mínima recomendada.
        /// </summary>
        public int Edad { get; set; }

        /// <summary>
        /// Diseñador(es) del juego.
        /// </summary>
        public string Autor { get; set; }

        /// <summary>
        /// Artista(s) que trabajó en el juego.
        /// </summary>
        public string Artista { get; set; }

        /// <summary>
        /// Indica si el juego está en la colección física del usuario.
        /// </summary>
        public bool EnColeccion { get; set; }

        /// <summary>
        /// Puntuación personal asignada por el usuario (no afecta la puntuación global).
        /// </summary>
        public int PuntuacionPersonal { get; set; }

        // === PROPIEDADES TRANSITORIAS (NO PERSISTENTES) ===
        // Marcadas con [Ignore] para que SQLite no las guarde en la BD

        /// <summary>
        /// Usado para selección múltiple en la UI (checkboxes).
        /// </summary>
        [Ignore]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Descripción detallada del juego (cargada desde API).
        /// </summary>
        [Ignore]
        public string Descripcion { get; set; }

        /// <summary>
        /// Mínimo número de jugadores requeridos.
        /// </summary>
        [Ignore]
        public int MinJugadores { get; set; }

        /// <summary>
        /// Máximo número de jugadores permitidos.
        /// </summary>
        [Ignore]
        public int MaxJugadores { get; set; }

        /// <summary>
        /// Rango formateado de jugadores (ej: "2-4").
        /// Propiedad calculada basada en MinJugadores/MaxJugadores.
        /// </summary>
        [Ignore]
        public string RangoJugadores => $"{MinJugadores}-{MaxJugadores}";

        /// <summary>
        /// Puntuación global formateada a 2 decimales (ej: "7.50").
        /// </summary>
        [Ignore]
        public string PuntuacionFormateada => Puntuacion.ToString("0.00");

        /// <summary>
        /// OBSOLETA, se paso a un sistema de puntuacion de 1-5, pensado en asignar estrellas en el futuro.
        /// Puntuación personal formateada a 2 decimales.
        /// </summary>
        [Ignore]
        public string PuntuacionPersonalFormateada => PuntuacionPersonal.ToString("0.00");
    }
}