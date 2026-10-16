using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;
using JFSForge.Revit.Services;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class PreencherParQtdCommand : IExternalCommand
    {
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
                var resumo = PreenchedorDeParQtd.Executar(doc);

                TaskDialog.Show("Preencher PAR_QTD", resumo.ParaTexto());
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JFS Forge - Erro", $"Não foi possível preencher o PAR_QTD:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}