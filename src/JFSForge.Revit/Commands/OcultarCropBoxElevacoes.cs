using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    internal class OcultarCropBoxElevacoes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Controle de expiração
            if (!ControleExpiracao.EstaValido())
            {
                TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
                return Result.Failed;
            }

            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            // 1. Coleta todas as Views do documento e aplica os filtros combinados
            List<View> elevacoesComCrop = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .Where(v => v.ViewType == ViewType.Elevation)
                .Where(v => v.CropBoxActive)
                .ToList();

            // 2. Abre a Transaction e aplica a alteração em cada view encontrada
            using (Transaction t = new Transaction(doc, "Ocultar Crop Box - Elevações"))
            {
                t.Start();

                foreach (View view in elevacoesComCrop)
                {
                    view.CropBoxVisible = false;
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}