using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class UnirElementosEstruturaisCommand : IExternalCommand
    {
        // ===== TypeComments (só pilar e viga são filtrados) =====
        private const string TypeCommentsPilar = "Pilar de concreto";
        private const string TypeCommentsViga = "Viga de concreto";

        // ===== Folga usada na busca de vizinhos por caixa envolvente =====
        private const double ToleranciaCm = 0.5;

        // ===== Quantas linhas listar no resumo final (falhas e avisos) =====
        private const int MaximoDeFalhasExibidas = 15;

        // Hierarquia: quanto MENOR o valor, MAIOR a prioridade.
        // O de menor valor corta o de maior valor (Conexão > Pilar > Viga > Piso).
        private enum TipoEstrutural
        {
            Conexao = 0,
            Pilar = 1,
            Viga = 2,
            Piso = 3
        }

        private class Resumo
        {
            public int ParesAnalisados;
            public int NovasUnioes;
            public int OrdensCorrigidas;
            public int JaCorretos;
            public int ConexoesNaoUnidas;
            public string? PrimeiroMotivoConexoes;
            public List<string> Falhas = new List<string>();
            public List<string> AvisosDoRevit = new List<string>();
        }

        // Lê os avisos que o Revit emite ao confirmar a transação e os remove da tela
        // (só avisos; erros de verdade não são tocados). O texto vai para o resumo final.
        private class CapturadorDeAvisos : IFailuresPreprocessor
        {
            public List<string> Mensagens = new List<string>();

            public FailureProcessingResult PreprocessFailures(FailuresAccessor falhas)
            {
                foreach (var mensagem in falhas.GetFailureMessages())
                {
                    var ids = string.Join(", ", mensagem.GetFailingElementIds().Select(i => i.Value));
                    Mensagens.Add($"{mensagem.GetDescriptionText()} (elementos: {ids})");

                    if (mensagem.GetSeverity() == FailureSeverity.Warning)
                        falhas.DeleteWarning(mensagem);
                }

                return FailureProcessingResult.Continue;
            }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!ControleExpiracao.EstaValido())
                {
                    TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
                    return Result.Failed;
                }

                var doc = commandData.Application.ActiveUIDocument.Document;

                if (!GarantirDocumentoSalvo(doc))
                    return Result.Cancelled;

                var elementos = ColetarElementos(doc);

                if (elementos.Count == 0)
                {
                    TaskDialog.Show("Unir Elementos Estruturais", "Nenhum elemento encontrado.");
                    return Result.Succeeded;
                }

                var resumo = new Resumo();
                var capturador = new CapturadorDeAvisos();

                using (var transaction = new Transaction(doc, "Unir elementos estruturais"))
                {
                    transaction.Start();

                    var opcoes = transaction.GetFailureHandlingOptions();
                    opcoes.SetFailuresPreprocessor(capturador);
                    transaction.SetFailureHandlingOptions(opcoes);

                    ProcessarPares(doc, elementos, resumo);
                    transaction.Commit();
                }

                resumo.AvisosDoRevit.AddRange(capturador.Mensagens);

                MostrarResumo(elementos, resumo);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JFS Forge - Erro", $"Não foi possível unir os elementos:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private bool GarantirDocumentoSalvo(Document doc)
        {
            // Projeto nunca salvo: não existe caminho para o Save() usar.
            if (string.IsNullOrEmpty(doc.PathName))
            {
                TaskDialog.Show(
                    "Unir Elementos Estruturais",
                    "O projeto ainda não foi salvo. Salve o arquivo (Salvar como) antes de rodar este comando.");
                return false;
            }

            // Nada alterado desde o último salvamento: já existe um ponto de retorno.
            if (!doc.IsModified)
                return true;

            var dialogo = new TaskDialog("Unir Elementos Estruturais")
            {
                MainInstruction = "Salvar o projeto antes de continuar?",
                MainContent =
                    "Este comando altera muitos elementos de uma vez. " +
                    "O projeto será salvo agora para você ter um ponto de retorno.",
                CommonButtons = TaskDialogCommonButtons.Cancel
            };
            dialogo.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Salvar e continuar");
            dialogo.DefaultButton = TaskDialogResult.CommandLink1;

            if (dialogo.Show() != TaskDialogResult.CommandLink1)
                return false;

            try
            {
                doc.Save();
                return true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Unir Elementos Estruturais", $"Não foi possível salvar o projeto:\n{ex.Message}");
                return false;
            }
        }

        private Dictionary<ElementId, TipoEstrutural> ColetarElementos(Document doc)
        {
            var elementos = new Dictionary<ElementId, TipoEstrutural>();

            Adicionar(doc, elementos, BuiltInCategory.OST_StructConnections, TipoEstrutural.Conexao, null);
            Adicionar(doc, elementos, BuiltInCategory.OST_StructuralColumns, TipoEstrutural.Pilar, TypeCommentsPilar);
            Adicionar(doc, elementos, BuiltInCategory.OST_StructuralFraming, TipoEstrutural.Viga, TypeCommentsViga);
            Adicionar(doc, elementos, BuiltInCategory.OST_Floors, TipoEstrutural.Piso, null);

            return elementos;
        }

        private void Adicionar(
            Document doc,
            Dictionary<ElementId, TipoEstrutural> destino,
            BuiltInCategory categoria,
            TipoEstrutural tipo,
            string? typeCommentsEsperado)
        {
            var coletados = new FilteredElementCollector(doc)
                .OfCategory(categoria)
                .WhereElementIsNotElementType();

            foreach (var elemento in coletados)
            {
                if (typeCommentsEsperado != null && ObterTypeComments(doc, elemento) != typeCommentsEsperado)
                    continue;

                destino[elemento.Id] = tipo;
            }
        }

        private string? ObterTypeComments(Document doc, Element elemento)
        {
            var tipoElemento = doc.GetElement(elemento.GetTypeId()) as ElementType;
            return tipoElemento?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS)?.AsString();
        }

        private void ProcessarPares(Document doc, Dictionary<ElementId, TipoEstrutural> elementos, Resumo resumo)
        {
            var tolerancia = UnitUtils.ConvertToInternalUnits(ToleranciaCm, UnitTypeId.Centimeters);

            var filtroCategorias = new ElementMulticategoryFilter(new List<BuiltInCategory>
            {
                BuiltInCategory.OST_StructConnections,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Floors
            });

            var paresProcessados = new HashSet<(long, long)>();

            foreach (var (idA, tipoA) in elementos)
            {
                var elementoA = doc.GetElement(idA);
                var idsVizinhos = ObterIdsVizinhos(doc, elementoA, tipoA, elementos, filtroCategorias, tolerancia, resumo);

                foreach (var idB in idsVizinhos)
                {
                    var chave = idA.Value < idB.Value ? (idA.Value, idB.Value) : (idB.Value, idA.Value);

                    if (!paresProcessados.Add(chave))
                        continue;

                    ProcessarPar(doc, elementoA, tipoA, doc.GetElement(idB), elementos[idB], resumo);
                }
            }
        }

        private HashSet<ElementId> ObterIdsVizinhos(
            Document doc,
            Element elementoA,
            TipoEstrutural tipoA,
            Dictionary<ElementId, TipoEstrutural> elementos,
            ElementMulticategoryFilter filtroCategorias,
            double tolerancia,
            Resumo resumo)
        {
            var ids = new HashSet<ElementId>();

            // Já unidos entram sempre: a geometria deles já está cortada,
            // então o filtro de interseção pode não encontrá-los.
            foreach (var idUnido in JoinGeometryUtils.GetJoinedElements(doc, elementoA))
            {
                if (elementos.ContainsKey(idUnido))
                    ids.Add(idUnido);
            }

            var caixa = elementoA.get_BoundingBox(null);

            if (caixa == null)
                return ids;

            var contorno = new Outline(
                caixa.Min - new XYZ(tolerancia, tolerancia, tolerancia),
                caixa.Max + new XYZ(tolerancia, tolerancia, tolerancia));

            try
            {
                // Conexões não aceitam ElementIntersectsElementFilter como elemento
                // de referência; para elas a interseção é testada a partir dos sólidos.
                IEnumerable<ElementId> encontrados = tipoA == TipoEstrutural.Conexao
                    ? BuscarPorSolidos(doc, elementoA, filtroCategorias, contorno, resumo)
                    : new FilteredElementCollector(doc)
                        .WherePasses(filtroCategorias)
                        .WhereElementIsNotElementType()
                        .WherePasses(new BoundingBoxIntersectsFilter(contorno))
                        .WherePasses(new ElementIntersectsElementFilter(elementoA))
                        .ToElementIds();

                foreach (var id in encontrados)
                {
                    if (id != elementoA.Id && elementos.ContainsKey(id))
                        ids.Add(id);
                }

                // Conexão x conexão: além do volume em comum, tenta também as outras
                // conexões cujas caixas envolventes se cruzam (inclui as que só se encostam).
                if (tipoA == TipoEstrutural.Conexao)
                {
                    var outrasConexoes = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructConnections)
                        .WhereElementIsNotElementType()
                        .WherePasses(new BoundingBoxIntersectsFilter(contorno))
                        .ToElementIds();

                    foreach (var id in outrasConexoes)
                    {
                        if (id != elementoA.Id && elementos.ContainsKey(id))
                            ids.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                resumo.Falhas.Add($"{tipoA} {elementoA.Id}: não foi possível buscar vizinhos ({ex.Message})");
            }

            return ids;
        }

        private HashSet<ElementId> BuscarPorSolidos(
            Document doc,
            Element elemento,
            ElementMulticategoryFilter filtroCategorias,
            Outline contorno,
            Resumo resumo)
        {
            var ids = new HashSet<ElementId>();
            var solidos = ObterSolidos(elemento);

            if (solidos.Count == 0)
            {
                resumo.Falhas.Add($"Conexão {elemento.Id}: sem geometria sólida para testar interseção");
                return ids;
            }

            foreach (var solido in solidos)
            {
                var encontrados = new FilteredElementCollector(doc)
                    .WherePasses(filtroCategorias)
                    .WhereElementIsNotElementType()
                    .WherePasses(new BoundingBoxIntersectsFilter(contorno))
                    .WherePasses(new ElementIntersectsSolidFilter(solido))
                    .ToElementIds();

                foreach (var id in encontrados)
                    ids.Add(id);
            }

            return ids;
        }

        private List<Solid> ObterSolidos(Element elemento)
        {
            var solidos = new List<Solid>();

            var opcoes = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = false
            };

            var geometria = elemento.get_Geometry(opcoes);

            if (geometria == null)
                return solidos;

            foreach (var objeto in geometria)
            {
                if (objeto is Solid solido && solido.Volume > 0)
                {
                    solidos.Add(solido);
                }
                else if (objeto is GeometryInstance instancia)
                {
                    // GetInstanceGeometry devolve os sólidos já na posição real do modelo.
                    foreach (var interno in instancia.GetInstanceGeometry())
                    {
                        if (interno is Solid solidoInterno && solidoInterno.Volume > 0)
                            solidos.Add(solidoInterno);
                    }
                }
            }

            return solidos;
        }

        private void ProcessarPar(
            Document doc,
            Element a, TipoEstrutural tipoA,
            Element b, TipoEstrutural tipoB,
            Resumo resumo)
        {
            resumo.ParesAnalisados++;

            using (var subTransaction = new SubTransaction(doc))
            {
                try
                {
                    subTransaction.Start();

                    bool novaUniao = false;

                    if (!JoinGeometryUtils.AreElementsJoined(doc, a, b))
                    {
                        JoinGeometryUtils.JoinGeometry(doc, a, b);
                        novaUniao = true;
                    }

                    bool ordemCorrigida = false;

                    // Mesmo tipo: tanto faz a ordem, não confere nada.
                    if (tipoA != tipoB)
                    {
                        var (cortador, cortado) = tipoA < tipoB ? (a, b) : (b, a);

                        if (!JoinGeometryUtils.IsCuttingElementInJoin(doc, cortador, cortado))
                        {
                            JoinGeometryUtils.SwitchJoinOrder(doc, a, b);
                            ordemCorrigida = true;
                        }
                    }

                    subTransaction.Commit();

                    if (novaUniao) resumo.NovasUnioes++;
                    if (ordemCorrigida) resumo.OrdensCorrigidas++;
                    if (!novaUniao && !ordemCorrigida) resumo.JaCorretos++;
                }
                catch (Exception ex)
                {
                    if (subTransaction.GetStatus() == TransactionStatus.Started)
                        subTransaction.RollBack();

                    // Conexão x conexão que o Revit recusar não vai para a lista de falhas:
                    // só entra na contagem, com o motivo do primeiro caso.
                    if (tipoA == TipoEstrutural.Conexao && tipoB == TipoEstrutural.Conexao)
                    {
                        resumo.ConexoesNaoUnidas++;
                        resumo.PrimeiroMotivoConexoes ??= ex.Message;
                    }
                    else
                    {
                        resumo.Falhas.Add($"{tipoA} {a.Id} x {tipoB} {b.Id}: {ex.Message}");
                    }
                }
            }
        }

        private void MostrarResumo(Dictionary<ElementId, TipoEstrutural> elementos, Resumo resumo)
        {
            int Contar(TipoEstrutural tipo) => elementos.Values.Count(t => t == tipo);

            var texto =
                $"Elementos processados: {elementos.Count}\n" +
                $"   Blocos de fundação: {Contar(TipoEstrutural.Conexao)}\n" +
                $"   Pilares: {Contar(TipoEstrutural.Pilar)}\n" +
                $"   Vigas: {Contar(TipoEstrutural.Viga)}\n" +
                $"   Lajes: {Contar(TipoEstrutural.Piso)}\n\n" +
                $"Pares analisados: {resumo.ParesAnalisados}\n" +
                $"   Novas uniões: {resumo.NovasUnioes}\n" +
                $"   Ordem corrigida: {resumo.OrdensCorrigidas}\n" +
                $"   Já estavam corretos: {resumo.JaCorretos}";

            if (resumo.ConexoesNaoUnidas > 0)
            {
                texto += $"\n   Conexão x Conexão não unidas: {resumo.ConexoesNaoUnidas}";
                texto += $"\n   Motivo informado pelo Revit (1º caso): {resumo.PrimeiroMotivoConexoes}";
            }

            if (resumo.AvisosDoRevit.Count > 0)
            {
                texto += "\n\nAvisos do Revit durante a união:\n" +
                    string.Join("\n", resumo.AvisosDoRevit.Distinct().Take(MaximoDeFalhasExibidas));
            }

            texto += $"\n\nFalhas: {resumo.Falhas.Count}";

            if (resumo.Falhas.Count > 0)
            {
                texto += "\n\n" + string.Join("\n", resumo.Falhas.Take(MaximoDeFalhasExibidas));

                if (resumo.Falhas.Count > MaximoDeFalhasExibidas)
                    texto += $"\n... e mais {resumo.Falhas.Count - MaximoDeFalhasExibidas}";
            }

            TaskDialog.Show("Unir Elementos Estruturais", texto);
        }
    }
}