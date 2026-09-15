using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace JFSForge.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ElevationCreatorExternalCommandRevit : IExternalCommand
    {
        UIApplication _uiapp;
        Autodesk.Revit.ApplicationServices.Application _app;
        UIDocument _uidoc;
        Autodesk.Revit.DB.Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _uiapp = commandData.Application;
            _app = _uiapp.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            double conversor = 30.48;
            int scale = 50;
            int inclinedWalls = 0;

            IList<Reference> wallReferences = new List<Reference>();
            IList<Element> structuralWallsElements = new List<Element>();
            IList<Element> nonStructuralWallsElements = new List<Element>();
            IList<Element> walls = new List<Element>();
            ViewFamilyType viewFamilyType = new FilteredElementCollector(_doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault<ViewFamilyType>(a => ViewFamily.Elevation == a.ViewFamily);

            IList<Wall> walls0 = new List<Wall>();
            IList<Wall> walls1 = new List<Wall>();
            IList<Wall> walls2 = new List<Wall>();
            IList<Wall> walls3 = new List<Wall>();
            IList<ViewSection> viewSections0 = new List<ViewSection>();
            IList<ViewSection> viewSections1 = new List<ViewSection>();
            IList<ViewSection> viewSections2 = new List<ViewSection>();
            IList<ViewSection> viewSections3 = new List<ViewSection>();


            try
            {
                TaskDialog taskElevationCreator = new TaskDialog("Criador de Elevações");
                taskElevationCreator.CommonButtons = TaskDialogCommonButtons.Cancel;
                taskElevationCreator.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Selecionar Paredes", "Seleciona as paredes para inserir elevações.");
                TaskDialogResult elevationCreatorresult = taskElevationCreator.Show();

                switch (elevationCreatorresult)
                {
                    case TaskDialogResult.Cancel:
                        TaskDialog.Show("Criador de elevações", "Operação cancelada");
                        break;
                    case TaskDialogResult.CommandLink1:
                        SelectionFilterRevit WallSelctionFilter = new SelectionFilterRevit(BuiltInCategory.OST_Walls);
                        wallReferences = _uiapp.ActiveUIDocument.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, WallSelctionFilter, "Selecione as paredes");
                        break;
                }
            }

            catch (Exception e)
            {
                message = e.Message;
                TaskDialog.Show("Erro", $"Você cancelou a seleção ou a operação foi cancelada pelo erro: {message}");
                return Result.Failed;
            }

            try
            {
                foreach (var wallReference in wallReferences)
                {
                    Element element = _doc.GetElement(wallReference);
                    Wall wall = element as Wall;
                    Autodesk.Revit.DB.Parameter param = wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);

                    if (param.AsInteger() == 0)
                    {
                        nonStructuralWallsElements.Add(wall);
                    }
                    else if (param.AsInteger() == 1)
                    {
                        structuralWallsElements.Add(wall);
                    }
                }

                TaskDialog confirmSelectionTask = new TaskDialog("Confirmação de seleção");
                confirmSelectionTask.MainContent = $"{wallReferences.Count} paredes selecionadas\n{structuralWallsElements.Count} paredes estruturais\n{nonStructuralWallsElements.Count} paredes de vedação";
                confirmSelectionTask.CommonButtons = TaskDialogCommonButtons.Cancel;
                confirmSelectionTask.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Desconsiderar Vedações");
                confirmSelectionTask.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Considerar Vedações");
                TaskDialogResult confirmSelectionResult = confirmSelectionTask.Show();

                switch (confirmSelectionResult)
                {
                    case TaskDialogResult.Cancel:
                        TaskDialog.Show("Criador de elevações", "Operação cancelada");
                        break;
                    case TaskDialogResult.CommandLink1:
                        walls = structuralWallsElements;
                        break;
                    case TaskDialogResult.CommandLink2:
                        walls = structuralWallsElements;
                        foreach (Wall nonStrutucturalWall in nonStructuralWallsElements)
                            walls.Add(nonStrutucturalWall);
                        break;
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                TaskDialog.Show("Erro", $"Erro: {message}");
                return Result.Failed;
            }
            using (Autodesk.Revit.DB.Transaction transaction1 = new Autodesk.Revit.DB.Transaction(_doc, "Criar Elevações"))
            {

                try
                {
                    transaction1.Start();

                    foreach (Wall wall in walls)
                    {

                        //Obtendo dados geométricos da parede
                        ElementId wallTypeId = wall.GetTypeId();
                        Element wallType = _doc.GetElement(wallTypeId);
                        Autodesk.Revit.DB.Parameter width = wallType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);

                        // Obter a altura da parede
                        var heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                        double wallHeight = heightParam.AsDouble();

                        // distancia da elevação da parede
                        double offset = (width.AsDouble() + (0.5 / conversor)) / 2;
                        Location location = wall.Location;
                        LocationCurve locationCurve = location as LocationCurve;
                        Curve curve = locationCurve.Curve;
                        XYZ curveMidPoint = curve.Evaluate(0.5, true);
                        Transform curveTransform = curve.ComputeDerivatives(0.5, true);
                        XYZ tangentCurve = curveTransform.BasisX.Normalize();
                        XYZ normalCurve = tangentCurve.CrossProduct(XYZ.BasisZ).Normalize();
                        XYZ wallOrientation = wall.Orientation.Normalize();

                        XYZ instancePoint = curveMidPoint - wallOrientation * offset;

                        //Vector normalizado com ponto inicial no instancePoint e final no curveMidPoint
                        XYZ elevationDirectionVector = (instancePoint - curveMidPoint).Normalize();

                        int elevationIndex = -1;

                        if (elevationDirectionVector.Y == 1)
                        {
                            elevationIndex = 3;
                        }
                        else if (elevationDirectionVector.Y == -1)
                        {
                            elevationIndex = 1;
                        }
                        else if (elevationDirectionVector.X == -1)
                        {
                            elevationIndex = 2;
                        }
                        else if (elevationDirectionVector.X == 1)
                        {
                            elevationIndex = 0;
                        }
                        else
                        {
                            inclinedWalls += 1;
                        }
                        if (elevationIndex != -1)
                        {
                            //instanciando o ElevationMarker no ponto
                            ElevationMarker elevationMarker = ElevationMarker.CreateElevationMarker(_doc, viewFamilyType.Id, instancePoint, scale);
                            //intancia a elevação na ElevationMarker
                            ViewSection viewSection = elevationMarker.CreateElevation(_doc, _uidoc.ActiveView.Id, elevationIndex);
                            //Setar a farOffset
                            Autodesk.Revit.DB.Parameter farOffsetParam = viewSection.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
                            farOffsetParam.Set(width.AsDouble() + (0.5 / conversor));

                            switch (elevationIndex)
                            {
                                case 0:
                                    viewSections0.Add(viewSection);
                                    walls0.Add(wall);
                                    break;
                                case 1:
                                    viewSections1.Add(viewSection);
                                    walls1.Add(wall);
                                    break;
                                case 2:
                                    viewSections2.Add(viewSection);
                                    walls2.Add(wall);
                                    break;
                                case 3:
                                    viewSections3.Add(viewSection);
                                    walls3.Add(wall);
                                    break;
                            }
                        }
                    }
                    transaction1.Commit();
                }
                catch (Exception e)
                {
                    message = e.Message;
                    transaction1.RollBack();
                    TaskDialog.Show("Erro", $"Erro: {message}");
                    return Result.Failed;
                }

            }
            if (inclinedWalls > 0)
            {
                TaskDialog.Show("AVISO", $"Há {inclinedWalls} paredes inclinadas, é necessário inserir as elevações manualmente.");
            }
            if (viewSections0.Count > 0)
            {
                CropBoxCorrection(walls0, viewSections0, 0);
            }
            if (viewSections1.Count > 0)
            {
                CropBoxCorrection(walls1, viewSections1, 1);
            }
            if (viewSections2.Count > 0)
            {
                CropBoxCorrection(walls2, viewSections2, 2);
            }
            if (viewSections3.Count > 0)
            {
                CropBoxCorrection(walls3, viewSections3, 3);
            }

            return Result.Succeeded;
        }
        private void CropBoxCorrection(IList<Wall> wallsT2, IList<ViewSection> viewSecitonT2, int index)
        {
            // Fator de conversão de centímetros para pés (1 pé = 30.48 cm)
            double conversor = 30.48;

            // Offsets em centímetros (convertidos para pés internamente)
            double verticalOffset = 100; // 100 cm
            double horizontalOffset = 80; // 80 cm

            // Converte os offsets para pés (unidades internas do Revit)
            double verticalOffsetFeet = verticalOffset / conversor;
            double horizontalOffsetFeet = horizontalOffset / conversor;

            if (index == 0 || index == 1 || index == 2 || index == 3)
            {
                foreach (var (wall, viewSection) in wallsT2.Zip(viewSecitonT2, (a, b) => (a, b)))
                {
                    // Abre a viewSection (torna a vista ativa)
                    //_uidoc.RequestViewChange(viewSection);

                    // BoundingBox da parede
                    var wallBox = wall.get_BoundingBox(null);

                    // Centroide da parede no sistema global
                    var wallCentroid = (wallBox.Min + wallBox.Max) / 2;

                    // Transformação da viewSection (sistema de coordenadas da vista)
                    Transform viewSectionTransform = viewSection.CropBox.Transform;

                    // Converte o centroide da parede para o sistema de coordenadas da vista
                    XYZ wallCentroidInView = viewSectionTransform.Inverse.OfPoint(wallCentroid);

                    // Dimensões da parede (comprimento e altura) em unidades internas (pés)
                    double wallLength = wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble(); // Comprimento em pés
                    double wallHeight = wallBox.Max.Z - wallBox.Min.Z; // Altura em pés

                    // Calcula os limites da cropBox no sistema de coordenadas da vista
                    double halfLength = (wallLength / 2) + horizontalOffsetFeet;
                    double halfHeight = (wallHeight / 2) + verticalOffsetFeet;

                    XYZ min = new XYZ(wallCentroidInView.X - halfLength, wallCentroidInView.Y - halfHeight, 0);
                    XYZ max = new XYZ(wallCentroidInView.X + halfLength, wallCentroidInView.Y + halfHeight, 0);

                    // Cria uma nova BoundingBox para a cropBox
                    BoundingBoxXYZ newCropBox = new BoundingBoxXYZ
                    {
                        Min = min,
                        Max = max,
                        Transform = viewSectionTransform // Mantém a transformação da viewSection
                    };

                    // Aplica a nova cropBox à viewSection
                    using (Transaction transaction2 = new Transaction(_doc, "Alterar CropBox"))
                    {
                        transaction2.Start();
                        viewSection.CropBox = newCropBox;
                        viewSection.CropBoxActive = true; // Ativa a cropBox
                        viewSection.CropBoxVisible = true; // Torna a cropBox visível
                        transaction2.Commit();
                    }
                }
            }
        }
    }

    public class SelectionFilterRevit : ISelectionFilter
    {
        public string ElementCategoryName { get; set; }
        public BuiltInCategory BuiltInCategory { get; set; } // Alterar para usar multiplos idiomas
        public SelectionFilterRevit(string elementCategoryName)
        {
            ElementCategoryName = elementCategoryName;
        }
        public SelectionFilterRevit(BuiltInCategory builtInCategory)
        {
            BuiltInCategory = builtInCategory;
        }

        public bool AllowElement(Element elem)
        {
            if (elem.Category.Name == ElementCategoryName || elem.Category.BuiltInCategory == BuiltInCategory)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
