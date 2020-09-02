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
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using DiffMatchPatch;

    using ICSharpCode.Decompiler;
    using ICSharpCode.Decompiler.CSharp;
    using ICSharpCode.Decompiler.TypeSystem;
    using Mono.Cecil;

    public static class Utils
    {
        #region Fields

        /// <summary>
        /// The debug build.
        /// </summary>
        public static String debug = @"'Debug|AnyCPU'";

        /// <summary>
        /// The release build.
        /// </summary>
        public static String release = @"'Release|AnyCPU'";

        /// <summary>
        /// The misplaced XML comments warning.
        /// </summary>
        public static Dictionary<String, String> XMLComments = new Dictionary<String, String>() {
            { "CS1570", "XML comment has badly formed XML" },
            { "CS1572", "XML comment has a param tag, but there is no parameter by that name" },
            { "CS1573", "Parameter has no matching param tag in the XML comment" },
            { "CS1587", "XML comment is not placed on a valid language element" },
            { "CS1591", "Missing XML comment for publicly visible type or member" }};

        /// <summary>
        /// The asms.
        /// </summary>
        static Dictionary<String, Assembly> asms = new Dictionary<String, Assembly>();

        /// <summary>
        /// The assemblies.
        /// </summary>
        private static Dictionary<String, CSharpDecompiler> Assemblies = new Dictionary<String, CSharpDecompiler>();

        /// <summary>
        /// The documents.
        /// </summary>
        private static Dictionary<String, XDocument> Documents = new Dictionary<String, XDocument>();

        /// <summary>
        /// The namespaces.
        /// </summary>
        private static Dictionary<String, XmlNamespaceManager> Namespaces = new Dictionary<String, XmlNamespaceManager>();


        /// <summary>
        /// The portable.
        /// </summary>
        public static String portable = "PORTABLE";

        /// <summary>
        /// The profiles lookup table.
        /// </summary>
        private static readonly Dictionary<String, String> profiles = new Dictionary<String, String>()
        {
            //! https://portablelibraryprofiles.stephencleary.com/
            //! https://docs.microsoft.com/en-us/nuget/schema/target-frameworks
            //
            { "Profile2","portable-net40+win8+sl4+wp7" },
            { "Profile3","portable-net40+sl4" },
            { "Profile4 ","portable-net45+sl4+win8+wp7" },
            { "Profile5", "portable-net4+win8"},
            { "Profile6", "portable-net403+win8" },
            { "Profile7", "portable-net45+win8" },
            { "Profile14", "portable-net4+sl50" },
            { "Profile18", "portable-net403+sl4" },
            { "Profile19", "portable-net403+sl50" },
            { "Profile23", "portable-net45+sl4" },
            { "Profile24", "portable-net45+sl50" },
            { "Profile31", "portable-win81+wp81" },
            { "Profile32", "portable-win81+wpa81" },
            { "Profile36", "portable-net40+sl4+win8+wp8" },
            { "Profile37", "portable-net4+sl50+win8" },
            { "Profile41", "portable-net403+sl4+win8" },
            { "Profile42", "portable-net403+sl50+win8" },
            { "Profile44", "portable-net451+win81" },
            { "Profile46", "portable-net45+sl4+win8" },
            { "Profile47", "portable-net45+sl50+win8" },
            { "Profile49", "portable-net45+wp8" },
            { "Profile78", "portable-net45+win8+wp8" },
            { "Profile84", "portable-wpa81+wp81" },
            { "Profile88", "portable-net40+sl4+win8+wp75" },
            { "Profile92", "portable-net4+win8+wpa81" },
            { "Profile95", "portable-net403+sl4+win8+wp7" },
            { "Profile96", "portable-net403+sl4+win8+wp75" },
            { "Profile102", "portable-net403+win8+wpa81" },
            { "Profile104", "portable-net45+sl4+win8+wp75" },
            { "Profile111", "portable-net45+win8+wpa81" },
            { "Profile136", "portable-net4+sl50+win8+wp8" },
            { "Profile143", "portable-net403+sl4+win8+wp8" },
            { "Profile147", "portable-net403+sl50+win8+wp8" },
            { "Profile151", "portable-net451+win81+wpa81" },
            { "Profile154", "portable-net45+sl4+win8+wp8" },
            { "Profile157", "portable-win81+wpa81+wp81" },
            { "Profile158", "portable-net45+sl50+win8+wp8" },
            { "Profile225", "portable-net4+sl50+win8+wpa81" },
            { "Profile240", "portable-net403+sl50+win8+wpa81" },
            { "Profile255", "portable-net45+sl50+win8+wpa81" },
            { "Profile259", "portable-net45+win8+wpa81+wp8" },
            { "Profile328", "portable-net4+sl50+win8+wpa81+wp8" },
            { "Profile336", "portable-net403+sl50+win8+wpa81+wp8" },
            { "Profile344", "portable-net45+sl50+win8+wpa81+wp8" },
        };

        /// <summary>
        /// The project.
        /// </summary>
        public static Regex rg_project = new Regex(@"\[(.*)\]");

        /// <summary>
        /// The file.
        /// </summary>
        public static Regex rg_file = new Regex(@"^([^\(])*");

        /// <summary>
        /// The create struct.
        /// </summary>
        public static Regex rg_cs = new Regex(@"CS(\d+):");

        /// <summary>
        /// The create struct.
        /// </summary>
        public static Regex rg_csa = new Regex(@"C(S|A)(\d+):");

        /// <summary>
        /// The square brackets.
        /// </summary>
        public static readonly Char[] SquareBrackets = new Char[] { '[', ']' };


        /// <summary>
        /// The workdir.
        /// </summary>
        public static String workdir;

        #endregion Fields

        #region Enumerations

        /// <summary>
        /// Values that represent test types.
        /// </summary>
        public enum TestTypes
        {
            /// <summary>
            /// An enum constant representing the unknown option.
            /// </summary>
            Unknown,
            /// <summary>
            /// An enum constant representing the microsoft option.
            /// </summary>
            Microsoft,
            /// <summary>
            /// An enum constant representing the nunit option.
            /// </summary>
            Nunit,
        }

        /// <summary>
        /// Values that represent assembly types.
        /// </summary>
        public enum AssemblyType
        {
            /// <summary>
            /// An enum constant representing the asset option.
            /// </summary>
            Asset,

            /// <summary>
            /// An enum constant representing the portable asset option.
            /// </summary>
            Portable_Asset,

            /// <summary>
            /// An enum constant representing the unit test option.
            /// </summary>
            UnitTest,

            /// <summary>
            /// An enum constant representing the Window forms option.
            /// </summary>
            WinForms,

            /// <summary>
            /// An enum constant representing the console option.
            /// </summary>
            Console,
        }

        /// <summary>
        /// Values that represent output types.
        /// </summary>
        public enum OutputType
        {
            /// <summary>
            /// An enum constant representing the unknown option.
            /// </summary>
            /// <remarks>This is an additional value used by RQAT</remarks>
            Unknown,

            /// <summary>
            /// Assembly.
            /// </summary>
            Library,

            /// <summary>
            /// Windows Executable.
            /// </summary>
            WinExe,

            /// <summary>
            /// Console Application.
            /// </summary>
            Exe,
        }

        #endregion Enumerations

        #region Methods

        /// <summary>
        /// Defined symbols.
        /// </summary>
        ///
        /// <param name="csproj"> The parameter. </param>
        ///
        /// <returns>
        /// A List&lt;String&gt;
        /// </returns>
        public static List<String> DefinedSymbols(String csproj)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
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
                            String defineConstants = propertyGroup.XPathSelectElement("ns:DefineConstants", namespaces)?.Value;

                            if (!String.IsNullOrEmpty(defineConstants))
                            {

                                return defineConstants.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            }
                        }
                    }
                }
            }

            return new List<String>();
        }

        /// <summary>
        /// Compares two String objects to determine their relative ordering.
        /// </summary>
        ///
        /// <remarks>
        /// Based on Google diff_match_patch.
        /// </remarks>
        ///
        /// <param name="a"> A String to process. </param>
        /// <param name="b"> A String to process. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public static String Diff(String a, String b)
        {
            Diff_Match_Patch dmp = new Diff_Match_Patch();

            List<Diff> diffs = dmp.diff_main(a, b, false);
            dmp.diff_cleanupSemantic(diffs);
            dmp.Diff_EditCost = 80;
            dmp.diff_cleanupEfficiency(diffs);

            return (dmp.patch_toText(dmp.patch_make(diffs))).Replace("%09", "\t");
        }

        /// <summary>
        /// Query if 'assembly' is asset.
        /// </summary>
        ///
        /// <param name="assembly"> The parameter. </param>
        ///
        /// <returns>
        /// True if asset, false if not.
        /// </returns>
        public static Boolean IsAsset(String assembly)
        {
            //! UnitTest Assemblies can/will return a false positive if they test an asset.
            //
            //if (IsUnitTest(assembly))
            //{
            //    return false;
            //}

            IModule asm = Disassemble(assembly).TypeSystem.Modules
                .ToList()
                .Find(p => p.AssemblyName.Equals("RageAssetManager")); // || p.AssemblyName.Equals("RageAssetManager_Portable")

            ITypeDefinition mgr = asm?.GetTypeDefinition("AssetPackage", "BaseAsset");

            return mgr != null;
        }

        /// <summary>
        /// Query if 'assembly' is .net core asset.
        /// </summary>
        ///
        /// <param name="assembly"> The parameter. </param>
        ///
        /// <returns>
        /// True if core asset, false if not.
        /// </returns>
        public static Boolean IsCoreAsset(String assembly)
        {
            IModule asm = Disassemble(assembly).TypeSystem.Modules
                .ToList()
                .Find(p => p.AssemblyName.Equals("RageAssetManager_Core"));

            ITypeDefinition mgr = asm?.GetTypeDefinition("AssetPackage", "BaseAsset");

            return mgr != null;
        }

        /// <summary>
        /// Query if 'assembly' is portable asset.
        /// </summary>
        ///
        /// <param name="assembly"> The parameter. </param>
        ///
        /// <returns>
        /// True if portable asset, false if not.
        /// </returns>
        public static Boolean IsPortableAsset(String assembly)
        {
            IModule asm = Disassemble(assembly).TypeSystem.Modules
                .ToList()
                .Find(p => p.AssemblyName.Equals("RageAssetManager_Portable"));

            ITypeDefinition mgr = asm?.GetTypeDefinition("AssetPackage", "BaseAsset");

            return mgr != null;
        }

        /// <summary>
        /// Query if 'parm' is unit test.
        /// </summary>
        ///
        /// <param name="csproj"> The parameter. </param>
        ///
        /// <returns>
        /// True if unit test, false if not.
        /// </returns>
        public static Boolean IsUnitTest(String csproj)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                return !String.IsNullOrEmpty(doc.Root.XPathSelectElement("ns:PropertyGroup/ns:TestProjectType", namespaces)?.Value);
            }

            return false;
        }

        /// <summary>
        /// Query if 'csproj' is asset manager.
        /// </summary>
        ///
        /// <param name="csproj"> The csproj. </param>
        ///
        /// <returns>
        /// True if asset manager, false if not.
        /// </returns>
        public static Boolean IsAssetManager(String csproj, IHost host)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                String AM = doc.Root.XPathSelectElement("ns:PropertyGroup/ns:AssemblyName", namespaces).Value;
                Boolean isAM = AM.StartsWith("RageAssetManager");

                if (isAM)
                {
                    host.AddResult(Severity.Warning, true, $"The {AM} Project should not be part of the repository.", 1);
                }

                return isAM;
            }

            return false;
        }

        /// <summary>
        /// Loads.
        /// </summary>
        ///
        /// <param name="csproj">     The csproj to load. </param>
        /// <param name="doc">        [out] The document. </param>
        /// <param name="namespaces"> [out] The namespaces. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public static Boolean Load(String csproj, out XDocument doc, out XmlNamespaceManager namespaces)
        {
            doc = null;
            namespaces = null;

            if (!Documents.ContainsKey(csproj))
            {
                try
                {
                    XDocument xd = XDocument.Load(csproj);

                    Documents[csproj] = xd;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            doc = Documents[csproj];

            if (!Namespaces.ContainsKey(csproj))
            {
                try
                {
                    XmlNamespaceManager xmlns = new XmlNamespaceManager(new NameTable());
                    xmlns.AddNamespace("ns", Documents[csproj].Root.GetDefaultNamespace().NamespaceName);

                    Namespaces[csproj] = xmlns;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            namespaces = Namespaces[csproj];

            return true;
        }

        /// <summary>
        /// Loads a file.
        /// </summary>
        ///
        /// <param name="filename"> Filename of the file. </param>
        ///
        /// <returns>
        /// An array of byte.
        /// </returns>
        public static Byte[] LoadFile(String filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open);

            Byte[] buffer = new Byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();

            return buffer;
        }

        /// <summary>
        /// Project output.
        /// </summary>
        ///
        /// <param name="csproj"> The parameter. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public static String ProjectOutput(String csproj)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                XElement firstPropertyGroup = doc.Root.XPathSelectElement("ns:PropertyGroup", namespaces);

                String assemblyName = firstPropertyGroup.XPathSelectElement("ns:AssemblyName", namespaces)?.Value;

                String projectDir = Path.GetDirectoryName(csproj);

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
                            String outputPath = propertyGroup.XPathSelectElement("ns:OutputPath", namespaces)?.Value;
                            String output = Path.GetFullPath(Path.Combine(projectDir, outputPath));

                            switch (outputType)
                            {
                                //! Console Applications.
                                //
                                case OutputType.Exe:
                                    return Path.GetFullPath(Path.Combine(output, Path.ChangeExtension(assemblyName, ".exe")));

                                //! Assemblies
                                //
                                case OutputType.Library:
                                    return Path.GetFullPath(Path.Combine(output, Path.ChangeExtension(assemblyName, ".dll")));

                                //! Windows GUI Applications
                                //
                                case OutputType.WinExe:
                                    return Path.GetFullPath(Path.Combine(output, Path.ChangeExtension(assemblyName, ".exe")));
                            }
                        }
                    }
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Project type.
        /// </summary>
        ///
        /// <param name="csproj"> The csproj. </param>
        ///
        /// <returns>
        /// A List&lt;String&gt;
        /// </returns>
        public static List<String> ProjectType(String csproj)
        {
            List<String> Types = new List<string>();

            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);

                XElement firstPropertyGroup = doc.Root.XPathSelectElement("ns:PropertyGroup", namespaces);

                String guids = firstPropertyGroup.XPathSelectElement("ns:ProjectTypeGuids", namespaces)?.Value;

                if (!String.IsNullOrEmpty(guids))
                {
                    foreach (String guid in guids.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Lookups.ProjectTypeGuids.ContainsValue(guid))
                        {
                            Types.Add(Lookups.ProjectTypeGuids.Where(p => p.Value.Equals(guid)).First().Key);
                        }
                        else
                        {
                            Types.Add($"Unknown Project TypeGuid {guid}.");
                        }
                    }
                }
            }

            return Types;
        }

        /// <summary>
        /// Project output type.
        /// </summary>
        ///
        /// <param name="csproj"> The parameter. </param>
        ///
        /// <returns>
        /// An OutputType.
        /// </returns>
        public static OutputType ProjectOutputType(String csproj)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                return (OutputType)Enum.Parse(typeof(OutputType), doc.Root.XPathSelectElement("ns:PropertyGroup/ns:OutputType", namespaces).Value);
            }

            return OutputType.Unknown;
        }

        /// <summary>
        /// Fixup assembly references.
        /// </summary>
        ///
        /// <param name="csproj"> The csproj. </param>
        public static void FixupAssemblyReferences(IHost host, String csproj)
        {
            //! Implemented
            //< Reference Include="RageAssetManager">
            //  <HintPath>..\..\AssetManager\RageAssetManager\bin\Debug\RageAssetManager.dll</HintPath>
            //</Reference>

            //! Fails (Needs a nuget restore?)
            // 
            // <HintPath>..\packages\Newtonsoft.Json.8.0.2\lib\net35\Newtonsoft.Json.dll</HintPath>

            //! see https://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-a-net-console-application
            String path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            Dictionary<String, String> Fixups = Directory.EnumerateFiles(Path.Combine(path, @"Fixups"), "*.dll")
                .ToDictionary(p => Path.GetFileName(p), p => p);

            String projectpath = Path.GetDirectoryName(csproj);

            Boolean modified = false;

            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                foreach (XElement propertyGroup in doc.Root.XPathSelectElements(@"ns:ItemGroup", namespaces))
                {
                    foreach (XElement reference in propertyGroup.XPathSelectElements(@"ns:Reference", namespaces))
                    {
                        if (reference.HasAttributes && reference.Attribute("Include") != null)
                        {
                            //<Reference Include="RageAssetManager" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                            //  <HintPath>..\..\AssetManager\RageAssetManager\bin\Debug\RageAssetManager.dll</HintPath>
                            //</Reference>
                            // 
                            String s = reference.Attribute("Include").Value;
                            if (reference.XPathSelectElement(@"ns:HintPath", namespaces) != null)
                            {
                                XElement hint = reference.XPathSelectElement(@"ns:HintPath", namespaces);

                                if (!(hint == null || String.IsNullOrEmpty(hint.Value)))
                                {
                                    String hintpath = Path.GetFullPath(Path.Combine(projectpath, hint.Value));

                                    if (!File.Exists(hintpath))
                                    {
                                        String am = Path.GetFileName(hint.Value);

                                        if (Fixups.ContainsKey(am))
                                        {
                                            host.AddResult(Severity.Info, true, $"Patching Hint Location of '{am}' in '{Path.GetFileName(csproj)}'.");

                                            hint.Value = Fixups[am];

                                            modified = true;
                                        }
                                        else
                                        {
                                            host.AddResult(Severity.Warning, false, $"Failed Patching Hint Location of '{am}' in '{Path.GetFileName(csproj)}'.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (modified)
                {
                    doc.Save(csproj);
                }
            }
        }

        /// <summary>
        /// Runtime version.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        ///
        /// <returns>
        /// A Version.
        /// </returns>
        public static Version RuntimeVersion(String parm)
        {
            if (!asms.ContainsKey(parm))
            {
                asms.Add(parm, Assembly.ReflectionOnlyLoad(Utils.LoadFile(parm)));
            }

            return Version.Parse(asms[parm].ImageRuntimeVersion.TrimStart('v'));
        }

        /// <summary>
        /// Sets DLL directory.
        /// </summary>
        ///
        /// <param name="pathName"> Full pathname of the file. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true /*, CallingConvention = CallingConvention.Cdecl*/)]
        public static extern bool SetDllDirectory(string pathName);

        /// <summary>
        /// Unit test type.
        /// </summary>
        ///
        /// <param name="csproj"> The parameter. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public static String UnitTestType(String csproj)
        {
            if (Utils.Load(csproj, out XDocument doc, out XmlNamespaceManager namespaces))
            {
                return doc.Root.XPathSelectElement("ns:PropertyGroup/ns:TestProjectType", namespaces)?.Value;
            }

            return String.Empty;
        }

        /// <summary>
        /// Disassembles.
        /// </summary>
        ///
        /// <param name="assembly"> The parameter. </param>
        ///
        /// <returns>
        /// A CSharpDecompiler.
        /// </returns>
        private static CSharpDecompiler Disassemble(string assembly)
        {
            if (!Assemblies.ContainsKey(assembly))
            {
                Assemblies.Add(assembly, new CSharpDecompiler(assembly, new DecompilerSettings()
                {
                    AlwaysUseBraces = true,
                    LoadInMemory = true,
                    RemoveDeadCode = false,
                }));
            }

            return Assemblies[assembly];
        }

        /// <summary>
        /// Method signature.
        /// </summary>
        ///
        /// <param name="typedef"> The typedef. </param>
        /// <param name="method">  The method. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public static String MethodSignature(ITypeDefinition typedef, IMethod method)
        {
            //! See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/accessibility-levels
            //
            //if (method.Accessibility == Accessibility.Public)
            //{
            StringBuilder sb = new StringBuilder();

            sb.Append($"{method.Accessibility} ");

            if (method.IsConstructor)
            {
                sb.Append($"{typedef.Name.ToLower()}(");
            }
            else
            {
                sb.Append($"{method.ReturnType.Name} {method.Name}(");
            }

            //if (method.HasGeneratedName())
            //{
            //    sb.Append("PPP");
            //}

            foreach (IParameter parmeter in method.Parameters)
            {
                if (parmeter.IsIn)
                {
                    sb.Append("in ");
                }
                if (parmeter.IsOut)
                {
                    sb.Append("out ");
                }
                if (parmeter.IsRef)
                {
                    sb.Append("ref ");
                }
                if (parmeter.IsParams)
                {
                    sb.Append("params ");
                }

#warning TODO Method Signature Generation - Emit generic types correctly.
#warning TODO Method Signature Generation - Abstract?

                sb.Append($"{parmeter.Type.Name}");

                if (parmeter.Type.TypeParameterCount != 0)
                {
                    sb.Append($"<");

                    foreach (IType type in parmeter.Type.TypeArguments)
                    {
                        sb.Append($"{type.Name}, ");
                    }

                    sb.Append($">");
                    sb.Replace(", >", ">");
                }
                ///*Console*/ (parm.Type.)
                sb.Append($" {parmeter.Name}");

                if (parmeter.IsOptional)
                {
                    sb.Append($" = {(parmeter.GetConstantValue() ?? "null")} ");
                }

                sb.Append(", ");
            }

            sb.Append($")");

            sb.Replace(", )", ")");

            return sb.ToString();
        }

        /// <summary>
        /// Method signature.
        /// </summary>
        ///
        /// <param name="typedef"> The typedef. </param>
        /// <param name="method">  The method. </param>
        ///
        /// <returns>
        /// A String.
        /// </returns>
        public static String MethodSignature(Mono.Cecil.TypeDefinition typedef, Mono.Cecil.MethodDefinition method)
        {
            //! See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/accessibility-levels
            //
            //if (method.Accessibility == Accessibility.Public)
            //{
            StringBuilder sb = new StringBuilder();

            if (method.IsPublic)
            {
                sb.Append($"public ");
            }
            if (method.IsPrivate)
            {
                sb.Append($"private ");
            }
            if (method.IsStatic)
            {
                sb.Append($"static ");
            }
            if (method.IsAbstract)
            {
                sb.Append($"abstract ");
            }
            if (method.IsVirtual)
            {
                sb.Append($"virtual ");
            }

#warning TODO Method Signature Generation - internal, override & protected

            if (method.IsConstructor)
            {
                sb.Append($"{typedef.Name}(");
            }
            else
            {
                sb.Append($"{method.ReturnType.Name} {method.Name}(");
            }

            //if (method.HasGeneratedName())
            //{
            //    sb.Append("PPP");
            //}

            foreach (ParameterDefinition parmeter in method.Parameters)
            {
                if (parmeter.IsIn && parmeter.IsOut)
                {
                    sb.Append("ref ");
                }
                else if (parmeter.IsIn)
                {
                    sb.Append("in ");
                }
                else if (parmeter.IsOut)
                {
                    sb.Append("out ");
                }

#warning TODO Method Signature Generation - params
                //if (parmeter.IsParams)
                //{
                //    sb.Append("params ");
                //}

#warning TODO Method Signature Generation - Emit generic types correctly.
#warning TODO Method Signature Generation - Abstract?
#warning TODO Method Signature Generation - Generic Parameters fail (List`1)
                if (parmeter.ParameterType is GenericInstanceType)
                {
                    if ((parmeter.ParameterType as GenericInstanceType).HasGenericArguments)
                    {
                        String name = parmeter.ParameterType.Name;
                        for (Int32 i = 1; i <= (parmeter.ParameterType as GenericInstanceType).GenericArguments.Count; i++)
                        {
                            name = name.Replace($"`{i}", String.Empty);
                        }
                        sb.Append($"{name}");


                        sb.Append($"<");

                        foreach (Mono.Cecil.TypeReference type in (parmeter.ParameterType as GenericInstanceType).GenericArguments)
                        {
                            sb.Append($"{type.Name}, ");
                        }

                        sb.Append($">");
                        sb.Replace(", >", ">");
                    }
                    else
                    {
                        Debug.WriteLine("Error");
                    }
                }
                else
                {
                    sb.Append($"{parmeter.ParameterType.Name}");
                }

                ///*Console*/ (parm.Type.)
                sb.Append($" {parmeter.Name}");

#warning TODO Method Signature Generation - Optional Parameters.

                //if (parmeter.IsOptional)
                //{
                //    sb.Append($" = {(parmeter.GetConstantValue() == null ? "null" : parmeter.GetConstantValue())} ");
                //}

                sb.Append(", ");
            }

            sb.Append($")");

            sb.Replace(", )", ")");

            return sb.ToString();
        }

        /// <summary>
        /// Searches for the first executable path.
        /// </summary>
        ///
        /// <param name="exe"> The executable. </param>
        ///
        /// <returns>
        /// The found executable path.
        /// </returns>
        public static List<String> FindExePath(String exe)
        {
            String expandedExe = Environment.ExpandEnvironmentVariables(exe);

            if (!File.Exists(expandedExe))
            {
                String exeName = Path.GetFileName(expandedExe);

                return (Environment.GetEnvironmentVariable("PATH") ?? String.Empty)
                     .Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Trim())
                     .Where(p => !String.IsNullOrEmpty(p))
                     .Where(p => File.Exists(Path.GetFullPath(Path.Combine(p, exeName))))
                     .Select(p => Path.GetFullPath(Path.Combine(p, exeName)))
                     .ToList();
            }
            else
            {
                return new List<String>() { Path.GetFullPath(expandedExe) };
            }
        }

        /// <summary>
        /// Builds.
        /// </summary>
        ///
        /// <param name="csproj"> The csproj. </param>
        /// <param name="host">   The host. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public static Boolean Build(String csproj, IHost host)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(csproj));

            return ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(csproj),
                Arguments = $"{Path.GetFileName(csproj)} /target:Clean;Build",
                FileName = host.MSBuildPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false) == 0;
        }

        /// <summary>
        /// Cleans.
        /// </summary>
        ///
        /// <param name="csproj"> The csproj. </param>
        /// <param name="host">   The host. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public static Boolean Clean(String csproj, IHost host)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(csproj));

            return ExecutePsi(new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(csproj),
                Arguments = $"{Path.GetFileName(csproj)} /target:Clean",
                FileName = host.MSBuildPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            }, host, out StringBuilder sb, false) == 0;
        }

        /// <summary>
        /// Executes the psi operation.
        /// </summary>
        ///
        /// <param name="psi">   The psi. </param>
        /// <param name="host">  The host. </param>
        /// <param name="sb">    [out] The sb. </param>
        /// <param name="logstd">   (Optional) True to log. </param>
        /// <param name="logerr">  (Optional) True to log errors. </param>
        /// <param name="error"> (Optional) The error. </param>
        ///
        /// <returns>
        /// An Int32.
        /// </returns>
        public static Int32 ExecutePsi(ProcessStartInfo psi, IHost host, out StringBuilder sb, Boolean logstd = true, Boolean logerr = true, Severity error = Severity.Error)
        {
            host.AddResult(Severity.Info, true, $"{Path.GetFileNameWithoutExtension(psi.FileName)}{(String.IsNullOrEmpty(psi.Arguments) ? String.Empty : " " + psi.Arguments)} Output:");

            Process p = Process.Start(psi);

            sb = new StringBuilder();

            //! Standard Output
            // 
            while (psi.RedirectStandardOutput && !p.StandardOutput.EndOfStream)
            {
                String line = p.StandardOutput.ReadLine();
                sb.AppendLine(line);

                if (logstd && !String.IsNullOrEmpty(line))
                {
                    host.AddResult(Severity.Info, true, line, 1);
                }
            }

            //! Error Output
            // 
            while (psi.RedirectStandardError && !p.StandardError.EndOfStream)
            {
                String line = p.StandardError.ReadLine();
                sb.AppendLine(line);

                if (logstd && !String.IsNullOrEmpty(line))
                {
                    //! Git has the habit to emit a number of messages in the error stream.
                    // 
                    host.AddResult(error, (error == Severity.Info), line, 1);
                }
            }

            p.WaitForExit();

            host.AddResult(p.ExitCode == 0 ? Severity.Info : Severity.Warning, p.ExitCode == 0, $"{Path.GetFileNameWithoutExtension(psi.FileName)} Job {(p.ExitCode == 0 ? "Success" : "Failure")} with (ExitCode: {p.ExitCode}).");

            return p.ExitCode;
        }

        #endregion Methods
    }
}