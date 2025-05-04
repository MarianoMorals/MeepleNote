using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class Coleccion {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int IdUsuario { get; set; }

        public int IdJuego { get; set; }
    }
}
