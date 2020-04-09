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

            SheetViewModel svm = new SheetViewModel
            {
                File = file,
                ContentEngine = new TextCte()
            };
            Macros macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacros(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacros(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacros(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacros(@"{FullPath}"));
            Assert.Equal($"{File.GetLastWriteTime(file)}", macros.ReplaceMacros(@"{DateRevised}"));
            Assert.Equal($"{File.GetCreationTime(file)}", macros.ReplaceMacros(@"{DateCreated}"));

            File.Delete(svm.File);
        }

        [Fact]
        public void NonExistantGoodFileName_Test()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);

            SheetViewModel svm = new SheetViewModel
            {
                File = file,
                ContentEngine = new TextCte()
            };
            Macros macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacros(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacros(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacros(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacros(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateCreated}"));
        }

        [Fact]
        public void NonExistantBogusPathFileName_Test()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);

            // Relpace the T in Temp with an invalid char
            file = file.Replace('T', Path.GetInvalidPathChars()[0]);

            SheetViewModel svm = new SheetViewModel
            {
                File = file,
                ContentEngine = new TextCte()
            };
            Macros macros = new Macros(svm);

            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacros(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacros(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacros(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            // return original path
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacros(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateCreated}"));
        }

        [Fact]
        public void NonExistantBogusFileNameFileName_Test()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);

            // Make filename invalid 
            file = file.Replace(@"\tmp", @$"\{Path.GetInvalidFileNameChars()[0]}mp");

            SheetViewModel svm = new SheetViewModel
            {
                File = file,
                ContentEngine = new TextCte()
            };
            Macros macros = new Macros(svm);

            // return original file
            Assert.Equal(Path.GetExtension(file), macros.ReplaceMacros(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(file), macros.ReplaceMacros(@"{FileName}"));
            Assert.Equal(file, macros.ReplaceMacros(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacros(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateCreated}"));
        }

        [Fact]
        public void TitleAsFileName_Test()
        {
            string title = $"Invalid File Char:{Path.GetInvalidFileNameChars()[1]}. Invalid Path Char: {Path.GetInvalidPathChars()[1]}.";

            SheetViewModel svm = new SheetViewModel
            {
                File = title,
                ContentEngine = new TextCte()
            };
            Macros macros = new Macros(svm);

            // return original file
            Assert.Equal("", macros.ReplaceMacros(@"{FileExtension}"));
            Assert.Equal(Path.GetFileName(title), macros.ReplaceMacros(@"{FileName}"));
            Assert.Equal(Path.GetFileName(title), macros.ReplaceMacros(@"{Title}"));
            Assert.Equal(Path.GetFileNameWithoutExtension(title), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            Assert.Equal("", macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(title, macros.ReplaceMacros(@"{FullPath}"));
            // it's not a real file so, dates should be minvalue
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateRevised}"));
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros(@"{DateCreated}"));
        }

        private SheetViewModel SetupSVM()
        {
            SheetViewModel svm = new SheetViewModel
            {
                ContentEngine = new TextCte()
            };
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
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileName()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFileName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void Title()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(svm.File, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileNameWithoutExtension()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFileNameWithoutExtension(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FileDirectoryName()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetDirectoryName(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void FullPath()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal(Path.GetFullPath(svm.File), macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal("", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }

        [Fact]
        public void DateFormatStrings()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            // Invalid - :c is not a valid datetime format specifier - instead of throwing an exception we return invalid macro
            string test = "{DatePrinted:c}";
            Assert.Equal(test, macros.ReplaceMacros(test));

            // Invalid - :c is not a valid datetime format specifier - instead of throwing an exception we return invalid macro
            // string.Format doesn't fail on this, thus we can't detect it
            //test = "{DatePrinted:HelloWorld}";
            //Assert.Equal(test, macros.ReplaceMacros(test));

            // Invalid - too many braces
            test = "{abc}}";
            Assert.Equal(test, macros.ReplaceMacros(test));

            // Invalid - too many braces
            test = "{{abc}";
            Assert.Equal(test, macros.ReplaceMacros(test));

            // Valid - embedded braces
            //test = "{{DateTime:u}}";
            //Assert.StartsWith($"{DateTime.Now:u}"[..^3], macros.ReplaceMacros(test));

            // Valid? - embedded newline
            test = "{\n}";
            Assert.Equal(test, macros.ReplaceMacros(test));

        }

        [Fact]
        public void DatePrinted()
        {
            // No way to test since uses DateTime.Now - assume DateRevised tests cover
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
            //  "u" Universal sortable date / time pattern.
            //  2009 - 06 - 15 13:45:30Z
            Assert.StartsWith($"{DateTime.Now:u}"[..^3], macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}:u}}"));
        }

        [Fact]
        public void DateRevised()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal($"{File.GetLastWriteTime(svm.File)}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal($"{File.GetLastWriteTime(svm.File)}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
        }


        [Fact]
        public void DateCreated()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            svm.File = ExistingFile;
            Assert.Equal($"{File.GetCreationTime(svm.File)}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = ExistingFileNoExtension;
            Assert.Equal($"{File.GetCreationTime(svm.File)}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFile;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NonExistingFileNoExtension;
            File.Delete(svm.File);
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidFileName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = InvalidDirectoryName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = NullName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            svm.File = EmptyName;
            Assert.Equal($"{DateTime.MinValue}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
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
            Assert.Equal(svm.ContentEngine.GetContentTypeName(), macros.ReplaceMacros($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.txt")).ConfigureAwait(true);
            //Assert.Equal("text/plain", svm.ContentEngine.GetContentType());
            //Assert.Equal(svm.ContentEngine.GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/html").ConfigureAwait(true);
            Assert.Equal("text/html", svm.ContentEngine.GetContentTypeName());
            Assert.Equal(svm.ContentEngine.GetContentTypeName(), macros.ReplaceMacros($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.html")).ConfigureAwait(true);
            //Assert.Equal("text/html", svm.ContentEngine.GetContentType());
            //Assert.Equal(svm.ContentEngine.GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

            svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine("text/code").ConfigureAwait(true);
            Assert.Equal("text/code", ((PrismCte)svm.ContentEngine).GetContentTypeName());
            Assert.Equal(((PrismCte)svm.ContentEngine).GetContentTypeName(), macros.ReplaceMacros($"{{{macroName}}}"));

            //svm.ContentEngine = await ContentTypeEngineBase.CreateContentTypeEngine(ContentTypeEngineBase.GetContentType("foo.cs")).ConfigureAwait(true);
            //Assert.Equal("csharp", ((PrismCte)svm.ContentEngine).GetContentType());
            //Assert.Equal(((PrismCte)svm.ContentEngine).GetContentType(), macros.ReplaceMacro($"{{{macroName}}}"));

        }

        [Fact]
        public void NumPages()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            Assert.Equal($"{0}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            // Hex
            Assert.Equal($"{0:X8}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}:X8}}"));

        }


        [Fact]
        public void Page()
        {
            SheetViewModel svm = SetupSVM();
            Macros macros = new Macros(svm);

            Assert.Equal($"{0}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));

            // Hex
            Assert.Equal($"{0:X8}", macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}:X8}}"));

        }
    }
}
