using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class Juego {
        [PrimaryKey]
        public int IdJuego { get; set; }

        public string FotoPortada { get; set; }

        public string Titulo { get; set; }

        public double Puntuacion { get; set; }

        public int NumeroJugadores { get; set; }

        public string DuracionEstimada { get; set; }

        public int Edad { get; set; }

        public string Autor { get; set; }

        public string Artista { get; set; }
        public bool EnColeccion { get; set; }

        [Ignore] // SQLite ignorará esta propiedad
        public bool IsSelected { get; set; }

        [Ignore]
        public string Descripcion { get; set; }

        [Ignore]
        public int MinJugadores { get; set; }

        [Ignore]
        public int MaxJugadores { get; set; }

        [Ignore]
        public string RangoJugadores => $"{MinJugadores}-{MaxJugadores}";

        [Ignore]
        public string PuntuacionFormateada => Puntuacion.ToString("0.00");
    }
}
