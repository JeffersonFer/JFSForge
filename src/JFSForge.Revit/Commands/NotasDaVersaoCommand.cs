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
                "• Unir Estrutura: une conexões, pilares, vigas e pisos na ordem de prioridade Conexão > Pilar > Viga > Piso, corrigindo uniões que estão na ordem errada\n" +
                "• O comando pede para salvar o projeto antes de unir, para você ter um ponto de retorno\n" +
                "• Aplicador de Tags: janela para aplicar tags em elevações\n" +
                "• Renumerador e Aplicador de Tags não fecham mais ao concluir\n" +
                "• O instalador instala o WebView2 Runtime quando ele não existe no computador\n\n" +
                "Correções:\n" +
                "• Corrigido o fechamento inesperado do Revit ao abrir as janelas de interface\n\n" +
                "Em desenvolvimento:\n" +
                "• Tags em cortes e plantas"
);

            return Result.Succeeded;
        }
    }

}
