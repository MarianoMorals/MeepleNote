using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class Partida {
        [PrimaryKey, AutoIncrement]
        public int IdPartida { get; set; }

        public int IdUsuario { get; set; }

        public int IdJuego { get; set; }

        public DateTime Fecha { get; set; }

        public string Ganador { get; set; }
    }
}
