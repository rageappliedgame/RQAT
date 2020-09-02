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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ICSharpCode.Decompiler;
    using ICSharpCode.Decompiler.CSharp;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IAssemblyPlugin))]
    [Export(typeof(IPlugin))]
    public class LocPlugin : IAssemblyPlugin
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
        public LocPlugin()
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
                return "Lines Of Code (LOC) metric";
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
        public string Description => "This plugin decompiles an Assembly and reports it's Lines Of Code (LOC) metric.";

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

        /// <summary>
        /// Decompiles.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        public String Decompile(String parm)
        {
            Version runtimeVersion = Utils.RuntimeVersion(parm);

            //! Asset Compiled with: .Net v2.0.50727
            //! Asset Compiled with: .Net v4.0.30319
            //
            Debug.WriteLine($"Asset Compiled with: .Net {runtimeVersion}");

            Boolean isAsset = Utils.IsAsset(parm);
            Boolean isPortableAsset = Utils.IsPortableAsset(parm);

#warning Also check runtime version/.net profile here.

            Debug.WriteLine($"Is Asset: {isAsset}");
            Debug.WriteLine($"Is Portable Asset: {isPortableAsset}");


            //{
            CSharpDecompiler decompilerA = new CSharpDecompiler(parm, new DecompilerSettings()
            {
                ShowXmlDocumentation = false,
                AlwaysUseBraces = true,
                LoadInMemory = true,
                RemoveDeadCode = true,
            });

            return decompilerA.DecompileWholeModuleAsString();
        }

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
            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Calculating Lines of Code of '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

            if (File.Exists(job.Parm))
            {
                List<String> lines = Decompile(job.Parm)
                    .Split(new char[] { '\r', '\n' }).ToList();
                Int32 LOC = lines.Count;

                //! See: https://dwheeler.com/sloccount/sloccount.html
                //! See: http://cloc.sourceforge.net/
                //
                List<String> nonemptylines = lines
                    .Where(p => !String.IsNullOrEmpty(p.Trim()))
                    .ToList();
                Int32 SLOC = nonemptylines.Count;

                List<String> trimmed = nonemptylines
                    .Where(p => !(new String[] { "{", "}" }.Contains(p.Trim())))
                    .ToList();
                Int32 LLOC = trimmed.Count;

                host.AddResult(Severity.Info, true, $"~ {LOC} Lines of Code (LOC) counted in {Path.GetFileName(job.Parm)}.");
                host.AddResult(Severity.Info, true, $"~ {SLOC} Source Lines of Code (SLOC) counted in {Path.GetFileName(job.Parm)}.");
                host.AddResult(Severity.Info, true, $"~ {LLOC} Logical Lines of Code (LLOC) counted in {Path.GetFileName(job.Parm)}.");
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Assembly.");

                return false;
            }

            return true;
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
            return (Path.GetExtension(parm).Equals(".dll", StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Methods
    }
}