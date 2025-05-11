using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeepleNote.Models
{
    public class DatosUsuario {
        public Usuario Perfil { get; set; }
        public List<Juego> Juegos { get; set; } = new();
        public List<Coleccion> Coleccion { get; set; } = new();
        public List<Partida> Partidas { get; set; } = new();
        public List<JugadorPartida> JugadoresPartida { get; set; } = new();
    }
}
