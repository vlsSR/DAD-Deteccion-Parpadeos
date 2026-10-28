using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoLectorParpadeos.Models
{
    public class Actividad
    {
        public string Hora { get; set; } = System.DateTime.Now.ToString("HH:mm:ss");
        public string Origen { get; set; } = string.Empty; // "VISIÓN" o "VOZ"
        public string Accion { get; set; } = string.Empty;
    }
}
