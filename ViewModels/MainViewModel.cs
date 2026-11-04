using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using ProyectoLectorParpadeos.Services;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

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
        }

        private void ActualizarEstadisticas()
        {
            OnPropertyChanged(nameof(TiempoOjosAbiertos));
            OnPropertyChanged(nameof(TiempoOjosCerrados));
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
