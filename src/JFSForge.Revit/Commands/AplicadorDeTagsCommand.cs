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
    public class AplicadorDeTagsCommand : IExternalCommand
    {
        private static ReactWindow? _janelaAberta;
        private static ExternalEvent? _aplicadorExternalEvent;
        private static AplicadorDeTagsHandler? _handler;

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

                _handler = new AplicadorDeTagsHandler();
                _aplicadorExternalEvent = ExternalEvent.Create(_handler);

                _janelaAberta = new ReactWindow("index.html#/aplicador-de-tags", largura: 900, altura: 500, tamanhoFixo: true);
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
                TaskDialog.Show("JFS Forge - Erro", $"Não foi possível abrir o Aplicador de Tags:\n{ex.Message}");
                return Result.Failed;
            }
        }

        private void ProcessarMensagemDoReact(string mensagemJson)
        {
            using var documento = JsonDocument.Parse(mensagemJson);
            var raiz = documento.RootElement;

            var tipo = raiz.GetProperty("tipo").GetString();

            if (tipo == "aplicar-tags")
            {
                _handler!.TipoDeVista = raiz.GetProperty("tipoDeVista").GetString() ?? string.Empty;
                _handler.ApenasVistaAtiva = raiz.GetProperty("apenasVistaAtiva").GetBoolean();
                _handler.TagJanelaAtiva = raiz.GetProperty("tagJanela").GetBoolean();
                _handler.TagPortaAtiva = raiz.GetProperty("tagPorta").GetBoolean();
                _handler.TagBarraHorizontalAtiva = raiz.GetProperty("tagBarraHorizontal").GetBoolean();
                _handler.TagVigaAtiva = raiz.GetProperty("tagViga").GetBoolean();
                _handler.TagPilarAtiva = raiz.GetProperty("tagPilar").GetBoolean();

                _aplicadorExternalEvent!.Raise();
            }
        }
    }
}