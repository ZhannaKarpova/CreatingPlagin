using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace CreationModelPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreationModelBase1
    {
        private Result AddRoof(Document doc, Level level1, Wall wall)
        {
            RoofType roofType = new FilteredElementCollector(doc)
               .OfClass(typeof(RoofType))
               .OfType<RoofType>()
               .Where(x => x.Name.Equals("Типовой - 400мм"))
               .Where(x => x.FamilyName.Equals("Базовая крыша"))
               .FirstOrDefault();

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
        }
    }
}