using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreationModelPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = SelectLevel(doc, "Уровень 1");
            Level level2 = SelectLevel(doc, "Уровень 2");
            List<Wall> walls = CreateWalls(level1, level2, doc);
            AddRoof(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1]);
            AddWindow(doc, level1, walls[2]);
            AddWindow(doc, level1, walls[3]);

            return Result.Succeeded;
        }
        private Result AddRoof(Document doc, Level level1, Wall wall)
        {
           
            Transaction transaction = new Transaction(doc, "Создание крыши");
            transaction.Start();

            ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            RoofType type = doc.GetElement(id) as RoofType;
            if (type == null)
            {
                TaskDialog.Show("Error", "Not RoofType");
                return Result.Failed;
            }
            // Crear esquema
            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 20, 20)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 20, 20), new XYZ(0, 40, 0)));
            // Obtener la elevación de la vista actual
            Level level = doc.ActiveView.GenLevel;
            if (level == null)
            {
                TaskDialog.Show("Error", "No es PlainView");
                return Result.Failed;
            }
            // Crear techo
            using (Transaction tr = new Transaction(doc))
            {
                tr.Start("Create ExtrusionRoof");
                ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
                doc.Create.NewExtrusionRoof(curveArray, plane, level, type, 0, 40);
                tr.Commit();


            }
            return Result.Succeeded;
            transaction.Commit();
        }
            #region "NewFootPrintRoof"
            //{
            // RoofType roofType = new FilteredElementCollector(doc)
            //     .OfClass(typeof(RoofType))
            //     .OfType<RoofType>()
            //     .Where(x => x.Name.Equals("Типовой - 400мм"))
            //     .Where(x => x.FamilyName.Equals("Базовая крыша"))
            //     .FirstOrDefault();
            // Transaction transaction = new Transaction(doc, "Создание крыши");
            // transaction.Start();
            // double wallWidth = walls[0].Width;
            // double dt = wallWidth / 2;
            // List<XYZ> points = new List<XYZ>();
            // points.Add(new XYZ(-dt, -dt, 0));
            // points.Add(new XYZ(dt, -dt, 0));
            // points.Add(new XYZ(dt, dt, 0));
            // points.Add(new XYZ(-dt, dt, 0));
            // points.Add(new XYZ(-dt, -dt, 0));
            // Application application = doc.Application;
            // CurveArray footprint = application.Create.NewCurveArray();
            // for (int i = 0; i < 4; i++)
            // {
            //     LocationCurve curve = walls[i].Location as LocationCurve;
            //     XYZ p1 = curve.Curve.GetEndPoint(0);
            //     XYZ p2 = curve.Curve.GetEndPoint(1);
            //     Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
            //     footprint.Append(line);
            // }
            // ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            // FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);

            // //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            // //iterator.Reset();
            // //while (iterator.MoveNext())
            //// {
            //    // ModelCurve modelCurve = iterator.Current as ModelCurve;
            //     //footprintRoof.set_DefinesSlope(modelCurve, true);
            //     //footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            // //}
            // foreach(ModelCurve m in footPrintToModelCurveMapping)
            // {
            //     footprintRoof.set_DefinesSlope(m, true);
            //     footprintRoof.set_SlopeAngle(m, 0.5);
            // }
            // transaction.Commit();
            #endregion
            //}
            private void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            Transaction transaction = new Transaction(doc, "Создание окон");
            transaction.Start();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();

            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            double offsetHeigh = UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(offsetHeigh);

            transaction.Commit();
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();
            Transaction transaction = new Transaction(doc, "Создание двери");
            transaction.Start();
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
            transaction.Commit();
        }

        public List<Wall> CreateWalls(Level level1, Level level2, Document doc)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Постронение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                walls.Add(wall);
            }
            transaction.Commit();
            return walls;
        }

        public Level SelectLevel(Document doc, string lavelName)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level = listLevel
                .Where(x => x.Name.Equals(lavelName))
                .FirstOrDefault();

            return level;
        }
    }
}
