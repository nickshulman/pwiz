using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model;
using pwiz.Skyline.Model.Lib;
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
            foreach (var filename in new[]{ "liborig.blib", "libvacuum.blib"})
            {
                var originalFile = Path.Combine(@"D:\skydata\20150901_Selevsek_yeast_shock\SWATH_data\OS\DIA-Umpire", filename);
                var libraryFile = Path.Combine(TestContext.TestDir, "testFile.blib");
                File.Copy(originalFile, libraryFile, true);
                var start = DateTime.UtcNow;
                var biblioSpecListSpec = new BiblioSpecLiteSpec("foo", libraryFile);
                var library = BiblioSpecLiteLibrary.Load(biblioSpecListSpec, new DefaultFileLoadMonitor(new SilentProgressMonitor()));
                Assert.IsNotNull(library);
                var duration = DateTime.UtcNow.Subtract(start);
                Console.Out.WriteLine("Time to load library {0}: {1}", filename, duration.TotalMilliseconds);
                var startCached = DateTime.UtcNow;
                var cachedLibrary = CallFunction(() => BiblioSpecLiteLibrary.Load(biblioSpecListSpec, new DefaultFileLoadMonitor(new SilentProgressMonitor())));
                Assert.IsNotNull(cachedLibrary);
                var durationCached = DateTime.UtcNow.Subtract(startCached);
                Console.Out.WriteLine("Time to load cached library {0}: {1}", filename, durationCached.TotalMilliseconds);
            }
        }

        private T CallFunction<T>(Func<T> function)
        {
            return function();
        }
    }
}
