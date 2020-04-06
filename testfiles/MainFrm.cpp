// mainfrm.cpp : implementation of the CMainFrame class
//

#include "stdafx.h"
#include "Ole2View.h"
#include "mainfrm.h"
#include "obj_vw.h"
DEFINE_GUID(CATID_Ole2ViewInterfaceViewers, 0x64454f82, 0xf827, 0x11ce, 0x90, 0x59, 0x8, 0x0, 0x36, 0xf1, 0x25, 0x2);

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif


/////////////////////////////////////////////////////////////////////////////
// CMainFrame
// Create a splitter window which splits an output text view and an input view
//                               |
//    OBJECT VIEW (CObjTreeView) | REGISTRY VIEW (CRegistryView)
//                               |
IMPLEMENT_DYNCREATE(CMainFrame, CFrameWnd)
#define new DEBUG_NEW

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
    //{{AFX_MSG_MAP(CMainFrame)
    ON_WM_CREATE()
    ON_WM_DESTROY()
    ON_COMMAND(ID_FILE_RUNREGEDIT, OnFileRunREGEDIT)
    ON_COMMAND(ID_VIEW_REFRESH, OnViewRefresh)
    ON_UPDATE_COMMAND_UI(ID_VIEW_REFRESH, OnUpdateViewRefresh)
	ON_COMMAND(ID_OBJECT_DELETE, OnObjectDelete)
	ON_UPDATE_COMMAND_UI(ID_OBJECT_DELETE, OnUpdateObjectDelete)
	ON_COMMAND(ID_OBJECT_VERIFY, OnObjectVerify)
	ON_UPDATE_COMMAND_UI(ID_OBJECT_VERIFY, OnUpdateObjectVerify)
	ON_COMMAND(ID_FILE_VIEWTYPELIB, OnFileViewTypeLib)
    ON_COMMAND(ID_IFACES_USEINPROCSERVER, OnUseInProcServer)
    ON_UPDATE_COMMAND_UI(ID_IFACES_USEINPROCSERVER, OnUpdateUseInProcServer)
    ON_COMMAND(ID_IFACES_USEINPROCHANDLER, OnUseInProcHandler)
    ON_UPDATE_COMMAND_UI(ID_IFACES_USEINPROCHANDLER, OnUpdateUseInProcHandler)
    ON_COMMAND(ID_IFACES_USELOCALSERVER, OnUseLocalServer)
    ON_UPDATE_COMMAND_UI(ID_IFACES_USELOCALSERVER, OnUpdateUseLocalServer)
	ON_COMMAND(ID_FILE_BINDTOAFILE, OnFileBind)
	ON_WM_DROPFILES()
	ON_WM_ACTIVATEAPP()
	ON_COMMAND(ID_VIEW_HIDDENCOMCATS, OnViewHiddenComCats)
	ON_UPDATE_COMMAND_UI(ID_VIEW_HIDDENCOMCATS, OnUpdateViewHiddenComCats)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// arrays of IDs used to initialize control bars

static UINT BASED_CODE indicators[] =
{
      ID_SEPARATOR,             // status line indicator
//    ID_INDICATOR_CAPS,
//    ID_INDICATOR_NUM,
//    ID_INDICATOR_SCRL,
};

/////////////////////////////////////////////////////////////////////////////
// CMainFrame construction/destruction

CMainFrame::CMainFrame()
{
    m_fCmdLineProcessed = FALSE;
    m_pObjTreeView = NULL ;
    m_pRegistryView = NULL ;

#ifdef _MAC
    m_fInOnCreateClient = FALSE ;
#endif
}

CMainFrame::~CMainFrame()
{
}

BOOL CMainFrame::LoadFrame(UINT nIDResource, DWORD dwDefaultStyle,
                CWnd* pParentWnd, CCreateContext* pContext)
{
    // Turn off auto update of title bar
    dwDefaultStyle &= ~((DWORD)FWS_ADDTOTITLE) ;
    BOOL f = CFrameWnd::LoadFrame(nIDResource, dwDefaultStyle,
                pParentWnd, pContext);

    return f ;
}
/////////////////////////////////////////////////////////////////////////////
// CMainFrame message handlers

int CMainFrame::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
#ifdef _MAC
    m_fInOnCreateClient = TRUE ;
#endif

    m_dropTarget.Register( this ) ;
    if (CFrameWnd::OnCreate(lpCreateStruct) == -1)
        return -1;

    if (!m_wndToolBar.Create(this) ||
        !m_wndToolBar.LoadToolBar(IDR_MAINFRAME))
    {
        TRACE(_T("Failed to create toolbar\n"));
        return -1;      // fail to create
    }

