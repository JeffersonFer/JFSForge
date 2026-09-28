using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;
using JFSForge.Revit.ExternalEventHandlers;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AplicadorDeTagsTesteCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            //Controle de expiração
            if (!ControleExpiracao.EstaValido())
            {
                TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
                return Result.Failed;
            }

            var handler = new AplicadorDeTagsHandler
            {
                TipoDeVista = "elevacoes",
                ApenasVistaAtiva = true,
                TagJanelaAtiva = true,
                TagPortaAtiva = true,
                TagBarraHorizontalAtiva = true,
                TagVigaAtiva = true,
                TagPilarAtiva = true,
            };

            handler.Execute(commandData.Application);

            return Result.Succeeded;
        }
    }
}