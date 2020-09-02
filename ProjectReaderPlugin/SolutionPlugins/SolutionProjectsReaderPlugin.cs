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
    public class SolutionProjectsReaderPlugin : ISolutionPlugin
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
        public SolutionProjectsReaderPlugin()
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
                return "*.Sln Project File Reader";
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
        public string Description => "This plugin schedules plugins for all projects detected in a solution.";

        /// <summary>
        /// Gets a value indicating whether this object is leaf (e.g. does not search for more jobs to run).
        /// </summary>
        ///
        /// <value>
        /// True if this object is leaf, false if not.
        /// </value>
        public bool IsLeaf => false;

        #endregion Properties

        #region Methods

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

            if (File.Exists(job.Parm))
            {
                host.AddResult(Severity.Info, true, $"[Examining Projects in '{Path.GetFileNameWithoutExtension(job.Parm)}' Solution]");

                Solution solution = new Solution();

                if (solution.Load(job.Parm))
                {
                    host.AddResult(Severity.Info, true, $"Loaded and Parsed Solution.");
                    host.AddResult(Severity.Info, true, $"Processing {solution.Projects.Count} Project Entries in Solution.", 1);

                    Int32 cnt = 0;
                    Int32 skipped = 0;

                    foreach (SolutionProject project in solution.Projects)
                    {
                        String csproj = Path.Combine(Path.GetDirectoryName(solution.SolutionPath), project.RelativePath);

                        if (project.ProjectTypeGuid.Equals(new Guid(Lookups.ProjectTypeGuids["C#"])))
                        {
                            if (File.Exists(csproj))
                            {
#warning RCSAA Specific Code (Skip AssetManager from processing if present).

                                if (Utils.IsAssetManager(csproj, host))
                                {
                                    host.AddResult(Severity.Warning, false, $"{Path.GetFileName(csproj)} Project skipped.", 1);
                                    skipped++;
                                }
                                else if (Utils.IsUnitTest(csproj))
                                {
                                    Utils.FixupAssemblyReferences(host, csproj);

                                    //! Quickly Build the output as this is an enumerating plugin. 
                                    //! We need assemblies for IsAsset() alike methods.
                                    // 
                                    if (Utils.Build(csproj, host))
                                    {
                                        String output = Utils.ProjectOutput(csproj);

                                        //! For UnitTests we skip other FileType.Project related checks as they 
                                        //! lead to failure to build due to locked RAGE assemblies.
                                        // 
                                        host.RecurseForTypes(job, FileType.UnitTest, output);
                                    }
                                    else
                                    {
                                        skipped++;
                                    }

                                    cnt++;
                                }
                                else
                                {
                                    Utils.FixupAssemblyReferences(host, csproj);

                                    //! Quickly Build the output as this is an enumerating plugin. 
                                    //! We need assemblies for IsAsset() alike methods.
                                    // 
                                    if (Utils.Build(csproj, host))
                                    {
                                        //! Recurse for FileType.Project.
                                        //
                                        host.RecurseForTypes(job, FileType.Project, csproj);
                                    }
                                    else
                                    {
                                        skipped++;
                                    }

                                    cnt++;

                                }
                            }
                            else
                            {
                                host.AddResult(Severity.Warning, false, $"{Path.GetFileName(csproj)} Project not found.", 1);
                            }
                        }
                        else
                        {
                            host.AddResult(Severity.Warning, false, $"{Path.GetFileName(csproj)} Non C# project skipped.", 1);
                            skipped++;
                        }
                    }

                    host.AddResult(Severity.Info, solution.Projects.Count != 0, $"{solution.Projects.Count} Projects Detected.", 1);
                    if (skipped != 0)
                    {
                        host.AddResult(Severity.Warning, solution.Projects.Count != 0, $"{skipped} Projects Skipped.", 1);
                    }

                    if (solution.Projects.Count == cnt + skipped)
                    {
                        host.AddResult(Severity.Info, solution.Projects.Count == cnt + skipped, $"All Projects Located.");
                    }
                    else
                    {
                        host.AddResult(Severity.Warning, solution.Projects.Count == cnt + skipped, $"Some Projects could not be Located.");
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