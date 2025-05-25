using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class PartidaPublica {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int IdJuego { get; set; }
        public int IdUsuarioOrganizador { get; set; }
        public string NombreOrganizador { get; set; }
        public string EmailContacto { get; set; }
        public DateTime Fecha { get; set; }
        public string Ciudad { get; set; }
        public int JugadoresRequeridos { get; set; }
        public bool Completada { get; set; }
    }
}
