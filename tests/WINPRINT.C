/*
      WINPRINT.C -- Main routines for WinPrint

      WinPrint - A Text File Printing Program for Windows

      (c) Copyright 1989-1991, Charles E. Kindel, Jr.
      
   REVISIONS

   DATE        VERSION     CHANGES
   1/10/90     0.5         First working copy.  Does not support fonts, or other fancy stuff.
   1/17/90     0.7         Added font dialog box.
   1/20/90     0.75        Completed font implentation (i think).
   1/20/90     0.8         Read filenames from command line, truncate file names in SelectList.
                           Allow wildcard selection to selectlist.
                           Read write default configuration.
   1/21/90     0.85        New Font box.  If rasterfont, allow user to select point size.
   1/22/90     0.86        Margins Box.
   1/23/90     0.87        Options box. No more Margins box.
                           Header/footer styles.
                           Date printed/revised.

   1/25/90     0.88        Info box.  New size (wider and not as tall).
                           Separate margins/hf box.
                           Fonts for Headers/footers.
                           At least get selection of landscape/portrait done.
                           Got rid of WinPrintDlg.
                           Icon and close.

   1/31/90     0.89        Info box !?!?!
                           Command Line.
                           Load/Save config
                           Help.

   2/13/90     0.90        Shareware registration system.
                           Printer Selection.
                           - When this version is finished start major testing and documentation.
                           - Go to 0.91 (BETA 1) when no known bugs.

   2/25/90     0.91        BETA 1
                           Select Printer - saved with current config.
                           Current config name is now saved at open... and save...
                           Verify save implemented.
                           Version expires on 3/15/90.

   2/27/90     0.91A       Minor fixes to .91
   3/6/90      0.91B       Fixed stack overflow with postscript driver.
                           Fixed save w/ no configname problem.
                           Fixed /p:<printer> problem
                           Made WM_CLOSE not prompt if bOptions.close is true.
                           Fixed problem with FF as last character of file.

   3/8/90      0.99        No known bugs
                           Help text all fixed up.
                           Made help system more memory efficient.
                           Shareware release.
                           
   3/10/90     1.00        Release 1. Name changed to WinPrint.

   3/19/90     1.01        Fixed bug regarding invalid setup name on command line.
                           Fixed bug regarding /GO with no file name on command line.
                           Made selected files list box select bar stay on filename last added.
                           Documentation fixes.

   3/24/90     1.02        /GO bug fixed.

   4/13/90     1.03        Windows 3.0 problem with Option Dlg box.
                           Now compiles cleanly with /W3 option.
                           Working of problem in ws_fonts regarding LaserMaster.
                           LaserMaster problem appears to be in driver.

   5/16/90     1.04        Might have fixed 3.0 problem!  Seems to be with proportional font!

   5/17/90     1.05        New shadows.  (DrawShadow)

   5/17/90     1.06        Fixed up shadow routine for Mike Werner.

   5/18/90     1.07        Another attempt at 3.0 compatibility.  No go.

   5/30/90     1.10        Charles Petzold informed me that under Windows 3.0, in order to
                           use a dialog box as a main window (as WinPrint does), the
                           cbWndExtra field in the WNDCLASS structure must be set to
                           DLGWINDOWEXTRA.  This identifier is not defined in Windows 2.1,
                           so I used a value of 1024 in this version.  My guess is that this
                           is a safe value, but there still might be problems.  Until I find
                           out the value of DLGWINDOWEXTRA, there is a risk in running this
                           version.  I have not been able to crash this version...yet.

   6/2/90      1.30        Version that can be uploaded to CIS...

   6/2/90      1.31        Fixed bug in margin dialog introduced in Win3.0.

   7/20/90     1.32        Address change for Bellevue
                           Full 3.0 compatibility.
                           Changed all sprintf's to wsprintf's and
                           str* to lstr*

   10/3/91     1.40        Made full Windows 3.0 application.
                           New makefile.

*/
/* ------------------ IMPORTS ------------------- */

