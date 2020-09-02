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
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
    public class ProjectAnalysisPlugin : IProjectPlugin
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
        public ProjectAnalysisPlugin()
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
        public string Name => "Project Analysis";

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
        public string Description => "This plugin compiles a project with Detailed Summary Output option (but without Xml Documentation Issues).";

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
        /// <param name="job"> The parameter. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;String,Boolean&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Building '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

            // /p:NoWarn="CS1570;CS1572;CS1587;CS1591"
            String NoXmlWarns = String.Join(";", Utils.XMLComments.Select(q => $"{q.Key}"));

            Directory.SetCurrentDirectory(Path.GetDirectoryName(job.Parm));

            // See https://stackoverflow.com/questions/26034558/how-to-force-msbuild-to-run-code-analysis-without-recompiling
            // See https://social.msdn.microsoft.com/forums/en-US/9e77b76c-3ca5-42b4-b946-5c9d71f27ab3/invoke-code-metrics-calculation-programmatically
            // 
            Int32 ExitCode = Utils.ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(job.Parm),
                Arguments = $"{Path.GetFileName(job.Parm)} /target:Clean;Build /detailedsummary /p:NoWarn=\"{NoXmlWarns}\" /p:RunCodeAnalysis=true",
                FileName = host.MSBuildPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false, true);

            //C:\Program Files(x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Microsoft.Common.CurrentVersion.targets(4365, 5): error MSB3027: Could not copy "C:\Users\Wim van der Vegt\AppData\Local\Temp\RQAT_h2ac2d32.w0e\ClientSideGameStorageAsset\GameStorageClientAsset\bin\Debug\GameStorageClientAsset.dll" to "bin\Debug\GameStorageClientAsset.dll".Exceeded retry count of 10.Failed.The file is locked by: "RQAT (19764)"[C: \Users\Wim van der Vegt\AppData\Local\Temp\RQAT_h2ac2d32.w0e\ClientSideGameStorageAsset\GameStorageUnitTests\GameStorageUnitTests.csproj]

            List<String> errors = new List<String>();

            //! Select All MSBuild Errors (MSB).
            // 
            foreach (String line in sb.ToString()
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(q => q.Contains("error MSB"))
                .ToList())
            {
                String msg = line.Substring(line.IndexOf($"error MSB") + "error ".Length);
                msg = msg.Substring(0, msg.IndexOf("["));

                if (!errors.Contains(msg))
                {
                    errors.Add(msg);
                }
            }

            foreach (String msg in errors)
            {
                host.AddResult(Severity.Error, true, $"{msg}", 1);
            }

            //! CS codes to exclude.
            // 
            List<String> csCodes = Utils.XMLComments.Keys.ToList();

            //! Select All Warnings (CAnnnn and CSnnnn).
            // 
            List<String> lines = sb.ToString()
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(q => q.Contains("warning C"))
                .ToList();

            //! This seems to work.
            //del / s *.lastcodeanalysissucceeded
            //msbuild DesktopBuild.proj /p:RunCodeAnalysis=true

            //! Code Analysis Output.
            List<String> cas = lines
                .Where(q => q.Contains("warning CA"))
                .Select(q => q.Trim())
                .Distinct()
                .ToList();

            //! Build Output.
            lines = lines
                .Where(q => !q.Contains("warning CA"))
                .Select(q => q.Trim())
                .Distinct()
                .ToList();

            //! Remove project name.
            //! Change full filename to filename only.
            // 
            for (Int32 i = 0; i < lines.Count; i++)
            {
                Match m1 = Utils.rg_project.Match(lines[i]);
                Match m2 = Utils.rg_file.Match(lines[i]);

                if (m1.Success && m2.Success)
                {
                    lines[i] = lines[i].Replace(m1.Value, String.Empty).Trim();
                    lines[i] = lines[i].Replace(m2.Groups[1].Value, $"{Path.GetFileName(m2.Groups[1].Value)}");
                }
            }

            //! Compress data by taking distinct items and group on the filename.
            // 
            IEnumerable<IGrouping<String, String>> warnings = lines
                .Distinct()
                .GroupBy(q => Path.GetFileName(Utils.rg_file.Match(q).Value.Trim('(')));

            Boolean issues = false;

            foreach (IGrouping<String, String> item in warnings)
            {
                host.AddResult(Severity.Info, true, $"{item.Key}");

                foreach (String line in item)
                {
                    if (Utils.rg_csa.IsMatch(line))
                    {
                        String cs = Utils.rg_csa.Match(line).Value.Trim(':');

                        // Skip Xml Comment Issues.
                        // 
                        if (csCodes.Contains(cs))
                        {
                            continue;
                        }

                        issues = true;

                        String msg = line.Substring(line.IndexOf($"{cs}: ") + $"{cs}: ".Length);

                        if (msg.Contains(" -- "))
                        {
                            msg = msg.Substring(msg.IndexOf(" -- ") + " -- ".Length);
                            msg = msg.Trim(new Char[] { '\'' });
                        }

                        String row = $"0000";

                        if (line.IndexOf("(") != -1 && line.IndexOf(")") != -1)
                        {
                            row = line.Substring(line.IndexOf("(") + 1);
                            row = row.Substring(0, row.IndexOf(")"));

                            if (row.IndexOf(",") != -1)
                            {
                                row = row.Substring(0, row.IndexOf(","));
                            }

                            if (Int32.TryParse(row, out Int32 r))
                            {
                                row = $"{r:0000}";
                            }
                        }

                        host.AddResult(Severity.Warning, true, $"line: {row} - {msg}", 1);
                    }
                }
            }

            if (cas.Count != 0)
            {
                host.AddResult(Severity.Info, true, $"Code Analysis");

                foreach (String line in cas)
                {
                    String cs = Utils.rg_csa.Match(line).Value.Trim(':');

                    issues = true;

                    String msg = line.Substring(line.IndexOf($"{cs}: ") + $"{cs}: ".Length);

                    String row = $"0000";

                    if (line.IndexOf("(") != -1 && line.IndexOf(")") != -1)
                    {
                        row = line.Substring(line.IndexOf("(") + 1);
                        row = row.Substring(0, row.IndexOf(")"));

                        if (row.IndexOf(",") != -1)
                        {
                            row = row.Substring(0, row.IndexOf(","));
                        }

                        if (Int32.TryParse(row, out Int32 r))
                        {
                            row = $"{r:0000}";
                        }
                    }

                    host.AddResult(Severity.Warning, true, $"line: {row} - {msg}", 1);
                }
            }

            if (!issues)
            {
                host.AddResult(Severity.Info, true, $"No warnings found."); // - {Utils.XMLComments[cs]}
            }

#warning warning MSB3061: Unable to delete file "C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\bin\Debug\RageAssetManager.dll". The process cannot access the file '<path>RageAssetManager.dll'

            return ExitCode == 0;
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
            return Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion Methods
    }
}