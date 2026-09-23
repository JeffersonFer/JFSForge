using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace JFSForge.UI.Windows;

public partial class ReactWindow : Window
{
    public event Action<string>? MensagemRecebida;

    public ReactWindow(string paginaHtml = "index.html", double largura = 800, double altura = 450, bool tamanhoFixo = true)
    {
        InitializeComponent();

        Width = largura;
        Height = altura;

        if (tamanhoFixo)
        {
            ResizeMode = ResizeMode.NoResize;
        }

        Loaded += async (s, e) => await CarregarPagina(paginaHtml);
    }

    private async Task CarregarPagina(string paginaHtml)
    {
        await WebView.EnsureCoreWebView2Async();

        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var pastaBase = Path.Combine(assemblyFolder!, "WebUI");

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets",
            pastaBase,
            CoreWebView2HostResourceAccessKind.Allow);

        WebView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            MensagemRecebida?.Invoke(e.WebMessageAsJson);
        };

        WebView.CoreWebView2.Navigate($"https://appassets/{paginaHtml}");
    }

    public void EnviarMensagemParaReact(object mensagem)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(mensagem);
        WebView.CoreWebView2?.PostWebMessageAsJson(json);
    }
}


//WebView.CoreWebView2.OpenDevToolsWindow(); // Debug
