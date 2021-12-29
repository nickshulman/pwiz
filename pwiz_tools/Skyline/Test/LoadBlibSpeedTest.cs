using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTest
{
    [TestClass]
    public class LoadBlibSpeedTest : AbstractUnitTest
    {
        const int FILE_FLAG_NO_BUFFERING = unchecked((int)0x20000000);

        [DllImport("KERNEL32", SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        static extern SafeFileHandle CreateFile(
            String fileName,
            int desiredAccess,
            System.IO.FileShare shareMode,
            IntPtr securityAttrs,
            System.IO.FileMode creationDisposition,
            int flagsAndAttributes,
            IntPtr templateFile);

        [TestMethod]
        public void TestLoadBlibSpeed()
        {
            var inputFile =
                @"D:\skydata\20150901_Selevsek_yeast_shock\SWATH_data\OS\DIA-Umpire\Selevsek_Yeast_umpire_09.blib";

            var outputFile = Path.Combine(TestContext.TestDir, "MyBlibFile.blib");
            File.Copy(inputFile, outputFile, true);
            // long fileSize = new FileInfo(inputFile).Length;
            // using (var inputStream = new FileStream(inputFile, FileMode.Open))
            // {
            //     // var handle = CreateFile(outputFile, (int)FileAccess.Write, FileShare.None, IntPtr.Zero, FileMode.Create,
            //     //     FILE_FLAG_NO_BUFFERING, IntPtr.Zero);
            //     using (var outputStream = new FileStream(outputFile, FileMode.Create))
            //     {
            //         StreamEx.TransferBytes(inputStream, outputStream, fileSize);
            //     }
            // }

            var start = DateTime.UtcNow;
            var biblioSpecListSpec = new BiblioSpecLiteSpec("foo", outputFile);
            var library = BiblioSpecLiteLibrary.Load(biblioSpecListSpec, new DefaultFileLoadMonitor(new SilentProgressMonitor()));
            Assert.IsNotNull(library);
            var duration = DateTime.UtcNow.Subtract(start);
            Console.Out.WriteLine("Time to load library: {0}", duration.TotalMilliseconds);
            var startCached = DateTime.UtcNow;
            var cachedLibrary = CallFunction(()=>BiblioSpecLiteLibrary.Load(biblioSpecListSpec, new DefaultFileLoadMonitor(new SilentProgressMonitor())));
            Assert.IsNotNull(cachedLibrary);
            var durationCached = DateTime.UtcNow.Subtract(startCached);
            Console.Out.WriteLine("Time to load cached library: {0}", durationCached.TotalMilliseconds);
        }

        private T CallFunction<T>(Func<T> function)
        {
            return function();
        }
    }
}
