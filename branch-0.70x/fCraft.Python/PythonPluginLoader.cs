using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using fCraft;
using fCraft.Events;

using IronPython.Hosting;
using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;
using IronPython.Runtime.Operations;


namespace fCraft.Python {
    /// <summary> Loads IronPython plugin files. </summary>
    public class PythonPluginLoader : IPluginLoader {
        private ScriptRuntime _ironPythonRuntime;
        private ScriptEngine _ironPythonEngine;

        public PythonPluginLoader() {
            _ironPythonEngine = IronPython.Hosting.Python.CreateEngine();
            _ironPythonRuntime = _ironPythonEngine.Runtime;
            _ironPythonRuntime.LoadAssembly(typeof(Server).Assembly);
        }

        public string[] PluginExtensions {
            get { return new [] { ".py" }; }
        }

        public PluginLoadResult LoadPlugins(string fileName) {
            try {
                ScriptSource script = _ironPythonEngine.CreateScriptSourceFromFile(fileName);
                CompiledCode code = script.Compile();
                ScriptScope scope = _ironPythonEngine.CreateScope();
                code.Execute(scope);

                List<IPlugin> plugins = new List<IPlugin>();
                foreach (KeyValuePair<string, dynamic> kvp in scope.GetItems().Where(kvp => kvp.Value is PythonType)) {
                    dynamic value = kvp.Value;
                    if (PythonOps.IsSubClass(value, DynamicHelpers.GetPythonTypeFromType(typeof(IPlugin)))) {
                        IPlugin plugin = value();
                        plugins.Add(plugin);
                    }
                }
                return new PluginLoadResult() {
                    LoadedPlugins = plugins,
                    LoadSuccessful = true, 
                };

            } catch (Exception e) {
                return new PluginLoadResult() {
                    Exception = e,
                    LoadSuccessful = false
                };
            }
        }

        private void LoadAssemblies() {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                _ironPythonRuntime.LoadAssembly(assembly);
            }
        }
    }
}