//    m_wndToolBar.EnableDocking(CBRS_ALIGN_ANY);
//    EnableDocking(CBRS_ALIGN_ANY);
//    DockControlBar(&m_wndToolBar);

    m_wndToolBar.SetBarStyle(m_wndToolBar.GetBarStyle() |
        CBRS_TOOLTIPS | CBRS_FLYBY);

    if (!m_wndStatusBar.Create(this) ||
        !m_wndStatusBar.SetIndicators(indicators, sizeof(indicators)/sizeof(UINT)) )
    {
        TRACE(_T("Failed to create status bar\n"));
        return -1;      // fail to create
    }

    UINT nID, nStyle ;
    int cxWidth ;
    m_wndStatusBar.GetPaneInfo( 0, nID, nStyle, cxWidth ) ;
    m_wndStatusBar.SetPaneInfo( 0, ID_SEPARATOR, nStyle | SBPS_POPOUT, cxWidth ) ;

    DragAcceptFiles( TRUE ) ;
#ifdef _MAC
    m_fInOnCreateClient = FALSE ;
#endif

    return 0;
}


void CMainFrame::OnDestroy()
{
    m_dropTarget.Revoke( ) ;
    CFrameWnd::OnDestroy();
    SavePosition() ;
}

BOOL CMainFrame::OnCreateClient(LPCREATESTRUCT /*lpcs*/,
     CCreateContext* pContext)
{
    ASSERT(pContext);
    // create a splitter with 1 row, 2 columns
    if (!m_wndSplitter.CreateStatic(this, 1, 2))
    {
        TRACE(_T("Failed to CreateStaticSplitter\n"));
        return FALSE;
    }

    // add the first splitter pane - the default view in column 0
    if (!m_wndSplitter.CreateView(0, 0,
        pContext->m_pNewViewClass, CSize(240, 50), pContext))
    {
        TRACE(_T("Failed to create first pane\n"));
        return FALSE;
    }

    if (!m_wndSplitter.CreateView(0, 1,
        RUNTIME_CLASS(CRegistryView), CSize(0, 0), pContext))
    {
        TRACE(_T("Failed to create second pane\n"));
        return FALSE;
    }
    m_pObjTreeView = (CObjTreeView*)m_wndSplitter.GetPane(0, 0) ;
    m_pRegistryView = (CRegistryView*)m_wndSplitter.GetPane(0,1) ;

    // activate the input view
    SetActiveView((CView*)m_wndSplitter.GetPane(0, 0));
    m_wndSplitter.SetColumnInfo( 0, 240, 0 ) ;

    return TRUE;
}

/////////////////////////////////////////////////////////////////////////////
// CMainFrame diagnostics

#ifdef _DEBUG
void CMainFrame::AssertValid() const
{
    CFrameWnd::AssertValid();
}

void CMainFrame::Dump(CDumpContext& dc) const
{
    CFrameWnd::Dump(dc);
}

#endif //_DEBUG


BOOL CMainFrame::SavePosition()
{
    CString szSection ;
    CString szKey ;

    szSection.LoadString( IDS_INI_CONFIG ) ;
    szKey.LoadString( IDS_INI_WNDPOS ) ;

    WINDOWPLACEMENT wp;
    CString szValue ;

    wp.length = sizeof( WINDOWPLACEMENT );
    GetWindowPlacement( &wp );

    int nWidth, n ;
    m_wndSplitter.GetColumnInfo( 0, nWidth, n ) ;

    LPTSTR p = szValue.GetBuffer( 255 ) ;
    wsprintf( p, _T("%d, %d, %d, %d, %d, %d, %d, %d, %d, %d, %d, %d"),
        wp.showCmd, wp.ptMinPosition.x, wp.ptMinPosition.y,
        wp.ptMaxPosition.x, wp.ptMaxPosition.y,
        wp.rcNormalPosition.left, wp.rcNormalPosition.top,
        wp.rcNormalPosition.right, wp.rcNormalPosition.bottom,
        nWidth, 
        (m_wndToolBar.GetStyle() & WS_VISIBLE) ? TRUE : FALSE, 
        (m_wndStatusBar.GetStyle() & WS_VISIBLE) ? TRUE : FALSE);

    szValue.ReleaseBuffer() ;
    theApp.WriteProfileString( szSection, szKey, szValue );
    return TRUE ;
}

