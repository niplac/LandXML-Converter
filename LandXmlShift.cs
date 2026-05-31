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

        ProcessNodeLists(xml, "PntList2D", 2);
        ProcessNodeLists(xml, "PntList3D", 3);
        ProcessSimpleNodes(xml);
        UpdateCoordinateSystemNode(xml);

        xml.Save(outputPath);
    }

    private static void ProcessNodeLists(XDocument xml, string elementName, int dim)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == elementName);

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Split(new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += dim)
            {
                double a = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double b = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);

                double north, east;

                // Detect which is Easting by its magnitude:
                if (a > b)
                {
                    east = a; north = b;
                }
                else
                {
                    north = a; east = b;
                }

                // Apply shift ONLY to Easting:
                east += Shift;

                // Write them back maintaining N E order:
                parts[i]     = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);

                // keep Z unchanged if dim == 3
            }

            n.Value = string.Join(" ", parts);
        }
    }

    private static void ProcessSimpleNodes(XDocument xml)
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
            var parts = n.Value.Split(new[] { ' ', '\n', '\r', '\t' },
                                      StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) continue;

            double a = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double b = double.Parse(parts[1], CultureInfo.InvariantCulture);

            double north, east;

            if (a > b) { east = a; north = b; }
            else       { north = a; east = b; }

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

        string wkt =
            "PROJCS[\"ETRS89 / UTM zone 32N\"," +
            "GEOGCS[\"ETRS89\"," +
            "DATUM[\"European_Terrestrial_Reference_System_1989\"," +
            "SPHEROID[\"GRS 1980\",6378137,298.257222101]]," +
            "PRIMEM[\"Greenwich\",0]," +
            "UNIT[\"Degree\",0.0174532925199433]]," +
            "PROJECTION[\"Transverse_Mercator\"]," +
            "PARAMETER[\"latitude_of_origin\",0]," +
            "PARAMETER[\"central_meridian\",9]," +
            "PARAMETER[\"scale_factor\",0.9996]," +
            "PARAMETER[\"false_easting\",500000]," +
            "PARAMETER[\"false_northing\",0]," +
            "UNIT[\"Meter\",1]]";

        cs.SetAttributeValue("ogcWktCode", wkt);
    }
}
