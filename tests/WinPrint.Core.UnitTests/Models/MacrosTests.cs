using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WinPrint.Core.ContentTypeEngines;
using Xunit;


namespace WinPrint.Core.UnitTests.Models
{
    public class MacrosTests
    {
        [Fact]
        public void RealFileName_Test()
        {
            var file = Path.GetTempFileName();

            var svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacro(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacro(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacro(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacro(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacro(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacro(@"{FullPath}"));
            Assert.Equal($"{File.GetLastWriteTime(file)}", macros.ReplaceMacro(@"{DateRevised}"));
            Assert.Equal($"{File.GetCreationTime(file)}", macros.ReplaceMacro(@"{DateCreated}"));

            File.Delete(svm.File);
        }

        [Fact]
        public void NonExistantGoodFileName_Test()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);

            var svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacro(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacro(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacro(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacro(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacro(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacro(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateCreated}"));
        }

        [Fact]
        public void NonExistantBogusPathFileName_Test()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);

            // Relpace the T in Temp with an invalid char
            file = file.Replace('T', Path.GetInvalidPathChars()[0]);

            var svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacro(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacro(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacro(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacro(@"{FileNameWithoutExtension}"));
            // return original path
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacro(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacro(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateCreated}"));
        }

        [Fact]
        public void NonExistantBogusFileNameFileName_Test()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);

            // Make filename invalid 
            file = file.Replace(@"\tmp", @$"\{Path.GetInvalidFileNameChars()[0]}mp");

            var svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);

            // return original file
            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacro(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacro(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacro(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacro(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacro(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacro(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateCreated}"));
        }

        [Fact]
        public void TitleAsFileName_Test()
        {
            var title = $"Invalid File Char:{Path.GetInvalidFileNameChars()[1]}. Invalid Path Char: {Path.GetInvalidPathChars()[1]}.";

            var svm = new SheetViewModel();
            svm.File = title;
            svm.ContentEngine = new TextCte();
            var macros = new Macros(svm);

            // return original file
            Assert.Equal("", macros.ReplaceMacro(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(title), macros.ReplaceMacro(@"{FileName}"));
            Assert.Equal(Path.GetFileName(title), macros.ReplaceMacro(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(title), macros.ReplaceMacro(@"{FileNameWithoutExtension}"));
            Assert.Equal("", macros.ReplaceMacro(@"{FileDirectoryName}"));
            Assert.Equal(title, macros.ReplaceMacro(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro(@"{DateCreated}"));
        }

        private SheetViewModel SetupSVM()
        {
            var svm = new SheetViewModel();
            svm.ContentEngine = new TextCte();
            return svm;
        }

        // Test file paths
        private string ExistingFile => Path.GetTempFileName();
        private string ExistingFileNoExtension { get {
                var f = Path.GetTempPath() + Path.GetFileNameWithoutExtension(Path.GetTempFileName());
                File.Create(f).Close();
                return f;
            }
        }
        private string NonExistingFile { get { var f = ExistingFile; File.Delete(f); return f; } }
        private string NonExistingFileNoExtension { get { var f = ExistingFileNoExtension; File.Delete(f); return f; } }
        private string InvalidFileName => Path.GetTempFileName().Replace(@"\tmp", @$"\{Path.GetInvalidFileNameChars()[0]}mp");
        private string InvalidDirectoryName => Path.GetTempFileName().Replace(@"\Temp", $@"\{Path.GetInvalidPathChars()[0]}emp");

        private string NullName => null;
        private string EmptyName = "";

        [Fact]
        public void FileExtension()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileName()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void Title()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(svm.File, macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileNameWithoutExtension()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileDirectoryName()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FullPath()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void DatePrinted()
        {
            // No way to test since uses DateTime.Now - assume DateRevised tests cover
        }

        [Fact]
        public void DateRevised()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal($"{File.GetLastWriteTime(svm.File)}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal($"{File.GetLastWriteTime(svm.File)}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }


        [Fact]
        public void DateCreated()
        {
            var svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal($"{File.GetCreationTime(svm.File)}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal($"{File.GetCreationTime(svm.File)}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacro($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public async void FileType()
        {
            // https://stackoverflow.com/questions/22598323/movenext-instead-of-actual-method-task-name
            string macroName = "FileType";

            var svm = new SheetViewModel();
            var macros = new Macros(svm);

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/plain").ConfigureAwait(true);
            Assert.Equal("text/plain", svm.ContentEngine.GetContentTypeName());
            Assert.Equal(svm.ContentEngine.GetContentTypeName(), macros.ReplaceMacro($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.txt")).ConfigureAwait(true);
            //Assert.Equal("text/plain", svm.ContentEngine.GetContentType());
            //Assert.Equal(svm.ContentEngine.GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/html").ConfigureAwait(true);
            Assert.Equal("text/html", svm.ContentEngine.GetContentTypeName());
            Assert.Equal(svm.ContentEngine.GetContentTypeName(), macros.ReplaceMacro($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.html")).ConfigureAwait(true);
            //Assert.Equal("text/html", svm.ContentEngine.GetContentType());
            //Assert.Equal(svm.ContentEngine.GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/code").ConfigureAwait(true);
            Assert.Equal("text/code", ((PrismCte)svm.ContentEngine).GetContentTypeName());
            Assert.Equal(((PrismCte)svm.ContentEngine).GetContentTypeName(), macros.ReplaceMacro($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.cs")).ConfigureAwait(true);
            //Assert.Equal("csharp", ((PrismCte)svm.ContentEngine).GetContentType());
            //Assert.Equal(((PrismCte)svm.ContentEngine).GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

        }
    }
}
