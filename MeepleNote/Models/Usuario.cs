using SQLite;
using System;

namespace MeepleNote.Models {

    /// <summary>
    /// Modelo que representa un usuario en el sistema.
    /// Maneja tanto el almacenamiento local (SQLite) como la autenticación remota (Firebase).
    /// </summary>
    public class Usuario {

        /// <summary>
        /// Clave primaria autoincremental para la base de datos local.
        /// No se usa para autenticación, solo para relaciones internas.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int IdUsuario { get; set; }

        /// <summary>
        /// Identificador único proporcionado por Firebase Authentication.
        /// Este es el ID principal para operaciones de autenticación.
        /// Formato típico: "abcdef123456..." (string alfanumérico)
        /// </summary>
        public string FirebaseUserId { get; set; }

        /// <summary>
        /// Nombre de visualización del usuario.
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Fecha de nacimiento del usuario.
        /// </summary>
        public DateTime FechaNacimiento { get; set; }

        /// <summary>
        /// Email del usuario (coincide con el de Firebase Authentication).
        /// </summary>
        public string Email { get; set; }

        /*  IMPORTANTE DE SEGURIDAD:
            No almacenamos contraseñas localmente. La autenticación se delega completamente
            a Firebase Authentication, que maneja:
            - Hash seguro de contraseñas
            - Autenticación multifactor
            - Recuperación de cuentas   */
    }
}