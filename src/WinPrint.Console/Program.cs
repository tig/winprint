﻿// Copyright Kindel, LLC - http://www.kindel.com
// Published under the MIT License at https://github.com/tig/winprint

/// <summary>
/// This is a dummy app used only to enable MSbuild to copy dependent DLLs.
/// </summary>
namespace WinPrint.Console {
    internal class Program {
        private static void Main(string[] args) {
            System.Console.WriteLine("This is a dummy app used only to enable MSbuild to copy dependent DLLs.");
        }
    }
}
