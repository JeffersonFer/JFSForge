using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFSForge.Core.Domain.Corpus
{
    public class BlocoNoTrecho
    {
        public string Tipo { get; }
        public double PosicaoX { get; }

        public BlocoNoTrecho(string tipo, double posicaoX)
        {
            Tipo = tipo;
            PosicaoX = posicaoX;
        }
    }
}
