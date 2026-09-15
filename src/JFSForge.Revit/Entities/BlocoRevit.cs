using Autodesk.Revit.DB;

namespace JFSForge.Revit.Entities
{
    public class BlocoRevit
    {
        public Autodesk.Revit.DB.FamilyInstance BlocoFamilyInstance { get; set; }
        public Autodesk.Revit.DB.Transform BlocoTransform { get; set; }
        public double BlocoLargura { get; set; }
        public double BlocoComprimento { get; set; }
        public double BlocoAltura { get; set; }
        public SeptoRevit? Septo1 { get; set; } //positivo em relação ao Hand
        public SeptoRevit? Septo2 { get; set; } //negativo em relação ao Hand
        public SeptoRevit? Septo3 { get; set; } //no centro do bloco
        public BlocoRevit(Autodesk.Revit.DB.FamilyInstance blocoFamilyInstance)
        {
            BlocoFamilyInstance = blocoFamilyInstance;
            BlocoTransform = blocoFamilyInstance.GetTransform();
            BlocoLargura = UnitUtils.ConvertFromInternalUnits(blocoFamilyInstance.Symbol.LookupParameter("Largura").AsDouble(), UnitTypeId.Centimeters);
            BlocoComprimento = UnitUtils.ConvertFromInternalUnits(blocoFamilyInstance.Symbol.LookupParameter("Comprimento").AsDouble(), UnitTypeId.Centimeters);
            BlocoAltura = UnitUtils.ConvertFromInternalUnits(blocoFamilyInstance.LookupParameter("SP_H_BLOCO").AsDouble(), UnitTypeId.Centimeters);
        }

        public void CriarSeptosTemporarios(Document doc, FamilySymbol? detailSymbol)
        {
            XYZ origin = BlocoTransform.Origin;  // ponto de origem
            XYZ basisX = BlocoTransform.BasisX;  // eixo X local (direção "hand")
            XYZ basisY = BlocoTransform.BasisY;  // eixo Y local (direção "facing")
            XYZ basisZ = BlocoTransform.BasisZ;  // eixo Z local (normal ao plano)

            double offsetPositiveXValor = 10;
            double offsetNegativeXValor = 10;

            if (BlocoComprimento == 34)
            {
                offsetPositiveXValor = 7.5;
            }

            else if (BlocoComprimento == 54)
            {
                offsetPositiveXValor = 17.5;
                offsetNegativeXValor = 17.5;
            }

            double offsetPositiveX = UnitUtils.ConvertToInternalUnits(offsetPositiveXValor, UnitTypeId.Centimeters);
            double offsetNegativeX = UnitUtils.ConvertToInternalUnits(offsetNegativeXValor, UnitTypeId.Centimeters);
            XYZ point1 = origin + basisX * offsetPositiveX;
            XYZ point2 = origin - basisX * offsetNegativeX;
            XYZ point3 = origin;

            if (detailSymbol == null)
                throw new Exception("Nenhum FamilySymbol de item de detalhe encontrado.");

            // Obter a view ativa
            View activeView = doc.ActiveView;

            // Calcular o ângulo de rotação a partir do vetor Hand
            // O ângulo é em relação ao eixo X global (1, 0, 0)
            XYZ xAxis = XYZ.BasisX;
            double angle = Math.Atan2(basisX.Y, basisX.X);

            #region Transaction para criar septos temporários
            if (BlocoComprimento > 9)
            {
                using (Transaction tx1 = new Transaction(doc, "Criar itens de detalhe"))
                {
                    tx1.Start();

                    // Desativa regeneração automática durante o loop
                    tx1.SetFailureHandlingOptions(
                        tx1.GetFailureHandlingOptions()
                         .SetDelayedMiniWarnings(true));

                    // Ativar o symbol se necessário
                    if (!detailSymbol.IsActive)
                        detailSymbol.Activate();

                    // Criar instância 1
                    FamilyInstance detail1 = doc.Create.NewFamilyInstance(
                        point1, detailSymbol, activeView);

                    // Criar instância 2
                    FamilyInstance detail2 = doc.Create.NewFamilyInstance(
                        point2, detailSymbol, activeView);

                    // Criar instância 3 (no centro do bloco)
                    FamilyInstance detail3 = doc.Create.NewFamilyInstance(
                        point3, detailSymbol, activeView);

                    // Aplicar rotação para alinhar com o Hand da família original
                    // Rotacionar em torno do eixo Z passando pelo ponto de inserção
                    if (Math.Abs(angle) > 1e-9)
                    {
                        Line axis1 = Line.CreateBound(point1, point1 + XYZ.BasisZ);
                        Line axis2 = Line.CreateBound(point2, point2 + XYZ.BasisZ);
                        Line axis3 = Line.CreateBound(point3, point3 + XYZ.BasisZ);
                        ElementTransformUtils.RotateElement(doc, detail1.Id, axis1, angle);
                        ElementTransformUtils.RotateElement(doc, detail2.Id, axis2, angle);
                        ElementTransformUtils.RotateElement(doc, detail3.Id, axis3, angle);
                    }

                    // Linhas de projeção e corte em vermelho
                    OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                    Color red = new Color(255, 0, 0);

                    ogs.SetProjectionLineColor(red);
                    ogs.SetCutLineColor(red);

                    // Espessura de linha (valor entre 1 e 16, conforme tabela de pesos do Revit)
                    ogs.SetProjectionLineWeight(5); // linhas de projeção
                    ogs.SetCutLineWeight(5);        // linhas de corte

                    activeView.SetElementOverrides(detail1.Id, ogs);
                    activeView.SetElementOverrides(detail2.Id, ogs);
                    activeView.SetElementOverrides(detail3.Id, ogs);

                    // Preenchimento em vermelho (se aplicável)
                    FillPatternElement solidFill = new FilteredElementCollector(doc)
                        .OfClass(typeof(FillPatternElement))
                        .Cast<FillPatternElement>()
                        .FirstOrDefault(fp => fp.GetFillPattern().IsSolidFill);

                    if (solidFill != null)
                    {
                        ogs.SetSurfaceForegroundPatternId(solidFill.Id);
                        ogs.SetSurfaceForegroundPatternColor(red);
                        ogs.SetCutForegroundPatternId(solidFill.Id);
                        ogs.SetCutForegroundPatternColor(red);
                    }

                    activeView.SetElementOverrides(detail1.Id, ogs);
                    activeView.SetElementOverrides(detail2.Id, ogs);
                    activeView.SetElementOverrides(detail3.Id, ogs);

                    if (BlocoComprimento == 19 && BlocoLargura == 14)
                    {
                        doc.Delete(detail1.Id);
                        doc.Delete(detail2.Id);
                    }

                    else if (BlocoComprimento == 19 && BlocoLargura == 19)
                    {
                        doc.Delete(detail1.Id);
                        doc.Delete(detail2.Id);
                        detail3.LookupParameter("TIPO").Set(4);
                    }

                    else if (BlocoComprimento == 34)
                    {
                        doc.Delete(detail3.Id);
                        detail2.LookupParameter("TIPO").Set(2);
                    }

                    else if (BlocoComprimento == 39 && BlocoLargura == 14)
                    {
                        doc.Delete(detail3.Id);
                    }

                    else if (BlocoComprimento == 39 && BlocoLargura == 19)
                    {
                        doc.Delete(detail3.Id);
                        detail1.LookupParameter("TIPO").Set(4);
                        detail2.LookupParameter("TIPO").Set(4);
                    }

                    else if (BlocoComprimento == 54)
                    {
                        detail3.LookupParameter("TIPO").Set(2);
                    }

                    tx1.Commit();
                }
            }
            #endregion
        }

