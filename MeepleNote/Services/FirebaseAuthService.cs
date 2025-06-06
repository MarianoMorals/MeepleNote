using Firebase.Auth;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Services {
    /// <summary>
    /// Servicio de autenticación usando Firebase Authentication.
    /// Maneja: login, registro, recuperación de contraseña y logout.
    /// </summary>
    public class FirebaseAuthService {
        // Clave API de Firebase
        private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI"; //Se añadira como parametro de la aplicacion en futuras actualizaciones.

        // Proveedor de autenticación de Firebase
        private readonly FirebaseAuthProvider _authProvider;

        /// <summary>
        /// Constructor: inicializa el proveedor de autenticación con la API key.
        /// </summary>
        public FirebaseAuthService() {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
        }

        /// <summary>
        /// Inicia sesión con email y contraseña.
        /// </summary>
        /// <param name="email">Email del usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>FirebaseAuthLink con token de acceso y datos del usuario</returns>
        /// <exception cref="FirebaseAuthException">Error específico de Firebase</exception>
        /// <exception cref="Exception">Otros errores</exception>
        public async Task<FirebaseAuthLink> LoginAsync(string email, string password) {
            try {
                return await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
            }
            catch (FirebaseAuthException ex) {
                Console.WriteLine($"FirebaseAuthService.LoginAsync: {ex.Message}");
                throw; // Relanza la excepción para manejo superior
            }
            catch (Exception ex) {
                Console.WriteLine($"FirebaseAuthService.LoginAsync (General Exception): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registra un nuevo usuario con email y contraseña.
        /// </summary>
        /// <param name="email">Email válido</param>
        /// <param name="password">Contraseña (mínimo 6 caracteres)</param>
        /// <returns>FirebaseAuthLink con token de acceso</returns>
        /// <exception cref="FirebaseAuthException">Si el email ya existe o es inválido</exception>
        public async Task<FirebaseAuthLink> RegisterAsync(string email, string password) {
            try {
                return await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
            }
            catch (FirebaseAuthException ex) {
                Console.WriteLine($"FirebaseAuthService.RegisterAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex) {
                Console.WriteLine($"FirebaseAuthService.RegisterAsync (General Exception): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envía un email para restablecer contraseña.
        /// </summary>
        /// <param name="email">Email registrado en Firebase</param>
        /// <exception cref="FirebaseAuthException">Si el email no existe</exception>
        public async Task SendPasswordResetEmailAsync(string email) {
            try {
                await _authProvider.SendPasswordResetEmailAsync(email);
            }
            catch (FirebaseAuthException ex) {
                Console.WriteLine($"FirebaseAuthService.SendPasswordResetEmailAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex) {
                Console.WriteLine($"FirebaseAuthService.SendPasswordResetEmailAsync (General Exception): {ex.Message}");
                throw;
            }
        }
    }
}