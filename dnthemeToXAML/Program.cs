using dnSpy.Themes;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace dnthemeToXAML
{
    class Program
    {
        static readonly string THEMES = "themes";
        static readonly string DNTHEME = "*.dntheme";
        static readonly string OUTPUTDIR = "resources";

        static void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        static void Main(string[] args)
        {
            var themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, THEMES);
            var themeDir = new DirectoryInfo(themePath);
            if (!themeDir.Exists ||
                themeDir.GetFiles(DNTHEME).Length == 0)
            {
                WriteMessage("请将{0}文件放到{1}文件夹下", DNTHEME, themePath);
                return;
            }

            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OUTPUTDIR);
            Directory.CreateDirectory(outputPath);

            var service = new ThemeService();
            foreach (var theme in service.AllThemes)
            {
                var rd = new ResourceDictionary();
                var name = theme.Name ?? theme.Guid.ToString();
                WriteMessage("正在处理{0}", name);
                foreach (var kv in theme.EnumerateResourceKeyValues())
                {
                    rd.Add(kv.Item1, kv.Item2);
                }
                var outFile = Path.Combine(outputPath, name) + ".xaml";
                WriteMessage("输出资源字典文件{0}", outFile);
                using (var writer = new XmlTextWriter(outFile, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    XamlWriter.Save(rd, writer);
                }
            }

            WriteMessage("完成，按任意键退出...");
            Console.ReadKey();
        }
    }
}
