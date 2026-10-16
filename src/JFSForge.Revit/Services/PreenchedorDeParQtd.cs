using Autodesk.Revit.DB;

namespace JFSForge.Revit.Services
{
    public class ResumoParQtd
    {
        public int Elevacoes { get; set; }
        public int Total { get; set; }
        public int NomeUnico { get; set; }
        public int ComAviso { get; set; }
        public int SemElevacao { get; set; }
        public int SemParametro { get; set; }
        public int Alterados { get; set; }
        public List<string> Falhas { get; } = new List<string>();

        public string ParaTexto(int maximoDeFalhas = 15)
        {
            var texto =
                $"Elevações analisadas: {Elevacoes}\n" +
                $"Graute horizontal no projeto: {Total}\n" +
                $"   Em uma elevação (nome gravado): {NomeUnico}\n" +
                $"   Em mais de uma elevação (aviso gravado): {ComAviso}\n" +
                $"   Fora de todas as elevações: {SemElevacao}\n" +
                $"   Sem o parâmetro {PreenchedorDeParQtd.NomeParametro}: {SemParametro}\n\n" +
                $"Valores alterados nesta execução: {Alterados}\n" +
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

    public static class PreenchedorDeParQtd
    {
        // ===== Identificação =====
        public const string NomeParametro = "PAR_QTD";
        private const string TypeCommentsGrauteHorizontal = "Graute Horizontal";

        // ===== Regras =====
        private const string PrefixoAviso = "AVISO";
        private const double ToleranciaAlinhamento = 0.999;

        private const string AvisoSemElevacao = "AVISO: não aparece em nenhuma elevação";

        /// <summary>
        /// Preenche o PAR_QTD de cada graute horizontal com o nome da elevação em que ele aparece.
        /// Se o elemento já estiver numa transação aberta pelo chamador, usa ela; senão abre a sua.
        /// </summary>
        public static ResumoParQtd Executar(Document doc)
        {
            var resumo = new ResumoParQtd();

            if (!doc.IsModifiable)
            {
                using var transacao = new Transaction(doc, "Preencher PAR_QTD");
                transacao.Start();
                Processar(doc, resumo);
                transacao.Commit();
            }
            else
            {
                Processar(doc, resumo);
            }

            return resumo;
        }

        private static void Processar(Document doc, ResumoParQtd resumo)
        {
            var elevacoes = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSection))
                .Cast<ViewSection>()
                .Where(v => v.ViewType == ViewType.Elevation && !v.IsTemplate)
                .ToList();

            resumo.Elevacoes = elevacoes.Count;

            var cacheTipos = new Dictionary<ElementId, bool>();
            var elevacoesPorElemento = MapearElevacoesPorElemento(doc, elevacoes, cacheTipos, resumo);

            var todos = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Where(e => EhGrauteHorizontal(doc, e, cacheTipos))
                .ToList();

            resumo.Total = todos.Count;

            foreach (var elemento in todos)
            {
                try
                {
                    ProcessarElemento(elemento, elevacoesPorElemento, resumo);
                }
                catch (Exception ex)
                {
                    resumo.Falhas.Add($"Elemento {elemento.Id}: {ex.Message}");
                }
            }
        }

        private static Dictionary<ElementId, HashSet<string>> MapearElevacoesPorElemento(
            Document doc,
            List<ViewSection> elevacoes,
            Dictionary<ElementId, bool> cacheTipos,
            ResumoParQtd resumo)
        {
            var mapa = new Dictionary<ElementId, HashSet<string>>();

            foreach (var elevacao in elevacoes)
            {
                try
                {
                    // O coletor com o id da vista devolve só o que a vista não oculta.
                    var visiveis = new FilteredElementCollector(doc, elevacao.Id)
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .WhereElementIsNotElementType()
                        .OfType<FamilyInstance>()
                        .Where(fi => EhGrauteHorizontal(doc, fi, cacheTipos))
                        .Where(fi => EstaApontandoParaAVista(fi, elevacao))
                        .ToList();

                    foreach (var fi in visiveis)
                    {
                        if (!mapa.TryGetValue(fi.Id, out var nomes))
                        {
                            nomes = new HashSet<string>();
                            mapa[fi.Id] = nomes;
                        }

                        nomes.Add(elevacao.Name);
                    }
                }
                catch (Exception ex)
                {
                    resumo.Falhas.Add($"Elevação {elevacao.Name}: {ex.Message}");
                }
            }

            return mapa;
        }

        private static void ProcessarElemento(
            Element elemento,
            Dictionary<ElementId, HashSet<string>> elevacoesPorElemento,
            ResumoParQtd resumo)
        {
            var parametro = elemento.LookupParameter(NomeParametro);

            if (parametro == null || parametro.IsReadOnly || parametro.StorageType != StorageType.String)
            {
                resumo.SemParametro++;
                return;
            }

            string? novoValor;
            Action contar;

            if (!elevacoesPorElemento.TryGetValue(elemento.Id, out var nomes) || nomes.Count == 0)
            {
                novoValor = AvisoSemElevacao;
                contar = () => resumo.SemElevacao++;
            }
            else if (nomes.Count == 1)
            {
                novoValor = nomes.First();
                contar = () => resumo.NomeUnico++;
            }
            else
            {
                var lista = string.Join(", ", nomes.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
                novoValor = $"{PrefixoAviso}: aparece em {nomes.Count} elevações ({lista})";
                contar = () => resumo.ComAviso++;
            }

            if (novoValor != null && (parametro.AsString() ?? string.Empty) != novoValor)
            {
                if (parametro.Set(novoValor))
                    resumo.Alterados++;
                else
                    resumo.Falhas.Add($"Elemento {elemento.Id}: o Revit não aceitou o valor de {NomeParametro}");
            }

            contar();
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

        // Mesma checagem do Aplicador de Tags: paralelo à direção da vista, nos dois sentidos.
        private static bool EstaApontandoParaAVista(FamilyInstance elemento, View vista)
        {
            var frente = elemento.FacingOrientation.Normalize();
            var direcaoDaVista = vista.ViewDirection.Normalize();

            return Math.Abs(frente.DotProduct(direcaoDaVista)) > ToleranciaAlinhamento;
        }
    }
}