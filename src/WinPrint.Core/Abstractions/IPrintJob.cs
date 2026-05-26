using System;

namespace WinPrint.Core.Abstractions;

public interface IPrintJob : IDisposable
{
    void Begin ();
    void PrintPage (int pageNumber, Action<IGraphicsContext, int> renderPage);
    void End ();
}
