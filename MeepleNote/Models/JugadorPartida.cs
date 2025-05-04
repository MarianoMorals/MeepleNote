using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class JugadorPartida {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int IdPartida { get; set; }

        public string NombreJugador { get; set; }
    }
}
