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
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
#if enabled
    [Export]
    [Export(typeof(IProjectPlugin))]
    [Export(typeof(IPlugin))]
#endif

    public class ProjectPlugin
#if enabled
        : IProjectPlugin
#endif
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
        public ProjectPlugin()
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
        public string Name => "*.csproj target plugin invoker";

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
            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Examnining '{Path.GetFileNameWithoutExtension(job.Parm)}' Project]");

            if (File.Exists(job.Parm))
            {
                if (Utils.Load(job.Parm, out XDocument doc, out XmlNamespaceManager namespaces))
                {
                    OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                    XElement firstPropertyGroup = doc.Root.XPathSelectElement("ns:PropertyGroup", namespaces);

                    String nameSpace = firstPropertyGroup.XPathSelectElement("ns:RootNamespace", namespaces)?.Value;
                    String assemblyName = firstPropertyGroup.XPathSelectElement("ns:AssemblyName", namespaces)?.Value;
                    String targetFrameworkVersion = firstPropertyGroup.XPathSelectElement("ns:TargetFrameworkVersion", namespaces)?.Value;
                    String targetFrameworkProfile = firstPropertyGroup.XPathSelectElement("ns:TargetFrameworkProfile", namespaces)?.Value;
                    String defineConstants = firstPropertyGroup.XPathSelectElement("ns:DefineConstants", namespaces)?.Value;

                    switch (outputType)
                    {
                        case OutputType.Exe:
                            //! Console Applications.
                            //
                            host.AddResult(Severity.Info, true, $"Console Application Project '{job.Parm}' ignored.");
                            break;

                        case OutputType.Library:
                            //! Assemblies
                            //
                            String projectDir = Path.GetDirectoryName(job.Parm);

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
                                        host.AddResult(Severity.Info, true, $"Found {Utils.debug} Condition.");

                                        // DONE <OutputPath>bin\Debug\</OutputPath>                         Check
                                        // DONE <DocumentationFile>bin\Debug\RageAssetManager.XML</DocumentationFile>   Check
                                        // TODO <DefineConstants>TRACE</DefineConstants>                    Check
                                        // DONE <DebugSymbols>true</DebugSymbols>                           Check
                                        // TODO <Prefer32Bit>false</Prefer32Bit>                            Check
                                        //
                                        String outputPath = propertyGroup.XPathSelectElement("ns:OutputPath", namespaces)?.Value;
                                        String debugSymbols = propertyGroup.XPathSelectElement("ns:DebugSymbols", namespaces)?.Value;
                                        String documentationFile = propertyGroup.XPathSelectElement("ns:DocumentationFile", namespaces)?.Value;

                                        String output = Path.GetFullPath(Path.Combine(projectDir, outputPath, Path.ChangeExtension(assemblyName, ".dll")));

                                        if (File.Exists(output))
                                        {
                                            host.AddResult(Severity.Info, true, $"Located '{output}'.");

                                            if (Utils.IsUnitTest(job.Parm))
                                            {
                                                host.RecurseForTypes(job, FileType.UnitTest, output);
                                            }
                                            else
                                            {
                                                host.RecurseForTypes(job, FileType.Assembly, output);
                                            }
                                        }
                                    }
                                }
                            }

                            break;

                        case OutputType.WinExe:
                            //! Windows GUI Applications
                            //
                            host.AddResult(Severity.Info, true, $"GUI Application Project '{job.Parm}' ignored.");
                            break;
                    }
                }
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Project.");
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
#warning ProjectPlugin is Disabled (duplicate of ProjectOutputReaderPlugin).
            return false;
            //return (Path.GetExtension(parm).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase));
        }

        #endregion Methods
    }
}