#include "precomp.h"

#include "winprint.h"
#include "fontutil.h"
#include "isz.h"
#include "dlgs.h"
#include "ws_dlg.h"
#include "ws_init.h"
#include "version.h"
#include "dlghelp.h"
#include "wintime.h"

/* ----------------- GLOBAL DATA --------------------- */
HWND        hwndMain ;
HANDLE      hInst ;
BOOL        fWin31 ;

//LPSTR       rglpsz[LAST_IDS - FIRST_IDS + 1] ;

char        szVerNum [32] ;

char        szRegisteredName [REG_NAME_LEN+1] ;
char        szCurrentConfig [CFG_NAME_LEN+1] ;

BOOL        bConfigOnCmdLine ;
BOOL        fGo ;
BOOL        bModify ;

HANDLE      ghDevMode ;    /* current devmode */
HANDLE      ghDevNames ;   /* current devnames */

HICON       hicoNoOrient = NULL ;
HICON       hicoPortrait = NULL ;
HICON       hicoLandscape= NULL ;

OPTIONS     Options ;
OPTIONS     TempOp ;

BOOL        fPrinting ;

HFONT       hfontSmall ;

BOOL        fSavePrinter = TRUE ;

short      nLogicalPixelsY ;
short      wPrinterBugs ;

BOOL       fFixedPitchOnly = TRUE ;

int PASCAL WinMain( HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpszCmdLine, int nCmdShow ) 
{
   HWND     hwndMain ;
   MSG      msg ;
   HACCEL   haccl ;

#ifdef EXPIRE
char     szDate [80] ;
#endif

   hInst = hInstance ;

   /*
    * Are we running Windows 3.1 or higher?
    */
   fWin31 = (BOOL)(LOWORD( GetVersion() ) >= 0x0A03) ;

   D( if (hWDB = wdbOpenSession( NULL,
                                 "WINPRINT", "wdb.ini", 0 ))
      {
         DPS( hWDB, "WDB is active!" ) ;
      }
   ) ;

   if ( VER_BUILD )
      wsprintf( szVerNum, (LPSTR)"%d.%.2d.%.3d",
                VER_MAJOR, VER_MINOR, VER_BUILD ) ;
   else
      wsprintf( szVerNum, (LPSTR)"%d.%.2d", VER_MAJOR, VER_MINOR ) ;

   DP1( hWDB, "\n\n**** %s %s %s (Windows Version 0x%04X)",
              (LPSTR)GRCS(IDS_APPNAME),
              (LPSTR)GRCS(IDS_VERSION),
              (LPSTR)szVerNum,
              LOWORD(GetVersion()) ) ;


   if (!hPrevInstance)
      if (!WinPrintInit (hInstance))
      {
         DP1( hWDB, "WinPrintInit failed" ) ;

         ErrorResBox( NULL, hInstance,
                      MB_ICONHAND | MB_SYSTEMMODAL,IDS_APPNAME, 
                      IDS_ERR_NOMEMORY ) ;

         return FALSE ;
      }

   fFixedPitchOnly = GetPrivateProfileInt( GRCS(IDS_INI_MAIN),
                                  GRCS(IDS_INI_FIXEDPITCHONLY),
                                  TRUE,
                                  GRCS(IDS_INI_FILENAME) ) ;


   /*
      * Get a really useful font
      */
   hfontSmall = ReallyCreateFont( NULL, "Helv", 8, RCF_NORMAL ) ;

   if (0 == (hwndMain = CreateDialog (hInstance, MAKEINTRESOURCE( DLG_MAIN ),
                     0, NULL)))
   {
      DP1( hWDB, "CreateDialog failed" ) ;

      ErrorResBox( NULL, hInstance,
                   MB_ICONHAND | MB_SYSTEMMODAL,IDS_APPNAME, 
                   IDS_ERR_NOMEMORY ) ;

      return FALSE ;
   }

   SendMessage (hwndMain, WM_MYINIT, 0, 0L) ;  /* MYINIT is defined as WM_USER + 1 */
   ShowWindow (hwndMain, nCmdShow) ;

   PostMessage(hwndMain, WM_GO, 0, 0L) ;

   haccl = LoadAccelerators( hInstance, MAKEINTRESOURCE( 1 ) ) ;

   while (GetMessage (&msg, NULL, 0, 0))
   {
      if (TranslateAccelerator( hwndMain, haccl, &msg ))
         DispatchMessage( &msg ) ;
      else
         if (!IsDialogMessage( hwndMain, &msg ))  
         {
            TranslateMessage( &msg ) ;
            DispatchMessage( &msg ) ;
         }
   }

   DeleteObject( hfontSmall ) ;

   return msg.wParam ;
}

