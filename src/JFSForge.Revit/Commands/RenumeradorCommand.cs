using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.UI.Windows;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RenumeradorCommand : IExternalCommand
    {
        private static ReactWindow? _janelaAberta;

        ExternalEvent renumeradorDeElevacoesExternalEvent;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            if (_janelaAberta != null && _janelaAberta.IsLoaded)
            {
                _janelaAberta.Activate();
                return Result.Succeeded;
            }

            _janelaAberta = new ReactWindow("index.html", largura: 500, altura: 500, tamanhoFixo: true);
            _janelaAberta.Closed += (s, e) => _janelaAberta = null;
            _janelaAberta.Show();

            return Result.Succeeded;
        }
    }
}
