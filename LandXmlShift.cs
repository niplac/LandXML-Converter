using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

public static class LandXmlShift
{
    // Easting offset to remove the m32 false easting
    private const double EastingShift = -32000000.0;

    public static void Convert(string inputPath, string outputPath)
    {
        XDocument xml = XDocument.Load(inputPath);

        FixCoordLists(xml, "PntList2D", 2);
        FixCoordLists(xml, "PntList3D", 3);
        FixCoordNodes(xml);
        FixCoordinateSystemMeta(xml);

        xml.Save(outputPath);
    }

    // ==============================
    // 1. FIXING PntList2D & PntList3D
    // ==============================
    private static void FixCoordLists(XDocument xml, string tag, int dim)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == tag);

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += dim)
            {
                double v1 = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double v2 = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);

                // Detect Easting by magnitude
                double north, east;
                if (v1 > v2)
                {
                    east = v1;
                    north = v2;
                }
                else
                {
                    north = v1;
                    east = v2;
                }

                // Apply ONLY to Easting
                east += EastingShift;

                // Write back in (N, E) order
                parts[i]     = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);
            }

            n.Value = string.Join(" ", parts);
        }
    }

    // ==============================
    // 2. FIXING Start/End/PI/Pnt nodes
    // ==============================
    private static void FixCoordNodes(XDocument xml)
    {
        var nodes = xml.Descendants()
            .Where(n => n.Name.LocalName == "Start"
                     || n.Name.LocalName == "End"
                     || n.Name.LocalName == "Center"
                     || n.Name.LocalName == "PI"
                     || n.Name.LocalName == "Pnt");

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) continue;

            double v1 = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double v2 = double.Parse(parts[1], CultureInfo.InvariantCulture);

            double north, east;
            if (v1 > v2)
            {
                east = v1;
                north = v2;
            }
            else
            {
                north = v1;
                east = v2;
            }

            east += EastingShift;

            parts[0] = north.ToString("F3", CultureInfo.InvariantCulture);
            parts[1] = east.ToString("F3", CultureInfo.InvariantCulture);

            n.Value = string.Join(" ", parts);
        }
    }

    // ==============================
    // 3. CIVIL 3D SAFE METADATA FIX
    // ==============================
    private static void FixCoordinateSystemMeta(XDocument xml)
    {
        var cs = xml.Descendants().FirstOrDefault(n => n.Name.LocalName == "CoordinateSystem");
        if (cs == null) return;

        // Remove all old attributes (to avoid Civil 3D auto transforming)
        cs.RemoveAttributes();

        // Civil 3D SAFE VERSION:
        // DO NOT PROVIDE WKT!
        // Civil 3D will ignore WKT and trust the drawing's coordinate system.
        cs.SetAttributeValue("desc", "NONE");
        cs.SetAttributeValue("horizontalDatum", "NONE");
        cs.SetAttributeValue("horizontalCoordinateSystemName", "NONE");
    }
}