BOOL CMainFrame::RestorePosition(int nCmdShow)
{
    CString sz ;
    CString szSection ;
    CString szKey ;
    BOOL fToolBar = TRUE ;
    BOOL fStatusBar = TRUE ;
    int  nWidth ;

    szSection.LoadString( IDS_INI_CONFIG ) ;
    szKey.LoadString( IDS_INI_WNDPOS ) ;

    WINDOWPLACEMENT wp;
    int     nConv;

    wp.length = sizeof( WINDOWPLACEMENT );
    wp.flags = 0 ;

    try
    {
        sz = theApp.GetProfileString(szSection, szKey, _T("") ) ;
        if (sz.IsEmpty())
            AfxThrowMemoryException();

        LPTSTR   lp = (LPTSTR)sz.GetBuffer( 255 );

        wp.showCmd = (WORD)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.ptMinPosition.x = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.ptMinPosition.y = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.ptMaxPosition.x = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.ptMaxPosition.y = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.rcNormalPosition.left = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.rcNormalPosition.top = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.rcNormalPosition.right = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        wp.rcNormalPosition.bottom = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        nWidth = (int)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        fToolBar = (BOOL)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        fStatusBar = (BOOL)ParseOffNumber( (LPTSTR FAR *)&lp, &nConv );
        if (!nConv)
            AfxThrowMemoryException();

        // Always strip off minimize.
        //
        if (wp.showCmd == SW_SHOWMINIMIZED)
            wp.showCmd = SW_SHOWNORMAL ;

        if (nCmdShow != SW_SHOWNORMAL || nCmdShow != SW_NORMAL)
            wp.showCmd = nCmdShow ;
    
        m_wndSplitter.SetColumnInfo( 0, nWidth, 0 ) ;
    }
    catch(CException *pException)
    {
        fToolBar = TRUE ;
        fStatusBar = TRUE ;
        ShowControlBar( &m_wndToolBar, fToolBar, TRUE ) ;
        ShowControlBar( &m_wndStatusBar, fStatusBar, TRUE ) ;
        ShowWindow( SW_SHOWNORMAL );
        pException->Delete();
        return FALSE ;
    }
    
    ShowControlBar( &m_wndToolBar, fToolBar, TRUE ) ;
    ShowControlBar( &m_wndStatusBar, fStatusBar, TRUE ) ;
    return (BOOL)SetWindowPlacement( &wp ) ;
}

void CMainFrame::OnFileRunREGEDIT()
{
#ifndef _MAC
    if (WinExec( "REGEDT32.EXE", SW_SHOWNORMAL ) < 32)
    	WinExec( "REGEDIT.EXE", SW_SHOWNORMAL ) ;
#endif
}

void CMainFrame::OnViewRefresh()
{
    COle2ViewDoc*   pDoc = (COle2ViewDoc*)GetActiveDocument() ;
    pDoc->UpdateAllViews( NULL, UPD_REFRESH ) ;
}

void CMainFrame::OnUpdateViewRefresh(CCmdUI* /*pCmdUI*/)
{
}

void CMainFrame::OnObjectDelete() 
{
    m_pObjTreeView->OnObjectDelete() ;
}

void CMainFrame::OnUpdateObjectDelete(CCmdUI* pCmdUI) 
{
    pCmdUI->Enable( m_pObjTreeView->IsValidSel() ) ;
}

void CMainFrame::OnObjectVerify() 
{
    m_pObjTreeView->OnObjectVerify() ;

}

void CMainFrame::OnUpdateObjectVerify(CCmdUI* pCmdUI) 
{
    pCmdUI->Enable( m_pObjTreeView->IsValidSel() ) ;
}

void CMainFrame::OnFileViewTypeLib()
{
	USES_CONVERSION;
    static TCHAR szFilter[] = _T("TypeLib Files (*.tlb;*.olb;*.dll;*.ocx;*.exe)|*.tlb;*.olb;*.dll;*.ocx;*.exe|AllFiles(*.*)|*.*|") ;

    CFileDialog dlg(TRUE, _T("*.tlb"), NULL,
                    OFN_FILEMUSTEXIST | OFN_HIDEREADONLY | OFN_PATHMUSTEXIST,
                    szFilter, this);
    if (IDOK != dlg.DoModal())
        return ;

    // Call LoadTypeLib
    LPTYPELIB lpTypeLib;
	HRESULT hr = ::LoadTypeLib(T2COLE(dlg.GetPathName()), &lpTypeLib);
    if (FAILED(hr))
    {
		CString szErrorMsg;
		szErrorMsg.Format(_T("LoadTypeLib( %s ) failed."),(LPCTSTR)dlg.GetPathName());
        ErrorMessage( szErrorMsg, hr );
        return ;
    }
	// call the interface wiewer
	ASSERT(lpTypeLib != NULL);
    ViewInterface( GetSafeHwnd(), IID_ITypeLib, (IUnknown*)lpTypeLib);
    VERIFY(0 == lpTypeLib->Release()) ;
}

