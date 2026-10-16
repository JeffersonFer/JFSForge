using System;
using System.IO;

namespace JFSForge.Revit.Application
{
    public static class ControleExpiracao
    {
        public static readonly DateTime DataExpiracao = new DateTime(2026, 11, 7, 23, 59, 59);
        private static readonly string ArquivoControle = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "JFSForge", "controle.dat");

        public static bool EstaValido()
        {
            if (RelogioFoiAdulterado())
                return false;

            if (DateTime.Now > DataExpiracao)
                return false;

            AtualizarControle();
            return true;
        }

        public static string ObterMensagemExpiracao()
        {
            return $"Esta versão do JFS Forge expirou em {DataExpiracao:dd/MM/yyyy}.\n\n" +
                   "Entre em contato com o desenvolvedor para obter uma versão atualizada.";
        }

        private static bool RelogioFoiAdulterado()
        {
            if (!File.Exists(ArquivoControle))
                return false;

            var conteudo = File.ReadAllText(ArquivoControle);

            if (!DateTime.TryParse(conteudo, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var ultimaDataVista))
                return false;

            return DateTime.Now < ultimaDataVista;
        }

        private static void AtualizarControle()
        {
            var pasta = Path.GetDirectoryName(ArquivoControle);
            Directory.CreateDirectory(pasta!);
            File.WriteAllText(ArquivoControle, DateTime.Now.ToString("o"));
        }
    }
}