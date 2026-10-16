using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;
using JFSForge.Revit.Services;

namespace JFSForge.Revit.Commands
{
    /// <summary>
    /// Base dos quatro comandos de renumeração. Cada classe derivada informa o modo
    /// e o critério; o PAR_QTD é sempre reprocessado antes, na mesma transação.
    /// </summary>
    public abstract class RenumerarAcoHorizontalBase : IExternalCommand
    {
        protected abstract ModoDeAtualizacao Modo { get; }
        protected abstract CriterioDeNumeracao Criterio { get; }
        protected abstract string TituloDoComando { get; }

        /// <summary>
        /// Define o destino das barras novas ou alteradas. Só os comandos incrementais
        /// perguntam; os de atualização completa usam o padrão. Retorna false se o
        /// usuário cancelar.
        /// </summary>
        protected virtual bool ResolverDestinoDeNovos(OpcoesDeRenumeracao opcoes)
        {
            opcoes.Novos = DestinoDeNovos.ProximoLivre;
            return true;
        }

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

                var opcoes = new OpcoesDeRenumeracao
                {
                    Modo = Modo,
                    Criterio = Criterio
                };

                if (!ResolverDestinoDeNovos(opcoes))
                    return Result.Cancelled;

                ResumoParQtd resumoParQtd;
                ResumoDeRenumeracao resumoRenumeracao;

                using (var transacao = new Transaction(doc, TituloDoComando))
                {
                    transacao.Start();

                    // O PAR_QTD é sempre reprocessado antes: a numeração por parede depende dele.
                    resumoParQtd = PreenchedorDeParQtd.Executar(doc);
                    resumoRenumeracao = CodificadorDeAco.Executar(doc, opcoes);

                    transacao.Commit();
                }

                TaskDialog.Show(
                    TituloDoComando,
                    "=== PAR_QTD ===\n" + resumoParQtd.ParaTexto() +
                    "\n\n=== Codificação e numeração ===\n" + resumoRenumeracao.ParaTexto());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JFS Forge - Erro", $"Não foi possível renumerar:\n{ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Diálogo compartilhado pelos dois comandos incrementais.
        /// </summary>
        protected bool PerguntarDestinoDeNovos(OpcoesDeRenumeracao opcoes)
        {
            var dialogo = new TaskDialog(TituloDoComando)
            {
                MainInstruction = "Qual número dar às barras novas ou alteradas?",
                CommonButtons = TaskDialogCommonButtons.Cancel
            };
            dialogo.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Próximo número livre",
                "Reaproveita os buracos deixados por barras apagadas.");
            dialogo.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "No final da sequência",
                "Usa o maior número em uso mais um, e mantém os buracos.");
            dialogo.DefaultButton = TaskDialogResult.CommandLink1;

            var resultado = dialogo.Show();

            if (resultado == TaskDialogResult.CommandLink1)
                opcoes.Novos = DestinoDeNovos.ProximoLivre;
            else if (resultado == TaskDialogResult.CommandLink2)
                opcoes.Novos = DestinoDeNovos.FinalDaSequencia;
            else
                return false;

            return true;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class RenumerarGeralCommand : RenumerarAcoHorizontalBase
    {
        protected override ModoDeAtualizacao Modo => ModoDeAtualizacao.Tudo;
        protected override CriterioDeNumeracao Criterio => CriterioDeNumeracao.Geral;
        protected override string TituloDoComando => "Renumerar - Geral";
    }

    [Transaction(TransactionMode.Manual)]
    public class RenumerarGeralPorParedeCommand : RenumerarAcoHorizontalBase
    {
        protected override ModoDeAtualizacao Modo => ModoDeAtualizacao.Tudo;
        protected override CriterioDeNumeracao Criterio => CriterioDeNumeracao.PorParede;
        protected override string TituloDoComando => "Renumerar - Geral por parede";
    }

    [Transaction(TransactionMode.Manual)]
    public class RenumerarAlteradosCommand : RenumerarAcoHorizontalBase
    {
        protected override ModoDeAtualizacao Modo => ModoDeAtualizacao.SomenteAlterados;
        protected override CriterioDeNumeracao Criterio => CriterioDeNumeracao.Geral;
        protected override string TituloDoComando => "Renumerar - Só alterados";

        protected override bool ResolverDestinoDeNovos(OpcoesDeRenumeracao opcoes)
            => PerguntarDestinoDeNovos(opcoes);
    }

    [Transaction(TransactionMode.Manual)]
    public class RenumerarAlteradosPorParedeCommand : RenumerarAcoHorizontalBase
    {
        protected override ModoDeAtualizacao Modo => ModoDeAtualizacao.SomenteAlterados;
        protected override CriterioDeNumeracao Criterio => CriterioDeNumeracao.PorParede;
        protected override string TituloDoComando => "Renumerar - Só alterados por parede";

        protected override bool ResolverDestinoDeNovos(OpcoesDeRenumeracao opcoes)
            => PerguntarDestinoDeNovos(opcoes);
    }
}