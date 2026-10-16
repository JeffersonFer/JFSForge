using Autodesk.Revit.DB;

namespace JFSForge.Revit.Services
{
    public enum ModoDeAtualizacao
    {
        Tudo,
        SomenteAlterados
    }

    public enum CriterioDeNumeracao
    {
        Geral,
        PorParede
    }

    public enum DestinoDeNovos
    {
        ProximoLivre,
        FinalDaSequencia
    }

    public class OpcoesDeRenumeracao
    {
        public ModoDeAtualizacao Modo { get; set; } = ModoDeAtualizacao.Tudo;
        public CriterioDeNumeracao Criterio { get; set; } = CriterioDeNumeracao.Geral;
        public DestinoDeNovos Novos { get; set; } = DestinoDeNovos.ProximoLivre;
    }

    public class ResumoDeRenumeracao
    {
        public int Total { get; set; }
        public int ComCodigoValido { get; set; }
        public int ComAviso { get; set; }
        public int NumerosMantidos { get; set; }
        public int NumerosNovos { get; set; }
        public int CodigosDistintos { get; set; }
        public int SemParametro { get; set; }
        public List<string> Falhas { get; } = new List<string>();

        public string ParaTexto(int maximoDeFalhas = 15)
        {
            var texto =
                $"Graute horizontal no projeto: {Total}\n" +
                $"   Com código válido: {ComCodigoValido}\n" +
                $"   Com aviso (fora da numeração): {ComAviso}\n" +
                $"   Sem os parâmetros necessários: {SemParametro}\n\n" +
                $"Códigos distintos: {CodigosDistintos}\n" +
                $"   Números mantidos: {NumerosMantidos}\n" +
                $"   Números novos: {NumerosNovos}\n\n" +
                $"Falhas: {Falhas.Count}";

            if (Falhas.Count > 0)
            {
                texto += "\n\n" + string.Join("\n", Falhas.Take(maximoDeFalhas));

                if (Falhas.Count > maximoDeFalhas)
                    texto += $"\n... e mais {Falhas.Count - maximoDeFalhas}";
            }

            return texto;
        }
    }

    public static class CodificadorDeAco
    {
        // ===== Parâmetros =====
        private const string ParametroCodigo = "COD_ACO";
        private const string ParametroNumero = "N";
        private const string ParametroQuantidade = "Quantidade";
        private const string ParametroParQtd = "PAR_QTD";
        private const string ParametroComprimento = "BARRA_COMPR_RETO_TRUNCADO";

        private static readonly string[] DobrasDoLadoA = { "DOBRA_10_A", "DOBRA_35_A", "DOBRA_35_AU" };
        private static readonly string[] DobrasDoLadoB = { "DOBRA_10_B", "DOBRA_35_B", "DOBRA_35_BU" };

        // ===== Identificação =====
        private const string TypeCommentsGrauteHorizontal = "Graute Horizontal";

        // ===== Regras =====
        private const string SemDobraA = "SEM_A";
        private const string SemDobraB = "SEM_B";
        private const string PrefixoAviso = "AVISO";
        private const string GrupoDeAvisos = "AVISO";   // "parede" das barras com PAR_QTD inválido
        private const double ToleranciaXYCm = 0.5;
        private const double ToleranciaAlinhamento = 0.999;

        private class Barra
        {
            public FamilyInstance Instancia = null!;
            public XYZ Posicao = null!;
            public string? Codigo;          // null quando há aviso
            public string? Aviso;
            public string ParQtd = string.Empty;
            public string NumeroAtual = string.Empty;
            public string CodigoAtual = string.Empty;
        }

        /// <summary>
        /// Recalcula COD_ACO, N e Quantidade do graute horizontal.
        /// Pressupõe que o PAR_QTD já foi atualizado pelo chamador.
        /// Usa a transação do chamador quando há uma aberta.
        /// </summary>
        public static ResumoDeRenumeracao Executar(Document doc, OpcoesDeRenumeracao opcoes)
        {
            var resumo = new ResumoDeRenumeracao();

            if (!doc.IsModifiable)
            {
                using var transacao = new Transaction(doc, "Renumerar aço horizontal");
                transacao.Start();
                Processar(doc, opcoes, resumo);
                transacao.Commit();
            }
            else
            {
                Processar(doc, opcoes, resumo);
            }

            return resumo;
        }

