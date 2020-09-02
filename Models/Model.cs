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
    using System.Globalization;
    using System.IO;
    using System.Linq;

    #region Enumerations

    /// <summary>
    /// Values that represent output types.
    /// </summary>
    public enum OutputType
    {
        /// <summary>
        /// An enum constant representing the unknown option.
        /// </summary>
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

    /// <summary>
    /// A solution.
    /// </summary>
    public class Solution
    {
        #region Fields

        /// <summary>
        /// The project guids.
        /// </summary>
        public static Dictionary<Guid, String> ProjectGuids = new Dictionary<Guid, String>()
        {
            { Guid.Parse("{06A35CCD-C46D-44D5-987B-CF40FF872267}"), "Deployment Merge Module" },
            { Guid.Parse("{14822709-B5A1-4724-98CA-57A101D1B079}"), "Workflow(C#)" },
            { Guid.Parse("{20D4826A-C6FA-45DB-90F4-C717570B9F32}"), "Legacy(2003) Smart Device(C#)" },
            { Guid.Parse("{2150E333-8FDC-42A3-9474-1A3956D46DE8}"), "Solution Folder" },
            { Guid.Parse("{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}"), "XNA(XBox)" },
            { Guid.Parse("{32F31D43-81CC-4C15-9DE6-3FC5453562B6}"), "Workflow Foundation" },
            { Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}"), "Model-View-Controller v5(MVC 5)" },
            { Guid.Parse("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}"), "Test" },
            { Guid.Parse("{3D9AD99F-2412-4246-B90B-4EAA41C64699}"), "Windows Communication Foundation(WCF)" },
            { Guid.Parse("{3EA9E505-35AC-4774-B492-AD1749C4943A}"), "Deployment Cab" },
            { Guid.Parse("{4D628B5B-2FBC-4AA6-8C16-197242AEB884}"), "Smart Device(C#)" },
            { Guid.Parse("{4F174C21-8C12-11D0-8340-0000F80270F8}"), "Database(other project types)" },
            { Guid.Parse("{54435603-DBB4-11D2-8724-00A0C9A8B90C}"), "Visual Studio 2015 Installer Project Extension" },
            { Guid.Parse("{593B0543-81F6-4436-BA1E-4747859CAAE2}"), "SharePoint(C#)" },
            { Guid.Parse("{603C0E0B-DB56-11DC-BE95-000D561079B0}"), "ASP.NET MVC 1" },
            { Guid.Parse("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}"), "Windows Presentation Foundation(WPF)" },
            { Guid.Parse("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}"), "Project Folders" },
            { Guid.Parse("{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}"), "Smart Device(VB.NET)" },
            { Guid.Parse("{6BC8ED88-2882-458C-8E55-DFD12B67127B}"), "Xamarin.iOS" },
            { Guid.Parse("{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}"), "XNA(Windows)" },
            { Guid.Parse("{76F1466A-8B6D-4E39-A767-685A06062A39}"), "Windows Phone 8/8.1 Blank/Hub/Webview App" },
            { Guid.Parse("{786C830F-07A1-408B-BD7F-6EE04809D6DB}"), "Portable Class Library" },
            { Guid.Parse("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"), "Windows(Visual C++)" },
            { Guid.Parse("{978C614F-708E-4E1A-B201-565925725DBA}"), "Deployment Setup" },
            { Guid.Parse("{A1591282-1198-4647-A2B1-27E5FF5F6F3B}"), "Silverlight" },
            { Guid.Parse("{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}"), "Universal Windows Class Library" },
            { Guid.Parse("{A860303F-1F3F-4691-B57E-529FC101A107}"), "Visual Studio Tools for Applications(VSTA)" },
            { Guid.Parse("{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}"), "Database" },
            { Guid.Parse("{AB322303-2255-48EF-A496-5904EB18DA55}"), "Deployment Smart Device Cab" },
            { Guid.Parse("{b69e3092-b931-443c-abe7-7e7b65f2a37f}"), "Micro Framework" },
            { Guid.Parse("{BAA0C2D2-18E2-41B9-852F-F413020CAA33}"), "Visual Studio Tools for Office(VSTO)" },
            { Guid.Parse("{BC8A1FFA-BEE3-4634-8014-F334798102B3}"), "Windows Store(Metro) Apps & Components" },
            { Guid.Parse("{BF6F8E12-879D-49E7-ADF0-5503146B24B8}"), "Dynamics 2012 AX C# in AOT" },
            { Guid.Parse("{C089C8C0-30E0-4E22-80C0-CE093F111A43}"), "Windows Phone 8/8.1 App(C#)" },
            { Guid.Parse("{C252FEB5-A946-4202-B1D4-9916A0590387}"), "Visual Database Tools" },
            { Guid.Parse("{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}"), "Legacy(2003) Smart Device(VB.NET)" },
            { Guid.Parse("{D399B71A-8929-442a-A9AC-8BEC78BB2433}"), "XNA(Zune)" },
            { Guid.Parse("{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}"), "Workflow(VB.NET)" },
            { Guid.Parse("{DB03555F-0C8B-43BE-9FF9-57896B3C5E56}"), "Windows Phone 8/8.1 App(VB.NET)" },
            { Guid.Parse("{E24C65DC-7377-472B-9ABA-BC803B73C61A}"), "Web Site" },
            { Guid.Parse("{E3E379DF-F4C6-4180-9B81-6769533ABE47}"), "Model-View-Controller v4(MVC 4)" },
            { Guid.Parse("{E53F8FEA-EAE0-44A6-8774-FFD645390401}"), "Model-View-Controller v3(MVC 3)" },
            { Guid.Parse("{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}"), "J#" },
            { Guid.Parse("{EC05E597-79D4-47f3-ADA0-324C4F7C7484}"), "SharePoint(VB.NET)" },
            { Guid.Parse("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"), "Xamarin.Android" },
            { Guid.Parse("{F135691A-BF7E-435D-8960-F99683D2D49C}"), "Distributed System" },
            { Guid.Parse("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"), "Windows(VB.NET)" },
            { Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279786}"), "F#" },
            { Guid.Parse("{F5B4F3BC-B597-4E2B-B552-EF5D8A32436F}"), "MonoTouch Binding" },
            { Guid.Parse("{F85E285D-A4E0-4152-9332-AB1D724D3325}"), "Model-View-Controller v2(MVC 2)" },
            { Guid.Parse("{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}"), "SharePoint Workflow" },
            { Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"), "Windows(C#)" },
            { Guid.Parse("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}"), "ASP.NET 5" },

            //! Duplicates
            //{ Guid.Parse("{6BC8ED88-2882-458C-8E55-DFD12B67127B}"), "MonoTouch" },
            //{ Guid.Parse("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}"), "C++" },
            //{ Guid.Parse("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"), "Mono for Android" },
            //{ Guid.Parse("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"), "VB.NET" },
            //{ Guid.Parse("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"), "C#" },
            //{ Guid.Parse("{F85E285D-A4E0-4152-9332-AB1D724D3325}"), "ASP.NET MVC 2" },
            //{ Guid.Parse("{E53F8FEA-EAE0-44A6-8774-FFD645390401}"), "ASP.NET MVC 3" },
            //{ Guid.Parse("{E3E379DF-F4C6-4180-9B81-6769533ABE47}"), "ASP.NET MVC 4" },
            //{ Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}"), "ASP.NET MVC 5" },
            //{ Guid.Parse("{349C5851-65DF-11DA-9384-00065B846F21}"), "Web Application" },
        };

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Solution()
        {
            Projects = new List<SolutionProject>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the format to use.
        /// </summary>
        ///
        /// <value>
        /// The format.
        /// </value>
        public Double Format { get; private set; }

        /// <summary>
        /// Gets or sets the minumim vs version.
        /// </summary>
        ///
        /// <value>
        /// The minumim vs version.
        /// </value>
        public Int32 MinumimVSVersion { get; private set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        ///
        /// <value>
        /// The name.
        /// </value>
        public String Name { get; private set; }

        /// <summary>
        /// Gets or sets the projects.
        /// </summary>
        ///
        /// <value>
        /// The projects.
        /// </value>
        public List<SolutionProject> Projects { get; private set; }

        /// <summary>
        /// Gets or sets the full pathname of the file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the file.
        /// </value>
        public String SolutionPath { get; private set; }

        /// <summary>
        /// Gets or sets the vs version.
        /// </summary>
        ///
        /// <value>
        /// The vs version.
        /// </value>
        public Int32 VSVersion { get; private set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Loads.
        /// </summary>
        ///
        /// <param name="solutionFile"> The solution file to load. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public Boolean Load(String solutionFile)
        {
            if (File.Exists(solutionFile))
            {
                SolutionPath = Path.GetFullPath(solutionFile);

                Name = Path.GetFileNameWithoutExtension(solutionFile);

                List<String> content = File.ReadAllLines(solutionFile).ToList();

                while (content.Count > 0)
                {
                    String line = content[0];

                    if (String.IsNullOrEmpty(line))
                    {
                        // Ignore
                    }
                    else if (line.StartsWith("Microsoft Visual Studio Solution File"))
                    {
                        if (Double.TryParse(line.Substring(line.LastIndexOf(' ') + 1), NumberStyles.Number, CultureInfo.InvariantCulture, out Double fmt))
                        {
                            Format = fmt;
                        }
                        else
                        {
                            // Error
                            return false;
                        }
                    }
                    else if (line.StartsWith("VisualStudioVersion"))
                    {
                        if (Version.TryParse(line.Substring(line.LastIndexOf(' ') + 1), out Version ver))
                        {
                            VSVersion = (14 - ver.Major) + 2015;
                        }
                        else
                        {
                            // Error
                            return false;
                        }
                    }
                    else if (line.StartsWith("MinimumVisualStudioVersion"))
                    {
                        if (Version.TryParse(line.Substring(line.LastIndexOf(' ') + 1), out Version ver))
                        {
                            MinumimVSVersion = (14 - ver.Major) + 2015;
                        }
                        else
                        {
                            // Error
                            return false;
                        }
                    }
                    else if (line.StartsWith("Project"))
                    {
                        //! Assumes a single line!
                        Projects.Add(new SolutionProject(line));
                    }
                    //else if (line.StartsWith("EndProject"))
                    //{
                    //    // Ignore
                    //}
                    else
                    {
                        // TODO Tie to iLog.
                        //
                        //Debug.Print($"Ignored line: {line}");
                    }

                    content.RemoveAt(0);
                }

                return true;
            }

            return false;
        }

        #endregion Methods
    }

    /// <summary>
    /// A solution project.
    /// </summary>
    public class SolutionProject
    {
        #region Fields

        // TODO GlobalSection(SolutionConfigurationPlatforms)
        // TODO GlobalSection(ProjectConfigurationPlatforms)
        // TODO GlobalSection(SolutionProperties)
        /// <summary>
        /// The profiles lookup table.
        /// </summary>
        public static Dictionary<String, String> profiles = new Dictionary<String, String>()
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

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="line"> The line. </param>
        public SolutionProject(String line)
        {
            String[] parts = line.Split(',');

            Int32 start = parts[0].IndexOf("{");
            Int32 end = parts[0].IndexOf("}");

            ProjectTypeGuid = Guid.Parse(parts[0]
                .Substring(start, end - start + 1));
            ProjectName = parts[0]
                .Substring("Project(".Length + 1)
                .Split(new String[] { " = " }, StringSplitOptions.RemoveEmptyEntries)[1]
                .Trim('"');
            RelativePath = parts[1]
                .Trim()
                .Trim('"');
            ProjectGuid = Guid.Parse(parts[2]
                .Trim()
                .Trim('"'));
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets a unique identifier of the project.
        /// </summary>
        ///
        /// <value>
        /// Unique identifier of the project.
        /// </value>
        public Guid ProjectGuid { get; private set; }

        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        ///
        /// <value>
        /// The name of the project.
        /// </value>
        public String ProjectName { get; private set; }

        /// <summary>
        /// Gets the type of the project.
        /// </summary>
        ///
        /// <value>
        /// The type of the project.
        /// </value>
        public String ProjectType
        {
            get
            {
                if (Solution.ProjectGuids.ContainsKey(ProjectTypeGuid))
                {
                    return Solution.ProjectGuids[ProjectTypeGuid];
                }
                else
                {
                    return $"Unknown Project Guid: {ProjectTypeGuid}";
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of the project.
        /// </summary>
        ///
        /// <value>
        /// The type of the project.
        /// </value>
        public Guid ProjectTypeGuid { get; private set; }

        /// <summary>
        /// Gets or sets the full pathname of the relative file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the relative file.
        /// </value>
        public String RelativePath { get; private set; }

        #endregion Properties
    }

    /// <summary>
    /// Values that represent severities.
    /// </summary>
    public enum Severity
    {
        Fatal,
        Error,
        Warning,
        Info,
        Debug,
    }

    /// <summary>
    /// A log entry.
    /// </summary>
    public class LogEntry
    {
        public LogEntry()
        {
            // Nothing
        }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        ///
        /// <value>
        /// The level.
        /// </value>
        public Int32 Level { get; set; }

        /// <summary>
        /// Gets or sets the Date/Time of the time stamp.
        /// </summary>
        ///
        /// <value>
        /// The time stamp.
        /// </value>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the result.
        /// </summary>
        ///
        /// <value>
        /// True if result, false if not.
        /// </value>
        public Boolean Result { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        ///
        /// <value>
        /// The message.
        /// </value>
        public String Message { get; set; }

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        ///
        /// <value>
        /// The severity.
        /// </value>
        public Severity Severity { get; set; }
    }
}