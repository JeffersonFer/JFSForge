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

        Loaded += async (s, e) => await CarregarPaginaComSeguranca(paginaHtml);
    }

    private async Task CarregarPaginaComSeguranca(string paginaHtml)
    {
        try
        {
            await CarregarPagina(paginaHtml);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível carregar a interface:\n{ex.Message}",
                "JFS Forge",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }

    private async Task CarregarPagina(string paginaHtml)
    {
        var pastaDeDados = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JFSForge", "WebView2");

        var ambiente = await CoreWebView2Environment.CreateAsync(null, pastaDeDados);
        await WebView.EnsureCoreWebView2Async(ambiente);

        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var pastaBase = Path.Combine(assemblyFolder!, "WebUI");

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets",
            pastaBase,
            CoreWebView2HostResourceAccessKind.Allow);

        WebView.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            try
            {
                MensagemRecebida?.Invoke(e.WebMessageAsJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao processar a mensagem da interface:\n{ex.Message}",
                    "JFS Forge",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        WebView.CoreWebView2.Navigate($"https://appassets/{paginaHtml}");
    }

    public void EnviarMensagemParaReact(object mensagem)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(mensagem);
            WebView.CoreWebView2?.PostWebMessageAsJson(json);
        }
        catch (Exception)
        {
            // Janela já fechada ou WebView descartado: não há para quem enviar.
        }
    }
}


//WebView.CoreWebView2.OpenDevToolsWindow(); // Debug