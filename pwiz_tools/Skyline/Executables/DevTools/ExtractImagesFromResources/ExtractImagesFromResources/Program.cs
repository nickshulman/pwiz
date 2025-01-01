using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine;

namespace ExtractImagesFromResources
{
    internal class Program
    {
        private static Dictionary<string, byte[]> ImageHeaders = new Dictionary<string, byte[]>
        {
            { "png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            { "jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { "bmp", new byte[] { 0x42, 0x4d } },
            { "gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } }
        };
        public class Options
        {
            [Value(0)]
            public string Path { get; set; }
        }
        
        static int Main(string[] args)
        {
            int result = -1;
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => result = ExtractImages(options))
                .WithNotParsed(HandleParseError);
            return result;
        }

        public static int ExtractImages(Options options)
        {
            var path = Path.GetFullPath(options.Path);
            if (File.Exists(path))
            {
                ExtractImagesFromFile(path);
            }
            else if (Directory.Exists(path))
            {
                ExtractImagesFromFolder(path);
            }
            else
            {
                Console.Error.WriteLine("{0} does not exist", path);
                return -1;
            }

            return 0;
        }

        public static void ExtractImagesFromFile(string resxPath)
        {
            var doc = XDocument.Load(resxPath);
            bool changed = false;
            foreach (XElement el in doc.Root.Elements("data"))
            {
                if (el.Attribute("type")?.Value != "System.Drawing.Bitmap, System.Drawing")
                {
                    continue;
                }

                if (el.Attribute("mimetype")?.Value != "application/x-microsoft.net.object.bytearray.base64")
                {
                    continue;
                }

                var valueElement = el.Elements("value").FirstOrDefault();
                if (valueElement == null)
                {
                    continue;
                }

                var bytes = Convert.FromBase64String(valueElement.Value);
                var extension = GetImageExtension(bytes);
                if (extension == null)
                {
                    continue;
                }

                var folder = Path.GetDirectoryName(resxPath);

                var filename = el.Attribute("name").Value + "." + extension;
                var imageFileName = Path.Combine(folder!, filename);
                File.WriteAllBytes(imageFileName, bytes);
                valueElement.Value = filename;
                el.SetAttributeValue("mimetype", null);
                changed = true;
            }

            if (changed)
            {
                doc.Save(resxPath);
            }
        }

        public static void ExtractImagesFromFolder(string path)
        {
            foreach (var f in Directory.GetFiles(path))
            {
                if (f.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    ExtractImagesFromFile(f);
                }
            }
        }
        static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var e in errors)
            {
                if (e is HelpRequestedError || e is VersionRequestedError)
                {
                    continue;
                }
                Console.WriteLine($"Error: {e}");
            }
        }

        private static string GetImageExtension(byte[] bytes)
        {
            foreach (var entry in ImageHeaders)
            {
                var header = entry.Value;
                if (header.SequenceEqual(bytes.Take(header.Length)))
                {
                    return entry.Key;
                }
            }

            return null;
        }


    }
}
