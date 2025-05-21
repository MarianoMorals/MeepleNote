using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class PartidaViewModel : INotifyPropertyChanged {
        public int IdPartida { get; set; }
        public int IdJuego { get; set; }

        private string _tituloJuego;
        public string TituloJuego {
            get => _tituloJuego;
            set {
                _tituloJuego = value;
                OnPropertyChanged();
            }
        }

        private string _fotoPortada;
        public string FotoPortada {
            get => string.IsNullOrEmpty(_fotoPortada) ? "icon_juego_desconocido.png" : _fotoPortada;
            set {
                _fotoPortada = value;
                OnPropertyChanged();
            }
        }

        public DateTime Fecha { get; set; }
        public string Ganador { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