        private static void Processar(Document doc, OpcoesDeRenumeracao opcoes, ResumoDeRenumeracao resumo)
        {
            var barras = Coletar(doc, resumo);
            resumo.Total = barras.Count;

            var validas = barras.Where(b => b.Codigo != null).ToList();
            resumo.ComCodigoValido = validas.Count;
            resumo.ComAviso = barras.Count - validas.Count;

            GravarCodigos(barras, resumo);

            var numeroPorChave = Numerar(validas, opcoes, resumo);
            GravarNumeros(barras, numeroPorChave, resumo);

            GravarQuantidades(barras, resumo);
        }

        private static List<Barra> Coletar(Document doc, ResumoDeRenumeracao resumo)
        {
            var barras = new List<Barra>();
            var cacheTipos = new Dictionary<ElementId, bool>();

            var instancias = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>()
                .Where(fi => EhGrauteHorizontal(doc, fi, cacheTipos));

            foreach (var instancia in instancias)
            {
                if (instancia.Location is not LocationPoint localizacao)
                {
                    resumo.Falhas.Add($"Elemento {instancia.Id}: sem ponto de localização");
                    continue;
                }

                var barra = new Barra
                {
                    Instancia = instancia,
                    Posicao = localizacao.Point,
                    ParQtd = instancia.LookupParameter(ParametroParQtd)?.AsString() ?? string.Empty,
                    NumeroAtual = instancia.LookupParameter(ParametroNumero)?.AsString() ?? string.Empty,
                    CodigoAtual = instancia.LookupParameter(ParametroCodigo)?.AsString() ?? string.Empty
                };

                MontarCodigo(instancia, barra, resumo);
                barras.Add(barra);
            }

            return barras;
        }

        private static void MontarCodigo(FamilyInstance instancia, Barra barra, ResumoDeRenumeracao resumo)
        {
            var comprimento = instancia.LookupParameter(ParametroComprimento);

            if (comprimento == null || !comprimento.HasValue)
            {
                barra.Aviso = $"{PrefixoAviso}: {ParametroComprimento} ausente ou vazio";
                resumo.SemParametro++;
                return;
            }

            var comprimentoCm = (int)Math.Round(
                UnitUtils.ConvertFromInternalUnits(comprimento.AsDouble(), UnitTypeId.Centimeters));

            var ladoA = ResolverDobra(instancia, DobrasDoLadoA, SemDobraA, "A", out var avisoA);
            if (avisoA != null)
            {
                barra.Aviso = avisoA;
                return;
            }

            var ladoB = ResolverDobra(instancia, DobrasDoLadoB, SemDobraB, "B", out var avisoB);
            if (avisoB != null)
            {
                barra.Aviso = avisoB;
                return;
            }

            barra.Codigo = $"CR{comprimentoCm}-{ladoA}-{ladoB}";
        }

        private static string ResolverDobra(
            FamilyInstance instancia,
            string[] nomes,
            string valorSemDobra,
            string lado,
            out string? aviso)
        {
            aviso = null;

            var marcadas = nomes
                .Where(nome => instancia.LookupParameter(nome)?.AsInteger() == 1)
                .ToList();

            if (marcadas.Count == 0)
                return valorSemDobra;

            if (marcadas.Count == 1)
                return marcadas[0];

            aviso = $"{PrefixoAviso}: mais de uma dobra marcada no lado {lado} ({string.Join(", ", marcadas)})";
            return valorSemDobra;
        }

        private static void GravarCodigos(List<Barra> barras, ResumoDeRenumeracao resumo)
        {
            foreach (var barra in barras)
            {
                var valor = barra.Codigo ?? barra.Aviso ?? $"{PrefixoAviso}: código não pôde ser calculado";
                Gravar(barra.Instancia, ParametroCodigo, valor, resumo);
            }
        }

