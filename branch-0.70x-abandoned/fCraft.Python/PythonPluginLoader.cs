// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// PythonPluginLoader based on code contributed by Jared Klopper (LgZ-optical).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;
using IronPython.Runtime.Operations;


namespace fCraft.Python {
    /// <summary> Loads IronPython plugin files. </summary>
    public class PythonPluginLoader : IPluginLoader {
        readonly ScriptRuntime ironPythonRuntime;
        readonly ScriptEngine ironPythonEngine;


        public PythonPluginLoader() {
            ironPythonEngine = IronPython.Hosting.Python.CreateEngine();
            ironPythonRuntime = ironPythonEngine.Runtime;
            ironPythonRuntime.LoadAssembly( typeof( Server ).Assembly );
        }


        public string[] PluginExtensions {
            get { return new[] { ".py" }; }
        }


        public IPlugin LoadPlugin( PluginDescriptor descriptor ) {
            string descriptorPath = Path.GetDirectoryName( descriptor.PluginDescriptorFileName );
            string fileName = Path.GetFullPath( Path.Combine( descriptorPath, descriptor.PluginFileName ) );

            ScriptSource script = ironPythonEngine.CreateScriptSourceFromFile( fileName );
            CompiledCode code = script.Compile();
            ScriptScope scope = ironPythonEngine.CreateScope();
            code.Execute( scope );

            IEnumerable<dynamic> typeList = scope.GetItems().Select( kvp => kvp.Value ).Where( item => item is PythonType );
            dynamic pluginType = typeList.FirstOrDefault( item => item.Name.Equals( descriptor ) );
            if( pluginType != null ) {
                if( PythonOps.IsSubClass( pluginType, DynamicHelpers.GetPythonTypeFromType( typeof( IPlugin ) ) ) ) {
                    IPlugin plugin = pluginType();
                    return plugin;
                } else {
                    throw new Exception( "Specified type does not implement IPlugin." );
                }
            } else {
                throw new Exception( "Specified type not found." );
            }
        }
    }
}