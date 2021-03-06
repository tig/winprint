﻿using System;
using System.IO;
using System.Reflection;
using WinPrint.Core.ContentTypeEngines;
using WinPrint.Core.Models;
using WinPrint.Core.Services;
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
            Assert.Equal(Path.GetFileNameWithoutExtension(file), macros.ReplaceMacros(@"{FileNameWithoutExtension}"));
            Assert.Equal(Path.GetDirectoryName(file), macros.ReplaceMacros(@"{FileDirectoryName}"));
            Assert.Equal(Path.GetFullPath(file), macros.ReplaceMacros(@"{FullPath}"));
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

            svm.Title = ExistingFile;
            Assert.Equal(svm.Title, macros.ReplaceMacros($"{{{MethodBase.GetCurrentMethod().Name}}}"));
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
        public void CteName()
        {
            // https://stackoverflow.com/questions/22598323/movenext-instead-of-actual-method-task-name
            string macroName = "CteName";

            SheetViewModel svm = new SheetViewModel();
            Macros macros = new Macros(svm);

            // TODO: Mock this out
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            foreach (var cte in ContentTypeEngineBase.GetDerivedClassesCollection())
            {
                (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(cte.GetType().Name);
                Assert.Equal(svm.ContentEngine.GetType().Name, macros.ReplaceMacros($"{{{macroName}}}"));

            }
        }

        [Fact]
        public void Language()
        {
            // https://stackoverflow.com/questions/22598323/movenext-instead-of-actual-method-task-name
            string macroName = "Language";

            SheetViewModel svm = new SheetViewModel();
            Macros macros = new Macros(svm);

            // TODO: Mock out
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);
            string input;
            string expectedLang;
            string contentType;

            input = "text/plain";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal("Plain Text", macros.ReplaceMacros($"{{{macroName}}}"));

            input = "text/html";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal("HTML", macros.ReplaceMacros($"{{{macroName}}}"));

            input = "text/ansi";
            expectedLang = "ANSI Text";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text";
            expectedLang = "Plain Text";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "TEXT";
            expectedLang = "Plain Text";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "*.ans";
            expectedLang = "ANSI Text";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "*.cs";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "*.CS";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "csharp";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "cSharp";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "C#";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "c#";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text/x-csharp";
            expectedLang = "C#";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text/x-smalltalk";
            expectedLang = "Smalltalk";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedLang, macros.ReplaceMacros($"{{{macroName}}}"));
        }


        [Fact]
        public void ContentType()
        {
            // https://stackoverflow.com/questions/22598323/movenext-instead-of-actual-method-task-name
            string macroName = "ContentType";

            SheetViewModel svm = new SheetViewModel();
            Macros macros = new Macros(svm);

            // TODO: Mock out
            ServiceLocator.Current.SettingsService.SettingsFileName = $"WinPrint.{GetType().Name}.json";
            File.Delete(ServiceLocator.Current.SettingsService.SettingsFileName);

            var settings = ServiceLocator.Current.SettingsService.ReadSettings();
            ModelLocator.Current.Settings.CopyPropertiesFrom(settings);

            var input = "text/plain";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal(input, svm.ContentType);
            Assert.Equal(input, macros.ReplaceMacros($"{{{macroName}}}"));

            input = "text/plain";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal(input, svm.ContentType);
            Assert.Equal(input, macros.ReplaceMacros($"{{{macroName}}}"));

            input = "text/ansi";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal(input, svm.ContentType);
            Assert.Equal(input, macros.ReplaceMacros($"{{{macroName}}}"));

            input = "text/html";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(input);
            Assert.Equal(input, svm.ContentType);
            Assert.Equal(input, macros.ReplaceMacros($"{{{macroName}}}"));

            var contentType = "text";
            var expectedCT = "text/plain";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text/plain";
            expectedCT = contentType;
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text/html";
            expectedCT = contentType;
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "text/ansi";
            expectedCT = contentType;
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "*.ans";
            expectedCT = "text/ansi";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "*.cs";
            expectedCT = "text/x-csharp";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));

            contentType = "csharp";
            expectedCT = "text/x-csharp";
            (svm.ContentEngine, svm.ContentType, svm.Language) = ContentTypeEngineBase.CreateContentTypeEngine(contentType);
            Assert.Equal(expectedCT, svm.ContentType);
            Assert.Equal(expectedCT, macros.ReplaceMacros($"{{{macroName}}}"));
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
