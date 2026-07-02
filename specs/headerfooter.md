> **Historical design spec.** This is an early design sketch. The macro names below have
> been updated to match the macros winprint actually ships (see
> `src/WinPrint.Core/Models/Macros.cs` and `MacroChoices`); a few names in the original
> draft (e.g. `PrintDate`, `FilePath`, `PageNumber`) never shipped under those names.

## Macros
DatePrinted
DateRevised
DateCreated
Page
NumPages
FileName
FileNameWithoutExtension
FileExtension
FileDirectoryName
FullPath
FileType
Title
Language
ContentType
CteName
Style

## How to use .NET string formatting and leverage interpolated strings (e.g. $"{DatePrinted:D}")

E.g. $"{}
