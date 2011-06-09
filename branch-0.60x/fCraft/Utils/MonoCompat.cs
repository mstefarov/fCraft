// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Diagnostics;
using System.Reflection;

namespace fCraft {
    /// <summary> Class dedicated to solving Mono compatibility issues </summary>
    public static class MonoCompat {

        public static bool IsCaseSensitive { get; private set; }

        public static bool IsMono { get; private set; }

        public static bool IsSGen { get; private set; }

        public static string MonoVersionString { get; private set; }

        public static Version MonoVersion { get; private set; }

        public static bool IsWindows { get; private set; }


        const BindingFlags MonoMethodFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
        static MonoCompat() {
            Type monoRuntimeType = typeof( object ).Assembly.GetType( "Mono.Runtime" );

            if( monoRuntimeType != null ) {
                IsMono = true;
                MethodInfo getDisplayNameMethod = monoRuntimeType.GetMethod( "GetDisplayName", MonoMethodFlags, null, Type.EmptyTypes, null );
                if( getDisplayNameMethod != null ) {
                    MonoVersionString = (string)getDisplayNameMethod.Invoke( null, null );
                    try {
                        string[] parts = MonoVersionString.Split( '.' );
                        int major = Int32.Parse( parts[0] );
                        int minor = Int32.Parse( parts[1] );
                        int revision = Int32.Parse( parts[2].Substring( 0, parts[2].IndexOf( ' ' ) ) );
                        MonoVersion = new Version( major, minor, revision );
                        IsSGen = (major == 2 && minor > 6);
                    } catch( Exception ) {
                        Logger.Log( "Could not parse Mono version.", LogType.Error );
                        MonoVersion = null;
                        IsSGen = false;
                    }
                } else {
                    AssumeUnknownMonoVersion();
                }
            } else {
                IsMono = false;
                AssumeUnknownMonoVersion();
            }

            switch( Environment.OSVersion.Platform ) {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    IsWindows = false;
                    break;
                default:
                    IsWindows = true;
                    break;
            }

            IsCaseSensitive = !IsWindows;
        }

        static void AssumeUnknownMonoVersion() {
            MonoVersionString = "Unknown";
            MonoVersion = null;
            IsSGen = false;
        }

        /// <summary>Starts a .NET process, using Mono if necessary.</summary>
        /// <param name="assemblyLocation">.NET executable path</param>
        /// <param name="assemblyArgs">Arguments to pass to the executable</param>
        /// <param name="detachIfMono">If true, new process will be detached under Mono</param>
        /// <returns>Process object</returns>
        public static Process StartDotNetProcess( string assemblyLocation, string assemblyArgs, bool detachIfMono ) {
            string binaryName, args;
            if( IsMono ) {
                if( IsSGen ) {
                    binaryName = "mono-sgen";
                } else {
                    binaryName = "mono";
                }
                args = "\"" + assemblyLocation + "\"";
                if( !String.IsNullOrEmpty( assemblyArgs ) ) {
                    args += " " + assemblyArgs;
                }
                if( detachIfMono ) {
                    args += " &";
                }
            } else {
                binaryName = assemblyLocation;
                args = assemblyArgs;
            }
            return Process.Start( binaryName, args );
        }


        /// <summary>Prepends the correct Mono name to the .NET executable, if needed.</summary>
        /// <param name="dotNetExecutable"></param>
        /// <returns></returns>
        public static string PrependMono( string dotNetExecutable ) {
            if( IsMono ) {
                if( IsSGen ) {
                    return "mono-sgen " + dotNetExecutable;
                } else {
                    return "mono " + dotNetExecutable;
                }
            } else {
                return dotNetExecutable;
            }
        }
    }
}