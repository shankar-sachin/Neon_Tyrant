using Raylib_cs;

namespace NeonTyrant;

public static class PixelChars
{
    private static readonly Color T = new(0, 0, 0, 0);

    public static readonly Color BgColor = new(10, 10, 26, 255);
    private static readonly Color GridLine = new(18, 18, 40, 255);

    private static Color[,] Build(string pattern, Dictionary<char, Color> palette, Color? fill = null)
    {
        var def = fill ?? T;
        var lines = pattern.Split('\n');
        var result = new Color[8, 8];
        for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                var ch = y < lines.Length && x < lines[y].Length ? lines[y][x] : '.';
                result[y, x] = palette.GetValueOrDefault(ch, def);
            }
        return result;
    }

    // Player @ - white body with cyan glow outline
    public static readonly Color[,] Player = Build(
        "........\n...@@...\n..@##@..\n..@##@..\n...@@...\n..@..@..\n..@..@..\n........",
        new() { { '@', new(0, 200, 255, 255) }, { '#', new(255, 255, 255, 255) } });

    // Enemy M - hot magenta body
    public static readonly Color[,] Enemy = Build(
        "........\n..MMMM..\n.M####M.\n.M#MM#M.\n.M####M.\n..M..M..\n...MM...\n........",
        new() { { 'M', new(255, 0, 255, 255) }, { '#', new(200, 0, 200, 255) } });

    // Boss B - dark/bright red
    public static readonly Color[,] Boss = Build(
        "........\n.BBBBBB.\n.B####B.\n.B#BB#B.\n.B####B.\n.BBBBBB.\n...BB...\n........",
        new() { { 'B', new(200, 0, 0, 255) }, { '#', new(255, 50, 50, 255) } });

    // Shard * - golden yellow diamond
    public static readonly Color[,] Shard = Build(
        "........\n....*...\n...***...\n..*****..\n...***...\n....*...\n........\n........",
        new() { { '*', new(255, 255, 0, 255) } });

    // Wall # - cool blue-gray brick
    public static readonly Color[,] Wall = Build(
        "########\n#=####=#\n##====##\n##=##=##\n##====##\n#=####=#\n########\n########",
        new() { { '#', new(40, 50, 80, 255) }, { '=', new(55, 65, 100, 255) } });

    // Platform = - lighter blue with highlight top edge
    public static readonly Color[,] Platform = Build(
        "HHHHHHHH\n========\n=#==#==#\n========\n==#===#=\n========\n===#=#==\n========",
        new() { { '=', new(60, 80, 140, 255) }, { '#', new(80, 100, 160, 255) }, { 'H', new(100, 120, 180, 255) } });

    // Hazard ^ - red/orange spike pointing up
    public static readonly Color[,] HazardSpike = Build(
        "........\n...R....\n...RR...\n..ORR...\n..ORRO..\n.OORRO..\n.OORROO.\nOOORROOO",
        new() { { 'R', new(255, 34, 34, 255) }, { 'O', new(255, 140, 0, 255) } });

    // Checkpoint C - green flag/beacon
    public static readonly Color[,] CheckpointFlag = Build(
        "..PFF...\n..PFFF..\n..PFFFF.\n..PFF...\n..P.....\n..P.....\n..P.....\n.PPP....",
        new() { { 'P', new(0, 200, 100, 255) }, { 'F', new(0, 255, 136, 255) } });

    // Exit E - cyan swirling portal
    public static readonly Color[,] ExitPortal = Build(
        "..=##=..\n.=#..#=.\n=#..#.#=\n#..#=..#\n#..=#..#\n=#.#..#=\n.=#..#=.\n..=##=..",
        new() { { '#', new(0, 255, 255, 255) }, { '=', new(0, 180, 220, 255) } });

    // Heart - red pixel heart for lives display
    public static readonly Color[,] Heart = Build(
        "........\n.##.##..\n########\n########\n.######.\n..####..\n...##...\n........",
        new() { { '#', new(255, 40, 80, 255) } });

    // Background - dark grid pattern
    public static readonly Color[,] Background = Build(
        ".......G\n.......G\n.......G\n.......G\n.......G\n.......G\n.......G\nGGGGGGGG",
        new() { { 'G', GridLine } },
        BgColor);

    public static Color[,]? GetTile(char tile) => tile switch
    {
        '#' => Wall,
        '=' => Platform,
        '^' => HazardSpike,
        '*' => Shard,
        'C' => CheckpointFlag,
        'E' => ExitPortal,
        '@' => Player,
        'M' => Enemy,
        'B' => Boss,
        ' ' => Background,
        _ => null,
    };
}
