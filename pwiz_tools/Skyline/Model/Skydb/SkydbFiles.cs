using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydbStorage.SkylineDocument;

namespace pwiz.Skyline.Model.Skydb
{
    public class SkydbFiles
    {
        public static ChromatogramCacheAdapter TryLoadSkydbFile(string documentFilePath)
        {
            var skydbPath = Path.ChangeExtension(documentFilePath, ".skydb");
            if (!File.Exists(skydbPath))
            {
                return null;
            }

            var skydbDocument = new SkylineDocumentImpl(skydbPath);
            return new ChromatogramCacheAdapter(skydbPath, skydbDocument.ExtractedDataFiles);
        }
    }
}
