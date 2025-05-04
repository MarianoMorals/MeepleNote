using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class Usuario {
        [PrimaryKey, AutoIncrement]
        public int IdUsuario { get; set; }

        public string Nombre { get; set; }

        public DateTime FechaNacimiento { get; set; }

        public string Email { get; set; } 

        public string Password { get; set; }
    }
}
