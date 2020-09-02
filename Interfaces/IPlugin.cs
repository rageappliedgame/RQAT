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

    #region Enumerations

    /// <summary>
    /// Values that represent project file types.
    /// </summary>
    [Flags]
    public enum FileType
    {
        Solution = 0x01,
        Project = 0x02,
        Assembly = 0x04,
        UnitTest = 0x08,
        Executable = 0x10,
#warning Add WinExe as well?
    }

    /// <summary>
    /// Values that represent maturities.
    /// </summary>
    public enum Maturity
    {
        alpha,
        beta,
        release,
    }

    #endregion Enumerations

    /// <summary>
    /// Interface for plugin.
    /// </summary>
    public interface IPlugin
    {
        #region Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        ///
        /// <value>
        /// The name.
        /// </value>
        String Name { get; }

        /// <summary>
        /// Gets the maturity.
        /// </summary>
        ///
        /// <value>
        /// The maturity.
        /// </value>
        Maturity Maturity { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        ///
        /// <value>
        /// The version.
        /// </value>
        Version Version { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        ///
        /// <value>
        /// The description.
        /// </value>
        String Description { get; }

        /// <summary>
        /// Gets a value indicating whether this object is leaf.
        /// </summary>
        ///
        /// <value>
        /// True if this object is leaf, false if not.
        /// </value>
        Boolean IsLeaf { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Executes the Core of the Plugin.
        /// </summary>
        ///
        /// <param name="level"> The execution nesting level. </param>
        /// <param name="parm">  The parameter. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        Boolean Execute(IJob job);

        /// <summary>
        /// Initializes this Plugin.
        /// </summary>
        ///
        /// <param name="host">  (Optional) The host. </param>
        void Initialize(IHost host = null);

        /// <summary>
        /// Checks for Support.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        Boolean Supports(String parm);

        #endregion Methods
    }
}