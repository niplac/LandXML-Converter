using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

public static class LandXmlShift
{
    // Easting shift only
    private const double EastingShift = -32000000.0;

    public static void Convert(string inputPath, string outputPath)
    {
        XDocument xml = XDocument.Load(inputPath);

        Fix2DLists(xml);
        Fix3DLists(xml);
        FixCoordGeom(xml);
        UpdateCoordinateSystemNode(xml);

        xml.Save(outputPath);
    }

    // ---- FIX PntList2D ----
    private static void Fix2DLists(XDocument xml)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == "PntList2D");

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Trim()
                .Split(new[] { ' ', '\t', '\n', '\r' },
                       StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += 2)
            {
                // ALWAYS assume LandXML = (northing, easting)
                double north = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double east  = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);

                // Apply ONLY to Easting (the 32… million number)
                east += EastingShift;

                parts[i]     = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);
            }

            n.Value = string.Join(" ", parts);
        }
    }

    // ---- FIX PntList3D ----
    private static void Fix3DLists(XDocument xml)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == "PntList3D");

        foreach (var n in nodes)
        {
            var parts = n.Value
                .Trim()
                .Split(new[] { ' ', '\t', '\n', '\r' },
                       StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += 3)
            {
                double north = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double east  = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);
                double z     = double.Parse(parts[i + 2], CultureInfo.InvariantCulture);

                // Shift only easting
                east += EastingShift;

                parts[i]     = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 2] = z.ToString("F3", CultureInfo.InvariantCulture);
            }

            n.Value = string.Join(" ", parts);
        }
    }

    // ---- FIX <Start>, <End>, <PI>, <Center>, etc ----
    private static void FixCoordGeom(XDocument xml)
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
                .Trim()
                .Split(new[] { ' ', '\t', '\n', '\r' },
                       StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2) continue;

            double north = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double east  = double.Parse(parts[1], CultureInfo.InvariantCulture);

            east += EastingShift;

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
