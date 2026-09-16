using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JFSForge.Revit.Entities;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GrauteadorExternalCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uiApp = commandData.Application;
                var app = uiApp.Application;
                var uidoc = uiApp.ActiveUIDocument;
                var doc = uidoc.Document;

                TaskDialog.Show("Info", "Selecione os blocos para criar Septos Temporários.");

                var blocosReferences = uidoc.Selection.PickObjects(ObjectType.Element, new GrauteadorBlocoSelectionFilter(), "Selecione os blocos para serem grauteados");
                var blocos = blocosReferences.Select(r => new BlocoRevit(doc.GetElement(r) as FamilyInstance)).ToList();

                FamilySymbol? detailSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_DetailComponents)
                .WherePasses(new ElementParameterFilter(
                    new FilterStringRule(
                        new ParameterValueProvider(
                            new ElementId(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)),
                                new FilterStringEquals(), "SEPTO"))).FirstOrDefault() as FamilySymbol;

                if (detailSymbol == null)
                {
                    TaskDialog.Show("Error!", "Família de SEPTO não carregada!");
                    return Result.Failed;
                }

                if (!detailSymbol.IsActive)
                {
                    detailSymbol.Activate();
                }

                FamilySymbol? pilarete2BarrasSymbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WherePasses(new ElementParameterFilter(
                    new FilterStringRule(
                        new ParameterValueProvider(
                            new ElementId(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)),
                                new FilterStringEquals(), "GRAUTE VERTICAL 2 BARRAS")))
                                .FirstOrDefault() as FamilySymbol;

                if (pilarete2BarrasSymbol == null)
                {
                    TaskDialog.Show("Error!", "Família de GRAUTE VERTICAL 2 BARRAS não carregada!");
                    return Result.Failed;
                }

                if (!pilarete2BarrasSymbol.IsActive)
                {
                    pilarete2BarrasSymbol.Activate();
                }

                using (var tg = new TransactionGroup(doc, "Criar Septos Temporários"))
                {
                    try
                    {
                        tg.Start();

                        foreach (var bloco in blocos)
                        {
                            bloco.CriarSeptosTemporarios(doc, detailSymbol);
                        }

                        TaskDialog.Show("Info", "Septos temporários criados. Agora selecione os septos para serem grauteados.");

                        var septosParaGrautearReferences = uidoc.Selection.PickObjects(ObjectType.Element, new SeptoSelectionFilter(), "Selecione os septos para serem grauteados");
                        var septosParaGrautear = septosParaGrautearReferences.Select(r => new Septo(doc.GetElement(r) as FamilyInstance)).ToList();

                        foreach (var septo in septosParaGrautear)
                        {
                            XYZ septoBasisX = septo.SeptoTransform.BasisX;
                            XYZ xAxis = XYZ.BasisX;
                            double angle = Math.Atan2(septoBasisX.Y, septoBasisX.X);

                            using (Transaction tx = new Transaction(doc, "Criar Pilarete"))
                            {
                                tx.Start();

                                tx.SetFailureHandlingOptions(
                                    tx.GetFailureHandlingOptions()
                                     .SetDelayedMiniWarnings(true));

                                XYZ pointInstance = new XYZ(septo.SeptoOrigin.X, septo.SeptoOrigin.Y, blocos[0].BlocoTransform.Origin.Z);
                                FamilyInstance pilareteInstance = doc.Create.NewFamilyInstance(pointInstance, pilarete2BarrasSymbol, StructuralType.NonStructural);
                                pilareteInstance.LookupParameter("SEPTO").Set(septo.Tipo);

                                if (Math.Abs(angle) > 1e-9)
                                {
                                    Line axix = Line.CreateBound(septo.SeptoOrigin, septo.SeptoOrigin + XYZ.BasisZ);
                                    ElementTransformUtils.RotateElement(doc, pilareteInstance.Id, axix, angle);
                                }

                                tx.Commit();
                            }
                        }

                        var septosCollector = new FilteredElementCollector(doc)
                                        .OfCategory(BuiltInCategory.OST_DetailComponents)
                                        .WhereElementIsNotElementType()
                                        .OfType<FamilyInstance>()
                                        .Where(fi => fi.Name == "SEPTO");

                        using (Transaction deleteTx = new Transaction(doc, "Deletar Septos Temporários"))
                        {
                            deleteTx.Start();
                            doc.Delete(septosCollector.Select(s => s.Id).ToList());
                            deleteTx.Commit();
                        }

                        tg.Assimilate();
                    }
                    catch (Exception e)
                    {
                        tg.RollBack();
                        TaskDialog.Show("Error", $"Erro ao criar septos temporários: {e.Message}");
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return Result.Failed;
            }
        }
    }

    public class SeptoSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Name == "SEPTO";
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class GrauteadorBlocoSelectionFilter : ISelectionFilter
    {
        public GrauteadorBlocoSelectionFilter()
        {
        }

        public bool AllowElement(Element elem)
        {
            return elem.Name.Contains("TBL");
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}