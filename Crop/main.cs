using System;
using System.Collections;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace Crop
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        private Document Document;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            this.Document = commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector viewFilter = new FilteredElementCollector(this.Document);
            viewFilter.OfClass(typeof(View));

            ArrayList views = new ArrayList();
            String confirmationMessage = "";
            foreach (Element e in viewFilter)
            {
                View v = (View)e;
                if (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.Elevation)
                {
                    if (v.Name.EndsWith("View"))
                    {
                        views.Add(v);
                        confirmationMessage = confirmationMessage + "-" + v.Name + "\n";
                    }
                }
            }

            TaskDialog confirmation = new TaskDialog("Crop");
            confirmation.MainInstruction = "Are these the correct views?";
            confirmation.MainContent = confirmationMessage;
            confirmation.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
            confirmation.DefaultButton = TaskDialogResult.Yes;

            if (confirmation.Show() == TaskDialogResult.Yes)
            {

                UIDocument uidoc = new UIDocument(this.Document);
                Selection sel = uidoc.Selection;
                Element element = this.Document.GetElement(sel.PickObject(ObjectType.Element, "Pick an element"));

                using (Transaction transaction = new Transaction(this.Document))
                {
                    if (transaction.Start("Setting crop") == TransactionStatus.Started)
                    {
                        foreach (object obj in views)
                        {
                            SizeCropToElement((View)obj, element);
                        }

                        if (transaction.Commit() == TransactionStatus.Committed)
                        {
                            return Result.Succeeded;
                        }
                    }
                }
            } else
            {
                return Result.Cancelled;
            }
            return Result.Failed;
        }

        #region Constantes
        private Double padding = 0.01;
        private const int XY = 0;
        private const int XZ = 1;
        private const int YZ = 2;
        #endregion

        #region Point manipulation
        private double GetPoint(XYZ src, int mode, bool horizontal)
        {
            if (horizontal)
            {
                if (mode == XY || mode == XZ)
                {
                    return src.X;
                }
                else
                {
                    return src.Y;
                }
            }
            else
            {
                if (mode == XZ || mode == YZ)
                {
                    return src.Z;
                }
                else
                {
                    return src.Y;
                }
            }
        }

        private XYZ ReturnXYZ(double primary, double secondary, int mode)
        {
            if (mode == YZ)
            {
                return new XYZ(0, primary, secondary);
            }
            if (mode == XZ)
            {
                return new XYZ(primary, 0, secondary);
            }
            else
            {
                return new XYZ(primary, secondary, 0);
            }
        }
        #endregion

        private XYZ[] CreatePoints(BoundingBoxXYZ bounds, int mode)
        {
            XYZ[] points = new XYZ[4];

            // Order square points are placed
            Boolean[][] pointOrder = new Boolean[][] { new Boolean[] { false, false }, new Boolean[] { false, true }, new Boolean[] { true, true }, new Boolean[] { true, false } };

            Transform trans = bounds.Transform;

            XYZ upper_right = trans.OfPoint(bounds.Max);
            XYZ lower_left = trans.OfPoint(bounds.Min);

            for (int currentPoint = 0; currentPoint <= 3; currentPoint++)
            {
                Double primary;
                if (pointOrder[currentPoint][0])
                {
                    primary = GetPoint(upper_right, mode, true) + padding;
                }
                else
                {
                    primary = GetPoint(lower_left, mode, true) - padding;
                }

                Double secondary;
                if (pointOrder[currentPoint][1])
                {
                    secondary = GetPoint(upper_right, mode, false) + padding;
                }
                else
                {
                    secondary = GetPoint(lower_left, mode, false) - padding;
                }

                points[currentPoint] = ReturnXYZ(primary, secondary, mode);
            }

            return points;
        }

        private CurveLoop CreateShape(BoundingBoxXYZ bounds, int mode)
        {
            XYZ[] points = CreatePoints(bounds, mode);

            CurveLoop shape = new CurveLoop();
            for (int currentPoint = 0; currentPoint <= 3; currentPoint++)
            {
                XYZ first = points[currentPoint];
                XYZ second = points[(currentPoint + 1) % 4];
                shape.Append(Line.CreateBound(first, second));
            }

            return shape;
        }

        private int GetViewDirection(View view)
        {
            XYZ direction = view.ViewDirection;

            double X = Math.Round(Math.Abs(direction.X));
            double Y = Math.Round(Math.Abs(direction.Y));
            double Z = Math.Round(Math.Abs(direction.Z));

            if (X == 1 && Y == 0 && Z == 0)
            {
                return YZ;
            }
            if (X == 0 && Y == 1 && Z == 0)
            {
                return XZ;
            }
            else
            {
                return XY;
            }
        }

        private void SizeCropToElement(View view, Element element)
        {
            CurveLoop cropShape = CreateShape(element.get_BoundingBox(view), GetViewDirection(view));
            view.GetCropRegionShapeManager().SetCropShape(cropShape);
        }

    }
}
