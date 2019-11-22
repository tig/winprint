using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using WinPrint.Core.Models;

namespace WinPrint {

    sealed internal class Macros {
        public SheetViewModel svm;

        public int NumPages { get { return svm.NumPages; } }
        public string FileExtension { get { return Path.GetExtension(svm.File); } }
        public string FileName { get { return Path.GetFileName(svm.File); } }
        public string FilePath { get { return Path.GetDirectoryName(FullyQualifiedPath); } }
        public string FullyQualifiedPath { get { return Path.GetFullPath(svm.File); } }
        public DateTime DatePrinted { get { return DateTime.Now; } }
        public DateTime DateRevised { get { return File.GetLastWriteTime(svm.File); } }
        public string FileType { get { return svm.Type; } }
        // BUGBUG: Single-instance
        public string Title { get { return ModelLocator.Current.Settings.Sheets[0].Title; } }

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
        internal string ReplaceMacro(string value, int pageNum) {
            return Regex.Replace(value, @"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", match => {
                var p = System.Linq.Expressions.Expression.Parameter(typeof(Macros), "Macros");

                Group startGroup = match.Groups["start"];
                Group propertyGroup = match.Groups["property"];
                Group formatGroup = match.Groups["format"];
                Group endGroup = match.Groups["end"];

                // TODO: BUGBUG: As written this is not thread-safe. We have to figure out a way
                // of passing pageNum through to the macro parser in a threadsafe way
                Page = pageNum;
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
