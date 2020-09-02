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
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// A queue.
    /// </summary>
    public class Jobs : List<Job>
    {
        /// <summary>
        /// Gets a value indicating whether this object is empty.
        /// </summary>
        ///
        /// <value>
        /// True if this object is empty, false if not.
        /// </value>
        public Boolean IsEmpty
        {
            get
            {
                return this.Count == 0;
            }
        }

        /// <summary>
        /// Adds plugin.
        /// </summary>
        ///
        /// <param name="plugin"> The plugin. </param>
        /// <param name="level">  The level. </param>
        /// <param name="parm">   The parameter. </param>
        public void Add(IPlugin plugin, Int32 level, String parm)
        {
            this.Add(new Job(plugin, level, parm));
        }

        /// <summary>
        /// Gets the get.
        /// </summary>
        ///
        /// <returns>
        /// A Job.
        /// </returns>
        public Job Get()
        {
            Job job = this[0];
            this.RemoveAt(0);
            return job;

            //return this.Dequeue();
        }

        /// <summary>
        /// Query if this object contains the given plugin.
        /// </summary>
        ///
        /// <param name="plugin"> The plugin. </param>
        /// <param name="parm">   The parameter. </param>
        /// <param name="exact">  (Optional) True to perform an exact check (e.g. include level). </param>
        ///
        /// <returns>
        /// True if the object is in this collection, false if not.
        /// </returns>
        public Boolean Contains(IPlugin plugin, String parm, Boolean exact = false)
        {
            switch (exact)
            {
                case true:
                    return this.Count(
                        p => p.Plugin.GetType().FullName.Equals(plugin.GetType().FullName)
                     && p.Parm.Equals(parm)) != 0;
                default:
                    return this.Count(
                        p => p.Plugin.GetType().FullName.Equals(plugin.GetType().FullName)
                     && p.Parm.Equals(parm)) != 0;
            }
        }
    }

    ///
    ///    /// <summary>
    ///    /// A queue item.
    ///    /// </summary>
    public class Job : IJob
    {
        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        private Job()
        {
            //
        }

        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        ///
        /// <param name="plugin"> The plugin. </param>
        /// <param name="level">  The level. </param>
        /// <param name="parm">   The parameter. </param>
        public Job(IPlugin plugin, Int32 level, String parm)
              : this()
        {
            Plugin = plugin;
            Level = level;
            Parm = parm;
        }

        /// <summary>
        /// Gets or sets the plugin.
        /// </summary>
        ///
        /// <value>
        /// The plugin.
        /// </value>
        public IPlugin Plugin
        {
            get; internal set;
        }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        ///
        /// <value>
        /// The level.
        /// </value>
        public Int32 Level
        {
            get; internal set;
        }

        /// <summary>
        /// Gets or sets the parameter.
        /// </summary>
        ///
        /// <value>
        /// The parameter.
        /// </value>
        public String Parm
        {
            get; internal set;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        ///
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Plugin.GetType().Name} - {Path.GetFileName(Parm)}";
        }
    }
}
