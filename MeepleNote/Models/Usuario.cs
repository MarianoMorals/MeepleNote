using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class Usuario {
        [PrimaryKey, AutoIncrement]
        public int IdUsuario { get; set; } // Clave primaria local autoincremental
        public string FirebaseUserId { get; set; } // ID del usuario en Firebase Authentication

        public string Nombre { get; set; }

        public DateTime FechaNacimiento { get; set; }

        public string Email { get; set; }

        // No guardaremos la contraseña directamente en la base de datos local por seguridad.
        // La autenticación se maneja con Firebase.
        // public string Password { get; set; }
    }
}