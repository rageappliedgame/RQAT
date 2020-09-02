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
    [Export(typeof(ISolutionPlugin))]
    [Export(typeof(IPlugin))]
    public class SolutionAssetDetectionPlugin : ISolutionPlugin
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
        public SolutionAssetDetectionPlugin()
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
                return "*.Sln Level Asset Pair Detection";
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
        public string Description => "RCSAA - This plugin reports the project types detected in a solution and checks the naming convention (suffix).";

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
        /// Detects if the solution contains a correctly named asset pair (normal/portable).
        /// </remarks>
        ///
        /// <param name="job"> The parameter. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;string,bool&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            String nasset = String.Empty;
            String passet = String.Empty;
            String casset = String.Empty;

#warning TODO Add Support for Test Suite and other exe (demo) project).

            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Examnining '{Path.GetFileNameWithoutExtension(job.Parm)}' Solution]");

            if (File.Exists(job.Parm))
            {
                Solution solution = new Solution();

                if (solution.Load(job.Parm))
                {
                    foreach (SolutionProject project in solution.Projects)
                    {
                        if (project.ProjectTypeGuid.Equals(new Guid(Lookups.ProjectTypeGuids["C#"])))
                        {
                            String csproj = Path.Combine(Path.GetDirectoryName(solution.SolutionPath), project.RelativePath);

                            if (File.Exists(csproj))
                            {
                                //! Skip Test Projects.
                                //! Skip AssetManager from processing if present.
                                if (Utils.ProjectOutputType(csproj) == Utils.OutputType.Library
                                    && !Utils.IsUnitTest(csproj)
                                    && !Utils.IsAssetManager(csproj, host))
                                {
                                    String output = Utils.ProjectOutput(csproj);

                                    if (File.Exists(output))
                                    {
#warning TODO IsAsset may fail a second time or just on loading.
                                        // System.IO.FileLoadException: 'Could not load file or assembly 'ICSharpCode.Decompiler, 
                                        // Version = 3.1.0.3652, Culture = neutral, PublicKeyToken = d4bfe873e7598c49' or one of its dependencies. 
                                        // The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)'

                                        if (Utils.IsAsset(output))
                                        {
                                            host.AddResult(Severity.Debug, true, $"Asset Detected: '{Path.GetFileNameWithoutExtension(output)}'.");

                                            nasset = Path.GetFileNameWithoutExtension(output);
                                        }
                                        else if (Utils.IsPortableAsset(output))
                                        {
                                            host.AddResult(Severity.Debug, true, $"Portable Asset Detected: '{Path.GetFileNameWithoutExtension(output)}'.");

                                            passet = Path.GetFileNameWithoutExtension(output);
                                        }
                                        else if (Utils.IsCoreAsset(output))
                                        {
                                            host.AddResult(Severity.Debug, true, $".Net Core Asset Detected: '{Path.GetFileNameWithoutExtension(output)}'.");

                                            casset = Path.GetFileNameWithoutExtension(output);
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
                                }
                            }
                            else
                            {
                                host.AddResult(Severity.Warning, false, $"Failed to Locate Project.");
                            }
                        }
                    }
                }
                else
                {
                    host.AddResult(Severity.Info, false, $"Failed to Load or Parse Solution {job.Parm}.");

                    return false;
                }
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Solution.");

                return false;
            }

            //Boolean misnamed = false;

            // No assets found.
            // 
            if (nasset.Length + passet.Length + casset.Length == 0)
            {
                host.AddResult(Severity.Warning, false, $"No Asset assembly detected.");

                return false;
            }

            //! Main asset not found.
            // 
            if (String.IsNullOrEmpty(nasset))
            {
                host.AddResult(Severity.Warning, false, $"Main Asset assembly not detected.");
            }

            //! Portable asset naming.
            // 
            if (String.IsNullOrEmpty(passet))
            {
                host.AddResult(Severity.Warning, false, $"Portable Asset assembly not detected.");
            }
            else
            {
                //! Portable asset naming convention, '_Portable' suffix.
                // 
                if (Path.GetFileNameWithoutExtension(passet).Equals(Path.GetFileNameWithoutExtension(nasset) + "_Portable"))
                {
                    host.AddResult(Severity.Info, true, $"Portable Asset assembly correctly named: '{Path.GetFileNameWithoutExtension(passet)}'.");
                }
                else
                {
                    host.AddResult(Severity.Warning, false, $"Portable Asset assembly misnamed, got '{Path.GetFileNameWithoutExtension(passet)}' expected '{Path.GetFileNameWithoutExtension(nasset) + "_Portable"}'.");

                    //misnamed = true;
                }
            }

            //! Core asset naming.
            // 
            if (String.IsNullOrEmpty(casset))
            {
                host.AddResult(Severity.Warning, false, $".Net Core Asset assembly not detected.");
            }
            else
            {
                //! Core asset naming convention, '_Core' suffix.
                // 
                if (Path.GetFileNameWithoutExtension(passet).Equals(Path.GetFileNameWithoutExtension(casset) + "_Core"))
                {
                    host.AddResult(Severity.Info, true, $".Net Core Asset assembly correctly named: '{Path.GetFileNameWithoutExtension(casset)}'.");
                }
                else
                {
                    host.AddResult(Severity.Warning, false, $".Net Core Asset assembly misnamed, got '{Path.GetFileNameWithoutExtension(passet)}' expected '{Path.GetFileNameWithoutExtension(nasset) + "_Portable"}'.");

                    //misnamed = true;
                }
            }

            return true;
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
            return (Path.GetExtension(parm).Equals(".sln", StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Methods
    }
}