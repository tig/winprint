using System;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace WinPrint.Core {
    sealed public class Macros {
        /// <summary>
        /// The SheetModel the Macros will pull data from.
        /// </summary>
        public SheetViewModel svm;

        /// <summary>
        /// Number of sheets to be printed.
        /// </summary>
        public int NumPages => svm.NumSheets;
        /// <summary>
        /// The extension (including the period ".").
        /// </summary>
        public string FileExtension => string.IsNullOrEmpty(svm.File) ? "" : Path.GetExtension(svm.File);
        /// <summary>
        /// The file name and extension. If FileName was not provided, Title will be used.
        /// </summary>
        public string FileName => GetFileNameOrTitle();
        /// <summary>
        /// The Title of the print request. If Title was not provided, the FileName will be used.
        /// </summary>
        public string Title => FullPath;
        /// <summary>
        /// The file name of the file without the extension and period ".".
        /// </summary>
        public string FileNameWithoutExtension => string.IsNullOrEmpty(svm.File) ? "" : Path.GetFileNameWithoutExtension(svm.File);
        /// <summary>
        /// The directory for the specified string without the filename or extension.
        /// </summary>
        public string FileDirectoryName => string.IsNullOrEmpty(svm.File) ? "" : Path.GetDirectoryName(FullPath);
        /// <summary>
        /// The absolute path for the file.
        /// </summary>
        public string FullPath => IsValidFilename(svm.File) ? Path.GetFullPath(svm.File) : (string.IsNullOrEmpty(svm.File) ? "" : svm.File);
        /// <summary>
        /// The time and date when printed.
        /// </summary>
        public DateTime DatePrinted => DateTime.Now;
        /// <summary>
        /// The time and date teh file was last revised.
        /// </summary>
        public DateTime DateRevised => IsValidFilename(svm.File) ? File.GetLastWriteTime(svm.File) : DateTime.MinValue;
        /// <summary>
        /// The file type (e.g. "text/plain" or "csharp").
        /// </summary>
        public DateTime DateCreated => IsValidFilename(svm.File) ? File.GetCreationTime(svm.File) : DateTime.MinValue;
        /// <summary>
        /// The file type (e.g. "text/plain" or "csharp").
        /// </summary>
        public string FileType => svm.ContentEngine == null ? "" : svm.ContentEngine.GetContentTypeName();

        /// <summary>
        /// The current sheet number.
        /// </summary>
        public int Page { get; set; }

        public Macros(SheetViewModel svm) {
            this.svm = svm;
        }

        // https://stackoverflow.com/questions/62771/how-do-i-check-if-a-given-string-is-a-legal-valid-file-name-under-windows#62855
        private bool IsValidFilename(string testName) {
            if (string.IsNullOrEmpty(testName)) {
                return false;
            }

            var containsABadCharacter = new Regex("["
                  + Regex.Escape(new string(System.IO.Path.GetInvalidPathChars())) + "]");
            if (containsABadCharacter.IsMatch(testName)) { return false; };

            // other checks for UNC, drive-path format, etc

            if (!File.Exists(testName)) {
                return false;
            }

            return true;
        }

        // Title and FileName are synomous. 
        private string GetFileNameOrTitle() {
            var retval = "";

            if (string.IsNullOrEmpty(svm.File)) {
                return retval;
            }

            try {
                retval = Path.GetFileName(svm.File);
            }
            catch (ArgumentException) {
                // invalid char in path 
                retval = svm.File;
            }

            return retval;
        }

        /// <summary>
        /// Replaces macros of the form "{property:format}" using regex and Dynamic Invoke
        /// From https://stackoverflow.com/questions/39874172/dynamic-string-interpolation/39900731#39900731
        /// and  https://haacked.com/archive/2009/01/14/named-formats-redux.aspx/
        /// 
        /// Note this does not work perfectly. Specifically some invalid format specifiers just cause
        /// string.Format to generate garbage (e.g. {DatePrinted:HelloWorld})
        /// </summary>
        /// <param name="value">A string with macros to be replaced</param>
        /// <param name="sheetNum"><Page #/param>
        /// <returns></returns>
        public string ReplaceMacros(string value) {
            return Regex.Replace(value, @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", match => {
                var p = Expression.Parameter(typeof(Macros), "Macros");

                var startGroup = match.Groups["start"];
                var propertyGroup = match.Groups["property"];
                var formatGroup = match.Groups["format"];
                var endGroup = match.Groups["end"];

                try {
                    // Generate and parse a LambdaExpression
                    var e = DynamicExpressionParser.ParseLambda(new[] { p }, null, propertyGroup.Value);
                    var computedValue = e.Compile().DynamicInvoke(this);
                    if (formatGroup.Success) {
                        // There's a format specifier
                        // The following does: string.Format("{0:formatGroup.Value}", computedValue)
                        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + formatGroup.Value + "}", computedValue);
                    }
                    else {
                        // Get here when there is no format specifier.
                        return (computedValue ?? "").ToString();
                    }
                }
                catch { //(ParseException ex) {
                    // Non-existant Property or other parse error
                    return match.Groups[0].Value;
                }
            });
        }
    }
}
