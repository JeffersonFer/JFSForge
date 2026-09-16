using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class DesativarFiltrosDWGCommand : IExternalCommand
    {
        private const string Prefixo = "DWG";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            // Pega apenas os View Templates
            var templates = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate && v.AreGraphicsOverridesAllowed())
                .ToList();

            var filtrosParamId = new ElementId(BuiltInParameter.VIS_GRAPHICS_FILTERS);

            int filtrosDesativados = 0;
            int templatesAfetados = 0;
            var templatesNaoControlados = new List<string>();

            using (var trans = new Transaction(doc, "Desativar Filtros DWG nos Templates"))
            {
                trans.Start();

                foreach (var template in templates)
                {
                    // Verifica se o template está controlando o parâmetro "Filters"
                    bool controlaFiltros = !template.GetNonControlledTemplateParameterIds()
                        .Any(id => id == filtrosParamId);

                    if (!controlaFiltros)
                    {
                        templatesNaoControlados.Add(template.Name);
                        continue; // alterar aqui não propagaria para as views
                    }

                    bool desativouNesteTemplate = false;

                    foreach (var filtroId in template.GetFilters())
                    {
                        var elementoFiltro = doc.GetElement(filtroId);
                        if (elementoFiltro == null)
                            continue;

                        if (!IniciaComDWG(elementoFiltro.Name))
                            continue;

                        if (!template.GetIsFilterEnabled(filtroId))
                            continue;

                        template.SetIsFilterEnabled(filtroId, false);
                        filtrosDesativados++;
                        desativouNesteTemplate = true;
                    }

                    if (desativouNesteTemplate)
                        templatesAfetados++;
                }

                trans.Commit();
            }

            var resumo = new StringBuilder();
            resumo.AppendLine($"Filtros DWG desativados: {filtrosDesativados}");
            resumo.AppendLine($"Templates afetados: {templatesAfetados}");

            if (templatesNaoControlados.Count > 0)
            {
                resumo.AppendLine();
                resumo.AppendLine("Templates que NÃO controlam 'Filters' (não afetados):");
                foreach (var nome in templatesNaoControlados.Distinct())
                    resumo.AppendLine($"- {nome}");
            }

            TaskDialog.Show("Desativar Filtros DWG", resumo.ToString());

            return Result.Succeeded;
        }

        private static bool IniciaComDWG(string nomeFiltro)
        {
            if (string.IsNullOrEmpty(nomeFiltro) || nomeFiltro.Length < Prefixo.Length)
                return false;

            var primeirasLetras = nomeFiltro.Substring(0, Prefixo.Length).ToUpperInvariant();
            return string.Equals(primeirasLetras, Prefixo, System.StringComparison.Ordinal);
        }
    }
}