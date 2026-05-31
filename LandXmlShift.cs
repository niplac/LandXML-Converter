using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

public static class LandXmlShift
{
    // LandXML uses (Northing, Easting) order.
    // Easting needs shift of -32,000,000 to convert to EPSG:25832.
    private const double EastingShift = -32000000.0;
    private const double NorthingShift = 0.0;

    public static void Convert(string inputPath, string outputPath)
    {
        XDocument xml = XDocument.Load(inputPath);

        ShiftPntList(xml, "PntList2D", 2);
        ShiftPntList(xml, "PntList3D", 3);
        ShiftCoordGeom(xml);
        UpdateCoordinateSystemNode(xml);

        xml.Save(outputPath);
    }

    // Handles <PntList2D> and <PntList3D>
    private static void ShiftPntList(XDocument xml, string tag, int dim)
    {
        var nodes = xml.Descendants().Where(n => n.Name.LocalName == tag);

        foreach (var n in nodes)
        {
            var parts = n.Value.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i += dim)
            {
                // LandXML: parts[i] = Northing, parts[i+1] = Easting
                double north = double.Parse(parts[i], CultureInfo.InvariantCulture);
                double east  = double.Parse(parts[i + 1], CultureInfo.InvariantCulture);

                // Apply shift ONLY to easting
                east += EastingShift;
                north += NorthingShift;

                // Write back in correct N E order
                parts[i]     = north.ToString("F3", CultureInfo.InvariantCulture);
                parts[i + 1] = east.ToString("F3", CultureInfo.InvariantCulture);

                // If 3D, keep Z exactly as is
                if (dim == 3)
                {
                    // parts[i + 2] stays unchanged (Z)
                }
            }

            n.Value = string.Join(" ", parts);
        }
    }

    // Handles <Start>, <End>, <Center>, <PI>, <Pnt>
    private static void ShiftCoordGeom(XDocument xml)
    {
        var nodes = xml.Descendants().Where(n =>
            n.Name.LocalName == "Start"  ||
            n.Name.LocalName == "End"    ||
            n.Name.LocalName == "Center" ||
            n.Name.LocalName == "PI"     ||
            n.Name.LocalName == "Pnt"
        );

        foreach (var n in nodes)
        {
            var parts = n.Value.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                continue;

            double north = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double east  = double.Parse(parts[1], CultureInfo.InvariantCulture);

            east  += EastingShift;
            north += NorthingShift;

            parts[0] = north.ToString("F3", CultureInfo.InvariantCulture);
            parts[1] = east.ToString("F3", CultureInfo.InvariantCulture);

            n.Value = string.Join(" ", parts);
        }
    }

    // Updates the coordinate system metadata to EPSG:25832
    private static void UpdateCoordinateSystemNode(XDocument xml)
    {
        var cs = xml.Descendants()
                    .FirstOrDefault(n => n.Name.LocalName == "CoordinateSystem");

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
