// Ole2View.h : main header file for the Ole2View application
//

#ifndef __AFXWIN_H__
    #error include 'stdafx.h' before including this file for PCH
#endif

#ifndef _Ole2View_H_
#define _Ole2View_H_

#include "iviewers\\iview.h"

#include "resource.h"       // main symbols
#include "util.h"
#include "mainfrm.h"  
#include "doc.h"

#ifdef WIN32
    #define GETCLASSUINT(hwnd, index)       (UINT)GetClassLong(hwnd, index)
    #define GETCLASSBRBACKGROUND(hwnd)      (HBRUSH)GETCLASSUINT(hwnd, GCL_HBRBACKGROUND)
    #define MGetModuleUsage(h)                  ((h), 2)
#else
    #define GETCLASSUINT(hwnd, index)       (UINT)GetClassWord(hwnd, index)
    #define GETCLASSBRBACKGROUND(hwnd)      (HBRUSH)GETCLASSUINT(hwnd, GCW_HBRBACKGROUND)
    #define MGetModuleUsage          GetModuleUsage
#endif

// override CListBox so we pass WM_COMMANDHELP on
//
class CMyListBox : public CListBox
{
public:
    DECLARE_DYNCREATE(CMyListBox)

protected:
    afx_msg LRESULT OnCommandHelp(WPARAM, LPARAM lParam) ;

    DECLARE_MESSAGE_MAP()
} ;

#include "regview.h"
#include "obj_vw.h"

#define IDB_FIRST       IDB_QUESTION
#define BMINDEX(x)      (x - IDB_FIRST)

/////////////////////////////////////////////////////////////////////////////
// COle2ViewApp:
// See Ole2View.cpp for the implementation of this class
//

class COle2ViewApp : public CWinApp
{
public:
	BOOL ProcessCmdLine();
    COle2ViewApp();

    CString     m_szStatusText ;

// Overrides
    virtual BOOL InitInstance();
    virtual int ExitInstance();
#ifdef _MAC
    virtual BOOL CreateInitialDocument();
#endif
// Implementation

    //{{AFX_MSG(COle2ViewApp)
    afx_msg void OnAppAbout();
    //}}AFX_MSG
    DECLARE_MESSAGE_MAP()
};

extern COle2ViewApp theApp ;
#if _MFC_VER >= 0x0300
extern OSVERSIONINFO  g_osvi ;
#endif

BOOL RegisterIViewersDLL(CWnd* pParent,BOOL fForce =FALSE);

extern CImageList* g_pImages ;

#endif // _Ole2View_H_

