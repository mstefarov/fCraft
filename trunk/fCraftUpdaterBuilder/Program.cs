using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;


namespace fCraftUpdaterBuilder {
    class Program {

        static readonly string[] FileList = {
            "AutoLauncher.exe",
            "ConfigTool.exe",
            "fCraft.dll",
            "fCraftConsole.exe",
            "fCraftUI.exe",
            "fCraftWinService.exe",
            "../../CHANGELOG.txt",
            "../../README.txt"
        };

        const string BinariesFileName = "../../fCraftUpdater/Resources/Payload.zip";


        static void Main( string[] args ) {
            FileInfo binaries = new FileInfo( BinariesFileName );
            if( binaries.Exists ) {
                binaries.Delete();
            }

            using( ZipStorer zs = ZipStorer.Create( binaries.FullName, "" ) ) {
                foreach( string file in FileList ) {
                    FileInfo fi = new FileInfo( file );
                    zs.AddFile( ZipStorer.Compression.Deflate, fi.FullName, fi.Name, "" );
                }
            }
        }
    }
}