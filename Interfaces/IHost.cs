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

    /// <summary>
    /// Interface for host.
    /// </summary>
    public interface IHost
    {
        #region Methods

        /// <summary>
        /// Plugins for file type.
        /// </summary>
        ///
        /// <param name="type"> The type. </param>
        ///
        /// <returns>
        /// A List&lt;IPlugin&gt;
        /// </returns>
        List<IPlugin> PluginsForFileType(FileType type);

        /// <summary>
        /// Recurse for type.
        /// </summary>
        ///
        /// <param name="job">      The calling plugin. </param>
        /// <param name="types">    The type. </param>
        /// <param name="filename"> Filename of the file. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        Boolean RecurseForTypes(IJob job, FileType types, String filename);

        /// <summary>
        /// Gets the results.
        /// </summary>
        ///
        /// <value>
        /// The results.
        /// </value>
        List<LogEntry> Results { get; }

        /// <summary>
        /// Adds a result.
        /// </summary>
        ///
        /// <param name="severity"> The severity. </param>
        /// <param name="result">   True to result. </param>
        /// <param name="message">  The message. </param>
        /// <param name="level">    (Optional) The level. </param>
        void AddResult(Severity severity, Boolean result, String message, Int32 level = 0);

        /// <summary>
        /// Gets the full pathname of the vs 2017 file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the vs 2017 file.
        /// </value>
        String VS2017Path
        {
            get;
        }

        /// <summary>
        /// Gets the full pathname of the milliseconds build file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the milliseconds build file.
        /// </value>
        String MSBuildPath
        {
            get;
        }

        /// <summary>
        /// Gets the git paths.
        /// </summary>
        ///
        /// <value>
        /// The git paths.
        /// </value>
        List<String> GitPaths
        {
            get;
        }

        /// <summary>
        /// Gets the full pathname of the git file.
        /// </summary>
        ///
        /// <value>
        /// The full pathname of the git file.
        /// </value>
        String GitPath
        {
            get;
        }

        /// <summary>
        /// Gets the nu get.
        /// </summary>
        ///
        /// <value>
        /// The nu get.
        /// </value>
        String NuGetPath
        {
            get;
        }

        ///// <summary>
        ///// Gets the work (temp) dir.
        ///// </summary>
        /////
        ///// <value>
        ///// The work (temp) dir.
        ///// </value>
        //String WorkDir
        //{
        //    get;
        //}

        #endregion Methods
    }
}