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
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
    public class ProjectOutputReaderPlugin : IProjectPlugin
    {
        #region Fields

#warning !! This plugin seems a dupe of ProjectOutputPlugin !!
#warning !! Add documentation field to plugin !!
#warning !! Add field if the plugin recurses (node) or not (leaf) !!

        /// <summary>
        /// The host.
        /// </summary>
        private IHost host;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProjectOutputReaderPlugin()
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
        public string Name => "*.csproj output detection plugin invoker";

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
        public string Description => "This plugin examines a *.csproj file and schedules plugins for its output (if an Assembly).";

        /// <summary>
        /// Gets a value indicating whether this object is leaf.
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
        /// A Dictionary&lt;String,Boolean&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            if (File.Exists(job.Parm) && TryAnalyzeProject(job, out OutputType outputType, out String outputFile))
            {
                switch (outputType)
                {
                    case OutputType.Exe:
                        host.AddResult(Severity.Warning, true, $"{outputType} Project Output not analysed.");
                        break;

                    case OutputType.Library:
#warning RCSAA Specific Code (Skip AssetManager from processing if present).
                        if (Utils.IsAssetManager(job.Parm, host))
                        {
                            //break;
                        }
                        else if (Utils.IsUnitTest(job.Parm))
                        {
                            host.RecurseForTypes(job, FileType.UnitTest, outputFile);
                        }
                        else
                        {
                            host.RecurseForTypes(job, FileType.Assembly, outputFile);
                        }
                        break;

                    case OutputType.WinExe:
                        host.AddResult(Severity.Warning, true, $"{outputType} Project Output not analysed.");
                        break;

                    case OutputType.Unknown:
                        host.AddResult(Severity.Warning, true, $"{outputType} Project Output not analysed.");
                        break;
                }
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Load or Parse Project.");

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
            return (Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase));
        }

        /// Analyze project.
        /// </summary>
        ///
        /// <param name="job"> The level. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        ///
        /// ### <param name="parm"> The parameter. </param>
        private Boolean TryAnalyzeProject(IJob job, out OutputType outputType, out String outputFile)
        {
            outputType = OutputType.Unknown;
            outputFile = String.Empty;

            if (Utils.Load(job.Parm, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                host.AddResult(Severity.Info, true, $"[Examining Output of '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

                outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                //! Warning Unity Projects have an extra PropertyGroup before the one containing the AssemblyName
                //<PropertyGroup>
                //  <LangVersion>4</LangVersion>
                //</PropertyGroup>

                IEnumerable<XElement> firstPropertyGroups = doc.Root.XPathSelectElements("ns:PropertyGroup", namespaces);
                XElement firstPropertyGroup = firstPropertyGroups.First(p => p.XPathSelectElement("ns:AssemblyName", namespaces) != null);

                String assemblyName = firstPropertyGroup.XPathSelectElement("ns:AssemblyName", namespaces)?.Value;

                if (String.IsNullOrEmpty(assemblyName))
                {
                    host.AddResult(Severity.Error, true, $"AssemblyName tag not found.");

                    return false;
                }

                String projectDir = Path.GetDirectoryName(job.Parm);

#warning C:\Temp\NuGet\ClientSideGameStorageAsset\GameStorageTestApp\GameStorageTestApp.csproj fails

                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:PropertyGroup", namespaces))
                {
                    XAttribute condition = propertyGroup.Attribute("Condition");

                    if (condition != null)
                    {
                        // TODO Decode this better (AnyCPU) for example.
                        //
                        String value = condition.Value;
                        if (value.Equals($" '$(Configuration)|$(Platform)' == {Utils.debug} "))
                        {
                            // DONE <OutputPath>bin\Debug\</OutputPath>                         Check
                            // DONE <DocumentationFile>bin\Debug\RageAssetManager.XML</DocumentationFile>   Check
                            // TODO <DefineConstants>TRACE</DefineConstants>                    Check
                            // DONE <DebugSymbols>true</DebugSymbols>                           Check
                            // TODO <Prefer32Bit>false</Prefer32Bit>                            Check
                            //
                            String outputPath = propertyGroup.XPathSelectElement("ns:OutputPath", namespaces)?.Value;
                            String debugSymbols = propertyGroup.XPathSelectElement("ns:DebugSymbols", namespaces)?.Value;
                            String documentationFile = propertyGroup.XPathSelectElement("ns:DocumentationFile", namespaces)?.Value;

                            outputFile = Path.GetFullPath(Path.Combine(projectDir, outputPath, Path.ChangeExtension(assemblyName, outputType == OutputType.Library ? ".dll" : ".exe")));

                            if (File.Exists(outputFile))
                            {
                                host.AddResult(Severity.Info, true, $"Loaded and Parsed Project.");

                                //! Console Applications.
                                //
                                switch (outputType)
                                {
                                    case OutputType.Exe:
                                        host.AddResult(Severity.Info, true, $"Detected Console Project '{Path.GetFileName(outputFile)}'.", 1);
                                        host.AddResult(Severity.Warning, true, $"Windows Console Projects ignored for now.", 1);
                                        break;
                                    case OutputType.Library:
                                        host.AddResult(Severity.Info, true, $"Detected Assembly Project '{Path.GetFileName(outputFile)}'.", 1);

                                        if (Utils.IsUnitTest(job.Parm) && Utils.ProjectType(job.Parm).Contains("Test"))
                                        {
                                            host.AddResult(Severity.Info, true, $"Detected UnitTest Project.", 1);
                                        }
                                        else
                                        {
                                            if (Utils.IsAsset(outputFile))
                                            {
                                                host.AddResult(Severity.Info, true, $"Detected Main RAGE Asset.", 1);
                                            }
                                            if (Utils.IsPortableAsset(outputFile))
                                            {
                                                host.AddResult(Severity.Info, true, $"Detected Portable RAGE Asset.", 1);
                                            }
                                            if (Utils.IsCoreAsset(outputFile))
                                            {
                                                host.AddResult(Severity.Info, true, $"Detected .Net Core RAGE Asset.", 1);
                                            }
                                        }
                                        break;
                                    case OutputType.WinExe:
                                        host.AddResult(Severity.Info, true, $"Detected Windows GUI Project '{Path.GetFileName(outputFile)}'.", 1);
                                        host.AddResult(Severity.Warning, true, $"Windows Windows GUI Projects ignored for now.", 1);
                                        break;
                                    case OutputType.Unknown:
                                        host.AddResult(Severity.Info, true, $"Detected Unnown Project '{Path.GetFileName(outputFile)}'.", 1);
                                        host.AddResult(Severity.Warning, true, $"Unknonw Projects ignored for now.", 1);
                                        break;
                                }

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion Methods
    }
}