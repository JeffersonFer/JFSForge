using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ElevationMarkerCleanerExternalCommandRevit : IExternalCommand
    {
        UIApplication _uiapp;
        Autodesk.Revit.ApplicationServices.Application _app;
        UIDocument _uidoc;
        Autodesk.Revit.DB.Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            _uiapp = commandData.Application;
            _app = _uiapp.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;

            using (Transaction transaction = new Transaction(_doc, "Deletar ElevationMark"))
            {

                try
                {
                    transaction.Start();

                    // Coleta ElevationMark no documento ativo
                    FilteredElementCollector elevationMarkColletor = new FilteredElementCollector(_doc).OfCategory(BuiltInCategory.OST_Elev);
                    //Inicia o contador de elementos deletados
                    int deletedElements = 0;
                    //Instancia a lista de markers para deletar
                    IList<ElevationMarker> markersToDelete = new List<ElevationMarker>();
                    // Instancia a lista de Ids para usar no delete
                    ICollection<ElementId> elementIds = new HashSet<ElementId>();

                    //Preenche a lista de Ids
                    foreach (Element element in elevationMarkColletor)
                    {
                        ElementId elementId = element.Id;
                        elementIds.Add(elementId);
                    }
                    //Cria uma seleção destacada dos elementos
                    _uidoc.Selection.SetElementIds(elementIds);

                    //Verifica e separa as markers vazias
                    foreach (Element element in elevationMarkColletor)
                    {

                        ElevationMarker elevationMarker = element as ElevationMarker;


                        bool? test = null;

                        if (elevationMarker is ElevationMarker elevationMarkerTrue)
                        {
                            test = elevationMarkerTrue.HasElevations();
                        }

                        if (test != null && test == false)
                        {
                            markersToDelete.Add(elevationMarker);
                            deletedElements++;
                        }
                    }

                    //Task de apresentação as quantidades de markers no projeto e as markers vazias encontradas - aprovação do processo de deleção
                    int count = elementIds.Count();

                    TaskDialog avisoTask = new TaskDialog("Aviso!");
                    avisoTask.MainInstruction = $"{count} Elevações encontradas no documento conforme seleção destacada\n{deletedElements} estão vazias\nDeseja continuar com a exclusão?";
                    avisoTask.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    avisoTask.CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
                    avisoTask.DefaultButton = TaskDialogResult.Ok;

                    TaskDialogResult result = avisoTask.Show();


                    //Cancel do processo de deleção
                    if (result == TaskDialogResult.Cancel)
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }
                    //Aprovação do processo de delação
                    else
                    {

                        foreach (ElevationMarker elevationMarker in markersToDelete)
                        {
                            Element element = elevationMarker as Element;
                            _doc.Delete(element.Id);
                        }

                        MessageBox.Show($"{deletedElements} ViewMarks deletadas.");

                        transaction.Commit();

                        return Result.Succeeded;
                    }


                }
                catch (Exception e)
                {
                    transaction.RollBack();
                    message = e.Message;
                    MessageBox.Show($"Não foi possível fazer a limpeza devido ao erro: {message}");
                    return Result.Failed;
                }
            }
        }
    }
}
