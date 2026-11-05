using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System;
using System.Windows;
using System.Windows.Media;

namespace ProyectoLectorParpadeos.Helpers
{
    public static class GraficoCircular
    {
        public static Geometry CrearSector(double inicioGrados, double barridoGrados, double radio)
        {
            if (barridoGrados <= 0.01)
                return Geometry.Empty;
            var centro = new Point(radio, radio);

            if (barridoGrados >= 359.99)
                return new EllipseGeometry(centro, radio, radio);

            var inicio = PuntoEnCirculo(centro, radio, inicioGrados);
            var fin = PuntoEnCirculo(centro, radio, inicioGrados + barridoGrados);

            var figura = new PathFigure { StartPoint = centro, IsClosed = true };
            figura.Segments.Add(new LineSegment(inicio, true));
            figura.Segments.Add(new ArcSegment(fin, new Size(radio, radio), 0,
                barridoGrados > 180, SweepDirection.Clockwise, true));

            var geometria = new PathGeometry();
            geometria.Figures.Add(figura);
            geometria.Freeze();
            return geometria;
        }

        private static Point PuntoEnCirculo(Point centro, double radio, double grados)
        {
            double rad = (grados - 90) * Math.PI / 180;
            return new Point(centro.X + radio * Math.Cos(rad), centro.Y + radio * Math.Sin(rad));
        }
    }
}
