using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Services
{
    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyCmcqsaPemAyArjJBBiV7nFm2TeXLFp9cI";
        private readonly FirebaseAuthProvider _authProvider;

        public FirebaseAuthService() {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(ApiKey));
        }

        public async Task<FirebaseAuthLink> LoginAsync(string email, string password) {
            return await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
        }

        // Puedes agregar otros métodos como:
        public async Task<FirebaseAuthLink> RegisterAsync(string email, string password) {
            return await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
        }

        public void Logout() {
            // Aquí podrías eliminar tokens o limpiar estados locales si lo necesitas
        }
    }
}
