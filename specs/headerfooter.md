## Planned macros
PrintDate
FileRevisedDate
FileCreatedDate
PageNumber
NumPages
FileName
FileExtension
FilePath
FullyQualifiedPath
FileType
FileSize
Printer
PaperSize
DocumentTitle
Author
Language
UserName
MachineName

## How to use .NET string formatting and leverage interpolated strings (e.g. $"{FileDate}")

E.g. $"{}

## WinPrint 2 (original) header/footer format macros
* &p - Page numberstucture
* &P - Total number of pages
* &d - Current date (short format).
* &D - Current date (long format).
* &t - Current time (no seconds).
* &T - Current time (incl. seconds).
* &r - Revised date (short format).
* &R - Revised date (long format).
* &v - Revised time (no seconds).
* &V - Revised time (incl. seconds).
* &s - Size of file in Kbytes. 
* &S - Size of file in bytes. 
* &n - Device name of printer job is being printed on ("HP LaserJet")
* &N - Name of output device job is being printed on ("LPT1:" or 
  "\\SERVER\SHARE (LPT1)").
* && - Ampersand.

### for all of the following, an upper-case equivelent would produce an upper case string.
* &f - Full path and filename.
* &b - Basename.  Everything but the extension. (i.e. "c:\foo\bar\hello")
* &i - Directory.  The directory component. This does not include the following backslash (i.e. "\foo\bar").
* &e - Extension. Begins with the '.'.  (i.e. ".c")
* &h - Path. The path to the file including drive and trailing
  backslash. ("c:\foo\bar\" or "\\server\share\foo\bar\")
* &o - Root. The filename, less the extension. ("hello")
* &l - Volume. The drive. Typically a letter and a colon, but may also be a UNC style path with URI scheme (i.e. "\\server\share" or https://domain).
* &u - User name.
* &m - Machine name.