using System.IO;
using WinPrint.Core.ContentTypeEngines;
using Xunit;


namespace WinPrint.Core
{
    public class MacrosTests
    {
        [Fact]
        public void ReplaceMacro_Test()
        {
            var svm = new SheetViewModel();
            //svm.NumSheets = 99;
            svm.File = @"c:\path\file.ext";
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);
            macros.Page = 2;

            var result = macros.ReplaceMacro(@"{NumPages}");
            Assert.Equal("0", result);

            result = macros.ReplaceMacro(@"{FileExtension}");
            Assert.Equal(".ext", result);

            result = macros.ReplaceMacro(@"{FileNameWithoutExtension}");
            Assert.Equal("file", result);

            result = macros.ReplaceMacro(@"{FileName}");
            Assert.Equal("file.ext", result);

            result = macros.ReplaceMacro(@"{FilePath}");
            Assert.Equal(@"c:\path", result);

            result = macros.ReplaceMacro(@"{FullyQualifiedPath}");
            Assert.Equal(@"c:\path\file.ext", result);

            //result = macros.ReplaceMacro(@"{DatePrinted}");
            //Assert.Equal($"{DateTime.Now}", result);

            var lwt = File.GetLastWriteTime(svm.File);
            result = macros.ReplaceMacro(@"{DateRevised}");
            Assert.Equal($"{lwt}", result);

            result = macros.ReplaceMacro(@"{FileType}");
            Assert.Equal($"text/plain", result);

            result = macros.ReplaceMacro(@"{Page}");
            Assert.Equal($"2", result);
        }
    }
}
