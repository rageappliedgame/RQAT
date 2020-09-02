namespace RQAT
{
    using System;
    using System.Collections.Generic;

    public static class Lookups
    {
        /// <summary>
        /// See https://portablelibraryprofiles.stephencleary.com/  
        /// See https://docs.microsoft.com/en-us/nuget/schema/target-frameworks.
        /// </summary>
        public static Dictionary<String, String> Profiles = new Dictionary<String, String>()
        {
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
        /// See https://blog.stephencleary.com/2012/05/framework-profiles-in-net.html.
        /// </summary>
        public static Dictionary<String, String> CoreProfiles = new Dictionary<String, String>()
        {
            { "portable-net45+win8", "netstandard1.1" },
            { "portable-win81+wp81", "netstandard1.0" },
            { "portable-win81+wpa81", "netstandard1.2" },
            { "portable-net451+win81", "netstandard1.2" },
            { "portable-net45+wp8", "netstandard1.0" },
            { "portable-net45+win8+wp8", "netstandard1.0" },
            { "portable-wpa81+wp81", "netstandard1.0" },
            { "portable-net45+win8+wpa81", "netstandard1.1" },
            { "portable-net451+win81+wpa81", "netstandard1.2" },
            { "portable-win81+wpa81+wp81", "netstandard1.0" },
            { "portable-net45+win8+wpa81+wp8", "netstandard1.0" },
        };

        /// <summary>
        /// See https://www.codeproject.com/reference/720512/list-of-visual-studio-project-type-guids.
        /// </summary>
        public static Dictionary<String, String> ProjectTypeGuids = new Dictionary<String, String>()
        {
            { "ASP.NET 5", "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}" },
            { "ASP.NET MVC 1", "{603C0E0B-DB56-11DC-BE95-000D561079B0}" },
            { "ASP.NET MVC 2", "{F85E285D-A4E0-4152-9332-AB1D724D3325}" },
            { "ASP.NET MVC 3", "{E53F8FEA-EAE0-44A6-8774-FFD645390401}" },
            { "ASP.NET MVC 4", "{E3E379DF-F4C6-4180-9B81-6769533ABE47}" },
            { "ASP.NET MVC 5", "{349C5851-65DF-11DA-9384-00065B846F21}" },
            { "C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" },
            { "C++", "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" },
            { "Database","{A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124}" },
            { "Database (other project types)", "{4F174C21-8C12-11D0-8340-0000F80270F8}" },
            { "Deployment Cab", "{3EA9E505-35AC-4774-B492-AD1749C4943A}" },
            { "Deployment Merge Module", "{06A35CCD-C46D-44D5-987B-CF40FF872267}" },
            { "Deployment Setup", "{978C614F-708E-4E1A-B201-565925725DBA}" },
            { "Deployment Smart Device Cab", "{AB322303-2255-48EF-A496-5904EB18DA55}" },
            { "Distributed System", "{F135691A-BF7E-435D-8960-F99683D2D49C}" },
            { "Dynamics 2012 AX C# in AOT", "{BF6F8E12-879D-49E7-ADF0-5503146B24B8}" },
            { "F#", "{F2A71F9B-5D33-465A-A702-920D77279786}" },
            { "J#", "{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}" },
            { "Legacy (2003) Smart Device (C#)", "{20D4826A-C6FA-45DB-90F4-C717570B9F32}" },
            { "Legacy (2003) Smart Device (VB.NET)", "{CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8}" },
            { "Micro Framework", "{b69e3092-b931-443c-abe7-7e7b65f2a37f}" },
            { "Model-View-Controller v2 (MVC 2)", "{F85E285D-A4E0-4152-9332-AB1D724D3325}" },
            { "Model-View-Controller v3 (MVC 3)", "{E53F8FEA-EAE0-44A6-8774-FFD645390401}" },
            { "Model-View-Controller v4 (MVC 4)", "{E3E379DF-F4C6-4180-9B81-6769533ABE47}" },
            { "Model-View-Controller v5 (MVC 5)", "{349C5851-65DF-11DA-9384-00065B846F21}" },
            { "Mono for Android", "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}" },
            { "MonoTouch", "{6BC8ED88-2882-458C-8E55-DFD12B67127B}" },
            { "MonoTouch Binding", "{F5B4F3BC-B597-4E2B-B552-EF5D8A32436F}" },
            { "Portable Class Library", "{786C830F-07A1-408B-BD7F-6EE04809D6DB}" },
            { "Project Folders", "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}" },
            { "SharePoint (C#)", "{593B0543-81F6-4436-BA1E-4747859CAAE2}" },
            { "SharePoint (VB.NET)", "{EC05E597-79D4-47f3-ADA0-324C4F7C7484}" },
            { "SharePoint Workflow", "{F8810EC1-6754-47FC-A15F-DFABD2E3FA90}" },
            { "Silverlight", "{A1591282-1198-4647-A2B1-27E5FF5F6F3B}" },
            { "Smart Device (C#)", "{4D628B5B-2FBC-4AA6-8C16-197242AEB884}" },
            { "Smart Device (VB.NET)", "{68B1623D-7FB9-47D8-8664-7ECEA3297D4F}" },
            { "Solution Folder", "{2150E333-8FDC-42A3-9474-1A3956D46DE8}" },
            { "Test", "{3AC096D0-A1C2-E12C-1390-A8335801FDAB}" },
            { "Universal Windows Class Library", "{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}" },
            { "VB.NET", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" },
            { "Visual Database Tools", "{C252FEB5-A946-4202-B1D4-9916A0590387}" },
            { "Visual Studio 2015 Installer Project Extension", "{54435603-DBB4-11D2-8724-00A0C9A8B90C}" },
            { "Visual Studio Tools for Applications (VSTA)", "{A860303F-1F3F-4691-B57E-529FC101A107}" },
            { "Visual Studio Tools for Office (VSTO)", "{BAA0C2D2-18E2-41B9-852F-F413020CAA33}" },
            { "Web Application", "{349C5851-65DF-11DA-9384-00065B846F21}" },
            { "Web Site", "{E24C65DC-7377-472B-9ABA-BC803B73C61A}" },
            { "Windows (C#)", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" },
            { "Windows (VB.NET)", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}" },
            { "Windows (Visual C++)", "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}" },
            { "Windows Communication Foundation (WCF)", "{3D9AD99F-2412-4246-B90B-4EAA41C64699}" },
            { "Windows Phone 8/8.1 Blank/Hub/Webview App", "{76F1466A-8B6D-4E39-A767-685A06062A39}" },
            { "Windows Phone 8/8.1 App (C#)", "{C089C8C0-30E0-4E22-80C0-CE093F111A43}" },
            { "Windows Phone 8/8.1 App (VB.NET)", "{DB03555F-0C8B-43BE-9FF9-57896B3C5E56}" },
            { "Windows Presentation Foundation (WPF)", "{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}" },
            { "Windows Store (Metro) Apps & Components", "{BC8A1FFA-BEE3-4634-8014-F334798102B3}" },
            { "Workflow (C#)", "{14822709-B5A1-4724-98CA-57A101D1B079}" },
            { "Workflow (VB.NET)", "{D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8}" },
            { "Workflow Foundation", "{32F31D43-81CC-4C15-9DE6-3FC5453562B6}" },
            { "Xamarin.Android", "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}" },
            { "Xamarin.iOS", "{6BC8ED88-2882-458C-8E55-DFD12B67127B}" },
            { "XNA (Windows)", "{6D335F3A-9D43-41b4-9D22-F6F17C4BE596}" },
            { "XNA (XBox)", "{2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2}" },
            { "XNA (Zune)", "{D399B71A-8929-442a-A9AC-8BEC78BB2433}" },
        };
    }
}