        /// <summary>
        /// Devolve o número de cada chave de numeração. A chave é só o código no modo geral,
        /// e parede + código no modo por parede, onde a sequência reinicia em cada parede.
        /// </summary>
        private static Dictionary<string, int> Numerar(
            List<Barra> validas,
            OpcoesDeRenumeracao opcoes,
            ResumoDeRenumeracao resumo)
        {
            var numeroPorChave = new Dictionary<string, int>();

            if (opcoes.Criterio == CriterioDeNumeracao.Geral)
            {
                NumerarGrupo(validas, b => b.Codigo!, opcoes, numeroPorChave, resumo);
            }
            else
            {
                var porParede = validas.GroupBy(b => string.IsNullOrWhiteSpace(b.ParQtd) || b.ParQtd.StartsWith(PrefixoAviso)
                    ? GrupoDeAvisos
                    : b.ParQtd);

                foreach (var parede in porParede)
                {
                    NumerarGrupo(
                        parede.ToList(),
                        b => $"{parede.Key}|{b.Codigo}",
                        opcoes,
                        numeroPorChave,
                        resumo);
                }
            }

            resumo.CodigosDistintos = numeroPorChave.Count;
            return numeroPorChave;
        }

        private static void NumerarGrupo(
            List<Barra> barras,
            Func<Barra, string> obterChave,
            OpcoesDeRenumeracao opcoes,
            Dictionary<string, int> numeroPorChave,
            ResumoDeRenumeracao resumo)
        {
            // Geral: Y decrescente, X crescente, Z decrescente.
            // Por parede: Z decrescente primeiro (a barra mais alta recebe 1), depois Y e X.
            var ordenadas = opcoes.Criterio == CriterioDeNumeracao.PorParede
                ? barras
                    .OrderByDescending(b => b.Posicao.Z)
                    .ThenByDescending(b => b.Posicao.Y)
                    .ThenBy(b => b.Posicao.X)
                    .ToList()
                : barras
                    .OrderByDescending(b => b.Posicao.Y)
                    .ThenBy(b => b.Posicao.X)
                    .ThenByDescending(b => b.Posicao.Z)
                    .ToList();

            var usados = new HashSet<int>();
            var pendentes = new List<string>();

            // No modo "só alterados", quem manteve o código conserva o número gravado.
            if (opcoes.Modo == ModoDeAtualizacao.SomenteAlterados)
            {
                foreach (var barra in ordenadas)
                {
                    var chave = obterChave(barra);

                    if (numeroPorChave.ContainsKey(chave))
                        continue;

                    if (barra.CodigoAtual == barra.Codigo
                        && int.TryParse(barra.NumeroAtual, out var numeroAtual)
                        && numeroAtual > 0
                        && usados.Add(numeroAtual))
                    {
                        numeroPorChave[chave] = numeroAtual;
                        resumo.NumerosMantidos++;
                    }
                }
            }

            foreach (var barra in ordenadas)
            {
                var chave = obterChave(barra);

                if (numeroPorChave.ContainsKey(chave) || pendentes.Contains(chave))
                    continue;

                pendentes.Add(chave);
            }

            foreach (var chave in pendentes)
            {
                var numero = opcoes.Modo == ModoDeAtualizacao.SomenteAlterados
                    && opcoes.Novos == DestinoDeNovos.FinalDaSequencia
                        ? (usados.Count == 0 ? 1 : usados.Max() + 1)
                        : ProximoLivre(usados);

                usados.Add(numero);
                numeroPorChave[chave] = numero;
                resumo.NumerosNovos++;
            }
        }

        private static int ProximoLivre(HashSet<int> usados)
        {
            var numero = 1;
            while (usados.Contains(numero))
                numero++;

            return numero;
        }

