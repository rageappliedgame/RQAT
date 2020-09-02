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
    using System.IO;

    using ICSharpCode.Decompiler;
    using ICSharpCode.Decompiler.CSharp;
    using ICSharpCode.Decompiler.TypeSystem;

    /// <summary>
    /// A public API detection plugin.
    /// </summary>
    [Export]
    [Export(typeof(IAssemblyPlugin))]
    [Export(typeof(IPlugin))]
    public class InterfaceDetectionPlugin : IAssemblyPlugin
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
        public InterfaceDetectionPlugin()
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
                return "Interface Detection";
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
        public string Description => "This plugin decompiles an Assembly and reports RAGE Interfaces used.";

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

            host.AddResult(Severity.Info, true, $"[Examining Output of '{Path.GetFileNameWithoutExtension(job.Parm)}' Project for Interfaces]");

            if (File.Exists(job.Parm))
            {
                Disassemble(job.Parm);
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Assembly.");

                return false;
            }

            return true;
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

        /// <summary>
        /// Disassembles.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        private void Disassemble(string parm)
        {
            CSharpDecompiler decompiler = new CSharpDecompiler(parm, new DecompilerSettings()
            {
                AlwaysUseBraces = true,
                LoadInMemory = true,

                //RemoveDeadCode = true,
            });

            foreach (ITypeDefinition typeInAssembly in decompiler.TypeSystem.GetAllTypeDefinitions())
            {
                // Look at public classes in the MainAssembly only.
                // It seems to complement the interfaces detected by the Blacklist Plugin.
                // 
                if (typeInAssembly.Accessibility == Accessibility.Public
                    && typeInAssembly.ParentModule == decompiler.TypeSystem.MainModule)
                {
                    if (typeInAssembly.Kind == TypeKind.Interface)
                    {
                        host.AddResult(Severity.Info, true, $"T:{typeInAssembly.Namespace}.{typeInAssembly.Name}");
                    }
                }
            }
        }

        #endregion Methods
    }
}