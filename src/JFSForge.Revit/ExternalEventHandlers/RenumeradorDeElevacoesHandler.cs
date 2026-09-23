using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JFSForge.UI.Windows;

namespace JFSForge.Revit.ExternalEventHandlers
{
    internal class RenumeradorDeElevacoesHandler : IExternalEventHandler
    {
        public string Acao { get; set; } = string.Empty;
        public string Prefixo { get; set; } = "PAR";
        public string ValorInicial { get; set; } = "1";
        public ReactWindow? Janela { get; set; }

        private IList<Reference> _referenciasSelecionadas = new List<Reference>();

        public void Execute(UIApplication app)
        {
            if (Acao == "selecionar")
            {
                ExecutarSelecao(app);
            }
            else if (Acao == "processar")
            {
                ExecutarProcessamento(app);
            }
        }

        private void ExecutarSelecao(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                var filtro = new ElevationMarkerSelectionFilter();

                _referenciasSelecionadas = uidoc.Selection.PickObjects(
                    ObjectType.Element, filtro, "Selecione as elevações para renumerar");

                Janela?.EnviarMensagemParaReact(new
                {
                    tipo = "selecaoConcluida",
                    quantidade = _referenciasSelecionadas.Count
                });

                Janela?.Dispatcher.Invoke(() => Janela.Activate());
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                Janela?.EnviarMensagemParaReact(new { tipo = "selecaoCancelada" });
                Janela?.Dispatcher.Invoke(() => Janela.Activate());
            }
        }

        private void ExecutarProcessamento(UIApplication app)
        {
            var doc = app.ActiveUIDocument.Document;
            var horizontais = new List<(ViewSection ViewSection, double CoordinateX, double CoordinateY)>();
            var verticais = new List<(ViewSection ViewSection, double CoordinateX, double CoordinateY)>();
            var total = _referenciasSelecionadas.Count;

            using (Transaction transaction = new Transaction(doc, "Renumeração de elevações"))
            {
                try
                {
                    transaction.Start();

                    int processadas = 0;
                    foreach (var reference in _referenciasSelecionadas)
                    {
                        var element = doc.GetElement(reference);
                        processadas++;

                        if (element is not ElevationMarker elevationMarker)
                            continue;

                        var box = elevationMarker.get_BoundingBox(app.ActiveUIDocument.ActiveView);
                        var pointLocation = (box.Min + box.Max) / 2;
                        var coordenadaX = Math.Round(pointLocation.X, 1);
                        var coordenadaY = Math.Round(pointLocation.Y, 1);

                        var indiceElevacao = 0;
                        ViewSection? viewSection = null;

                        for (int i = 0; i < 4; i++)
                        {
                            var viewSectionId = elevationMarker.GetViewId(i);

                            if (viewSectionId != null && viewSectionId != ElementId.InvalidElementId)
                            {
                                viewSection = doc.GetElement(viewSectionId) as ViewSection;
                                indiceElevacao = i;
                            }
                        }

                        if (viewSection == null)
                            continue;

                        if (indiceElevacao == 1 || indiceElevacao == 3)
                        {
                            horizontais.Add((viewSection, coordenadaX, coordenadaY));
                        }
                        else if (indiceElevacao == 0 || indiceElevacao == 2)
                        {
                            verticais.Add((viewSection, coordenadaX, coordenadaY));
                        }
                    }

                    int renumTemp = 0;
                    foreach (var item in horizontais)
                    {
                        item.ViewSection.Name = $"{renumTemp}";
                        renumTemp++;
                    }
                    foreach (var item in verticais)
                    {
                        item.ViewSection.Name = $"{renumTemp}";
                        renumTemp++;
                    }

                    OrdenarPorPosicao(horizontais, ehHorizontal: true);
                    OrdenarPorPosicao(verticais, ehHorizontal: false);

                    int valorAtual = int.Parse(ValorInicial);

                    foreach (var item in horizontais)
                    {
                        item.ViewSection.Name = $"{Prefixo}{valorAtual}";
                        valorAtual++;
                    }

                    foreach (var item in verticais)
                    {
                        item.ViewSection.Name = $"{Prefixo}{valorAtual}";
                        valorAtual++;
                    }

                    transaction.Commit();

                    var totalRenumerado = horizontais.Count + verticais.Count;

                    Janela?.Dispatcher.Invoke(() => Janela.Close());

                    TaskDialog.Show("Renumerador de Elevações", $"Renumeração concluída. {totalRenumerado} elevação(ões) renumerada(s).");
                }
                catch (Exception e)
                {
                    transaction.RollBack();
                    TaskDialog.Show("Erro na renumeração", $"Erro: {e.Message}");
                }
            }
        }

        private void OrdenarPorPosicao(List<(ViewSection ViewSection, double CoordinateX, double CoordinateY)> elementos, bool ehHorizontal)
        {
            elementos.Sort((a, b) =>
            {
                if (ehHorizontal)
                {
                    int comparacaoY = b.CoordinateY.CompareTo(a.CoordinateY);
                    return comparacaoY != 0 ? comparacaoY : a.CoordinateX.CompareTo(b.CoordinateX);
                }
                else
                {
                    int comparacaoX = a.CoordinateX.CompareTo(b.CoordinateX);
                    return comparacaoX != 0 ? comparacaoX : a.CoordinateY.CompareTo(b.CoordinateY);
                }
            });
        }

        public string GetName()
        {
            return "Renumeração de elevações";
        }

        private class ElevationMarkerSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is ElevationMarker;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }
    }
}