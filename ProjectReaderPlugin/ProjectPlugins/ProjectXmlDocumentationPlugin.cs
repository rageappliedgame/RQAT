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
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
    public class ProjectXmlDocumentationPlugin : IProjectPlugin
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
        public ProjectXmlDocumentationPlugin()
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
        public string Name => "Xml Documentation Checks";

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
        public string Description => "This plugin compiles a project and checks for Xml Documentation Issues.";

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

            host.AddResult(Severity.Info, true, $"[Examining '{Path.GetFileNameWithoutExtension(job.Parm)}' Project's Xml Documentation]");

            /// <remark>
            /// uses System.Diagnostics.CodeAnalysis;
            /// 
            /// and the attribute:
            /// 
            /// [SuppressMessage("Microsoft.Usage", "CS1591")]
            /// 
            /// works much like:
            /// 
            /// #pragma warning disable 1591
            /// #pragma warning restore 1591
            /// </remark>

            if (Utils.Load(job.Parm, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:PropertyGroup", namespaces))
                {
                    XAttribute condition = propertyGroup.Attribute("Condition");

                    if (condition != null)
                    {
                        String value = condition.Value;
                        if (value.Equals($" '$(Configuration)|$(Platform)' == {Utils.debug} "))
                        {
                            host.AddResult(Severity.Info, true, $"Found {Utils.debug} Condition.");

                            String xml = propertyGroup.XPathSelectElement("ns:DocumentationFile", namespaces)?.Value;

                            if (!String.IsNullOrEmpty(xml))
                            {
                                String documentationFile = Path.Combine(Path.GetDirectoryName(job.Parm), xml);

                                if (!String.IsNullOrEmpty(documentationFile) && File.Exists(documentationFile))
                                {
                                    host.AddResult(Severity.Info, true, $"Found '{Path.GetFileName(documentationFile)}' XML Documentation.");
                                }
                                else
                                {
                                    host.AddResult(Severity.Warning, true, $"'Missing XML Documentation.");
                                }
                            }

                            break;
                        }
                    }
                }
            }

            Directory.SetCurrentDirectory(Path.GetDirectoryName(job.Parm));

            Int32 ExitCode = Utils.ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(job.Parm),
                Arguments = $"{Path.GetFileName(job.Parm)} /target:Clean;Build",
                FileName = host.MSBuildPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false, true);

            //! Print only the lines with one of the Xml Documentation Code Style codes in it.
            // 
            //GameStorageClientAsset.cs(64,65): warning CS1570: XML comment has badly formed XML -- 'An identifier was expected.' [C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\GameStorageClientAsset.csproj]
            //ISerializer.cs(77,26): warning CS1572: XML comment has a param tag for 'type', but there is no parameter by that name [C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\GameStorageClientAsset.csproj]
            //Node.cs(178,58): warning CS1573: Parameter 'purpose' has no matching param tag in the XML comment for 'Node.Node(GameStorageClientAsset, string, StorageLocations)' (but other parameters do) [C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\GameStorageClientAsset.csproj]
            //NodeValue.cs(29,5): warning CS1587: XML comment is not placed on a valid language element [C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\GameStorageClientAsset.csproj]
            //NodeValue.cs(33,18): warning CS1591: Missing XML comment for publicly visible type or member 'NodePaths' [C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageClientAsset\GameStorageClientAsset.csproj]

            //! CS codes to include.
            // 
            List<String> csCodes = Utils.XMLComments.Keys.ToList();

            //! Select Warnings.
            // 
            List<String> lines = sb.ToString()
                .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(q => q.Contains("warning CS"))
                //.Where((s, b) => csCodes.IndexOf(s) != -1)
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

            //! Group output per file and per CS code.
            // 
            foreach (IGrouping<String, String> item in warnings)
            {
                foreach (String cs in csCodes)
                {
                    if (item.Count(r => r.Contains(cs)) == 0)
                    {
                        continue;
                    }

                    issues = true;

                    host.AddResult(Severity.Info, true, $"{cs} - {item.Key}"); // - {Utils.XMLComments[cs]}

                    foreach (String line in item)
                    {
                        if (line.Contains(cs))
                        {
                            String msg = line.Substring(line.IndexOf($"{cs}: ") + $"{cs}: ".Length);

                            //msg = msg.Substring(0, msg.IndexOf("[")).Trim();

                            if (msg.Contains(" -- "))
                            {
                                msg = msg.Substring(msg.IndexOf(" -- ") + " -- ".Length);
                                msg = msg.Trim(new Char[] { '\'' });
                            }

                            String row = line.Substring(line.IndexOf("(") + 1);
                            row = row.Substring(0, row.IndexOf(","));
                            if (Int32.TryParse(row, out Int32 r))
                            {
                                row = $"{r:0000}";
                            }

                            host.AddResult(Severity.Warning, true, $"line: {row} - {msg}", 1);
                        }
                    }
                }
            }

            if (!issues)
            {
                host.AddResult(Severity.Info, true, $"No XML Documentation issues found."); // - {Utils.XMLComments[cs]}
            }

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
            return (Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Methods
    }
}