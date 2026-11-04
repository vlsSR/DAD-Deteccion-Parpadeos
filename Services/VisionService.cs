using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp; 

namespace ProyectoLectorParpadeos.Services
{
    internal class VisionService
    {
        private const int FramesParpadeo = 3;
        private readonly CascadeClassifier detectorRostro;
        private readonly CascadeClassifier detectorOjos;
        private VideoCapture? camara;
        private Task? bucle;
        private volatile bool ejecutando;
        private int framesSinOjos;

        // true = abiertos, false = cerrados, null = no hay cara
        public event Action<Mat, bool?>? FotogramaProcesado;

        public VisionService()
        {
            string directorioBase = AppContext.BaseDirectory;
            string rutaDetectorRostro = Path.Combine(
                directorioBase, "haarcascade_frontalface_default.xml");
            string rutaDetectorOjos = Path.Combine(
                directorioBase, "haarcascade_eye.xml");

            if (!File.Exists(rutaDetectorRostro) || !File.Exists(rutaDetectorOjos))
            {
                throw new FileNotFoundException(
                    "No se encontraron los clasificadores Haar junto al ejecutable. " +
                    "Asegúrese de incluir haarcascade_frontalface_default.xml y " +
                    "haarcascade_eye.xml en el proyecto.");
            }

            detectorRostro = new CascadeClassifier(rutaDetectorRostro);
            detectorOjos = new CascadeClassifier(rutaDetectorOjos);
        }

        public bool Iniciar()
        {
            if (ejecutando) return true;

            camara = new VideoCapture(0);
            if (!camara.IsOpened())
            {
                camara.Dispose();
                camara = null;
                return false;
            }

            framesSinOjos = 0;
            ejecutando = true;
            bucle = Task.Run(BucleLectura);
            return true;
        }

        public void Detener()
        {
            ejecutando = false;
            camara?.Release();
        }

        private void BucleLectura()
        {
            using var imagenColor = new Mat();
            using var imagenGris = new Mat();

            while (ejecutando)
            {
                camara!.Read(imagenColor);
                if (imagenColor.Empty()) continue;

                Cv2.CvtColor(imagenColor, imagenGris, ColorConversionCodes.BGR2GRAY);

                Rect[] rostros = detectorRostro.DetectMultiScale(
                    imagenGris, 1.1, 4, HaarDetectionTypes.ScaleImage, new Size(80, 80));

                bool? ojosAbiertos = null;

                if (rostros.Length > 0)
                {
                    Rect rostro = rostros.MaxBy(r => r.Width);   // la cara más grande
                    Cv2.Rectangle(imagenColor, rostro, Scalar.FromRgb(0, 255, 0), 2);

                    // Buscar ojos solo en la mitad superior de la cara
                    var zonaOjos = new Rect(rostro.X, rostro.Y, rostro.Width, rostro.Height / 2);
                    using var regionDetectada = new Mat(imagenGris, zonaOjos);

                    Rect[] ojos = detectorOjos.DetectMultiScale(
                        regionDetectada, 1.1, 6, HaarDetectionTypes.ScaleImage, new Size(20, 20));

                    foreach (var ojo in ojos)
                    {
                        // las coordenadas del ojo son relativas a la región detectada: sumar el origen
                        var r = new Rect(zonaOjos.X + ojo.X, zonaOjos.Y + ojo.Y, ojo.Width, ojo.Height);
                        Cv2.Rectangle(imagenColor, r, Scalar.FromRgb(255, 255, 0), 2);
                    }

                    framesSinOjos = ojos.Length > 0 ? 0 : framesSinOjos + 1;
                    ojosAbiertos = framesSinOjos < FramesParpadeo;
                }
                else
                {
                    framesSinOjos = 0;
                }

                FotogramaProcesado?.Invoke(imagenColor.Clone(), ojosAbiertos);
            }
        }
    }
}
