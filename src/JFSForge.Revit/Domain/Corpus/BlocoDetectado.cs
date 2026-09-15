namespace JFSForge.Revit.Domain.Corpus
{
    public class BlocoDetectado
    {
        public string Tipo { get; }
        public double PosicaoX { get; }

        public BlocoDetectado(string tipo, double posicaoX)
        {
            Tipo = tipo;
            PosicaoX = posicaoX;
        }
    }
}
