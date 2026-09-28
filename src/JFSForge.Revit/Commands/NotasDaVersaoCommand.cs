using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;
using System.Reflection;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class NotasDaVersaoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            //Controle de expiração
            if (!ControleExpiracao.EstaValido())
            {
                TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
                return Result.Failed;
            }

            var versao = Assembly.GetExecutingAssembly().GetName().Version;
            var versaoFormatada = $"{versao.Major}.{versao.Minor}.{versao.Build}";

            TaskDialog.Show(
                 "JFS Forge - Notas da Versão",
                 $"Versão {versaoFormatada}\n\n" +
                 "Novidades:\n" +
                 "• Aplicador de Tags: janela para aplicar tags em elevações (janelas, portas, aço horizontal, vigas e pilares)\n" +
                 "• Aço horizontal com tag alternativa quando a barra é curta\n" +
                 "• Interface em tema escuro, com componentes reutilizáveis\n" +
                 "• O instalador agora instala o WebView2 Runtime automaticamente quando ele não existe no computador\n\n" +
                 "Correções:\n" +
                 "• Corrigido o fechamento inesperado do Revit ao abrir as janelas de interface\n" +
                 "• Botão do Renumerador abria a tela errada\n\n" +
                 "Em desenvolvimento:\n" +
                 "• Tags em cortes e plantas"
            );

            return Result.Succeeded;
        }
    }

}
