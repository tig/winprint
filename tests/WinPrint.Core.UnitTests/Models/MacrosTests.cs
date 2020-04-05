using System;
using System.IO;
using System.Reflection;
using WinPrint.Core.ContentTypeEngines;
using Xunit;
using Xunit.Abstractions;

namespace WinPrint.Core.UnitTests.Models
{
    public class MacrosTests : TestModelsBase
    {
        public MacrosTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void RealFileName_Test()
        {
            string file = Path.GetTempFileName();

            SheetViewModel svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            Macros macros = new Macros(svm);

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
            string file = Path.GetTempFileName();
            File.Delete(file);

            SheetViewModel svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            Macros macros = new Macros(svm);

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
            string file = Path.GetTempFileName();
            File.Delete(file);

            // Relpace the T in Temp with an invalid char
            file = file.Replace('T', Path.GetInvalidPathChars()[0]);

            SheetViewModel svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            Macros macros = new Macros(svm);

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
            string file = Path.GetTempFileName();
            File.Delete(file);

            // Make filename invalid 
            file = file.Replace(@"\tmp", @$"\{Path.GetInvalidFileNameChars()[0]}mp");

            SheetViewModel svm = new SheetViewModel();
            svm.File = file;
            svm.ContentEngine = new TextCte();
            Macros macros = new Macros(svm);

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
            string title = $"Invalid File Char:{Path.GetInvalidFileNameChars()[1]}. Invalid Path Char: {Path.GetInvalidPathChars()[1]}.";

            SheetViewModel svm = new SheetViewModel();
            svm.File = title;
            svm.ContentEngine = new TextCte();
            Macros macros = new Macros(svm);

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
            SheetViewModel svm = new SheetViewModel();
            svm.ContentEngine = new TextCte();
            return svm;
        }

        // Test file paths
        private string ExistingFile => Path.GetTempFileName();
        private string ExistingFileNoExtension
        {
            get
            {
                string f = Path.GetTempPath() + Path.GetFileNameWithoutExtension(Path.GetTempFileName());
                File.Create(f).Close();
                return f;
            }
        }
        private string NonExistingFile { get { string f = ExistingFile; File.Delete(f); return f; } }
        private string NonExistingFileNoExtension { get { string f = ExistingFileNoExtension; File.Delete(f); return f; } }
        private string InvalidFileName => Path.GetTempFileName().Replace(@"\tmp", @$"\{Path.GetInvalidFileNameChars()[0]}mp");
        private string InvalidDirectoryName => Path.GetTempFileName().Replace(@"\Temp", $@"\{Path.GetInvalidPathChars()[0]}emp");

        private string NullName => null;
        private string EmptyName = "";



        [Fact]
        public void FileExtension()
        {
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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
            SheetViewModel svm = SetupSVM();
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

            SheetViewModel svm = new SheetViewModel();
            Macros macros = new Macros(svm);

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
