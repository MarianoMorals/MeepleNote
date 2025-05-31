using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models {
    public class PartidaPublicaViewModel {
        public string Id { get; set; }
        public string TituloJuego { get; set; }
        public string FotoPortada { get; set; }
        public string Ciudad { get; set; }
        public DateTime Fecha { get; set; }
        public int JugadoresRequeridos { get; set; }
        public string Organizador { get; set; }
        public string EmailContacto { get; set; }
        public bool EsOrganizador { get; set; }
    }
}
