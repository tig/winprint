using System;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace WinPrint.Core {
    sealed internal class Macros {
        public SheetViewModel svm;

        public int NumPages { get { return svm.NumSheets; } }
        public string FileExtension { get { return string.IsNullOrEmpty(svm.File) ? "" : Path.GetExtension(svm.File); } }
        public string FileName { get { return string.IsNullOrEmpty(svm.File) ? "" : Path.GetFileName(svm.File); } }
        public string FilePath { get { return string.IsNullOrEmpty(svm.File) ? "" :  Path.GetDirectoryName(FullyQualifiedPath); } }
        public string FullyQualifiedPath { get { return string.IsNullOrEmpty(svm.File) ? "" : Path.GetFullPath(svm.File); } }
        public static DateTime DatePrinted { get { return DateTime.Now; } }
        public DateTime DateRevised { get { return string.IsNullOrEmpty(svm.File) ? DateTime.Now :  File.GetLastWriteTime(svm.File); } }
        public string FileType { get { return string.IsNullOrEmpty(svm.File) ? "" : svm.Type; } }

        public int Page { get; set; }

        internal Macros(SheetViewModel svm) {
            this.svm = svm;
        }

        /// <summary>
        /// Replaces macros of the form "{property:format}" using regex and Dynamic Invoke
        /// From https://stackoverflow.com/questions/39874172/dynamic-string-interpolation/39900731#39900731
        /// and  https://haacked.com/archive/2009/01/14/named-formats-redux.aspx/
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal string ReplaceMacro(string value, int sheetNum) {
            return Regex.Replace(value, @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", match => {
                var p = System.Linq.Expressions.Expression.Parameter(typeof(Macros), "Macros");

                Group startGroup = match.Groups["start"];
                Group propertyGroup = match.Groups["property"];
                Group formatGroup = match.Groups["format"];
                Group endGroup = match.Groups["end"];

                // TODO: BUGBUG: As written this is not thread-safe. We have to figure out a way
                // of passing pageNum through to the macro parser in a threadsafe way
                Page = sheetNum;
                LambdaExpression e;
                try {
                    e = DynamicExpressionParser.ParseLambda(new[] { p }, null, propertyGroup.Value);
                }
                catch (ParseException ex) {
                    // Non-existant Property or other parse error
                    return propertyGroup.Value;
                }
                if (formatGroup.Success)
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + formatGroup.Value + "}", e.Compile().DynamicInvoke(this));
                else
                    return (e.Compile().DynamicInvoke(this) ?? "").ToString();
            });
        }
    }
}
