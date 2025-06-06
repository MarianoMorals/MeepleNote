using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    /// <summary>
    /// Clase originalmente pensada para gestionar la coleccion.
    /// Obsoleta, por el uso de la adicion del atributo IDUsuario (firebase) a los propios juegos.
    /// En proximas actualizaciones desaparecera, cuando se asegure que no comprometa el correcto funcionamiento de la app.
    /// </summary>
    public class Coleccion {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int IdUsuario { get; set; }

        public int IdJuego { get; set; }
    }
}
