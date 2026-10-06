using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace JFSForge.Revit.Application;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        if (!ControleExpiracao.EstaValido())
        {
            TaskDialog.Show("JFS Forge - Versão Expirada", ControleExpiracao.ObterMensagemExpiracao());
            return Result.Failed;
        }

        int diasRestantes = (ControleExpiracao.DataExpiracao - DateTime.Now).Days;
        TaskDialog.Show(
            "JFS Forge",
            $"Versão de teste - {diasRestantes} dia(s) restante(s) até {ControleExpiracao.DataExpiracao:dd/MM/yyyy}.");

        try
        {
            CreateRibbonTab(application, "JFS-Alvenaria");
            CreateRibbonTab(application, "JFS-Concreto");

            CreateButton(application, "JFS-Alvenaria", "Geral", "NotasDaVersaoButton",
                "Notas da\nVersão", "JFSForge.Revit.Commands.NotasDaVersaoCommand",
                "Exibe as notas da versão atual do JFS Forge",
                "NotasDaVersao32.png", "NotasDaVersao16.png");

            CreateButton(application, "JFS-Concreto", "Geral", "NotasDaVersaoButton",
                "Notas da\nVersão", "JFSForge.Revit.Commands.NotasDaVersaoCommand",
                "Exibe as notas da versão atual do JFS Forge",
                "NotasDaVersao32.png", "NotasDaVersao16.png");

            CreateButton(application, "JFS-Alvenaria", "Paredes", "AgruparParedesButton",
                "Agrupar\nElementos", "JFSForge.Revit.Commands.AgruparParedesCommand",
                "Agrupa elementos de alvenaria estrutural relacionados a uma parede",
                "AgruparParedes32.png", "AgruparParedes16.png");

            CreateButton(application, "JFS-Alvenaria", "Blocos", "CompatibilizadorBlocosButton",
                "Compatibilizar", "JFSForge.Revit.Commands.CompatibilizadorBlocos",
                "Verifica aberturas nas paredes e compatibiliza os blocos que ficam parcialmente atravessados",
                "CompatibilizarBlocos32.png", "CompatibilizarBlocos16.png");

            CreateButton(application, "JFS-Alvenaria", "Aço", "ArmadorDeParedesButton",
                "Aço\nHorizontal", "JFSForge.Revit.Commands.ArmadorDeParedes",
                "Calcula e cria automaticamente o aço horizontal de cintas, vergas e contravergas",
                "ArmadorDeParedes32.png", "ArmadorDeParedes16.png");

            CreateButton(application, "JFS-Alvenaria", "Aço", "GrauteadorButton",
                "Grautear\nPilaretes", "JFSForge.Revit.Commands.GrauteadorExternalCommand",
                "Cria pilaretes de graute vertical a partir de septos identificados nos blocos selecionados",
                "Grauteador32.png", "Grauteador16.png");

            CreateButton(application, "JFS-Alvenaria", "Machine Learning", "ColetarCorpusButton",
                "Coletar\nCorpus", "JFSForge.Revit.Commands.ColetarCorpusCommand",
                "Coleta dados de modulação de blocos em trechos de parede, para análise e aprendizagem de máquina",
                "ColetarCorpus32.png", "ColetarCorpus16.png");

            CreateButton(application, "JFS-Alvenaria", "Elevações", "ElevationCreatorButton",
                "Criar\nElevações", "JFSForge.Revit.Commands.ElevationCreatorExternalCommandRevit",
                "Cria elevações automaticamente para as paredes selecionadas, com ajuste de CropBox",
                "ElevationCreator32.png", "ElevationCreator16.png");

            CreateButton(application, "JFS-Alvenaria", "Elevações", "ElevationMarkerCleanerButton",
                "Limpar\nElevações", "JFSForge.Revit.Commands.ElevationMarkerCleanerExternalCommandRevit",
                "Remove marcadores de elevação (ElevationMarker) vazios do documento",
                "ElevationMarkerCleaner32.png", "ElevationMarkerCleaner16.png");

            CreateButton(application, "JFS-Alvenaria", "Elevações", "RenumeradorButton",
                "Renumerador", "JFSForge.Revit.Commands.RenumeradorCommand",
                "Abre a tela do Renumerador",
                "Renumerador32.png", "Renumerador16.png");

            CreateButton(application, "JFS-Alvenaria", "Filtros DWG", "AtivarFiltrosDWGButton",
                "Ativar", "JFSForge.Revit.Commands.AtivarFiltrosDWGCommand",
                "Ativa filtros que começam com 'DWG' em todos os View Templates que controlam o parâmetro Filters",
                "AtivarFiltrosDWG32.png", "AtivarFiltrosDWG16.png");

            CreateButton(application, "JFS-Alvenaria", "Filtros DWG", "DesativarFiltrosDWGButton",
                "Desativar", "JFSForge.Revit.Commands.DesativarFiltrosDWGCommand",
                "Desativa filtros que começam com 'DWG' em todos os View Templates que controlam o parâmetro Filters",
                "DesativarFiltrosDWG32.png", "DesativarFiltrosDWG16.png");

            CreateButton(application, "JFS-Alvenaria", "Crop", "OcultarCropBoxElevacoesButton",
                "Ocultar Crop\nElevações", "JFSForge.Revit.Commands.OcultarCropBoxElevacoes",
                "Oculta o CropBox de todas as views de elevação com Crop Box ativo",
                "OcultarCropBoxElevacoes32.png", "OcultarCropBoxElevacoes16.png");

            CreateButton(application, "JFS-Alvenaria", "Tags", "AplicadorDeTagsButton",
                "Aplicador\nde Tags", "JFSForge.Revit.Commands.AplicadorDeTagsCommand",
                "Aplica tags em janelas, portas, aço horizontal, vigas e pilares em vistas de elevação, cortes e plantas",
                "AplicadorDeTags32.png", "AplicadorDeTags16.png");

            CreateButton(application, "JFS-Alvenaria", "Estrutura", "UnirElementosEstruturaisAlvenariaButton",
                "Unir\nEstrutura", "JFSForge.Revit.Commands.UnirElementosEstruturaisCommand",
                "Une blocos de fundação, pilares, vigas e lajes na ordem de prioridade: Bloco de fundação > Pilar > Viga > Laje",
                "UnirElementosEstruturais32.png", "UnirElementosEstruturais16.png");

            CreateButton(application, "JFS-Concreto", "Estrutura", "UnirElementosEstruturaisConcretoButton",
                "Unir\nEstrutura", "JFSForge.Revit.Commands.UnirElementosEstruturaisCommand",
                "Une blocos de fundação, pilares, vigas e lajes na ordem de prioridade: Bloco de fundação > Pilar > Viga > Laje",
                "UnirElementosEstruturais32.png", "UnirElementosEstruturais16.png");
        }
        catch (System.Exception ex)
        {
            TaskDialog.Show("JFS Forge - Erro no OnStartup", ex.ToString());
        }

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private static void CreateRibbonTab(UIControlledApplication application, string tabName)
    {
        try
        {
            application.CreateRibbonTab(tabName);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            // Aba já existe - ignora
        }
    }

    private static void CreateButton(
        UIControlledApplication application,
        string tabName,
        string panelName,
        string buttonName,
        string buttonText,
        string commandClassName,
        string toolTip,
        string? largeImageFile = null,
        string? smallImageFile = null)
    {
        var panel = GetOrCreatePanel(application, tabName, panelName);

        var buttonData = new PushButtonData(
            buttonName,
            buttonText,
            Assembly.GetExecutingAssembly().Location,
            commandClassName
        );

        buttonData.ToolTip = toolTip;

        if (largeImageFile is not null)
            buttonData.LargeImage = LoadImage(largeImageFile);

        if (smallImageFile is not null)
            buttonData.Image = LoadImage(smallImageFile);

        panel.AddItem(buttonData);
    }

    private static RibbonPanel GetOrCreatePanel(UIControlledApplication application, string tabName, string panelName)
    {
        var panels = application.GetRibbonPanels(tabName);
        var panelExistente = panels.FirstOrDefault(p => p.Name == panelName);

        return panelExistente ?? application.CreateRibbonPanel(tabName, panelName);
    }

    private static BitmapImage LoadImage(string fileName)
    {
        var assemblyFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var imagePath = System.IO.Path.Combine(assemblyFolder!, "Resources", fileName);

        return new BitmapImage(new System.Uri(imagePath, System.UriKind.Absolute));
    }
}