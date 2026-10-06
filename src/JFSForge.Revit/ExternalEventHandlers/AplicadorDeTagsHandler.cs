using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.UI.Windows;

namespace JFSForge.Revit.ExternalEventHandlers
{
    internal class AplicadorDeTagsHandler : IExternalEventHandler
    {
        // ===== Nomes de famílias de tag (ajustar aqui se o nome mudar no Revit) =====
        private const string TagJanela = "JANELA_ELEVAÇÃO";
        private const string TagPorta = "PORTA_ELEVAÇÃO";
        private const string TagBarraHorizontalPadrao = "TAG_HORIZ_1";
        private const string TagBarraHorizontalCurta = "TAG_HORIZ_2";
        private const string TagViga = "VIGA_ELEVAÇÃO";
        private const string TagPilar = "PILAR_ELEVAÇÃO";

        // ===== Valores de TypeComments usados para filtrar cada categoria =====
        private const string TypeCommentsEstrutural = "Estrutural";
        private const string TypeCommentsBarraHorizontal = "Detalhe barra horizontal";
        private const string TypeCommentsViga = "Viga de concreto";
        private const string TypeCommentsPilar = "Pilar de concreto";

        // ===== Regra de negócio: comprimento mínimo para trocar a tag de barra =====
        private const double ComprimentoMinimoBarraCm = 120.0;

        // ===== Estado recebido da UI =====
        public string TipoDeVista { get; set; } = string.Empty;
        public bool ApenasVistaAtiva { get; set; }
        public bool TagJanelaAtiva { get; set; }
        public bool TagPortaAtiva { get; set; }
        public bool TagBarraHorizontalAtiva { get; set; }
        public bool TagVigaAtiva { get; set; }
        public bool TagPilarAtiva { get; set; }
        public ReactWindow? Janela { get; set; }

        public void Execute(UIApplication app)
        {
            switch (TipoDeVista)
            {
                case "elevacoes":
                    AplicarTagsEmElevacoes(app);
                    break;
            }
        }

        public string GetName()
        {
            return "Aplicador de Tags";
        }

        private void AplicarTagsEmElevacoes(UIApplication app)
        {
            var document = app.ActiveUIDocument.Document;

            using (TransactionGroup transactionGroup = new TransactionGroup(document, "Aplicar Tags - Elevações"))
            {
                transactionGroup.Start();

                var viewSections = ObterViewSections(app, ViewType.Elevation);

                foreach (var viewSection in viewSections)
                {
                    if (TagJanelaAtiva)
                    {
                        AplicarTagsPorCategoria(
                            document, viewSection, BuiltInCategory.OST_Windows, TypeCommentsEstrutural,
                            filtrarPorAlinhamento: true,
                            criarTag: (doc, view, elemento) => CriarTagJanela(doc, view, elemento));
                    }

                    if (TagPortaAtiva)
                    {
                        AplicarTagsPorCategoria(
                            document, viewSection, BuiltInCategory.OST_Doors, TypeCommentsEstrutural,
                            filtrarPorAlinhamento: true,
                            criarTag: (doc, view, elemento) => CriarTagPorta(doc, view, elemento));
                    }

                    if (TagBarraHorizontalAtiva)
                    {
                        AplicarTagsPorCategoria(
                            document, viewSection, BuiltInCategory.OST_GenericModel, TypeCommentsBarraHorizontal,
                            filtrarPorAlinhamento: true,
                            criarTag: (doc, view, elemento) => CriarTagBarraHorizontal(doc, view, elemento));
                    }

                    if (TagVigaAtiva)
                    {
                        AplicarTagsPorCategoria(
                            document, viewSection, BuiltInCategory.OST_StructuralFraming, TypeCommentsViga,
                            filtrarPorAlinhamento: false,
                            criarTag: (doc, view, elemento) => CriarTagSimples(doc, view, elemento, TagViga));
                    }

                    if (TagPilarAtiva)
                    {
                        AplicarTagsPorCategoria(
                            document, viewSection, BuiltInCategory.OST_StructuralColumns, TypeCommentsPilar,
                            filtrarPorAlinhamento: false,
                            criarTag: (doc, view, elemento) => CriarTagSimples(doc, view, elemento, TagPilar));
                    }
                }

                transactionGroup.Assimilate();
            }

            Janela?.EnviarMensagemParaReact(new { tipo = "concluido" });
        }

        private List<ViewSection> ObterViewSections(UIApplication app, ViewType tipoDeVista)
        {
            var document = app.ActiveUIDocument.Document;

            if (ApenasVistaAtiva)
            {
                if (app.ActiveUIDocument.ActiveView is ViewSection viewSectionAtiva)
                    return new List<ViewSection> { viewSectionAtiva };

                return new List<ViewSection>();
            }

            return new FilteredElementCollector(document)
                .OfClass(typeof(ViewSection))
                .Cast<ViewSection>()
                .Where(view => view.ViewType == tipoDeVista && !view.IsTemplate)
                .ToList();
        }

