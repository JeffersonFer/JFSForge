using Autodesk.Revit.UI;

namespace JFSForge.Revit.ExternalEventHandlers
{
    internal class RenumeradorDeElevacoesHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try
            {
                TaskDialog.Show("Teste", "Teste processado");
            }

            catch(Exception e)
            {
                TaskDialog.Show("Erro na renumeração", $"Erro: {e.Message}");
            }
        }

        public string GetName()
        {
            return "Renumeração de elevações";
        }
    }
}
