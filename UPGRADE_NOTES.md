# .NET 10 and C# 12 Upgrade Notes

## Completed Upgrades

### Main Repository Projects

All projects in the main repository have been successfully upgraded to .NET 10 and C# 12:

- **WinPrint.Core**: netcoreapp3.1 → net10.0-windows, C# 8.0 → C# 12
- **WinPrint.LiteHtml**: netcoreapp3.1 → net10.0-windows
- **WinPrint.Console**: netcoreapp3.1 → net10.0-windows
- **WinPrint.WinForms**: netcoreapp3.1 → net10.0-windows
- **WinPrint.Core.UnitTests**: netcoreapp3.1 → net10.0-windows

### Package Updates

Major package updates include:
- System.Linq.Dynamic.Core: 1.1.0 → 1.7.1 (fixes CVE vulnerability)
- PowerShell SDK: 7.0.0 → 7.4.0
- Serilog: 2.9.1 → 4.0.0
- xUnit: 2.4.1 → 2.7.0
- Microsoft.NET.Test.Sdk: 16.6.1 → 17.9.0
- System.Drawing.Common: 4.7.0 → 8.0.0

### Build System

- GitHub Actions workflow updated to use .NET 10.0.x
- PostBuild events made cross-platform compatible with OS conditionals

## Submodule Updates Required

The following submodules need to be updated in their respective repositories:

### 1. libvt100 (https://github.com/tig/libvt100.git)

**File**: `src/libvt100.csproj`

Changes made:
- SDK: Microsoft.NET.Sdk.WindowsDesktop → Microsoft.NET.Sdk
- TargetFrameworks: netcoreapp3.1 → net10.0;net10.0-windows (multi-target)
- LangVersion: 8.0 → 12
- Added: EnableWindowsTargeting = true
- System.Drawing.Common: 4.7.0 → 8.0.0
- Added conditional UseWindowsForms for net10.0-windows target

**Reason**: The library needed multi-targeting to support both Windows and non-Windows builds. The net10.0 target allows the library to be referenced by cross-platform projects, while net10.0-windows provides Windows Forms support.

### 2. PowershellAsync (https://github.com/tig/PowershellAsync.git)

**File**: `PowerShellAsync/PowerShellAsync.csproj`

Changes made:
- TargetFramework: netcoreapp3.1 → net10.0
- System.Management.Automation: 7.0.0 → 7.4.0

**Reason**: Updated to support .NET 10 and latest PowerShell SDK.

## Next Steps

1. **For submodule owners**: The changes made to libvt100 and PowershellAsync should be committed to their respective repositories
2. **After submodule updates**: Update the submodule references in this repository to point to the new commits
3. **Testing**: Run full integration tests on Windows to ensure all functionality works correctly
4. **Optional**: Consider updating the installer project (currently uses .NET Framework 4.7.2)

## Verification

✅ All projects build successfully on Linux (CI environment)  
✅ All unit tests pass  
✅ Cross-platform build compatibility verified  
✅ Security vulnerability in System.Linq.Dynamic.Core resolved
