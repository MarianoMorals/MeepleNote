using Firebase.Auth;
using System;
using System.Threading.Tasks;

namespace MeepleNote.Services {
    public class FirebaseAuthService {
        private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";
        private readonly FirebaseAuthProvider _authProvider;
        public FirebaseAuthService() {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
        }
        public async Task<FirebaseAuthLink> LoginAsync(string email, string password) {
            try {
                return await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
            }
            catch (FirebaseAuthException ex) {
                Console.WriteLine($"FirebaseAuthService.LoginAsync: {ex.Message}");
                throw;
            }
            catch (Exception ex) {
                Console.WriteLine($"FirebaseAuthService.LoginAsync (General Exception): {ex.Message}");
                throw;
            }
        }
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
        public void Logout() {
        }
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
