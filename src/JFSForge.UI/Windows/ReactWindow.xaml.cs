using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace JFSForge.UI.Windows;

public partial class ReactWindow : Window
{
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

        //WebView.CoreWebView2.OpenDevToolsWindow(); // Debug

        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var pastaBase = Path.Combine(assemblyFolder!, "WebUI");

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets",
            pastaBase,
            CoreWebView2HostResourceAccessKind.Allow);

        WebView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            var mensagemRecebida = e.WebMessageAsJson;
            MessageBox.Show($"Mensagem recebida do React:\n{mensagemRecebida}");
        };

        WebView.CoreWebView2.Navigate($"https://appassets/{paginaHtml}");
    }
}

