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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    using OfficeOpenXml;
    using OfficeOpenXml.Style;

    using SimpleJSON;

    public partial class Form1 : Form
    {
        #region Fields


        /// <summary>
        /// Full pathname of the assessment file.
        /// </summary>
        private const string AssessmentPath = "assessment.xlsx";

        /// <summary>
        /// State of the plugin enabled.
        /// </summary>
        private readonly string EnabledStateFile = Environment.ExpandEnvironmentVariables(@"%AppData%\RQAT\EnabledState.json");

        /// <summary>
        /// Pathname of the output folder.
        /// </summary>
        private readonly string OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);

        /// <summary>
        /// The directory wildcards.
        /// </summary>
        String[] directoryWildcards = { ".git" };

        /// <summary>
        /// The file wildcards.
        /// </summary>
        String[] fileWildcards = { ".sln", ".csproj", ".dll" };

        //! Red
        /// <summary>
        /// The host.
        /// </summary>
        Host host = new Host();

        /// <summary>
        /// The repositories, solutions, projects and assemblies dropped or pasted.
        /// </summary>
        private List<String> repos = new List<String>()
        {
        };

        /// <summary>
        /// The roots of the Plugin TreeView.
        /// </summary>
        Dictionary<String, TreeNode> roots = new Dictionary<String, TreeNode>();

        /// <summary>
        /// The supported formats.
        /// </summary>
        private List<String> supportedFormats = new List<String>
        {
            "Text",
            "msSourceUrl",
            "UniformResourceLocator",
            "FileName",
            "FileDrop",
        };

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Analyze files.
        /// </summary>
        ///
        /// <param name="targets"> The targets. </param>
        private void AnalyzeFiles(List<String> targets)
        {
            String xlsx = Path.Combine(OutputFolder, AssessmentPath);

            if (File.Exists(xlsx))
            {
                if (Host.IsFileLocked(xlsx))
                {
                    MessageBox.Show($"Output file '{xlsx}' is in use/locked.", "RQAT", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    return;
                }
                else
                {
                    File.Delete(xlsx);
                }
            }

            using (Host.xlsx = new ExcelPackage(new FileInfo(xlsx)))
            {
                Host.JobQueue.Clear();
                Host.FinishedJobs.Clear();

                chart1.Series[0].Points.Clear();
                chart1.Series[0].BorderWidth = 2;

                chart1.Series[1].Points.Clear();
                chart1.Series[1].BorderWidth = 2;

                Host.OnQueuesChanged += Host_OnQueuesChanged;

                {
                    //! List plugin detected and loaded info.
                    //
                    ExcelWorksheet worksheet = Host.xlsx.Workbook.Worksheets.Add("Loaded Plugins");

                    Host.Sheet = worksheet.Index;
                    Host.row = 1;
                    Host.col = 1;
                    Host.group = 1;

                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Value = "Plugin";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 1].Value = "Type";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = "Interface";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 3].Value = "Version";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 4].Value = "Maturity";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 5].Value = "Description";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 6].Value = "Leaf";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 7].Value = "Enabled";

                    for (Int32 i = 0; i < 8; i++)
                    {
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + i].Style.Font.Bold = true;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    Host.row += 1;

                    foreach (IPlugin plugin in host.container.GetExportedValues<IPlugin>()
                        .OrderBy(p => p.GetType().GetInterfaces().First().Name)
                        .ThenBy(p => p.GetType().Name))
                    {
                        Type[] ifs = plugin.GetType().GetInterfaces();
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Value = plugin.Name;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 1].Value = plugin.GetType().Name;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = ifs.First().Name;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 3].Value = plugin.Version;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 4].Value = plugin.Maturity;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 5].Value = plugin.Description;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 6].Value = plugin.IsLeaf;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 7].Value = host.EnabledPlugins[plugin.GetType().Name] ? "Enabled" : "Disabled";

                        Host.row += 1;
                    }

                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells.AutoFitColumns(8, 50);
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Column(Host.col + 5).Width = 50;
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Column(Host.col + 5).Style.WrapText = true;
                }

                //! Run tests for each target file.
                //
                foreach (String target in targets)
                {
                    //! Initialize a worksheet for each target.
                    ExcelWorksheet worksheet = String.IsNullOrEmpty(Path.GetFileNameWithoutExtension(target))
                        ? Host.xlsx.Workbook.Worksheets.Add($"Sheet{Host.xlsx.Workbook.Worksheets.Count + 1}")
                        : Host.xlsx.Workbook.Worksheets.Add(Path.GetFileNameWithoutExtension(target));

                    Host.Sheet = worksheet.Index;
                    Host.row = 1;
                    Host.col = 1;

                    //*.Sln NuGet Package Generation Check    GameStorageClientAsset.sln Debug   Success SolutionNuGetPlugin.Execute('C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset.sln')  0,674522141
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 0].Value = "Plugin";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 1].Value = "Parameter";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 2].Value = "Severity";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 3].Value = "Result";
                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + 4].Value = "Message";

                    for (Int32 i = 0; i < 4; i++)
                    {
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + i].Style.Font.Bold = true;
                        Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells[Host.row, Host.col + i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                    Host.row++;

                    //! Initial Queue Counts.
                    //
                    Host.QueuesChanged();

                    ILog log = host.container.GetExportedValue<ILog>();

                    Utils.workdir = Path.Combine(Path.GetTempPath(), $"RQAT_{Path.GetRandomFileName()}");

                    //! Perform Initial population of the Queue.
                    //! Try all Non-Leaf Plugins (so those that enumerate).
                    //! This because git can be both a url ending in .git or a directory.
                    //
                    foreach (IPlugin plugin in host.container.GetExportedValues<IPlugin>()
                        .Where(p => !p.IsLeaf))
                    {
                        if (host.EnabledPlugins[plugin.GetType().Name] && plugin.Supports(target))
                        {
                            Host.JobQueue.Add(plugin, 0, target);

                            Host.QueuesChanged();

                            Application.DoEvents();

                            Debug.Print($"**** {plugin.GetType().Name} queued for '{target}'.");
                        }
                    }

                    Debug.Print($"**** {Host.JobQueue.Count} job(s) queued.");
                    Debug.Print($"**** {Host.FinishedJobs.Count} job(s) finished.");

                    if (host.HasNextJob())
                    {
                        toolStripStatusLabel1.Text = $"{Path.GetFileName(host.PeekAtNextJob().Parm)}"; statusStrip1.Refresh();
                        toolStripStatusLabel2.Text = $"{host.PeekAtNextJob()?.Plugin.Name}";
                        statusStrip1.Refresh();
                    }

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    //! Execute jobs until done.
                    //
                    while (host.HasNextJob() && host.ExecuteNextJob())
                    {
                        sw.Stop();
                        toolStripStatusLabel3.Text = $"{(sw.ElapsedMilliseconds / 1000.0).ToString("0.000")} sec";

                        Debug.Print($"**** {Host.JobQueue.Count} job(s) queued.");
                        Debug.Print($"**** {Host.FinishedJobs.Count} job(s) finished.");

                        toolStripStatusLabel1.Text = $"{Path.GetFileName(host.PeekAtNextJob().Parm)}";
                        toolStripStatusLabel2.Text = $"{host.PeekAtNextJob()?.Plugin.Name}";
                        statusStrip1.Refresh();

                        Application.DoEvents();

                        sw.Reset();
                        sw.Start();
                    }
                    sw.Stop();

                    Debug.Print($"**** {Host.JobQueue.Count} job(s) queued.");
                    Debug.Print($"**** {Host.FinishedJobs.Count} job(s) finished.");

                    //! Final Queue Counts.
                    //
                    Host.QueuesChanged();

                    Host.xlsx.Workbook.Worksheets[Host.Sheet].Cells.AutoFitColumns();
                }

                //host.container.ReleaseExport<ILog>(log);
                Host.OnQueuesChanged -= Host_OnQueuesChanged;

                Host.xlsx.Save();

                Process.Start(Host.xlsx.File.FullName);
            }
        }

        /// <summary>
        /// Event handler. Called by CurrentDomain for assembly load events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Assembly load event information. </param>
        private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.WriteLine(args.LoadedAssembly.CodeBase);
        }

        /// <summary>
        /// Event handler. Called by Form1 for drag drop events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Drag event information. </param>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessData(e.Data);
        }

        /// <summary>
        /// Event handler. Called by Form1 for drag enter events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Drag event information. </param>
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            //foreach (String format in e.Data.GetFormats())
            //{
            //    Debug.WriteLine(format);
            //}

            foreach (String format in supportedFormats)
            {
                if (e.Data.GetDataPresent(format))
                {
                    e.Effect = DragDropEffects.All;

                    return;
                }
            }

            e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// Event handler. Called by Form1 for load events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(repos.ToArray());
            comboBox1.SelectedIndex = repos.Count == 0 ? -1 : 0;
            SelectedBtn.Enabled = comboBox1.SelectedIndex != -1;

            toolStripStatusLabel1.Text = "Items available for Analysis:";
            toolStripStatusLabel2.Text = $"{comboBox1.Items.Count}";
            toolStripStatusLabel3.Text = String.Empty;

            // NOTE Log only used to get this Assembly. So it loads ILog as well as IHost.
            //
            ILog log = host.container.GetExportedValue<ILog>();

            JSONObject enabledState = new JSONObject();

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            if (!Directory.Exists(Path.GetDirectoryName(EnabledStateFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EnabledStateFile));
            }

            if (File.Exists(EnabledStateFile))
            {
                enabledState = JSONNode.Parse(File.ReadAllText(EnabledStateFile)).AsObject;
            }

            foreach (KeyValuePair<String, Boolean> kvp in host.EnabledPlugins)
            {
                JSONObject json = new JSONObject();
                json[kvp.Key] = kvp.Value;
                enabledState.Add(json);
            }

            File.WriteAllText(EnabledStateFile, enabledState.ToString(2));

            treeView1.Nodes.Clear();

            foreach (IPlugin plugin in host.container.GetExportedValues<IPlugin>())
            {
                String name = plugin.GetType().GetInterfaces().Where(p => !p.Name.Equals(typeof(IPlugin).Name)).First().Name;

                if (!roots.ContainsKey(name))
                {
                    String key = name;
                    String text = !name.Equals(typeof(IPlugin).Name)
                         ? name.Replace("Plugin", String.Empty).Substring(1)
                         : "<No type>";

                    TreeNode tmp = treeView1.Nodes.Add(name, text);

                    //! Default checked.
                    tmp.Checked = (enabledState[name] == null)
                        ? true
                        : enabledState[name].AsBool;

                    tmp.Expand();

                    roots.Add(name, tmp);
                }

                TreeNode node = roots[name].Nodes.Add(plugin.GetType().Name, plugin.Name);

                node.Checked = (enabledState[node.Name] == null)
                    ? true
                    : enabledState[node.Name].AsBool;

                node.ToolTipText = plugin.Description;
            }

            foreach (KeyValuePair<String, TreeNode> kvp in roots)
            {
                kvp.Value.Expand();
            }
        }

        /// <summary>
        /// Host on queues changed.
        /// </summary>
        ///
        /// <param name="queued">   The queued. </param>
        /// <param name="finished"> The finished. </param>
        private void Host_OnQueuesChanged(int queued, int finished)
        {
            if (chart1.InvokeRequired)
            {
                chart1.Invoke(new Host.QueuesChangedHandler(Host_OnQueuesChanged), queued, finished);
            }
            else
            {
                chart1.Series[0].Points.AddY(queued);
                chart1.Series[1].Points.AddY(finished);
            }
        }

        /// <summary>
        /// Event handler. Called by pasteMenuItem for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void pasteMenuItem_Click(object sender, EventArgs e)
        {
            ProcessData(Clipboard.GetDataObject());
        }

        /// <summary>
        /// Process the data described by data.
        /// </summary>
        ///
        /// <param name="data"> The data. </param>
        private void ProcessData(IDataObject data)
        {
            try
            {
                repos.Clear();

                //! System.String
                //! UnicodeText
                //! Text

                foreach (String format in data.GetFormats())
                {
                    Debug.Print(format);
                }

                if (data.GetDataPresent("Text"))
                {
                    Debug.Print("Handling Text");

                    String text = data.GetData("Text").ToString().TrimEnd('\0');

                    if (data.GetDataPresent("UniformResourceLocator"))
                    {
                        Object obj = data.GetData("UniformResourceLocator");

                        if (!text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            text = "http://" + text;
                        }
                    }
                    else if (data.GetDataPresent("Text"))
                    {
                        CheckFileOrFolder(text);
                    }

                    if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                    {
                        //! URL
                        //
                        if (text.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                        {
                            repos.Add(text);
                        }
                    }
                }
                else if (data.GetDataPresent("FileDrop"))
                {
                    String[] files = ((String[])data.GetData("FileDrop"));

                    foreach (String file in files)
                    {
                        CheckFileOrFolder(file);
                    }
                }

                comboBox1.SelectedIndex = -1;
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(repos.ToArray());
                comboBox1.SelectedIndex = repos.Count == 0 ? -1 : 0;

                SelectedBtn.Enabled = comboBox1.SelectedIndex != -1;

                toolStripStatusLabel1.Text = "Items available for Analysis:";
                toolStripStatusLabel2.Text = $"{comboBox1.Items.Count}";
                toolStripStatusLabel3.Text = String.Empty;
            }
            catch
            {
                //
            }

            foreach (String item in repos)
            {
                Debug.WriteLine(item);
            }
        }

        private void CheckFileOrFolder(string file)
        {
            String qualified = Path.GetFullPath(file);

            //! Filenames
            //
            if (File.Exists(qualified))
            {
                if (fileWildcards.ToList().Any(p => p.Equals(Path.GetExtension(qualified), StringComparison.OrdinalIgnoreCase)))
                {
                    repos.Add(qualified);
                }
            }
            //! Directories
            //
            else if (Directory.Exists(qualified))
            {
                foreach (String wildcard in directoryWildcards)
                {
                    foreach (String projectDirectory in Directory.EnumerateDirectories(qualified, $"*{wildcard}"))
                    {
                        repos.Add(projectDirectory);
                    }
                }

                foreach (String wildcard in fileWildcards)
                {
                    foreach (String projectFile in Directory.EnumerateFiles(qualified, $"*{wildcard}"))
                    {
                        repos.Add(projectFile);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler. Called by SelectedBtn for click events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Event information. </param>
        private void SelectedBtn_Click(object sender, EventArgs e)
        {
            SelectedBtn.Enabled = false;

            if (comboBox1.SelectedItem != null)
            {
                List<String> targets = new List<String>() {
                    comboBox1.SelectedItem.ToString()
                };

                AnalyzeFiles(targets);
            }

            SelectedBtn.Enabled = true;
        }

        /// <summary>
        /// Event handler. Called by treeView1 for after check events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Tree view event information. </param>
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            ILog log = host.container.GetExportedValue<ILog>();

            log.Print($"{e.Node.Name} - {e.Node.Text}");

            if (e.Node.Parent == null)
            {
                foreach (TreeNode n in e.Node.Nodes)
                {
                    n.Checked = e.Node.Checked;
                }
            }
            else
            {
                host.EnabledPlugins[e.Node.Name] = e.Node.Checked;
            }

            if (e.Action != TreeViewAction.Unknown)
            {
                JSONObject enabledState = new JSONObject();

                //! Save root nodes.
                //
                foreach (TreeNode tn in ((TreeView)sender).Nodes)
                {
                    enabledState.Add(tn.Name, tn.Checked);
                }

                //! Save non-root nodes by using host.EnabledPlugins
                //
                foreach (KeyValuePair<String, Boolean> kvp in host.EnabledPlugins)
                {
                    enabledState.Add(kvp.Key, kvp.Value);
                }

                File.WriteAllText(EnabledStateFile, enabledState.ToString(2));
            }
        }

        /// <summary>
        /// Event handler. Called by treeView1 for before check events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Tree view cancel event information. </param>
        private void treeView1_BeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            ILog log = host.container.GetExportedValue<ILog>();

            String key = e.Node.Name;
            foreach (Lazy<IPlugin> lp in host.container.GetExports<IPlugin>())
            {

#warning DoubleClick fails with this code (node stays checked but shows unchecked);

                log.Print(lp.Value.Name);
                if (key.Equals(lp.Value.GetType().Name))
                {
                    if (!lp.Value.IsLeaf)
                    {
                        //! If the node is checked we bailout for non-leaf nodes.
                        //
                        if (e.Node.Checked)
                        {
                            e.Cancel = !lp.Value.IsLeaf;
                        }
                    }
                }
            }
        }

        #endregion Methods
    }
}