void WINAPI ViewInterface( HWND hwnd, REFIID riid, IUnknown *punk )
{
    IInterfaceViewer* piv = NULL ;
    SCODE sc ;

    // Look in the registry for the "Ole2ViewIViewer=" key for this iid
    //
    TCHAR szKey[128] ;
    TCHAR szValue[80] ;
    TCHAR szInterface[80] ;

	USES_CONVERSION;
	// get the string from CLSID in TCHAR format
    LPOLESTR lpszOleIID = NULL;
    ::StringFromCLSID(riid, &lpszOleIID);
	ASSERT(lpszOleIID != NULL);

	LPTSTR lpszIID = OLE2T(lpszOleIID);
	ASSERT(lpszIID != NULL);
    IMalloc* pmal = NULL ;
    ::CoGetMalloc( MEMCTX_TASK, &pmal ) ;
    pmal->Free( lpszOleIID ) ;
    pmal->Release() ;

    wsprintf(szKey, _T("Interface\\%s"), lpszIID) ;

    LONG cb = sizeof(szInterface);
	*szInterface = _T('\0');
    if (::RegQueryValue(HKEY_CLASSES_ROOT, szKey, szInterface, &cb) != ERROR_SUCCESS)
    {
        lstrcpy( szInterface, "<no name>" ) ;
    }

    wsprintf( szKey, _T("Interface\\%s\\Ole2ViewIViewerCLSID"), lpszIID );

    cb = sizeof(szValue) ;
	*szValue = _T('\0');
    CLSID clsid ;
    if (::RegQueryValue(HKEY_CLASSES_ROOT, szKey, szValue, &cb) == ERROR_SUCCESS)
    {
        sc = ::CLSIDFromString( T2OLE(szValue), &clsid ) ;
        if (FAILED(sc))
        {
            CString str;
		    str.Format(_T("Could not convert the CLSID of the %s interface viewer."), szInterface);
            ErrorMessage( str, sc ) ;
            return ;
        }
    }
    else
    {
        // Use the default
        clsid = CATID_Ole2ViewInterfaceViewers ;
    }


_CreateInstance:
    sc = ::CoCreateInstance( clsid, NULL, CLSCTX_SERVER, IID_IInterfaceViewer, (void**)&piv ) ;
    if (SUCCEEDED(sc))
    {
        IUnknown* ptemp = NULL ;
        if (punk)
            punk->QueryInterface( riid, (void**)&ptemp ) ;
         piv->View( hwnd, riid, ptemp ) ;
         piv->Release() ;
         if (ptemp)
            ptemp->Release() ;
    }
    else
    {
        CString str;
		if (sc == REGDB_E_CLASSNOTREG || sc == 0x8007007E /* File not found*/)
		{
			// Attempt to register IViewers.DLL
			//
			if (RegisterIViewersDLL(CWnd::FromHandle(hwnd)))
			{
				// Try again
				goto _CreateInstance ;
			}

		}
		else
		{
			str.Format(_T("The %s interface viewer failed to load."), szInterface);
		    ErrorMessage( str, sc ) ;
		}
    }
}

void CMainFrame::OnUseInProcServer()
{
    m_pObjTreeView->OnUseInProcServer();
}

void CMainFrame::OnUpdateUseInProcServer(CCmdUI* pCmdUI)
{
    m_pObjTreeView->OnUpdateUseInProcServer( pCmdUI );
}

void CMainFrame::OnUseInProcHandler()
{
   m_pObjTreeView->OnUseInProcHandler();
}

void CMainFrame::OnUpdateUseInProcHandler(CCmdUI* pCmdUI)
{
    m_pObjTreeView->OnUpdateUseInProcHandler( pCmdUI ) ;
}

void CMainFrame::OnUseLocalServer()
{
    m_pObjTreeView->OnUseLocalServer() ;
}

void CMainFrame::OnUpdateUseLocalServer(CCmdUI* pCmdUI)
{
    m_pObjTreeView->OnUpdateUseLocalServer( pCmdUI ) ;
}

