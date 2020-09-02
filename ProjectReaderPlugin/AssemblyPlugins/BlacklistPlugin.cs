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
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;

    using Mono.Cecil;

    /// <summary>
    /// A public API detection plugin.
    /// </summary>
    [Export]
    [Export(typeof(IAssemblyPlugin))]
    [Export(typeof(IPlugin))]
    public class BlacklistedPlugin : IAssemblyPlugin
    {
        #region Fields

        /// <summary>
        /// The host.
        /// </summary>
        private IHost host;

        /// <summary>
        /// The namespaces to avoid.
        /// </summary>
        private List<String> NamespacesToAvoid = new List<String>() { "System.Diagnosics" };

        /// <summary>
        /// The blacklisted namespaces.
        /// </summary>
        private readonly Dictionary<String, List<String>> BlacklistedNamespaces = new Dictionary<String, List<String>>()
        {
            { "System.IO",new List<String> { "File", "Path", "Directory" } }
        };

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BlacklistedPlugin()
        {
            //
        }

        #endregion Constructors

        #region Properties

        public Maturity Maturity
        {
            get
            {
                return Maturity.alpha;
            }
        }

        public String Name
        {
            get
            {
                return "Detection of blacklisted classes";
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
        public string Description => "This plugin decompiles an Assembly and reports any blacklisted classed used.";

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

        public bool Execute(IJob job)
        {
            host.AddResult(Severity.Debug, true, $"{job.GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Examining Output of '{Path.GetFileNameWithoutExtension(job.Parm)}' Project for blacklisted References]");

            if (File.Exists(job.Parm))
            {
                GetReferencesList(job.Parm);
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Assembly.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/40433300/list-all-references-calling-a-method-using-mono-cecil.
        /// </summary>
        ///
        /// <param name="assemblyName"> Name of the assembly. </param>
        private void GetReferencesList(string assemblyName)
        {
            //! #warning MOVE CODE TO IASSEMBLYPLUGIN (check for isAsset/isPortableAsset).
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyName);

            Boolean hit = false;

            // Notes: Cecil helps a lot by dynamically analyzing the assembly.
            //
            foreach (ModuleDefinition module in assembly.Modules)
            {
                // 1) Check for method patterns 

                foreach (TypeReference type in module.GetTypeReferences())
                {
                    try
                    {
                        TypeDefinition td = type.Resolve();

                        if (type.Namespace.Equals("AssetPackage") && td.IsInterface && (td.HasMethods || td.HasProperties || td.HasFields))
                        {
                            // Seems to complement the Interface Detection Plugin.
                            // 
                            host.AddResult(Severity.Info, true, $"Bridge interface: {type.FullName}"); // or type.FullName
                        }

                        if ((NamespacesToAvoid.Contains(type.Namespace) && td.IsClass))
                        {
                            host.AddResult(Severity.Warning, false, $"Class to avoid: {type.FullName} [{td.Name}]"); // or type.FullName
                            hit = true;
                        }

                        if (BlacklistedNamespaces.ContainsKey(type.Namespace) &&
                            td.IsClass && BlacklistedNamespaces[type.Namespace].Contains(td.Name))
                        {
                            host.AddResult(Severity.Warning, false, $"Class causing portability issues: {type.FullName} [{td.Name}]"); // or type.FullName
                            hit = true;
                        }
                    }
                    catch /*(Exception e)*/
                    {
                        //Debug.WriteLine($"{e.GetType().Name} - {e.Message}");
                    }
                }
            }

            if (!hit)
            {
                host.AddResult(Severity.Info, true, $"No blacklisted classes detected."); // or type.FullName
            }
        }

        /// <summary>
        /// Initializes this Plugin.
        /// </summary>
        ///
        /// <param name="host"> (Optional) The host. </param>
        public void Initialize(IHost host = null)
        {
            this.host = host;
        }

        /// <summary>
        /// Checks for Support.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public bool Supports(string parm)
        {
            return Path.GetExtension(parm).Equals(".dll", StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion Methods
    }
}