using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace JFSForge.Revit.Application;

public class App : IExternalApplication
{
    private static readonly DateTime DataExpiracao = new DateTime(2026, 10, 15);
    private static readonly string ArquivoControle = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "JFSForge", "controle.dat");

    public Result OnStartup(UIControlledApplication application)
    {
        if (RelogioFoiAdulterado())
        {
            TaskDialog.Show(
                "JFS Forge - Versão Expirada",
                "Não foi possível validar a data do sistema.\n\n" +
                "Entre em contato com o desenvolvedor para obter uma versão atualizada.");

            return Result.Failed;
        }

        if (DateTime.Now > DataExpiracao)
        {
            TaskDialog.Show(
                "JFS Forge - Versão Expirada",
                $"Esta versão do JFS Forge expirou em {DataExpiracao:dd/MM/yyyy}.\n\n" +
                "Entre em contato com o desenvolvedor para obter uma versão atualizada.");

            return Result.Failed;
        }

        AtualizarControle();

        int diasRestantes = (DataExpiracao - DateTime.Now).Days;
        TaskDialog.Show(
            "JFS Forge",
            $"Versão de teste - {diasRestantes} dia(s) restante(s) até {DataExpiracao:dd/MM/yyyy}.");

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

    private static bool RelogioFoiAdulterado()
    {
        if (!System.IO.File.Exists(ArquivoControle))
            return false;

        var conteudo = System.IO.File.ReadAllText(ArquivoControle);

        if (!DateTime.TryParse(conteudo, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var ultimaDataVista))
            return false;

        return DateTime.Now < ultimaDataVista;
    }

    private static void AtualizarControle()
    {
        var pasta = System.IO.Path.GetDirectoryName(ArquivoControle);
        System.IO.Directory.CreateDirectory(pasta!);
        System.IO.File.WriteAllText(ArquivoControle, DateTime.Now.ToString("o"));
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