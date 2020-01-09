using dnSpy.Themes;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using MediaColor = System.Windows.Media.Color;

namespace DnthemeToResourceDictionary
{
    class Transfer
    {
        static readonly string THEMES = "themes";
        static readonly string DNTHEME = "*.dntheme";
        static readonly string OUTPUTDIR = "resources";
        static readonly XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        static void WriteMessage(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        static int Run(string[] args)
        {
            var themePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, THEMES);
            var themeDir = new DirectoryInfo(themePath);
            if (!themeDir.Exists ||
                themeDir.GetFiles(DNTHEME).Length == 0)
            {
                WriteMessage("请将{0}文件放到{1}文件夹下", DNTHEME, themePath);
                return -1;
            }

            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OUTPUTDIR);
            Directory.CreateDirectory(outputPath);

            var service = new ThemeService();
            var xml = new XDocument();
            foreach (var theme in service.AllThemes)
            {
                var rd = new ResourceDictionary();
                var root = CreateRootElement(xml);
                var name = theme.Name ?? theme.Guid.ToString();
                WriteMessage("正在处理{0}", name);
                foreach (var kv in theme.EnumerateResourceKeyValues())
                {
                    if (kv.Item2 is MediaColor color)
                    {
                        CreateColorElement(root, kv.Item1.ToString(), color);
                    }
                    else if (kv.Item2 is Brush brush)
                    {
                        CreateBrushElement(root, kv.Item1.ToString(), brush);
                    }
                    else if (kv.Item2 is LinearGradientBrush linearGradientBrush)
                    {
                        CreateLinearGradientBrushElement(root, kv.Item1.ToString(), linearGradientBrush);
                    }
                    else if (kv.Item2 is DrawingBrush drawingBrush)
                    {
                        CreateDrawingBrushElement(root, kv.Item1.ToString(), drawingBrush);
                    }
                    else
                    {
                        continue;
                    }
                }
                var outFile = Path.Combine(outputPath, name) + ".xaml";
                WriteMessage("输出资源字典文件{0}", outFile);
                xml.Save(outFile, SaveOptions.None);
            }

            WriteMessage("完成，按任意键退出...");
            Console.ReadKey();

            return 0;
        }

        private static void CreateDrawingBrushElement(XElement root, string key, DrawingBrush drawingBrush)
        {
            //<DrawingBrush x:Key="aaa" TileMode="FlipX" ViewboxUnits="Absolute" ViewportUnits="Absolute" Viewbox="0, 0, 5, 4" Viewport="0, 0, 5, 4">
            //    <DrawingBrush.Drawing>
            //        <GeometryDrawing Brush="AliceBlue">
            //            <GeometryDrawing.Geometry>
            //                <GeometryGroup>
            //                    <RectangleGeometry Rect="0, 0, 1, 1"/>
            //                    <RectangleGeometry Rect="4, 0, 1, 1"/>
            //                    <RectangleGeometry Rect="2, 2, 1, 1"/>
            //                </GeometryGroup>
            //            </GeometryDrawing.Geometry>
            //        </GeometryDrawing>
            //    </DrawingBrush.Drawing>
            //</DrawingBrush>
            var element = new XElement("DrawingBrush",
                new XAttribute(x + "Key", key),
                new XAttribute("TileMode", drawingBrush.TileMode),
                new XAttribute("ViewboxUnits", drawingBrush.ViewboxUnits),
                new XAttribute("ViewportUnits", drawingBrush.ViewportUnits),
                new XAttribute("Viewbox", drawingBrush.Viewbox),
                new XAttribute("Viewport", drawingBrush.Viewport));
            var dr = new XElement("DrawingBrush.Drawing");
            if (drawingBrush.Drawing is GeometryDrawing geometryDrawing)
            {
                var gd = new XElement("GeometryDrawing",
                    new XAttribute("Brush", geometryDrawing.Brush));
                var geometry = new XElement("GeometryDrawing.Geometry");
                var group = new XElement("GeometryGroup");
                if (geometryDrawing.Geometry is GeometryGroup geometryGroup)
                {
                    foreach (RectangleGeometry item in geometryGroup.Children)
                    {
                        var child = new XElement("RectangleGeometry",
                            new XAttribute("Rect", item.Rect));
                        group.Add(child);
                    }
                }
                geometry.Add(group);
                gd.Add(geometry);
                dr.Add(gd);
            }
            element.Add(dr);
            root.Add(element);
        }

        private static void CreateLinearGradientBrushElement(XElement root, string key, LinearGradientBrush linearGradientBrush)
        {
            //<!--PressedBrush-->
            //<LinearGradientBrush x:Key="PressedBrush" EndPoint="0.5,0.971" StartPoint="0.5,0.042">
            //    <GradientStop Color="#4C000000" Offset="0" />
            //    <GradientStop Color="#26FFFFFF" Offset="1" />
            //    <GradientStop Color="#4C000000" Offset="0.467" />
            //    <GradientStop Color="#26FFFFFF" Offset="0.479" />
            //</LinearGradientBrush>
            var element = new XElement("LinearGradientBrush",
                new XAttribute(x + "Key", key),
                new XAttribute("StartPoint", linearGradientBrush.StartPoint),
                new XAttribute("EndPoint", linearGradientBrush.EndPoint));
            foreach (var gradientStop in linearGradientBrush.GradientStops)
            {
                var child = new XElement("GradientStop",
                    new XAttribute("Color", gradientStop.Color),
                    new XAttribute("Offset", gradientStop.Offset));
                element.Add(child);
            }
            root.Add(element);
        }

        private static void CreateBrushElement(XElement root, string key, Brush brush)
        {
            //<SolidColorBrush x:Key="TextBrush" Color="#FFFFFFFF" />
            var element = new XElement("SolidColorBrush",
                new XAttribute(x + "Key", key),
                new XAttribute("Color", brush));
            root.Add(element);
        }

        private static void CreateColorElement(XElement root, string key, System.Windows.Media.Color color)
        {
            //<Color x:Key="DefaultColor">#FF9BB1C5</Color>
            var element = new XElement("Color",
                new XAttribute(x + "Key", key),
                //new XAttribute("A", color.A),
                //new XAttribute("R", color.R),
                //new XAttribute("G", color.G),
                //new XAttribute("B", color.B)
                color);
            root.Add(element);
        }

        private static XElement CreateRootElement(XDocument xml)
        {
            var element = new XElement("ResourceDictionary",
                new XAttribute("xmlns", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
                new XAttribute(XNamespace.Xmlns + "x", x));
            xml.Add(element);
            return element;
        }
    }
}
