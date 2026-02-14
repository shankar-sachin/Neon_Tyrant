using Raylib_cs;

namespace NeonTyrant;

public static class IconBuilder
{
    public static Image Build32x32()
    {
        var bg = new Color(10, 10, 26, 255);
        var cyan = new Color(0, 200, 255, 255);
        var white = new Color(255, 255, 255, 255);
        var glow = new Color(0, 120, 180, 200);
        var magenta = new Color(255, 0, 255, 255);

        string[] pattern =
        [
            "................................",
            "................................",
            "..GGGGGG..........GGGGGGGG....",
            "..GCCCCG..........GCCCCCCG....",
            "..GCWCCG..........GCCWCCCG....",
            "..GCWWCG..........GCCWCCCG....",
            "..GCWCWG..........GCCWCCCG....",
            "..GCWCCWG.........GCCWCCCG....",
            "..GCWCCCWG........GCCWCCCG....",
            "..GCWCCCCG........GCCCCCWG....",
            "..GCWCCCCG........GCCCCWCG....",
            "..GCWCCCCG........GCCCWCCG....",
            "..GCWCCCCG........GCCWCCCG....",
            "..GCWCCCCG........GCWCCCCG....",
            "..GCWCCCCG........GWCCCCCG....",
            "..GCCCCCCG........GCCCCCCG....",
            "..GGGGGGGG........GGGGGGGG....",
            "................................",
            "................................",
            "................................",
            "......MMMMMMMMMMMMMMMM........",
            "......MMMMMMMMMMMMMMMM........",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
            "................................",
        ];

        var img = Raylib.GenImageColor(32, 32, bg);
        for (var y = 0; y < 32; y++)
        {
            var row = pattern[y];
            for (var x = 0; x < 32 && x < row.Length; x++)
            {
                var color = row[x] switch
                {
                    'C' => cyan,
                    'W' => white,
                    'G' => glow,
                    'M' => magenta,
                    _ => (Color?)null,
                };
                if (color.HasValue)
                    Raylib.ImageDrawPixel(ref img, x, y, color.Value);
            }
        }

        return img;
    }
}
