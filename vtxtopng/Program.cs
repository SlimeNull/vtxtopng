using CommandLine;
using CommandLine.Text;
using System.Text.RegularExpressions;

internal class Program
{
    internal class Options
    {
        [Value(0, MetaName = "image", HelpText = "image file to crop", Required = true)]
        public string? ImagePath { get; set; }

        [Value(1, MetaName = "data", HelpText = "image position and size data file", Required = true)]
        public string? DataPath { get; set; }
    }

    private static void Main(string[] args)
    {
        Parser parser = new Parser((ss) =>
        {
            ss.HelpWriter = null;
        });

        ParserResult<Options> result = parser.ParseArguments<Options>(args);

        HelpText help = HelpText.AutoBuild(result, (h) =>
        {
            h.Copyright = "Copyright (C) 2022 SlimeNull";

            return h;
        });

        result
            .WithParsed(Run)
            .WithNotParsed(errs => Console.WriteLine(help));
    }

    private static void Run(Options options)
    {
        Regex reg = new Regex(@"(\s*\[\d+\.\d+\.0\]\s*uvCropped\s*=\s*\{\s*\(\s*(?<left1>-?\d+\.?-?\d*),\s*(?<top1>-?\d+\.?-?\d*)\s*\),\s*\(\s*(?<right1>-?\d+\.?-?\d*),\s*(?<bottom1>-?\d+\.?-?\d*)\s*\)\s*\})(.*\n)*?.*(\s*\[\d+\.\d+\.0\]\s*uvUncropped\s*=\s*\{\s*\(\s*(?<left2>-?\d+\.?-?\d*),\s*(?<top2>-?\d+\.?-?\d*)\s*\),\s*\(\s*(?<right2>-?\d+\.?-?\d*),\s*(?<bottom2>-?\d+\.?-?\d*)\s*\)\s*\})");

        Image origin = Image.FromFile(options.ImagePath!);
        string data = File.ReadAllText(options.DataPath!);

        string outputDir = Path.GetDirectoryName(options.ImagePath) is string originDir ?
            Path.Combine(originDir, "vtxtopng_output") : "vtxtopng_output";

        Directory.CreateDirectory(outputDir);

        int index = 0;
        foreach (Match match in reg.Matches(data))
        {
            float
                left1 = float.Parse(match.Groups["left1"].Value),
                top1 = float.Parse(match.Groups["top1"].Value),
                right1 = float.Parse(match.Groups["right1"].Value),
                bottom1 = float.Parse(match.Groups["bottom1"].Value),
                left2 = float.Parse(match.Groups["left2"].Value),
                top2 = float.Parse(match.Groups["top2"].Value),
                right2 = float.Parse(match.Groups["right2"].Value),
                bottom2 = float.Parse(match.Groups["bottom2"].Value);

            string outputName = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(options.ImagePath)}{index}{Path.GetExtension(options.ImagePath)}");

            SizeF spriteDrawSize = new SizeF(
                origin.Width * (right1 - left1),
                origin.Height * (bottom1 - top1));
            SizeF spriteSize = new SizeF(
                origin.Width * (right2 - left2),
                origin.Height * (bottom2 - top2));
            PointF spriteSrcPos = new PointF(
                origin.Width * left1,
                origin.Height * top1);
            PointF spriteDestPos = new PointF(
                origin.Width * (left1 - left2),
                origin.Height * (top1 - top2));
            using Bitmap sprite = new Bitmap(
                (int)spriteSize.Width,
                (int)spriteSize.Height);
            using Graphics g = Graphics.FromImage(sprite);
            g.DrawImage(origin, new RectangleF(spriteDestPos, spriteDrawSize), new RectangleF(spriteSrcPos, spriteDrawSize), GraphicsUnit.Pixel);

            sprite.SetResolution(origin.HorizontalResolution, origin.VerticalResolution);
            sprite.Save(outputName);

            index++;
        }
    }
}