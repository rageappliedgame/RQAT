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
    using System.Text;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
    public class ProjectCodeAnalysisPlugin : IProjectPlugin
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
        public ProjectCodeAnalysisPlugin()
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
                return "Code Analysis";
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
        public string Description => "This plugin compiles a project with Code Analysis option.";

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
        /// PORTABLE define is correctly set (for both portable and .net core projects).
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

            host.AddResult(Severity.Info, true, $"[Building '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

            // See https://stackoverflow.com/questions/26034558/how-to-force-msbuild-to-run-code-analysis-without-recompiling
            // See https://social.msdn.microsoft.com/forums/en-US/9e77b76c-3ca5-42b4-b946-5c9d71f27ab3/invoke-code-metrics-calculation-programmatically
            // 
            // Analyze.CalculateCodeMetricsforSolution
            // 
            Directory.SetCurrentDirectory(Path.GetDirectoryName(job.Parm));

            return Utils.ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(job.Parm),
                Arguments = $"{Path.GetFileName(job.Parm)} /target:Clean;Build /p:RunCodeAnalysis=false",
                FileName = host.MSBuildPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false, true) == 0;

#warning warning MSB3061: Unable to delete file "C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\bin\Debug\RageAssetManager.dll". The process cannot access the file '<path>RageAssetManager.dll'

            //return p.ExitCode == 0;
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
            return (Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Methods
    }
}