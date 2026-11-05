using System;
using System.Collections.Generic;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ProyectoLectorParpadeos.Models
{
    public partial class BarraParpadeos : ObservableObject
    {
        public int Minuto { get; set; }
        
        [ObservableProperty]
        private int cantidad;

        [ObservableProperty]
        private double altura = 2;
    }

}