        private static void GravarNumeros(
            List<Barra> barras,
            Dictionary<string, int> numeroPorChave,
            ResumoDeRenumeracao resumo)
        {
            foreach (var barra in barras)
            {
                if (barra.Codigo == null)
                {
                    Gravar(barra.Instancia, ParametroNumero, barra.Aviso ?? $"{PrefixoAviso}: sem código", resumo);
                    continue;
                }

                var chaveGeral = barra.Codigo;
                var paredeOuAviso = string.IsNullOrWhiteSpace(barra.ParQtd) || barra.ParQtd.StartsWith(PrefixoAviso)
                    ? GrupoDeAvisos
                    : barra.ParQtd;
                var chavePorParede = $"{paredeOuAviso}|{barra.Codigo}";

                var chave = numeroPorChave.ContainsKey(chavePorParede) ? chavePorParede : chaveGeral;

                if (!numeroPorChave.TryGetValue(chave, out var numero))
                {
                    Gravar(barra.Instancia, ParametroNumero, $"{PrefixoAviso}: número não atribuído", resumo);
                    continue;
                }

                Gravar(barra.Instancia, ParametroNumero, numero.ToString(), resumo);
            }
        }

        /// <summary>
        /// Quantidade conta as barras empilhadas: mesma orientação, mesmo COD_ACO e
        /// mesmos X e Y (tolerância de 0,5 cm). Só o Z varia. Todas as barras do grupo
        /// recebem a contagem. PAR_QTD diferente dentro de um grupo é erro.
        /// </summary>
        private static void GravarQuantidades(List<Barra> barras, ResumoDeRenumeracao resumo)
        {
            var tolerancia = UnitUtils.ConvertToInternalUnits(ToleranciaXYCm, UnitTypeId.Centimeters);
            var grupos = new List<List<Barra>>();

            foreach (var barra in barras)
            {
                if (barra.Codigo == null)
                    continue;

                var grupo = grupos.FirstOrDefault(g =>
                    g[0].Codigo == barra.Codigo &&
                    Math.Abs(g[0].Posicao.X - barra.Posicao.X) <= tolerancia &&
                    Math.Abs(g[0].Posicao.Y - barra.Posicao.Y) <= tolerancia &&
                    MesmaOrientacao(g[0].Instancia, barra.Instancia));

                if (grupo == null)
                {
                    grupos.Add(new List<Barra> { barra });
                }
                else
                {
                    grupo.Add(barra);
                }
            }

            foreach (var barra in barras.Where(b => b.Codigo == null))
            {
                Gravar(barra.Instancia, ParametroQuantidade, barra.Aviso ?? $"{PrefixoAviso}: sem código", resumo);
            }

            foreach (var grupo in grupos)
            {
                var paredes = grupo.Select(b => b.ParQtd).Distinct().ToList();

                var valor = paredes.Count > 1
                    ? $"{PrefixoAviso}: barras alinhadas com {ParametroParQtd} diferente ({string.Join(", ", paredes)})"
                    : grupo.Count.ToString();

                foreach (var barra in grupo)
                    Gravar(barra.Instancia, ParametroQuantidade, valor, resumo);
            }
        }

        private static bool MesmaOrientacao(FamilyInstance a, FamilyInstance b)
        {
            var frenteA = a.FacingOrientation.Normalize();
            var frenteB = b.FacingOrientation.Normalize();

            return Math.Abs(frenteA.DotProduct(frenteB)) > ToleranciaAlinhamento;
        }

        private static void Gravar(FamilyInstance instancia, string nomeDoParametro, string valor, ResumoDeRenumeracao resumo)
        {
            var parametro = instancia.LookupParameter(nomeDoParametro);

            if (parametro == null || parametro.IsReadOnly || parametro.StorageType != StorageType.String)
            {
                resumo.Falhas.Add($"Elemento {instancia.Id}: parâmetro {nomeDoParametro} ausente ou não é texto");
                return;
            }

            if ((parametro.AsString() ?? string.Empty) == valor)
                return;

            if (!parametro.Set(valor))
                resumo.Falhas.Add($"Elemento {instancia.Id}: o Revit não aceitou o valor de {nomeDoParametro}");
        }

        private static bool EhGrauteHorizontal(Document doc, Element elemento, Dictionary<ElementId, bool> cacheTipos)
        {
            var tipoId = elemento.GetTypeId();

            if (cacheTipos.TryGetValue(tipoId, out var resultado))
                return resultado;

            var tipo = doc.GetElement(tipoId) as ElementType;
            var typeComments = tipo?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS)?.AsString();

            resultado = typeComments == TypeCommentsGrauteHorizontal;
            cacheTipos[tipoId] = resultado;
            return resultado;
        }
    }
}