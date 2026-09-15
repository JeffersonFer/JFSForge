using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFSForge.Core.Domain.Corpus
{
    public static class TrechoDadosInversor
    {
        public static TrechoDados Inverter(TrechoDados original)
        {
            var blocosFiada1Invertidos = original.Fiada1
                .Select(b => new BlocoNoTrecho(b.Tipo, original.ComprimentoTrecho - b.PosicaoX))
                .ToList();

            var blocosFiada2Invertidos = original.Fiada2
                .Select(b => new BlocoNoTrecho(b.Tipo, original.ComprimentoTrecho - b.PosicaoX))
                .ToList();

            var aberturasInvertidas = original.Aberturas
                .Select(a => new AberturaNoTrecho(original.ComprimentoTrecho - a.PosicaoXCentro, a.Comprimento))
                .ToList();

            return new TrechoDados(
                original.EspessuraParede,
                original.ComprimentoTrecho,
                original.FimEhLivre,               // início e fim trocam
                original.InicioEhLivre,
                original.AmarracaoFiada1NoFim,      // amarrações também trocam de lado
                original.AmarracaoFiada2NoFim,
                original.AmarracaoFiada1NoInicio,
                original.AmarracaoFiada2NoInicio,
                aberturasInvertidas,
                blocosFiada1Invertidos,
                blocosFiada2Invertidos);
        }
    }
}