        private void AplicarTagsPorCategoria(
            Document document,
            ViewSection viewSection,
            BuiltInCategory categoria,
            string typeCommentsEsperado,
            bool filtrarPorAlinhamento,
            Action<Document, ViewSection, Element> criarTag)
        {
            var elementosVisiveis = ObterElementosVisiveis(document, viewSection, categoria, typeCommentsEsperado);

            foreach (var elemento in elementosVisiveis)
            {
                if (filtrarPorAlinhamento && !EstaAlinhadoComAVista(elemento, viewSection))
                    continue;

                if (JaPossuiTag(document, elemento, viewSection))
                    continue;

                criarTag(document, viewSection, elemento);
            }
        }

        private List<Element> ObterElementosVisiveis(Document document, ViewSection viewSection, BuiltInCategory categoria, string typeCommentsEsperado)
        {
            return new FilteredElementCollector(document, viewSection.Id)
                .OfCategory(categoria)
                .WhereElementIsNotElementType()
                .Where(elemento =>
                {
                    var tipoElemento = document.GetElement(elemento.GetTypeId()) as ElementType;
                    var parametroTypeComments = tipoElemento?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS);
                    return parametroTypeComments != null && parametroTypeComments.AsString() == typeCommentsEsperado;
                })
                .ToList();
        }

        private bool JaPossuiTag(Document document, Element elemento, ViewSection viewSection)
        {
            var idsComTag = new FilteredElementCollector(document, viewSection.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .SelectMany(tag => tag.GetTaggedLocalElementIds())
                .ToList();

            return idsComTag.Contains(elemento.Id);
        }

        private bool EstaAlinhadoComAVista(Element elemento, ViewSection viewSection)
        {
            if (elemento is not FamilyInstance familyInstance)
                return false;

            var vetorFrente = familyInstance.FacingOrientation.Normalize();
            var vetorDaVista = viewSection.ViewDirection.Normalize();
            var produtoEscalar = vetorFrente.DotProduct(vetorDaVista);

            return Math.Abs(produtoEscalar) > 0.999;
        }

        private void CriarTagJanela(Document document, ViewSection viewSection, Element elemento)
        {
            if (elemento.Location is not LocationPoint locationPoint)
                return;

            var alturaBruta = document.GetElement(elemento.GetTypeId()).LookupParameter("Altura bruta").AsDouble();
            var pontoZ = locationPoint.Point.Z - (68.0 / 38.48) + (alturaBruta / 2);
            var ponto = new XYZ(locationPoint.Point.X, locationPoint.Point.Y, pontoZ);

            CriarTagComPontoEspecifico(document, viewSection, elemento, ponto, TagJanela);
        }

        private void CriarTagPorta(Document document, ViewSection viewSection, Element elemento)
        {
            if (elemento.Location is not LocationPoint locationPoint)
                return;

            var alturaBruta = document.GetElement(elemento.GetTypeId()).LookupParameter("Altura bruta").AsDouble();
            var pontoZ = locationPoint.Point.Z + (alturaBruta / 2);
            var ponto = new XYZ(locationPoint.Point.X, locationPoint.Point.Y, pontoZ);

            CriarTagComPontoEspecifico(document, viewSection, elemento, ponto, TagPorta);
        }

        private void CriarTagBarraHorizontal(Document document, ViewSection viewSection, Element elemento)
        {
            if (elemento.Location is not LocationPoint locationPoint)
                return;

            var compReto = elemento.LookupParameter("C_RETO").AsDouble();
            var dobra35A = elemento.LookupParameter("DOBRA_35_A").AsInteger();
            var dobra35B = elemento.LookupParameter("DOBRA_35_B").AsInteger();

            var comprimentoMinimoInterno = UnitUtils.ConvertToInternalUnits(ComprimentoMinimoBarraCm, UnitTypeId.Centimeters);
            var usarTagCurta = (dobra35A == 1 || dobra35B == 1) && compReto < comprimentoMinimoInterno;

            var nomeDaTag = usarTagCurta ? TagBarraHorizontalCurta : TagBarraHorizontalPadrao;

            CriarTagComPontoEspecifico(document, viewSection, elemento, locationPoint.Point, nomeDaTag);
        }

        private void CriarTagSimples(Document document, ViewSection viewSection, Element elemento, string nomeDaTag)
        {
            XYZ? ponto = null;

            if (elemento.Location is LocationPoint locationPoint)
            {
                ponto = locationPoint.Point;
            }
            else if (elemento.Location is LocationCurve locationCurve)
            {
                var curva = locationCurve.Curve;
                ponto = curva.Evaluate(0.5, true);
            }

            if (ponto == null)
                return;

            CriarTagComPontoEspecifico(document, viewSection, elemento, ponto, nomeDaTag);
        }

        private void CriarTagComPontoEspecifico(Document document, ViewSection viewSection, Element elemento, XYZ ponto, string nomeDaTag)
        {
            var tagTypeId = ObterIdDaTagPorNome(document, nomeDaTag);

            if (tagTypeId == null)
                return;

            using (Transaction transaction = new Transaction(document, "Adicionar Tag"))
            {
                transaction.Start();

                IndependentTag.Create(
                    document,
                    tagTypeId,
                    viewSection.Id,
                    new Reference(elemento),
                    false,
                    TagOrientation.Horizontal,
                    ponto);

                transaction.Commit();
            }
        }

        private ElementId? ObterIdDaTagPorNome(Document document, string nomeDaTag)
        {
            var tipoDeTag = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .FirstOrDefault(simbolo => simbolo.Name == nomeDaTag);

            return tipoDeTag?.Id;
        }
    }
}