using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {

    /// <summary>
    /// ViewModel para representar partidas en la interfaz de usuario,
    /// implementando INotifyPropertyChanged para notificar cambios en las propiedades.
    /// </summary>
    public class PartidaViewModel : INotifyPropertyChanged {

        // === PROPIEDADES ESTÁTICAS ===

        /// <summary>
        /// Identificador único de la partida (clave primaria).
        /// No notifica cambios por ser identificador inmutable.
        /// </summary>
        public int IdPartida { get; set; }

        /// <summary>
        /// ID del juego asociado (clave foránea).
        /// No notifica cambios por ser identificador inmutable.
        /// </summary>
        public int IdJuego { get; set; }

        // === PROPIEDADES CON NOTIFICACIÓN DE CAMBIOS ===

        private string _tituloJuego;
        /// <summary>
        /// Título del juego asociado a la partida.
        /// Notifica automáticamente cambios a la UI mediante PropertyChanged.
        /// </summary>
        public string TituloJuego {
            get => _tituloJuego;
            set {
                if (_tituloJuego != value) {
                    _tituloJuego = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _fotoPortada;
        /// <summary>
        /// Ruta de la imagen de portada del juego.
        /// Proporciona un valor por defecto ("icon_juego_desconocido.png") si está vacía.
        /// Notifica cambios a la UI automáticamente.
        /// </summary>
        public string FotoPortada {
            get => string.IsNullOrEmpty(_fotoPortada) ? "icon_juego_desconocido.png" : _fotoPortada;
            set {
                if (_fotoPortada != value) {
                    _fotoPortada = value;
                    OnPropertyChanged();
                }
            }
        }

        // === PROPIEDADES SIN NOTIFICACIÓN (para datos estáticos) ===

        /// <summary>
        /// Fecha y hora en que se jugó la partida.
        /// Se considera inmutable después de su creación.
        /// </summary>
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Nombre del jugador ganador.
        /// No notifica cambios por ser dato histórico.
        /// </summary>
        public string Ganador { get; set; }

        // === IMPLEMENTACIÓN DE INotifyPropertyChanged ===

        /// <summary>
        /// Evento para notificar cambios en las propiedades a la capa de presentación.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Método auxiliar para invocar el evento PropertyChanged.
        /// Usa CallerMemberName para evitar especificar manualmente el nombre de la propiedad.
        /// </summary>
        /// <param name="propertyName">Nombre de la propiedad cambiada</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}