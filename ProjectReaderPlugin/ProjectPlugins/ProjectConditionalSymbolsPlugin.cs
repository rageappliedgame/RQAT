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

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
    public class ProjectConditionalSymbolsPlugin : IProjectPlugin
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
        public ProjectConditionalSymbolsPlugin()
        {
            // Nothing
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
                return "Conditional Compilation Symbol Check";
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
        public string Description => "RCSAA - This plugin checks if the PORTABLE symbol is set correctly for the various Asset Assemblies.";

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
        /// Executes.
        /// </summary>
        ///
        /// <remarks>
        /// Detects if the solution contains a correctly named asset pair (normal/portable) and the
        /// PORTABLE define is correctly set.
        /// </remarks>
        ///
        /// <param name="job"> The parameter. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;string,bool&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Examining '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

            //! UnitTest Assemblies can/will return a false positive if they test an Asset.
            // 
            if (!Utils.IsUnitTest(job.Parm))
            {
                String output = Utils.ProjectOutput(job.Parm);

                if (File.Exists(output))
                {
                    //! Non Portable Assets, no PORTABLE Symbol.
                    // 
                    if (Utils.IsAsset(output))
                    {
                        if (Utils.DefinedSymbols(job.Parm).Contains(Utils.portable))
                        {
                            host.AddResult(Severity.Error, true, $"'{Utils.portable}' conditional compilation symbol is defined for Normal Asset.");
                        }
                        else
                        {
                            return true;
                        }
                    }

                    //! Portable Assets, PORTABLE Symbol.
                    // 
                    if (Utils.IsPortableAsset(output))
                    {
                        if (!Utils.DefinedSymbols(job.Parm).Contains(Utils.portable))
                        {
                            host.AddResult(Severity.Warning, true, $"'{Utils.portable}' conditional compilation symbol is not defined for Portable Asset.");
                        }
                        else
                        {
                            return true;
                        }
                    }

                    //! .Net Core Assets, PORTABLE Symbol.
                    // 
                    if (Utils.IsCoreAsset(output))
                    {
                        if (!Utils.DefinedSymbols(job.Parm).Contains(Utils.portable))
                        {
                            host.AddResult(Severity.Warning, true, $"'{Utils.portable}' conditional compilation symbol is not defined for .Net Core Asset.");
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(output))
                    {
                        host.AddResult(Severity.Warning, false, $"Failed to Locate Output.");
                    }
                    else
                    {
                        host.AddResult(Severity.Warning, false, $"Failed to Load Output: '{Path.GetFileName(output)}'.");
                    }
                }

#warning Check as well symbol is used in properties.cs to enable shared compilation.
            }

            return false;
        }

        /// <summary>
        /// Initializes this object.
        /// </summary>
        ///
        /// <param name="host"> The host. </param>
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
            return (Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase)
                && File.Exists(parm)
                && (Utils.ProjectOutputType(parm) == Utils.OutputType.Library));
            //&& (Utils.IsAsset(parm) || Utils.IsPortableAsset(parm))
            //&& !Utils.IsUnitTest(parm));
        }

        #endregion Methods
    }
}