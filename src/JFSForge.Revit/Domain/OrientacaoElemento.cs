using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFSForge.Revit.Domain
{
    public class OrientacaoElemento
    {
        public XYZ EixoX { get; }
        public XYZ EixoY { get; }
        public XYZ EixoZ { get; }

        public OrientacaoElemento(XYZ eixoX, XYZ eixoY, XYZ eixoZ)
        {
            EixoX = eixoX;
            EixoY = eixoY;
            EixoZ = eixoZ;
        }
    }
}