void CMainFrame::OnFileBind()
{
    m_pObjTreeView->OnFileBind() ;
}

CMyOleDropTarget::CMyOleDropTarget ()
{
}

CMyOleDropTarget::~CMyOleDropTarget()
{
}

DROPEFFECT CMyOleDropTarget::OnDragEnter(CWnd* , COleDataObject* , DWORD , CPoint )
{
    DROPEFFECT de = DROPEFFECT_COPY ;
    return de ;
}

DROPEFFECT CMyOleDropTarget::OnDragOver(CWnd*, COleDataObject* , DWORD dwKeyState, CPoint )
{
    DROPEFFECT de ;
    // check for force link
    if ((dwKeyState & (MK_CONTROL|MK_SHIFT)) == (MK_CONTROL|MK_SHIFT))
        de = DROPEFFECT_LINK;
    // check for force copy
    else if ((dwKeyState & MK_CONTROL) == MK_CONTROL)
        de = DROPEFFECT_COPY;
    // check for force move
    else if ((dwKeyState & MK_ALT) == MK_ALT)
        de = DROPEFFECT_MOVE;
    // default -- recommended action is move
    else
        de = DROPEFFECT_MOVE;

    return de;
}

BOOL CMyOleDropTarget::OnDrop(CWnd* , COleDataObject* pDataObject, DROPEFFECT , CPoint )
{
    CMainFrame*  pfrm = (CMainFrame*)CWnd::FromHandle(m_hWnd) ;

    IUnknown* punk = NULL ;
    HRESULT hr = pDataObject->m_lpDataObject->QueryInterface(IID_IUnknown, (void**)&punk);
    if (FAILED(hr))
    {
        ErrorMessage( _T("QueryInterface(IID_IUnknown) failed on the data object."), hr ) ;
        punk = NULL ;
    }
    else
    {
        pfrm->m_pObjTreeView->AddObjectInstance(punk, _T("Drag and Drop Data Object")) ;
        punk->Release() ;
    }

    return (DROPEFFECT)-1;  // not implemented
}


void CMainFrame::OnDropFiles(HDROP hDropInfo) 
{
	SetActiveWindow();      // activate us first !
	UINT nFiles = ::DragQueryFile(hDropInfo, (UINT)-1, NULL, 0);

	for (UINT iFile = 0; iFile < nFiles; iFile++)
	{
		TCHAR szFileName[_MAX_PATH];
		::DragQueryFile(hDropInfo, iFile, szFileName, _MAX_PATH);
		
        CMainFrame* pfrm = (CMainFrame*)AfxGetApp()->GetMainWnd() ;
        ASSERT(pfrm);
        IUnknown* punk = NULL ;
        if (SUCCEEDED(pfrm->m_pObjTreeView->BindToFile(szFileName, &punk)))
        {
            pfrm->m_pObjTreeView->AddObjectInstance(punk, szFileName) ;
            punk->Release() ;
        }
        else
        {
            USES_CONVERSION;
            HRESULT hr ;

            // Maybe it's a TLB file
            LPTYPELIB lpTypeLib;
	        hr = ::LoadTypeLib(T2COLE(szFileName), &lpTypeLib);
            if (SUCCEEDED(hr))
            {
	            // call the interface wiewer
	            ASSERT(lpTypeLib != NULL);
                ViewInterface( pfrm->GetSafeHwnd(), IID_ITypeLib, (IUnknown*)lpTypeLib);
                VERIFY(0 == lpTypeLib->Release()) ;
            }
            else
            {       
		        CString szErrorMsg;
		        szErrorMsg.Format(_T("The file droped (%s) is not a valid persistent OLE object or Type Library file."),(LPCTSTR)szFileName);
                ErrorMessage( szErrorMsg, hr );
            }
        }
	}
	::DragFinish(hDropInfo);
}

void CMainFrame::OnActivateApp(BOOL bActive, HTASK hTask)
{
#ifdef _MAC
    if (m_fInOnCreateClient)
	    CFrameWnd::OnActivateApp(FALSE, hTask);
    else
#endif
	    CFrameWnd::OnActivateApp(bActive, hTask);
}

void CMainFrame::OnViewHiddenComCats() 
{
   m_pObjTreeView->OnViewHiddenComCats();
}

void CMainFrame::OnUpdateViewHiddenComCats(CCmdUI* pCmdUI) 
{
    m_pObjTreeView->OnUpdateViewHiddenComCats( pCmdUI ) ;
}
