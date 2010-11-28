﻿/*
 *  Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 *
 */
using System;
using System.Windows.Forms;
using fCraft;

namespace fCraftUI {
    static class Program {

        [STAThread]
        static void Main( string[] args ) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
#if DEBUG
            Application.Run( new MainForm( args ) );
#else
            try {
                Application.Run( new MainForm( args ) );
            } catch( Exception ex ) {
                Logger.UploadCrashReport( "Unhandled exception in fCraftUI", "fCraftUI", ex );
                Logger.Log( "Unhandled exception in fCraftUI: " + ex, LogType.FatalError );
                Server.CheckForCommonErrors( ex );
            }
#endif
        }
    }
}