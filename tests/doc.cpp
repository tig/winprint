// doc.cpp : implementation of the COle2ViewDoc class
//

#include "stdafx.h"
#include "Ole2View.h"

#include "doc.h"

#ifdef _DEBUG
#undef THIS_FILE
static char BASED_CODE THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// COle2ViewDoc

IMPLEMENT_DYNCREATE(COle2ViewDoc, CDocument)
#define new DEBUG_NEW

BEGIN_MESSAGE_MAP(COle2ViewDoc, CDocument)
    //{{AFX_MSG_MAP(COle2ViewDoc)
        // NOTE - the ClassWizard will add and remove mapping macros here.
        //    DO NOT EDIT what you see in these blocks of generated code !
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// COle2ViewDoc construction/destruction

COle2ViewDoc::COle2ViewDoc()
{
    m_clsidCur = GUID_NULL ;
    m_szObjectCur = "" ;
	m_nType = CObjectData::typeUnknown ;
    CString szSection ;
    CString szKey ;
    szSection.LoadString( IDS_INI_CONFIG ) ;

    szKey.LoadString( IDS_INI_CLSCTX ) ;
    m_dwClsCtx = (DWORD)theApp.GetProfileInt( szSection, szKey, CLSCTX_LOCAL_SERVER | CLSCTX_INPROC_SERVER ) ;

    szKey = _T("ViewHiddenComCats");
    m_fViewHiddenComCats = (BOOL)theApp.GetProfileInt( szSection, szKey, FALSE) ;
}

COle2ViewDoc::~COle2ViewDoc()
{

    CString szSection ;
    CString szKey ;
    szSection.LoadString( IDS_INI_CONFIG ) ;

    szKey.LoadString( IDS_INI_CLSCTX ) ;
    theApp.WriteProfileInt( szSection, szKey, (WORD)m_dwClsCtx ) ;

    szKey = _T("ViewHiddenComCats");
    theApp.WriteProfileInt( szSection, szKey, m_fViewHiddenComCats) ;
}

BOOL COle2ViewDoc::OnNewDocument()
{
//    if (!CDocument::OnNewDocument())
//        return FALSE;
    return TRUE;
}

void COle2ViewDoc::OnCloseDocument()
{
    CDocument::OnCloseDocument();
}       


/////////////////////////////////////////////////////////////////////////////
// COle2ViewDoc diagnostics

#ifdef _DEBUG
void COle2ViewDoc::AssertValid() const
{
    CDocument::AssertValid();
}

void COle2ViewDoc::Dump(CDumpContext& dc) const
{
    CDocument::Dump(dc);
}

#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// COle2ViewDoc commands

