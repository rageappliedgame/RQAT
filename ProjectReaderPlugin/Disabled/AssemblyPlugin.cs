/*
 * Copyright 2020 Open University of the Netherlands
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * This project has received funding from the European Union’s Horizon
 * 2020 research and innovation programme under grant agreement No 644187.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace RQAT
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using AssetPackage;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
#if enabled
    [Export]
    [Export(typeof(IAssemblyPlugin))]
    [Export(typeof(IPlugin))]
#endif
    public class AssemblyPlugin : MarshalByRefObject
#if enabled
        , IAssemblyPlugin
#endif
    {
        #region Fields

        /// <summary>
        /// The host.
        /// </summary>
        private IHost host;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AssemblyPlugin()
        {
            //
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the maturity.
        /// </summary>
        ///
        /// <value>
        /// The maturity.
        /// </value>
        public Maturity Maturity
        {
            get
            {
                return Maturity.alpha;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        ///
        /// <value>
        /// The name.
        /// </value>
        public String Name
        {
            get
            {
                return "Assembly Loader";
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        ///
        /// <value>
        /// The version.
        /// </value>
        public Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Gets the description.
        /// </summary>
        ///
        /// <value>
        /// The description.
        /// </value>
        public string Description => "This plugin is disabled.";

        /// <summary>
        /// Gets a value indicating whether this object is leaf.
        /// </summary>
        ///
        /// <value>
        /// True if this object is leaf, false if not.
        /// </value>
        public bool IsLeaf => true;

        #endregion Properties

        #region Methods

        ///// <summary>
        ///// The ads.
        ///// </summary>
        //private static readonly AppDomainSetup ads;

        /// <summary>
        /// The ad.
        /// </summary>
        private static AppDomain ad;

        /// <summary>
        /// Executes.
        /// </summary>
        ///
        /// <param name="job"> The parameter. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;string,bool&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            host.AddResult(Severity.Debug, true, $"{job.GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Decompiling '{Path.GetFileNameWithoutExtension(job.Parm)}' Assembly]");

            if (File.Exists(job.Parm))
            {
                //! See https://stackoverflow.com/questions/658498/how-to-load-an-assembly-to-appdomain-with-all-references-recursively
                //! See https://github.com/jduv/AppDomainToolkit
                // 
                //! See https://msdn.microsoft.com/en-us/library/7hcs6az6(v=vs.110).aspx (this actually works as it does not lock Assemblies).
                // 
                String dir = Path.GetDirectoryName(job.Parm);

                if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                {
                    //Utils.SetDllDirectory(Path.GetDirectoryName(dir));

                    //ads = AppDomain.CurrentDomain.SetupInformation;
                    //ads.PrivateBinPath = dir;

                    //ad = AppDomain.CreateDomain("Mine", AppDomain.CurrentDomain.Evidence, ads);
                    //ad.Load(GetType().Assembly.FullName);
                    //ad.AssemblyLoad += Ad_AssemblyLoad;
                    //AppDomain.CurrentDomain.SetupInformation.SetPrivateBinPath(dir);

                    ad = AppDomain.CurrentDomain;

                    //if (parm.Contains("_Portable"))
                    //{
                    //    asm1 = AppDomain.CurrentDomain.Load(Utils.LoadFile(
                    //        Path.Combine(Path.GetDirectoryName(parm), "RageAssetManager_Portable.dll")
                    //        ));
                    //}
                    // 

                    //! Fails 2nd time.
                    //Assembly x = Assembly.ReflectionOnlyLoad(Utils.LoadFile(parm));
                    // 
                    //var asm = ad.CreateInstanceFrom(parm, "BaseAsset");

                    ad.Load(Utils.LoadFile(@"C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\bin\Debug\RageAssetManager.dll"));
                    ad.Load(Utils.LoadFile(@"C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset_Portable\bin\Debug\RageAssetManager_Portable.dll"));

                    Assembly asm = ad.Load(Utils.LoadFile(job.Parm));

                    host.AddResult(Severity.Info, true, $"Loaded Assembly.");

                    host.AddResult(Severity.Info, true, $"Assembly using .Net {asm.ImageRuntimeVersion}.");

                    Debug.Print(asm.Location);

                    Type ba = null;
                    Type bas = null;

                    //var a = AppDomain.CurrentDomain.CreateInstance(parm, "BaseAsset");

                    //! For portable assembly ExportedTypes fails with:
                    //! Could not load type 'AssetPackage.BaseAsset' from assembly 'GameStorageClientAsset_Portable
                    //! Might be that base asset is identical between the .Net 3.5 & .Net Portable assemblies.
                    // 
                    //  
                    foreach (Type t in asm.ExportedTypes)
                    {
                        if (t.BaseType != null && t.BaseType.Name.Equals(typeof(BaseAsset).Name))
                        {
                            ba = t;
                        }
                        if (t.BaseType != null && t.BaseType.Name.Equals(typeof(BaseSettings).Name))
                        {
                            bas = t;
                        }

                        //GameStorageClientAsset,BaseAsset
                        //GameStorageClientAssetSettings, BaseSettings

                        //Debug.Print($"{t.Name},{(t.BaseType != null ? t.BaseType.Name : "<none>")}");
                    }

                    //Utils.SetDllDirectory(Path.GetDirectoryName(null));

                    BaseAsset asset = (BaseAsset)Activator.CreateInstance(ba);
                    ISettings Settings = asset.Settings;

                    MethodInfo mi = asset.GetType().GetRuntimeMethod("CheckHealth", new Type[] { });

                    //! Fails as AssetManager is to old (no AssetPackage.RequestSetttings.hasBinaryResponse field).
                    // 
                    //Debug.Print($"CheckHealth: {mi.Invoke(asset, new object[] { })})");
                }
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Assembly.");

                return false;
            }

            return true;
        }

        private void Ad_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.Print(args.LoadedAssembly.FullName);
        }



        /// <summary>
        /// Initializes this object.
        /// </summary>
        public void Initialize(IHost host)
        {
            this.host = host;
        }

        /// <summary>
        /// Supports.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public bool Supports(String parm)
        {
            return false; //(Path.GetExtension(parm).Equals(".dll", StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Event handler. Called by CD for type resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_TypeResolve(object sender, ResolveEventArgs args)
        {
            Debug.Print("<TYPE>" + args.Name);

            return args.RequestingAssembly;
        }

        /// <summary>
        /// Event handler. Called by CD for assembly load events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Assembly load event information. </param>
        private static void Cd_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.Print("<LOAD>" + args.LoadedAssembly.FullName);
        }

        /// <summary>
        /// Event handler. Called by CD for assembly resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.Print("<RESOLVE>" + args.Name);

            //String fn = args.Name;

            //if (File.Exists(fn))
            //{
            //    return Assembly.LoadFile(fn);
            //}
            //else
            //{
            return Assembly.Load(args.Name);
            //}
        }

        /// <summary>
        /// Event handler. Called by CD for reflection only assembly resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            //! See https://blogs.msdn.microsoft.com/jasonz/2004/05/31/why-isnt-there-an-assembly-unload-method/
            //! See https://stackoverflow.com/questions/123391/how-to-unload-an-assembly-from-the-primary-appdomain
            //! See https://blogs.msdn.microsoft.com/raulperez/2010/03/04/net-reflection-and-unloading-assemblies/
            //! See https://stackoverflow.com/questions/5783228/effect-of-loaderoptimizationattribute
            // 
            Debug.Print("<ORIGIN>" + args.RequestingAssembly.GetName());
            Debug.Print("<RESOLVE>" + args.Name);

            //String fn = Path.Combine(
            //    Path.GetDirectoryName(args.RequestingAssembly.CodeBase),
            //    Path.ChangeExtension(args.Name.Substring(0, args.Name.IndexOf(',')), ".dll"));
            //if (File.Exists(fn))
            //{
            //    return Assembly.ReflectionOnlyLoadFrom(fn);
            //}
            //else
            //{
            return Assembly.ReflectionOnlyLoad(args.Name);
            //}
        }

        #endregion Methods
    }

    /// <summary>
    /// A proxy.
    /// 
    /// See https://stackoverflow.com/questions/658498/how-to-load-an-assembly-to-appdomain-with-all-references-recursively
    /// </summary>
    public class ProxyDomain : MarshalByRefObject
    {
        public Assembly GetAssembly(string assemblyPath)
        {
            try
            {
                return Assembly.LoadFile(assemblyPath);
            }
            catch (Exception)
            {
                return null;
                // throw new InvalidOperationException(ex);
            }
        }
    }
}