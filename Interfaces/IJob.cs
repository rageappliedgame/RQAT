namespace RQAT
{
    using System;

    /// <summary>
    /// Interface for job.
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Gets the level.
        /// </summary>
        ///
        /// <value>
        /// The level.
        /// </value>
        Int32 Level
        {
            get;
        }

        /// <summary>
        /// Gets or sets the plugin.
        /// </summary>
        ///
        /// <value>
        /// The plugin.
        /// </value>
        IPlugin Plugin
        {
            get;
        }

        /// <summary>
        /// Gets or sets the parameter.
        /// </summary>
        ///
        /// <value>
        /// The parameter.
        /// </value>
        String Parm
        {
            get;
        }
    }
}
