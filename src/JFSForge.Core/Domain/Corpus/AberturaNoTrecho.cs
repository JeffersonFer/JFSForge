using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFSForge.Core.Domain.Corpus
{
    public class AberturaNoTrecho
    {
        public double PosicaoXCentro { get; }
        public double Comprimento { get; }

        public AberturaNoTrecho(double posicaoXCentro, double comprimento)
        {
            PosicaoXCentro = posicaoXCentro;
            Comprimento = comprimento;
        }
    }
}
