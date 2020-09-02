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

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IVersionControlPlugin))]
    [Export(typeof(IPlugin))]
    public class GitPlugin : IVersionControlPlugin
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
        public GitPlugin()
        {
            // Nothing
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the description.
        /// </summary>
        ///
        /// <value>
        /// The description.
        /// </value>
        public string Description => "This plugin clones local or remote reprositories into a temporary directory.";

        /// <summary>
        /// Gets a value indicating whether this object is leaf.
        /// </summary>
        ///
        /// <value>
        /// True if this object is leaf, false if not.
        /// </value>
        public bool IsLeaf => false;

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
        public string Name => "*.git repository clone";

        /// <summary>
        /// Gets the version.
        /// </summary>
        ///
        /// <value>
        /// The version.
        /// </value>
        public Version Version => new Version(1, 0, 0, 0);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Executes.
        /// </summary>
        ///
        /// <param name="job"> The level. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;String,Boolean&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            if (!File.Exists(host.GitPath))
            {
                return false;
            }

            //! Fix for temporarily supporting problematic repositories.
            // 
            String repo = job.Parm.Replace(".GIT", ".git");

            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{repo}')");

            host.AddResult(Severity.Info, true, $"[Cloning '{Path.GetFileNameWithoutExtension(job.Parm)}' Repository]");

            host.AddResult(Severity.Info, true, $"Using: '{host.GitPath}'.");
            host.AddResult(Severity.Info, true, $"Git version is: '{DoGitVersion(Utils.workdir)}'.");

            if (DoGitClone(job, new Uri(repo), Utils.workdir))
            {
                host.AddResult(Severity.Info, true, $"[Cloning Submodules]");

                String topofrepo = Path.Combine(Utils.workdir, Path.GetFileNameWithoutExtension(new Uri(repo).Segments.Last()));

                //! git submodule update --init --recursive
                //
                Utils.ExecutePsi(new ProcessStartInfo
                {
                    WorkingDirectory = topofrepo,
                    Arguments = "submodule update --init --recursive",
                    FileName = host.GitPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }, host, out StringBuilder sb1, true, true, Severity.Info);

                //! Cache Solutions as NuGet Restore may add new ones;
                List<String> solutions = Directory.EnumerateFiles(Utils.workdir, "*.sln", SearchOption.AllDirectories).ToList();

                host.AddResult(Severity.Info, true, $"Detected {solutions.Count} Solutions.");

                host.AddResult(Severity.Info, true, $"[Restoring NuGet Packages]");

                foreach (String solution in solutions)
                {
                    //! nuget restore OpenConsole.sln
                    //
                    Utils.ExecutePsi(new ProcessStartInfo
                    {
                        WorkingDirectory = Path.GetDirectoryName(solution),//topofrepo,
                        Arguments = "restore", // \"" + file + "\"",
                        FileName = host.NuGetPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    }, host, out StringBuilder sb2, true);
                }

                //! Search for Solutions files and add them to the queue.
                //
                foreach (String solution in solutions)
                {
                    if (solution.Contains(@"\packages\"))
                    {
                        continue;
                    }

#warning Performing a Test build after checkout fails on references to AssetManager.

                    //host.AddResult(Severity.Info, true, $"[Performing a Test build of '{Path.GetFileNameWithoutExtension(solution)}' Solution]");

                    //!  msbuild OpenConsole.sln
                    // 
                    //!  NOTE: We do not log the output as it will be recompiled later again.
                    //Utils.ExecutePsi(new ProcessStartInfo
                    //{
                    //    WorkingDirectory = Utils.workdir,
                    //    Arguments = $"\"{solution}\"",
                    //    FileName = host.MSBuildPath,
                    //    RedirectStandardOutput = true,
                    //    RedirectStandardError = true,
                    //    CreateNoWindow = true,
                    //    UseShellExecute = false,
                    //}, host, out StringBuilder sb3, false);

                    host.RecurseForTypes(job, FileType.Solution, solution);
                }

                return true;
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

            switch (host.GitPaths.Count)
            {
                case 0:
                    host.AddResult(Severity.Warning, false, $"Git.exe not found on PATH.");
                    return;
                case 1:
                    break;
                default:
                    foreach (String fn in host.GitPaths)
                    {
                        host.AddResult(Severity.Warning, true, $"Git.exe found at: '{fn}'.");
                    }
                    break;
            }

            if (!Directory.Exists(Utils.workdir))
            {
                Directory.CreateDirectory(Utils.workdir);
            }
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
            if (Uri.TryCreate(parm, UriKind.Absolute, out Uri uri))
            {
                if (uri.IsFile && Directory.Exists(uri.AbsolutePath))
                {
                    //! Check for a .git folder.
                    //
                    foreach (String dir in Directory.EnumerateDirectories(uri.AbsolutePath, ".git"))
                    {
                        return true;
                    }
                    if (uri.AbsolutePath.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return false;
                }
                else if (!String.IsNullOrEmpty(uri.Host) && uri.AbsolutePath.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Executes the git clone operation.
        /// </summary>
        ///
        /// <param name="job">     The job. </param>
        /// <param name="uri">     URI of the resource. </param>
        /// <param name="workdir"> The workdir. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        private bool DoGitClone(IJob job, Uri uri, String workdir)
        {
            return Utils.ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = workdir,
                Arguments = $"clone {uri}",
                FileName = host.GitPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false) == 0;
        }

        /// <summary>
        /// Executes the git version operation.
        /// </summary>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        private String DoGitVersion(String workdir)
        {
            return Utils.ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = workdir,
                Arguments = $"--version",
                FileName = host.GitPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false) == 0
                ? sb.ToString().Trim()
                : "Error";
        }

        #endregion Methods
    }
}