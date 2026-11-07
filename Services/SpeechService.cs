using System;
using System.Collections.Generic;
using System.Text;
using System.Speech.Recognition;

namespace ProyectoLectorParpadeos.Services
{
    public class SpeechService
    {
        private SpeechRecognitionEngine recognizer;

        public event Action<string> ComandoDetectado;

        public SpeechService()
        {
            try
            {
                recognizer = new SpeechRecognitionEngine();

                // Definir los comandos de voz, en este caso como los verbos y los objetos son fijos no creo variaciones
                Choices comandos = new Choices();
                comandos.Add(new string[] { "Tiempo abierto", "Tiempo cerrado", "Total parpadeos", "salir" });

                GrammarBuilder gb = new GrammarBuilder();
                gb.Append(comandos);

                Grammar g = new Grammar(gb);
                recognizer.LoadGrammar(g);
                recognizer.SpeechRecognized += Recognizer_SpeechRecognized;

                recognizer.SetInputToDefaultAudioDevice();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al inicializar micrófono: " + ex.Message);
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.6f) // Filtrar por confianza (> 60%)
            {
                ComandoDetectado?.Invoke(e.Result.Text.ToUpper());
            }
        }
    }
}
