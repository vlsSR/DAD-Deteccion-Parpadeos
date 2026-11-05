using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using ProyectoLectorParpadeos.Services;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

using System.Collections.ObjectModel;
using System.Windows.Media;
using ProyectoLectorParpadeos.Helpers;
using ProyectoLectorParpadeos.Models;

namespace ProyectoLectorParpadeos.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static readonly TimeSpan DuracionMaximaParpadeo = TimeSpan.FromSeconds(3);

        private readonly VisionService visionService = new();
        private DateTime? inicioEstado;
        private bool? estadoAnterior;
        private DateTime? inicioCicloParpadeo;
        private bool camaraActiva;

        private DateTime? inicioSesion;
        private const double RadioGrafico = 65;
        private const int MaximoMinutosMostrados = 6;

        public ObservableCollection<BarraParpadeos> ParpadeosPorMinuto { get; } = new();

        [ObservableProperty]
        private Geometry? sectorAbiertos;

        [ObservableProperty]
        private Geometry? sectorCerrados;

        [ObservableProperty]
        private string porcentajeAbiertos = "Abiertos: 0 %";

        [ObservableProperty]
        private string porcentajeCerrados = "Cerrados: 0 %";

        [ObservableProperty]
        private BitmapSource? imagenVideo;

        [ObservableProperty]
        private int totalParpadeos;

        [ObservableProperty]
        private double segundosOjosAbiertos;

        [ObservableProperty]
        private double segundosOjosCerrados;

        public MainViewModel()
        {
            visionService.FotogramaProcesado += ProcesarFotograma;
        }

        [RelayCommand]
        private void IniciarCamara()
        {
            if (camaraActiva)
                return;

            ReiniciarEstadisticas();
            camaraActiva = visionService.Iniciar();
        }

        [RelayCommand]
        private void DetenerCamara()
        {
            if (!camaraActiva)
                return;

            AcumularEstadoActual(DateTime.UtcNow);
            camaraActiva = false;
            visionService.Detener();
            estadoAnterior = null;
            inicioEstado = null;
            inicioCicloParpadeo = null;
            ActualizarEstadisticas();
        }

        private void ProcesarFotograma(Mat imagen, bool? ojosAbiertos)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImagenVideo = ConvertirImagen(imagen);
                    ProcesarEstado(ojosAbiertos, DateTime.UtcNow);
                });
            }
            finally
            {
                imagen.Dispose();
            }
        }

        private void ProcesarEstado(bool? ojosAbiertos, DateTime ahora)
        {
            if (!camaraActiva)
                return;

            if (ojosAbiertos is null)
            {
                AcumularEstadoActual(ahora);
                estadoAnterior = null;
                inicioEstado = null;
                inicioCicloParpadeo = null;
                ActualizarEstadisticas();
                return;
            }

            if (estadoAnterior == ojosAbiertos)
            {
                AcumularEstadoActual(ahora);
                inicioEstado = ahora;
                ActualizarEstadisticas();
                return;
            }

            AcumularEstadoActual(ahora);

            if (ojosAbiertos == false && estadoAnterior == true)
            {
                inicioCicloParpadeo = inicioEstado;
            }
            else if (ojosAbiertos == true && estadoAnterior == false &&
                     inicioCicloParpadeo is not null &&
                     ahora - inicioCicloParpadeo.Value < DuracionMaximaParpadeo)
            {
                TotalParpadeos++;
                RegistrarParpadeoEnMinuto(ahora);
                inicioCicloParpadeo = null;
            }
            else if (ojosAbiertos == true)
            {
                inicioCicloParpadeo = null;
            }

            estadoAnterior = ojosAbiertos;
            inicioEstado = ahora;
            ActualizarEstadisticas();
        }

        private void AcumularEstadoActual(DateTime ahora)
        {
            if (estadoAnterior is null || inicioEstado is null)
                return;

            var segundos = (ahora - inicioEstado.Value).TotalSeconds;
            if (estadoAnterior.Value)
                SegundosOjosAbiertos += segundos;
            else
                SegundosOjosCerrados += segundos;
        }

        private void ReiniciarEstadisticas()
        {
            TotalParpadeos = 0;
            SegundosOjosAbiertos = 0;
            SegundosOjosCerrados = 0;
            estadoAnterior = null;
            inicioEstado = null;
            inicioCicloParpadeo = null;
            ImagenVideo = null;

            inicioSesion = DateTime.UtcNow;
            ParpadeosPorMinuto.Clear();
            AsegurarMinutos(inicioSesion.Value);
            ActualizarGraficoCircular();
        }

        private void ActualizarEstadisticas()
        {
            OnPropertyChanged(nameof(TiempoOjosAbiertos));
            OnPropertyChanged(nameof(TiempoOjosCerrados));
            ActualizarGraficoCircular();
            AsegurarMinutos(DateTime.UtcNow);
        }

        private void ActualizarGraficoCircular()
        {
            double total = SegundosOjosAbiertos + SegundosOjosCerrados;

            if (total <= 0)
            {
                SectorAbiertos = null;
                SectorCerrados = null;
                PorcentajeAbiertos = "Abiertos: 0 %";
                PorcentajeCerrados = "Cerrados: 0 %";
                return;
            }

            double fraccionAbiertos = SegundosOjosAbiertos / total;
            SectorAbiertos = GraficoCircular.CrearSector(0, fraccionAbiertos * 360, RadioGrafico);
            SectorCerrados = GraficoCircular.CrearSector(fraccionAbiertos * 360, (1 - fraccionAbiertos) * 360, RadioGrafico);
            PorcentajeAbiertos = $"Abiertos: {fraccionAbiertos * 100:0} %";
            PorcentajeCerrados = $"Cerrados: {(1 - fraccionAbiertos) * 100:0} %";
        }

        private void RegistrarParpadeoEnMinuto(DateTime ahora)
        {
            AsegurarMinutos(ahora);
            if (ParpadeosPorMinuto.Count > 0)
                ParpadeosPorMinuto[^1].Cantidad++;
            RecalcularAlturas();
        }

        private void AsegurarMinutos(DateTime ahora)
        {
            if (inicioSesion is null)
                return;

            int minutoActual = (int)(ahora - inicioSesion.Value).TotalMinutes + 1;
            int ultimo = ParpadeosPorMinuto.Count > 0 ? ParpadeosPorMinuto[^1].Minuto : 0;

            while (ultimo < minutoActual)
            {
                ultimo++;
                ParpadeosPorMinuto.Add(new BarraParpadeos { Minuto = ultimo });
            }

            while (ParpadeosPorMinuto.Count > MaximoMinutosMostrados)
                ParpadeosPorMinuto.RemoveAt(0);

            RecalcularAlturas();
        }

        private void RecalcularAlturas()
        {
            if (ParpadeosPorMinuto.Count == 0)
                return;

            int maximo = Math.Max(5, ParpadeosPorMinuto.Max(b => b.Cantidad));
            foreach (var barra in ParpadeosPorMinuto)
                barra.Altura = Math.Max(2, barra.Cantidad / (double)maximo * 100);
        }

        public string TiempoOjosAbiertos => $"{SegundosOjosAbiertos:0.00} s";

        public string TiempoOjosCerrados => $"{SegundosOjosCerrados:0.00} s";

        private static BitmapImage ConvertirImagen(Mat imagen)
        {
            Cv2.ImEncode(".png", imagen, out var datos);
            using var stream = new System.IO.MemoryStream(datos);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}
