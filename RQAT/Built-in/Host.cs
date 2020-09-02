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
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Primitives;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.Win32;

    using OfficeOpenXml;
    using OfficeOpenXml.Style;

    [Export]
    [Export(typeof(IHost))]
    public class Host : IHost
    {
#if MEF

        /// <summary>
        /// The container.
        /// </summary>
        /// <summary>
        /// The container.
        /// </summary>
        public CompositionContainer container = null;

#else

        /// <summary>
        /// The container.
        /// </summary>
        public IoCContainer container = new IoCContainer();

#endif

        #region Fields

        /// <summary>
        /// The col.
        /// </summary>
        public static Int32 col = 1;

        /// <summary>
        /// The finished jobs.
        /// </summary>
        public static Jobs FinishedJobs = new Jobs();

        /// <summary>
        /// Queue of jobs.
        /// </summary>
        public static Jobs JobQueue = new Jobs();

        /// <summary>
        /// Full pathname of the plugin file.
        /// </summary>
        public static string PluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins");

        /// <summary>
        /// The row.
        /// </summary>
        public static Int32 row = 1;

        /// <summary>
        /// The group.
        /// </summary>
        public static Int32 group = 1;

        /// <summary>
        /// The sheet.
        /// </summary>
        public static Int32 Sheet = 0;

        /// <summary>
        /// The XLSX.
        /// </summary>
        public static ExcelPackage xlsx;

        /// <summary>
        /// The enabled plugins.
        /// </summary>
        public Dictionary<String, Boolean> EnabledPlugins = new Dictionary<String, Boolean>();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Host()
        {
            GitPaths = Utils.FindExePath("git.exe");

            NuGetPaths = Utils.FindExePath("nuget.exe");

            Results = new List<LogEntry>();

            Directory.CreateDirectory(PluginPath);

#if MEF
            AggregateCatalog catalog = new AggregateCatalog();
            catalog.Changed += Catalog_Changed;

            catalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog(@".\Plugins"));

            container = new CompositionContainer(catalog);

            try
            {
                container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }
#else
            container.InjectPluginsFromFolder(PluginPath);

            container.InjectAssembly(typeof(Log).Assembly);

            container.CompleteConfiguration();
#endif

            //! Detemine path of MsBuild.exe
            // See https://developercommunity.visualstudio.com/content/problem/2813/cant-find-registry-entries-for-visual-studio-2017.html
            //
            RegistryKey hklm = Registry.LocalMachine;
            RegistryKey vs7 = hklm.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7", false);

            if (vs7 == null)
            {
                // ERROR
            }

            this.VS2017Path = vs7.GetValue("15.0").ToString();

            if (!Directory.Exists(MSBuildPath))
            {
                // ERROR
            }

            this.MSBuildPath = Path.Combine(VS2017Path, @"MSBuild\15.0\Bin\MSBuild.exe");

            if (!File.Exists(MSBuildPath))
            {
                // ERROR
            }
        }

        #endregion Constructors

        #region Delegates

        /// <summary>
        /// Handler, called when the queues changed.
        /// </summary>
        ///
        /// <param name="queued">   The queued. </param>
        /// <param name="finished"> The finished. </param>
        public delegate void QueuesChangedHandler(Int32 queued, Int32 finished);

        #endregion Delegates

        #region Events

        /// <summary>
        /// Event queue for all listeners interested in OnQueuesChanged events.
        /// </summary>
        public static event QueuesChangedHandler OnQueuesChanged;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets the full pathname of the milliseconds build file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the milliseconds build file.
        /// </value>
        public String MSBuildPath { get; private set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        ///
        /// <value>
        /// The results.
        /// </value>
        public List<LogEntry> Results { get; set; }

        /// <summary>
        /// Gets the full pathname of the vs 2017 file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the vs 2017 file.
        /// </value>
        public String VS2017Path { get; private set; }

        /// <summary>
        /// Gets or sets the full pathname of the git file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the git file.
        /// </value>
        public String GitPath => GitPaths.Count == 0 ? "git.exe" : GitPaths.First();

        /// <summary>
        /// The git.exe Paths.
        /// </summary>
        ///
        /// <value>
        /// The git paths.
        /// </value>
        public List<String> GitPaths
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets the nuget Path.
        /// </summary>
        ///
        /// <value>
        /// The nu get.
        /// </value>
        public String NuGetPath => NuGetPaths.Count == 0 ? "nuget.exe" : NuGetPaths.First();

        /// <summary>
        /// Gets the nu get paths.
        /// </summary>
        ///
        /// <value>
        /// The nu get paths.
        /// </value>
        public List<String> NuGetPaths
        {
            get; private set;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Returns true if the file exists and is locked for R/W access.
        /// </summary>
        ///
        /// <param name="filename"> The File to be checked. </param>
        ///
        /// <returns>
        /// Returns true if the file exists and is locked for R/W acces.
        /// </returns>
        public static Boolean IsFileLocked(String filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);

                return true;
            }
        }

        /// <summary>
        /// Queues changed.
        /// </summary>
        public static void QueuesChanged()
        {
            Debug.WriteLine($"[Queue]");
            foreach (Job job in JobQueue)
            {
                Debug.WriteLine($"{job}");
            }
            Debug.WriteLine(String.Empty);

            DoQueuesChanged(JobQueue.Count, FinishedJobs.Count);
        }

        /// <summary>
        /// Adds a result.
        /// </summary>
        ///
        /// <param name="severity"> The severity. </param>
        /// <param name="result">   True to result. </param>
        /// <param name="message">  The message. </param>
        /// <param name="level">    (Optional) The level. </param>
        public void AddResult(Severity severity, Boolean result, String message, Int32 level = 0)
        {
            //LogEntry le = new LogEntry()
            //{
            //    Severity = severity,
            //    Result = result,
            //    Message = message,
            //    TimeStamp = DateTime.Now,
            //};

            //! DEBUG OUTPUT SUPRESSED !!
            //
            if (severity == Severity.Debug)
            {
                return;
            }

            //Results.Add();

            //! Make sure all messages end with a colon.
            // 
            if (message.StartsWith("[") && message.EndsWith("]"))
            {
                //Host.row++;
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = message;
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Style.Font.Bold = true;
            }
            else
            {
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Value = severity;
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.PatternType = ExcelFillStyle.Solid;

                switch (severity)
                {
                    case Severity.Debug:
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.BackgroundColor.SetColor(SystemColors.Window);
                        break;
                    case Severity.Info:
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.BackgroundColor.SetColor(result
                        ? Color.PaleGreen
                        : Color.LemonChiffon);
                        break;
                    case Severity.Warning:
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.BackgroundColor.SetColor(Color.LightSalmon);
                        break;
                    case Severity.Error:
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        break;
                    case Severity.Fatal:
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Style.Fill.BackgroundColor.SetColor(Color.Red);
                        break;
                }

                Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 1].Value = result ? "Success" : "Failure";
                if (!result)
                {
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 1].Style.Font.Color.SetColor(Color.Red);
                }

                if (message.EndsWith(":"))
                {
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = message;
                }
                else
                {
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = String.Concat(message.TrimEnd('.'), ".");
                }
            }

            Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Style.Indent = 2 * level;

            if (String.IsNullOrEmpty(Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, 1].Value?.ToString()))
            {
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Row(Host.row).OutlineLevel = Host.group;
                Host.xlsx.Workbook.Worksheets[Host.Sheet].Row(Host.row).Collapsed = true;
            }

            //! Not that useful and rendered as a floating-point number.
            //
            //Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 3].Value = DateTime.Now.TimeOfDay;

            Host.row++;

            //Debug.WriteLine($"AddResult({severity}, {result}, {job.Level}, '{message}')");
        }

        /// <summary>
        /// Determines if we can next job.
        /// </summary>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public Boolean ExecuteNextJob()
        {
            //! Get the next job to be performed.
            //
            Job job = JobQueue.First();

            //! Note this in the spreadsheet output.
            //
            Host.col = 1;
            //Host.group++;

            if (Host.row > 2)
            {
                Host.row++;
            }

            // Start Group.
            Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col].Value = job.Plugin.Name;
            Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col++].Style.Indent = job.Level * 2;
            Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col++].Value = Path.GetFileName(job.Parm);

            Int32 jobs = JobQueue.Count;

            try
            {
                //! Initialize Plugin
                //
                job.Plugin.Initialize(host: this);

                //! Execute Plugin
                //
                if (!job.Plugin.Execute(job))
                {
                    AddResult(Severity.Error, false, $"Failed to execute {job.Plugin.GetType().Name}");
                }
                else
                {
                    if (JobQueue.Count != jobs)
                    {
                        AddResult(Severity.Info, true, $"Scheduled {(JobQueue.Count - jobs).ToString()} new analysis jobs");
                    }
                }
            }
            catch (Exception e)
            {
                AddResult(Severity.Fatal, false, $"{e.GetType().Name} - {e.Message}");
            }
            finally
            {
                //! Always remove so an Exception won't result in an endless retry.
                JobQueue.RemoveAt(0);
                FinishedJobs.Add(job);
            }

            QueuesChanged();

            //! Return true if there is more jobs to be performed.
            //
            return !JobQueue.IsEmpty;
        }

        /// <summary>
        /// Query if this object has next job.
        /// </summary>
        ///
        /// <returns>
        /// True if next job, false if not.
        /// </returns>
        public Boolean HasNextJob()
        {
            return JobQueue.Count != 0;
        }

        /// <summary>
        /// Peek at next job.
        /// </summary>
        ///
        /// <returns>
        /// A Job.
        /// </returns>
        public Job PeekAtNextJob()
        {
            return JobQueue[0];
        }

        /// <summary>
        /// Plugins for file type.
        /// </summary>
        ///
        /// <param name="type"> The type. </param>
        ///
        /// <returns>
        /// A List&lt;IPlugin&gt;
        /// </returns>
        public List<IPlugin> PluginsForFileType(FileType type)
        {
            List<IPlugin> result = new List<IPlugin>();

            if (type.IsFlagSet(FileType.Solution))
            {
#if MEF
                result.AddRange(container.GetExportedValues<ISolutionPlugin>());
#else
                result.AddRange(container.MultiResolve<IPlugin, ISolutionPlugin>());
#endif
            }
            if (type.IsFlagSet(FileType.Project))
            {
#if MEF
                result.AddRange(container.GetExportedValues<IProjectPlugin>());
#else
                result.AddRange(container.MultiResolve<IPlugin, IProjectPlugin>());
#endif
            }
            if (type.IsFlagSet(FileType.Assembly))
            {
#if MEF
                result.AddRange(container.GetExportedValues<IAssemblyPlugin>());
#else
                result.AddRange(container.MultiResolve<IPlugin, IAssemblyPlugin>());
#endif
            }
            if (type.IsFlagSet(FileType.UnitTest))
            {
#if MEF
                result.AddRange(container.GetExportedValues<ITestPlugin>());
#else
                result.AddRange(container.MultiResolve<IPlugin, ITestPlugin>());
#endif
            }
            if (type.IsFlagSet(FileType.Executable))
            {
#if MEF
                result.AddRange(container.GetExportedValues<IExecutablePlugin>());
#else
                result.AddRange(container.MultiResolve<IPlugin, ITestPlugin>());
#endif
            }

            return result;
        }

        /// <summary>
        /// Recurse for type.
        /// </summary>
        ///
        /// <param name="job">   The calling plugin. </param>
        /// <param name="types"> The type. </param>
        /// <param name="parm">  Filename of the file. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;String,Boolean&gt;
        /// </returns>
        public Boolean RecurseForTypes(IJob job, FileType types, String parm)
        {
            AddResult(Severity.Debug, true, $"RecurseForTypes({job.Plugin.GetType().Name},{types},'{parm}')");

            if (File.Exists(parm))
            {
                AddResult(Severity.Debug, true, $"File: '{parm}' found");

                Int32 cnt = JobQueue.Count;
                Int32 parentIndex = JobQueue.IndexOf((Job)job);
                Int32 childs = 0;

                foreach (FileType type in types.ToValues())
                {
                    foreach (IPlugin plugin in PluginsForFileType(type)
                        .OrderBy(p => p.IsLeaf))
                    {
                        if (EnabledPlugins[plugin.GetType().Name])
                        {
                            if (!JobQueue.Contains(plugin, parm)
                                && !FinishedJobs.Contains(plugin, parm))
                            {
                                if (plugin.Supports(parm))
                                {
                                    JobQueue.Insert(parentIndex + childs + 1, new Job(plugin, job.Level + 1, parm));

                                    Debug.Print($"**** {plugin.GetType().Name} queued for '{parm}'.");

                                    childs++;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                AddResult(Severity.Error, false, $"{job.Plugin.GetType().Name} -> {types} File '{parm}' missing");
            }

            return true;
        }

        /// <summary>
        /// Executes the queues changed operation.
        /// </summary>
        ///
        /// <param name="queued">   The queued. </param>
        /// <param name="finished"> The finished. </param>
        private static void DoQueuesChanged(Int32 queued, Int32 finished)
        {
            OnQueuesChanged?.Invoke(queued, finished);
        }

        /// <summary>
        /// Event handler. Called by Catalog for changed events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Composable part catalog change event information. </param>
        private void Catalog_Changed(object sender, ComposablePartCatalogChangeEventArgs e)
        {
            foreach (ComposablePartDefinition a in e.AddedDefinitions)
            {
                Debug.Print($"ADDED {a.ToString()}");
            }
            foreach (ComposablePartDefinition r in e.RemovedDefinitions)
            {
                Debug.Print($"REMOVED {r.ToString()}");
            }
        }

        #endregion Methods
    }
}