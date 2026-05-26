using System;
using System.Collections.Generic;

namespace WinPrint.Core.Abstractions;

public interface IPrintJob : IDisposable
{
    void Begin ();
    void PrintPage (int pageNumber, Action<IGraphicsContext, int> renderPage);
    void End ();
}
