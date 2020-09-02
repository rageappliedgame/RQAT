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
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(ISolutionPlugin))]
    [Export(typeof(IPlugin))]
    public class SolutionNuGetPlugin : ISolutionPlugin
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
        public SolutionNuGetPlugin()
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
                return "*.Sln NuGet Package Generation Check";
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
        public string Description => "This plugin tries to create a NuGet package with RAGE Asset and their sources from a solution (and checks for neccesary metadata).";

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
        /// Adds a platform file.
        /// </summary>
        ///
        /// <param name="ns">                     The ns. </param>
        /// <param name="targetFrameworkVersion"> Target framework version. </param>
        /// <param name="targetFrameworkProfile"> Target framework profile. </param>
        /// <param name="asmfile">                The asmfile. </param>
        ///
        /// <returns>
        /// An XElement.
        /// </returns>
        private XElement CreatePlatformFile(XNamespace ns, String targetFrameworkVersion, String targetFrameworkProfile, String asmfile)
        {
            XElement asm = new XElement(ns + "file");

            asm.SetAttributeValue("src", asmfile);

            if (String.IsNullOrEmpty(targetFrameworkProfile))
            {
                if (targetFrameworkVersion.StartsWith("v"))
                {
                    asm.SetAttributeValue("target", String.Format(@"lib\{0}\{1}", targetFrameworkVersion.Replace("v", "net"), Path.GetFileName(asmfile)));
                }
                else
                {
                    // TODO possibly incorrect version, should result in a warning.
                    // 
                    asm.SetAttributeValue("target", String.Format(@"lib\{0}\{1}", targetFrameworkVersion, Path.GetFileName(asmfile)));
                }
            }
            else
            {
                if (SolutionProject.profiles.ContainsKey(targetFrameworkProfile))
                {
                    asm.SetAttributeValue("target", String.Format(@"lib\{0}\{1}", SolutionProject.profiles[targetFrameworkProfile], Path.GetFileName(asmfile)));
                }
                else
                {
                    //! TODO incorrect/unknown profile, should result in a error.
                    asm.SetAttributeValue("target", String.Format(@"lib\{0}\{1}", targetFrameworkProfile, Path.GetFileName(asmfile)));
                }
            }

            return asm;
        }

        /// <summary>
        /// Creates source file.
        /// </summary>
        ///
        /// <param name="ns">     The ns. </param>
        /// <param name="src">    Source for the. </param>
        /// <param name="target"> Target for the. </param>
        ///
        /// <returns>
        /// The new source file.
        /// </returns>
        private XElement CreateSourceFile(XNamespace ns, String src, String target)
        {
            XElement file = new XElement(ns + "file");
            file.SetAttributeValue("src", src);
            file.SetAttributeValue("target", Path.Combine("src", target));

            return file;
        }

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
            List<String> csprojs = new List<String>();

            String root = Path.GetDirectoryName(job.Parm);
            if (!root.EndsWith(@"\"))
            {
                root += @"\";
            }

            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Checking '{Path.GetFileNameWithoutExtension(job.Parm)}' Solution]");

            if (File.Exists(job.Parm))
            {
                Solution solution = new Solution();

                if (solution.Load(job.Parm))
                {
                    XDocument nuspec = new XDocument(new XDeclaration("1.0", "utf-8", null));
                    XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd");

                    nuspec.Add(new XElement(ns + "package"));

                    XElement metadata = new XElement(ns + "metadata");
                    XElement files = new XElement(ns + "files");
                    XElement dependencies = new XElement(ns + "dependencies");

                    host.AddResult(Severity.Info, true, "Nuspec Metadata");

                    AddMetaData("id", Path.GetFileNameWithoutExtension(job.Parm), ns, metadata);
                    AddMetaData("licenseUrl", "https://www.gamecomponents.eu", ns, metadata);
                    AddMetaData("requireLicenseAcceptance", "true", ns, metadata);

                    //! Where to get the release notes/readme ?
                    AddMetaData("releaseNotes", "This is the initial release.", ns, metadata);
                    AddMetaData("tags", "RCSAA component.", ns, metadata);

                    foreach (SolutionProject project in solution.Projects)
                    {
                        if (project.ProjectTypeGuid.Equals(new Guid(Lookups.ProjectTypeGuids["C#"])))
                        {
                            String csproj = Path.Combine(Path.GetDirectoryName(solution.SolutionPath), project.RelativePath);

                            XmlNamespaceManager namespaces = new XmlNamespaceManager(new NameTable());

                            XDocument doc = XDocument.Load(csproj);

                            namespaces.AddNamespace("ns", doc.Root.GetDefaultNamespace().NamespaceName);

                            OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                            switch (outputType)
                            {
                                case OutputType.Library:
                                    //! Skip Test Projects
                                    String testProjectType = doc.Root.XPathSelectElement("ns:PropertyGroup/ns:TestProjectType", namespaces)?.Value;
                                    if (String.IsNullOrEmpty(testProjectType))
                                    {
                                        csprojs.Add(csproj);
                                    }
                                    break;
                                case OutputType.WinExe:
                                    //! Skip Executable Projects
                                    break;
                                case OutputType.Exe:
                                    //! Skip Console Projects
                                    break;
                            }
                        }
                    }

                    metadata.Add(dependencies);

                    Boolean VersionAdded = false;

#warning There should ideally be only 2 projects left.
#warning Version info on both should match as the 2nd properties file is linked.

                    foreach (String csproj in csprojs)
                    {
                        //! 0) Examine *.csproj files containing assemblies only.
                        //
                        XmlNamespaceManager namespaces = new XmlNamespaceManager(new NameTable());

                        XDocument doc = XDocument.Load(csproj);

                        namespaces.AddNamespace("ns", doc.Root.GetDefaultNamespace().NamespaceName);

                        // TODO <RootNamespace>AssetManagerPackage</RootNamespace>      - Check Naming Convention
                        // TODO <AssemblyName>RageAssetManager</AssemblyName>           - Check Naming Convention
                        // DONE <TargetFrameworkVersion>v3.5</TargetFrameworkVersion>   - Check Value & Convert into nuspec format
                        // TODO <DefineConstants>TRACE;DEBUG</DefineConstants>          - Check Values (presence of PORTABLE).

                        OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                        XElement firstPropertyGroup = doc.Root.XPathSelectElement("ns:PropertyGroup", namespaces);

                        String nameSpace = firstPropertyGroup.XPathSelectElement("ns:RootNamespace", namespaces)?.Value;
                        String assemblyName = firstPropertyGroup.XPathSelectElement("ns:AssemblyName", namespaces)?.Value;
                        String targetFrameworkVersion = firstPropertyGroup.XPathSelectElement("ns:TargetFrameworkVersion", namespaces)?.Value;
                        String targetFrameworkProfile = firstPropertyGroup.XPathSelectElement("ns:TargetFrameworkProfile", namespaces)?.Value;
                        String defineConstants = firstPropertyGroup.XPathSelectElement("ns:DefineConstants", namespaces)?.Value;

                        String projectDir = Path.GetDirectoryName(csproj);

                        switch (outputType)
                        {
                            case OutputType.Library:

                                //! 1) Examine PropertyGroups related to Debug/Release output.
                                //
                                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:PropertyGroup", namespaces))
                                {
                                    XAttribute condition = propertyGroup.Attribute("Condition");

                                    if (condition != null)
                                    {
                                        // TODO Decode this better (AnyCPU) for example.
                                        //
                                        String value = condition.Value;
                                        if (value.Equals($" '$(Configuration)|$(Platform)' == {Utils.release} "))
                                        {
                                            Debug.Print($"FOUND {Utils.release} Condition");

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
                                                Debug.Print($"{output}");
                                            }

                                            if (!VersionAdded)
                                            {
                                                //! Add version only once. Info is located in the assemblies (AssemblyInfo.cs file).
                                                //
                                                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Path.Combine(projectDir, output));

                                                AddMetaData("version", fvi.FileVersion, ns, metadata);
                                                AddMetaData("authors", fvi.CompanyName, ns, metadata);

                                                //! Get URL from .git?
                                                AddMetaData("projectUrl", "https://www.gamecomponents.eu", ns, metadata);
                                                AddMetaData("copyright", fvi.LegalCopyright, ns, metadata);
                                                AddMetaData("description", fvi.FileDescription, ns, metadata);

                                                VersionAdded = true;
                                            }

                                            //! Add Assembly to Platform Files.
                                            // 
                                            String asmfile = Path.GetFullPath(Path.Combine(projectDir, output)).Replace(root, String.Empty);
                                            if (File.Exists(output))
                                            {
                                                files.Add(CreatePlatformFile(ns, targetFrameworkVersion, targetFrameworkProfile, asmfile));
                                            }

                                            //! Check and add Debug Symbols to Platform Files.
                                            // 
                                            if (!String.IsNullOrEmpty(debugSymbols) && debugSymbols.Equals("true"))
                                            {
                                                String symbols = Path.ChangeExtension(asmfile, ".pdb");

                                                if (File.Exists(Path.ChangeExtension(output, ".pdb")))
                                                {
                                                    files.Add(CreatePlatformFile(ns, targetFrameworkVersion, targetFrameworkProfile, symbols));
                                                }
                                            }

                                            //! Check and add Xml Documentation to Platform Files.
                                            // 
                                            if (!String.IsNullOrEmpty(documentationFile))
                                            {
                                                String documentation = Path.Combine(projectDir, documentationFile);

                                                if (File.Exists(documentation))
                                                {
                                                    files.Add(CreatePlatformFile(ns, targetFrameworkVersion, targetFrameworkProfile, documentation.Replace(root, "")));
                                                }
                                            }
                                        }
                                        else if (value.Equals($" '$(Configuration)|$(Platform)' == {Utils.debug} "))
                                        {
                                            Debug.Print($"FOUND {Utils.debug} Condition");
                                        }
                                    }
                                }

                                //! 2) Examine ItemGroup related to References.
                                //
                                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:ItemGroup", namespaces))
                                {
                                    foreach (XElement reference in propertyGroup.XPathSelectElements(@"ns:Reference", namespaces))
                                    {
                                        Debug.Print($"Reference: {Path.GetFullPath(Path.Combine(projectDir, reference.Value))}");
                                    }
                                }

                                //! 3) Examine ItemGroup related to Sources.
                                //
                                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:ItemGroup", namespaces))
                                {
                                    foreach (XElement compile in propertyGroup.XPathSelectElements(@"ns:Compile", namespaces))
                                    {
                                        //! Check for linked sources and omit them.
                                        //
                                        if (compile.XPathSelectElements(@"ns:Link", namespaces).Count() == 0)
                                        {

#warning Make sure the portable project is 100% linked else generate a warning/error.

                                            String source = Path.GetFullPath(Path.Combine(projectDir, compile.Attribute("Include").Value)).Replace(root, String.Empty);
                                            String rel = Path.GetDirectoryName(compile.Attribute("Include").Value);

                                            Debug.Print($"Source: {source}");

                                            files.Add(CreateSourceFile(ns, source, rel));
                                        }
                                    }
                                }

                                //! 4) Examine ItemGroup related toEmbedded Resources
                                //
                                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:ItemGroup", namespaces))
                                {
                                    foreach (XElement embeddedResource in propertyGroup.XPathSelectElements(@"ns:EmbeddedResource", namespaces))
                                    {
                                        if (embeddedResource.XPathSelectElements(@"ns:Link", namespaces).Count() == 0)
                                        {
                                            String resource = Path.GetFullPath(Path.Combine(projectDir, embeddedResource.Attribute("Include").Value)).Replace(root, String.Empty);
                                            String rel = Path.GetDirectoryName(embeddedResource.Attribute("Include").Value);

                                            Debug.Print($"EmbeddedResource: {resource}");

                                            files.Add(CreateSourceFile(ns, resource, rel));
                                        }
                                    }
                                }
                                break;
                            case OutputType.WinExe:
                                break;
                            case OutputType.Exe:
                                break;
                        }

                        //! 5) Check package file
                        //
                        //<?xml version="1.0" encoding="utf-8"?>
                        //<packages>
                        //  <package id="NuGet.Build.Packaging" version="0.2.2" targetFramework="net35" developmentDependency="true" />
                        //</packages>

                        String packageConfig = Path.Combine(Path.GetDirectoryName(csproj), "packages.config");

                        if (File.Exists(packageConfig))
                        {
                            XDocument packages = XDocument.Load(packageConfig);

                            foreach (XElement package in packages.Root.XPathSelectElements(@"package"))
                            {
                                XElement dependency = new XElement(ns + "dependency");
                                dependency.SetAttributeValue("id", package.Attribute("id").Value);
                                dependency.SetAttributeValue("version", package.Attribute("version").Value);

                                dependencies.Add(dependency);
                            }
                        }
                    }

                    nuspec.Root.Add(metadata);
                    nuspec.Root.Add(files);

                    //! 6) Save nuspec file.
                    //
                    nuspec.Save(Path.ChangeExtension(job.Parm, ".nuspec"));

                    host.AddResult(Severity.Info, true, "Building Nuget package");

                    //! 7) Pack nuspec file into a nupkg file.
                    //
                    return Utils.ExecutePsi(new ProcessStartInfo
                    {
                        WorkingDirectory = root,
                        Arguments = "pack " + Path.Combine(root, Path.ChangeExtension(job.Parm, ".nuspec")),
                        FileName = host.NuGetPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    }, host, out StringBuilder sb, true) == 0;
                }
                else
                {
                    host.AddResult(Severity.Info, false, $"Failed to Load or Parse Solution {job.Parm}.");
                }
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate Solution.");
            }

            return false;
        }

        private void AddMetaData(String tag, String value, XNamespace ns, XElement metadata)
        {
            host.AddResult(Severity.Info, true, $"{tag}: '{value}'", 1);

#warning handle ini file values here?

            metadata.Add(new XElement(ns + tag, value));
            //! Where to get this url ?
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