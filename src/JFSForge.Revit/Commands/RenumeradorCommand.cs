using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.UI.Windows;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    internal class RenumeradorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var janela = new ReactWindow("index.html", largura: 400, altura: 300, tamanhoFixo: true);
            janela.Show();
            return Result.Succeeded;
        }
    }
}
