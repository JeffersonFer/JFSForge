using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JFSForge.Revit.Application;
using JFSForge.Revit.ExternalEventHandlers;
using JFSForge.UI.Windows;
using System.Text.Json;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RenumeradorCommand : IExternalCommand
    {
        private static ReactWindow? _janelaAberta;
        private static ExternalEvent? _renumeradorExternalEvent;
        private static RenumeradorDeElevacoesHandler? _handler;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //Controle de expiração
                if (!ControleExpiracao.EstaValido())
                {
                    TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
                    return Result.Failed;
                }

                if (_janelaAberta != null && _janelaAberta.IsLoaded)
                {
                    _janelaAberta.Activate();
                    return Result.Succeeded;
                }

                _handler = new RenumeradorDeElevacoesHandler();
                _renumeradorExternalEvent = ExternalEvent.Create(_handler);

                _janelaAberta = new ReactWindow("index.html#/renumerador", largura: 500, altura: 500, tamanhoFixo: true);
                _handler.Janela = _janelaAberta;

                _janelaAberta.MensagemRecebida += (mensagemJson) =>
                {
                    ProcessarMensagemDoReact(mensagemJson);
                };

                _janelaAberta.Closed += (s, e) => _janelaAberta = null;
                _janelaAberta.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                _janelaAberta = null;
                message = ex.Message;
                TaskDialog.Show("JFS Forge - Erro", $"Não foi possível abrir o Renumerador:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private void ProcessarMensagemDoReact(string mensagemJson)
        {
            using var documento = JsonDocument.Parse(mensagemJson);
            var raiz = documento.RootElement;

            var tipo = raiz.GetProperty("tipo").GetString();

            if (tipo == "selecionar")
            {
                _handler!.Acao = "selecionar";
                _renumeradorExternalEvent!.Raise();
            }
            else if (tipo == "processar")
            {
                _handler!.Acao = "processar";
                _handler.Prefixo = raiz.GetProperty("prefixo").GetString() ?? "PAR";
                _handler.ValorInicial = raiz.GetProperty("valorInicial").GetString() ?? "1";
                _renumeradorExternalEvent!.Raise();
            }
        }
    }
}