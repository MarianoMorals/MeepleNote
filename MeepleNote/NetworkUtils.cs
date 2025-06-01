using Microsoft.Maui.Networking;

public static class NetworkUtils {
    public static bool TieneConexionInternet() {
        return Connectivity.NetworkAccess == NetworkAccess.Internet;
    }
}