
NAME
    Out-WinPrint
    
SYNTAX
    Out-WinPrint [<CommonParameters>]
    
    Out-WinPrint [[-PrinterName] <string>] [-SheetDefintion <string>] [-Landscape {No | Yes}] [-LineNumbers {No | Yes}] [-ContentTypeEngine <string>] [-FileName <string>] [-Title <string>] [-InputObject <psobject>] [-WhatIf] [<CommonParameters>]
    
    Out-WinPrint [-InstallUpdate] [-Force] [<CommonParameters>]
    
    
PARAMETERS
    -ContentTypeEngine <string>
        Name of the WinPrint Content Type Engine to use (default is "text/plain").
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      Engine
        Dynamic?                     false
        
    -FileName <string>
        FileName to be displayed in header/footer with the {FileName} (or {Title}) macros. If ContentType is not specified, the Filename will be used to try to determine the content type engine to use.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      File
        Dynamic?                     false
        
    -Force
        Allows winprint to kill the host Powershell process when updating.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Updates
        Aliases                      None
        Dynamic?                     false
        
    -InputObject <psobject>
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       true (ByValue)
        Parameter set name           Print
        Aliases                      None
        Dynamic?                     false
        
    -InstallUpdate
        If an updated version of winprint is available online, download and install it.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Updates
        Aliases                      None
        Dynamic?                     false
        
    -Landscape <OutWinPrint+YesNo>
        If specified (Yes or No) overrides the landscape setting in the sheet defintion.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      None
        Dynamic?                     false
        
    -LineNumbers <OutWinPrint+YesNo>
         If specfied, overrides the line numbers setting in the sheet defintion (Yes, No).
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      None
        Dynamic?                     false
        
    -PrinterName <string>
        The name of the printer to print to. If not specified the default printer will be used.
        
        Required?                    false
        Position?                    0
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      Name
        Dynamic?                     false
        
    -SheetDefintion <string>
        Name of the WinPrint sheet definition to use (e.g. "Default 2-Up")
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      Sheet
        Dynamic?                     false
        
    -Title <string>
        Title to be displayed in header/footer with the {Title} or {FileName} macros.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      None
        Dynamic?                     false
        
    -WhatIf
        Output is the number of sheets that would be printed. Use -Verbose to print the count of pages.
        
        Required?                    false
        Position?                    Named
        Accept pipeline input?       false
        Parameter set name           Print
        Aliases                      None
        Dynamic?                     false
        
    <CommonParameters>
        This cmdlet supports the common parameters: Verbose, Debug,
        ErrorAction, ErrorVariable, WarningAction, WarningVariable,
        OutBuffer, PipelineVariable, and OutVariable. For more information, see
        about_CommonParameters (https://go.microsoft.com/fwlink/?LinkID=113216). 
    
    
INPUTS
    System.Management.Automation.PSObject
    
    
OUTPUTS
    System.Object
    
ALIASES
    wp
    

REMARKS
    None


