namespace RQAT
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using static RQAT.Utils;

    /// <summary>
    /// A base project plugin.
    /// </summary>
    public class BaseProjectPlugin
    {
        /// <summary>
        /// The host.
        /// </summary>
        protected IHost host;

        /// <summary>
        /// Type of the output.
        /// </summary>
        protected OutputType outputType = OutputType.Unknown;

        /// <summary>
        /// The output file.
        /// </summary>
        protected String outputFile = String.Empty;

        /// <summary>
    }
}
