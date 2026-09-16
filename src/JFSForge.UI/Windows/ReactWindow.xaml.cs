using System.IO;
using System.Reflection;
using System.Windows;

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

        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var pastaBase = Path.Combine(assemblyFolder!, "WebUI");
        var caminhoCompleto = Path.Combine(pastaBase, paginaHtml);

        WebView.CoreWebView2.Navigate(new Uri(caminhoCompleto).AbsoluteUri);
    }
}