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
                "• Renumeração do aço horizontal, em quatro botões: Geral, Por Parede, Só Alterados e Só Alterados Por Parede\n" +
                "• O parâmetro PAR_QTD é preenchido com o nome da elevação em que a barra aparece, e recebe aviso quando aparece em nenhuma ou em mais de uma\n" +
                "• O COD_ACO identifica a barra pelo comprimento reto e pelas dobras (ex.: CR125-DOBRA_10_A-DOBRA_35_BU). Barras de mesmo código recebem o mesmo número\n" +
                "• O parâmetro Quantidade conta as barras iguais alinhadas na vertical\n" +
                "• Nos comandos Só Alterados, você escolhe se as barras novas pegam o próximo número livre ou vão para o final da sequência\n" +
                "• Unir Estrutura: une blocos de fundação, pilares, vigas e lajes na ordem de prioridade, e pede para salvar o projeto antes\n\n" +
                "Melhorias:\n" +
                "• Armador de Paredes: o rebaixo do aço horizontal agora considera o parâmetro H_Solid do bloco\n" +
                "• Ativar/Desativar Filtros DWG: os templates de vistas 3D não são mais alterados\n" +
                "• Renumerador de Elevações e Aplicador de Tags não fecham mais ao concluir\n\n" +
                "Correções:\n" +
                "• Corrigido o fechamento inesperado do Revit ao abrir as janelas de interface"
            );

            return Result.Succeeded;
        }
    }

}
