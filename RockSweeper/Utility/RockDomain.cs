using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Handles executing code in a custom Rock domain that does not interfere with the main
    /// application domain.
    /// </summary>
    [Serializable]
    public class RockDomain : IDisposable
    {
        #region Properties

        AppDomain Domain { get; set; }

        /// <summary>
        /// Gets the executor for actions to be performed inside the Rock domain.
        /// </summary>
        /// <value>
        /// The executor for actions to be performed inside the Rock domain.
        /// </value>
        public RemoteExec Executor { get; private set; }

        private RemotingSponsor Sponsor { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RockDomain"/> class.
        /// </summary>
        /// <param name="rockWebPath">The path to the RockWeb folder.</param>
        /// <exception cref="FileNotFoundException">Could not locate the Rock.dll</exception>
        public RockDomain( string rockWebPath )
        {
            var rockDllPath = Path.Combine( rockWebPath, "bin", "Rock.dll" );

            if ( !File.Exists( rockDllPath ) )
            {
                throw new FileNotFoundException( "Could not locate the Rock.dll" );
            }

            var setupInfo = new AppDomainSetup
            {
                ApplicationName = "Rock",
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                PrivateBinPath = Path.Combine( rockWebPath, "bin" )
            };

            Domain = AppDomain.CreateDomain( "Rock", AppDomain.CurrentDomain.Evidence, setupInfo );
            Domain.AssemblyResolve += AppDomain_AssemblyResolve;

            var executeType = typeof( RemoteExec );
            Executor = Domain.CreateInstanceAndUnwrap( executeType.Assembly.FullName, executeType.FullName ) as RemoteExec;

            Sponsor = new RemotingSponsor( Executor );
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Executor = null;
            AppDomain.Unload( Domain );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Finds the types that inherit from the given base type.
        /// </summary>
        /// <param name="baseTypeName">Name of the base type.</param>
        /// <returns>Collection of full class names.</returns>
        public string[] FindTypes( string baseTypeName )
        {
            return Executor.Execute( ( name ) =>
            {
                var baseType = Type.GetType( name + ", Rock" );

                var types = FindTypes( baseType );

                return types.Keys.ToArray();
            }, baseTypeName );
        }

        #endregion

        #region Private Rock Reflection Methods

        /// <summary>
        /// Finds the all the types that implement or inherit from the baseType. NOTE: It will only search the Rock.dll and also in assemblies that reference Rock.dll. The baseType
        /// will not be included in the result
        /// </summary>
        /// <param name="baseType">base type.</param>
        /// <param name="typeName">typeName can be specified to filter it to a specific type name</param>
        /// <returns></returns>
        private static SortedDictionary<string, Type> FindTypes( Type baseType, string typeName = null )
        {
            SortedDictionary<string, Type> types = new SortedDictionary<string, Type>();

            var assemblies = GetPluginAssemblies();

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            assemblies.Add( executingAssembly );

            foreach ( var assemblyEntry in assemblies )
            {
                var typeEntries = SearchAssembly( assemblyEntry, baseType );
                foreach ( KeyValuePair<string, Type> typeEntry in typeEntries )
                {
                    if ( string.IsNullOrWhiteSpace( typeName ) || typeEntry.Key == typeName )
                    {
                        if ( !types.ContainsKey( typeEntry.Key ) )
                        {
                            types.Add( typeEntry.Key, typeEntry.Value );
                        }
                    }
                }
            }

            return types;
        }

        /// <summary>
        /// Searches the assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="baseType">Type of the base.</param>
        /// <returns></returns>
        private static Dictionary<string, Type> SearchAssembly( Assembly assembly, Type baseType )
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();

            try
            {
                foreach ( Type type in assembly.GetTypes() )
                {
                    if ( !type.IsAbstract )
                    {
                        if ( baseType.IsInterface )
                        {
                            foreach ( Type typeInterface in type.GetInterfaces() )
                            {
                                if ( typeInterface == baseType )
                                {
                                    types.Add( type.FullName, type );
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Type parentType = type.BaseType;
                            while ( parentType != null )
                            {
                                if ( parentType == baseType )
                                {
                                    types.Add( type.FullName, type );
                                    break;
                                }
                                parentType = parentType.BaseType;
                            }
                        }
                    }
                }
            }
            catch ( ReflectionTypeLoadException ex )
            {
                string dd = ex.Message;
            }

            return types;
        }

        /// <summary>
        /// Gets a list of Assemblies in the ~/Bin and ~/Plugins folders that are possible Rock plugin assemblies
        /// </summary>
        /// <returns></returns>
        private static List<Assembly> GetPluginAssemblies()
        {
            // Add executing assembly's directory
            string binDirectory = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;

            // Add all the assemblies in the 'Plugins' subdirectory
            string pluginsFolder = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "Plugins" );

            // blacklist of files that would never have Rock MEF components or Rock types
            string[] ignoredFileStart = { "Lucene.", "Microsoft.", "msvcr100.", "System.", "JavaScriptEngineSwitcher.", "React.", "CacheManager." };

            // get all *.dll in the bin and plugin directories except for blacklisted ones
            var assemblyFileNames = Directory.EnumerateFiles( binDirectory, "*.dll", SearchOption.AllDirectories ).ToList();

            if ( Directory.Exists( pluginsFolder ) )
            {
                assemblyFileNames.AddRange( Directory.EnumerateFiles( pluginsFolder, "*.dll", SearchOption.AllDirectories ) );
            }

            assemblyFileNames = assemblyFileNames.Where( a => !a.EndsWith( ".resources.dll", StringComparison.OrdinalIgnoreCase )
                                        && !ignoredFileStart.Any( i => Path.GetFileName( a ).StartsWith( i, StringComparison.OrdinalIgnoreCase ) ) ).ToList();

            // get a lookup of already loaded assemblies so that we don't have to load it unnecessarily
            var loadedAssembliesDictionary = new Dictionary<string, Assembly>();
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where( a => !a.IsDynamic && !a.GlobalAssemblyCache && !string.IsNullOrWhiteSpace( a.Location ) );

            foreach ( var a in loadedAssemblies )
            {
                if ( !loadedAssembliesDictionary.ContainsKey( new Uri( a.CodeBase ).LocalPath ) )
                {
                    loadedAssembliesDictionary.Add( new Uri( a.CodeBase ).LocalPath, a );
                }

            }

            List<Assembly> pluginAssemblies = new List<Assembly>();

            foreach ( var assemblyFileName in assemblyFileNames )
            {
                Assembly assembly = null;

                if ( loadedAssembliesDictionary.ContainsKey( assemblyFileName ) )
                {
                    assembly = loadedAssembliesDictionary[assemblyFileName];
                }

                if ( assembly == null )
                {
                    try
                    {
                        // if an assembly is found that isn't loaded yet, load it into the CurrentDomain
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName( assemblyFileName );
                        assembly = AppDomain.CurrentDomain.Load( assemblyName );
                    }
                    catch
                    {
                        /* Intentionally ignored */
                    }
                }

                if ( assembly != null )
                {
                    bool isRockAssembly = false;

                    // only search inside dlls that are Rock.dll or reference Rock.dll
                    if ( assemblyFileName.EndsWith( "\\Rock.dll", StringComparison.OrdinalIgnoreCase ) )
                    {
                        isRockAssembly = true;
                    }
                    else
                    {
                        List<AssemblyName> referencedAssemblies = assembly.GetReferencedAssemblies().ToList();

                        if ( referencedAssemblies.Any( a => a.Name.Equals( "Rock", StringComparison.OrdinalIgnoreCase ) ) )
                        {
                            isRockAssembly = true;
                        }
                    }

                    if ( isRockAssembly )
                    {
                        pluginAssemblies.Add( assembly );
                    }
                }
            }

            return pluginAssemblies.ToList();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the AssemblyResolve event of the AppDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ResolveEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private Assembly AppDomain_AssemblyResolve( object sender, ResolveEventArgs args )
        {
            var name = new AssemblyName( args.Name );

            var path = Path.Combine( AppDomain.CurrentDomain.SetupInformation.PrivateBinPath, name.Name + ".dll" );
            var assembly = Assembly.LoadFrom( path );

            return assembly;
        }

        #endregion

        #region Support Classes

        [Serializable]
        public class RemoteExec : MarshalByRefObject
        {
            /// <summary>
            /// Executes the specified action.
            /// </summary>
            /// <param name="action">The action.</param>
            public void Execute( Action action )
            {
                action?.Invoke();
            }

            /// <summary>
            /// Executes the specified action.
            /// </summary>
            /// <typeparam name="T">The type of the parameter.</typeparam>
            /// <param name="action">The action.</param>
            /// <param name="p1">The parameter.</param>
            public void Execute<T>( Action<T> action, T p1 )
            {
                action?.Invoke( p1 );
            }

            /// <summary>
            /// Executes the specified action.
            /// </summary>
            /// <typeparam name="TResult">The type of the result.</typeparam>
            /// <param name="action">The action.</param>
            /// <returns>The value returned by the action.</returns>
            public TResult Execute<TResult>( Func<TResult> action )
            {
                if ( action != null )
                {
                    return action.Invoke();
                }

                return default( TResult );
            }

            /// <summary>
            /// Executes the specified action.
            /// </summary>
            /// <typeparam name="T">The type of the parameter.</typeparam>
            /// <typeparam name="TResult">The type of the result.</typeparam>
            /// <param name="action">The action.</param>
            /// <param name="p1">The parameter.</param>
            /// <returns>The value returned by the action.</returns>
            public TResult Execute<T, TResult>( Func<T, TResult> action, T p1 )
            {
                if ( action != null )
                {
                    return action.Invoke( p1 );
                }

                return default( TResult );
            }
        }

        /// <see cref="https://stackoverflow.com/questions/18680664/remoting-sponsor-stops-being-called"/>
        public class RemotingSponsor : MarshalByRefObject, ISponsor, IDisposable
        {
            /*
             * @CoryNelson said :
             * I've since determined that the ILease objects of my sponsors 
             * themselves are being GCed. They start out with the default 5min lease 
             * time, which explains how often my sponsors are being called. When I 
             * set my InitialLeaseTime to 1min, the ILease objects are continually        
             * renewed due to their RenewOnCallTime being the default of 2min.
             * 
             */

            ILease _lease;

            public RemotingSponsor( MarshalByRefObject mbro )
            {
                _lease = ( ILease ) RemotingServices.GetLifetimeService( mbro );
                if ( _lease == null ) throw new NotSupportedException( "Lease instance for MarshalByRefObject is NULL" );
                _lease.Register( this );
            }

            public TimeSpan Renewal( ILease lease )
            {
                System.Diagnostics.Debug.WriteLine( "RemotingSponsor.Renewal called" );
                return this._lease != null ? lease.InitialLeaseTime : TimeSpan.Zero;
            }


            public void Dispose()
            {
                if ( _lease != null )
                {
                    _lease.Unregister( this );
                    _lease = null;
                }
            }

            public override object InitializeLifetimeService()
            {
                /*
                 *
                 * @MatthewLee said:
                 *   It's been a long time since this question was asked, but I ran into this today and after a couple hours, I figured it out. 
                 * The 5 minutes issue is because your Sponsor which has to inherit from MarshalByRefObject also has an associated lease. 
                 * It's created in your Client domain and your Host domain has a proxy to the reference in your Client domain. 
                 * This expires after the default 5 minutes unless you override the InitializeLifetimeService() method in your Sponsor class or this sponsor has its own sponsor keeping it from expiring.
                 *   Funnily enough, I overcame this by returning Null in the sponsor's InitializeLifetimeService() override to give it an infinite timespan lease, and I created my ISponsor implementation to remove that in a Host MBRO.
                 * Source: https://stackoverflow.com/questions/18680664/remoting-sponsor-stops-being-called
                */
                return ( null );
            }
        }

        #endregion
    }
}
