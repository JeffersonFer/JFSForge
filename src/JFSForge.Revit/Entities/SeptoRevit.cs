using Autodesk.Revit.DB;

namespace JFSForge.Revit.Entities
{
    public class SeptoRevit
    {
        public Autodesk.Revit.DB.FamilyInstance SeptoFamilyInstance { get; set; }
        public int Tipo { get; set; }

        public Transform SeptoTransform { get; set; }
        public XYZ SeptoOrigin { get; set; }

        public SeptoRevit(FamilyInstance familyInstance)
        {
            SeptoFamilyInstance = familyInstance;
            Tipo = familyInstance.LookupParameter("TIPO")?.AsInteger() ?? 1;
            SeptoTransform = familyInstance.GetTransform();
            SeptoOrigin = SeptoTransform.Origin;
        }
    }
}
