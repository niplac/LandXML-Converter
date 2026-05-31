using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

public static class LandXmlShift
{
    private const double Shift = -32000000.0;

    public static void Convert(string inputPath, string outputPath)
    {
        XDocument xml = XDocument.Load(inputPath);

        ShiftList(xml, "PntList2D", 2);
        ShiftList(xml, "PntList3D", 3);
        ShiftSimpleNodes(xml);
        UpdateCoordinateSystemNode(xml);

        xml.Save(outputPath);
    }

    private static void ShiftList(XDocument xml, string tag, int dim)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == tag);

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += dim)
            {
                double a = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double b = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);

                double north, east;

                // If first value is EASTING (32 million), swap:
                if (a > b)
                {
                    east = a;
                    north = b;
                }
                else
                {
                    north = a;
                    east = b;
                }

                // Apply only to EASTING
                east += Shift;

                // Write N E back
                parts[i] = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);
            }

            n.Value = string.Join(" ", parts);
        }
    }

    private static void ShiftSimpleNodes(XDocument xml)
    {
        var nodes = xml.Descendants().Where(n =>
            n.Name.LocalName == "Start" ||
            n.Name.LocalName == "End" ||
            n.Name.LocalName == "Center" ||
            n.Name.LocalName == "PI" ||
            n.Name.LocalName == "Pnt"
        );

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                continue;

            double a = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double b = double.Parse(parts[1], CultureInfo.InvariantCulture);

            double north, east;

            // swap if first value is larger than second
            if (a > b)
            {
                east = a;
                north = b;
            }
            else
            {
                north = a;
                east = b;
            }

            east += Shift;

            parts[0] = north.ToString("F3", CultureInfo.InvariantCulture);
            parts[1] = east.ToString("F3", CultureInfo.InvariantCulture);

            n.Value = string.Join(" ", parts);
        }
    }

    private static void UpdateCoordinateSystemNode(XDocument xml)
    {
        var cs = xml.Descendants().FirstOrDefault(n => n.Name.LocalName == "CoordinateSystem");
        if (cs == null) return;

        cs.SetAttributeValue("desc", "ETRS89 / UTM zone 32N (EPSG:25832)");
        cs.SetAttributeValue("horizontalCoordinateSystemName", "EPSG:25832");
        cs.SetAttributeValue("horizontalDatum", "ETRS89");
    }
}