        public Solid CriarSolidoBloco()
        {
            XYZ origin = BlocoTransform.Origin;
            XYZ basisX = BlocoTransform.BasisX;
            XYZ basisY = BlocoTransform.BasisY;
            XYZ basisZ = BlocoTransform.BasisZ;

            var largSobre2 = UnitUtils.ConvertToInternalUnits(BlocoLargura / 2, UnitTypeId.Centimeters);
            var compSobre2 = UnitUtils.ConvertToInternalUnits(BlocoComprimento / 2, UnitTypeId.Centimeters);

            XYZ ponto1 = new XYZ(-compSobre2, -largSobre2, 0);
            XYZ ponto2 = new XYZ(compSobre2, -largSobre2, 0);
            XYZ ponto3 = new XYZ(compSobre2, largSobre2, 0);
            XYZ ponto4 = new XYZ(-compSobre2, largSobre2, 0);

            CurveLoop perfil = new CurveLoop();
            perfil.Append(Line.CreateBound(ponto1, ponto2));
            perfil.Append(Line.CreateBound(ponto2, ponto3));
            perfil.Append(Line.CreateBound(ponto3, ponto4));
            perfil.Append(Line.CreateBound(ponto4, ponto1));

            Solid solidOrigem = GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { perfil },
                XYZ.BasisZ, UnitUtils.ConvertToInternalUnits(BlocoAltura, UnitTypeId.Centimeters));

            Solid solidPosicionado;
            try
            {
                LocationPoint locationPoint = BlocoFamilyInstance.Location as LocationPoint;
                XYZ posicao = locationPoint.Point;
                double rotacao = locationPoint.Rotation;

                Transform tRotacao = Transform.CreateRotation(XYZ.BasisZ, rotacao);
                Transform tTranslacao = Transform.CreateTranslation(new XYZ(posicao.X, posicao.Y, posicao.Z + UnitUtils.ConvertToInternalUnits(1, UnitTypeId.Centimeters)));
                Transform transform = tTranslacao.Multiply(tRotacao);

                solidPosicionado = SolidUtils.CreateTransformed(solidOrigem, transform);
            }
            finally
            {
                solidOrigem.Dispose();
            }

            return solidPosicionado;
        }

    }
}