//*************************************************************
//
//  GetRCString
//
//  Purpose:
//      Retrieves a string from the resource STRINGTABLE
//      This routine will read (minimum) up to 384 characters before it
//      begins to overwrite.  MAX length per/read is 128!!
//
//  Parameters:
//      UINT wID
//      
//
//  Return: (LPSTR)
//      Pointer to a static buffer that contains the string
//
//  Comments:
//
//
//  History:    Date       Author     Comment
//               2/12/92   MSM        Created
//
//*************************************************************

#define BUF_SIZE    2048

LPSTR WINAPI GetRCString(UINT wID, HINSTANCE hInst)
{
    static char szString[ BUF_SIZE ];
    static LPSTR lpIndex = (LPSTR)szString;
    static WORD  wEmpty = BUF_SIZE;
    LPSTR lp;
    WORD  wLen;

read_string:
    // Add 1 for the NULL terminator
    wLen = LoadString( hInst, wID, lpIndex, wEmpty ) + 1;

    if (wLen == wEmpty)
    {
        wEmpty = BUF_SIZE;
        lpIndex = (LPSTR)szString;
        goto read_string;
    }

    if (wLen==1)
    {
        DP1( hWDB, "LoadString failed!  ID = %d", wID );
        lpIndex[0]=0;
    }
    lp = lpIndex;

    lpIndex += wLen;
    wEmpty -= wLen;

    return lp;

} //*** GetRCString


BOOL FAR PASCAL CmpOptions( VOID )
{
   if (Options.lfCurFont.lfHeight != TempOp.lfCurFont.lfHeight ||
       Options.lfCurFont.lfWeight != TempOp.lfCurFont.lfWeight || 
       Options.lfCurFont.lfItalic != TempOp.lfCurFont.lfItalic || 
       Options.lfCurFont.lfCharSet != TempOp.lfCurFont.lfCharSet || 
       Options.lfCurFont.lfPitchAndFamily != TempOp.lfCurFont.lfPitchAndFamily )
      return FALSE ;

   if (lstrcmpi( Options.lfCurFont.lfFaceName, TempOp.lfCurFont.lfFaceName ))
   {
      DP1( hWDB, "CurFont" ) ;
      return FALSE ;
   }

   if (Options.lfHFFont.lfHeight != TempOp.lfHFFont.lfHeight ||
       Options.lfHFFont.lfWeight != TempOp.lfHFFont.lfWeight || 
       Options.lfHFFont.lfItalic != TempOp.lfHFFont.lfItalic || 
       Options.lfHFFont.lfCharSet != TempOp.lfHFFont.lfCharSet || 
       Options.lfHFFont.lfPitchAndFamily != TempOp.lfHFFont.lfPitchAndFamily )
      return FALSE ;

   if (lstrcmpi( Options.lfHFFont.lfFaceName, TempOp.lfHFFont.lfFaceName ))
   {
      DP1( hWDB, "HFFont" ) ;
      return FALSE ;
   }

   if (memicmp( &Options.wHeaderMask, &TempOp.wHeaderMask,
                sizeof( OPTIONS ) - 2 * sizeof(LOGFONT) ))
   {
      DP1( hWDB, "They differ in other" ) ;
      return FALSE ;
   }
   return TRUE ;
}   
