namespace JFSForge.Revit.Domain.Corpus
{
    public class AberturaDetectada
    {
        public double PosicaoXCentro { get; }
        public double Comprimento { get; }

        public AberturaDetectada(double posicaoXCentro, double comprimento)
        {
            PosicaoXCentro = posicaoXCentro;
            Comprimento = comprimento;
        }
    }
}
