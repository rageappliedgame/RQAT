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
    /// Enum extensions.
    /// </summary>
    public static class EnumExtensions
    {
        #region Methods

        /// <summary>
        /// A T extension method that query if 'value' is flag set.
        /// </summary>
        ///
        /// <exception cref="ArgumentException"> Thrown when one or more arguments have unsupported or
        ///                                      illegal values. </exception>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="value"> The value to act on. </param>
        /// <param name="flag">  The flag. </param>
        ///
        /// <returns>
        /// True if flag set, false if not.
        /// </returns>
        public static bool IsFlagSet<T>(this T value, T flag)
            where T : struct
        {
            CheckIsEnum<T>(true);

            UInt64 lValue = Convert.ToUInt64(value);
            UInt64 lFlag = Convert.ToUInt64(flag);

            return (lValue & lFlag) != 0;
        }

        /// <summary>
        /// Check is enum.
        /// </summary>
        ///
        /// <exception cref="ArgumentException"> Thrown when one or more arguments have unsupported or
        ///                                      illegal values. </exception>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="withFlags"> True to with flags. </param>
        private static void CheckIsEnum<T>(bool withFlags)
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof(T).FullName));

            if (withFlags && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute", typeof(T).FullName));
        }

        /// <summary>
        /// Enumerates to values in this collection.
        /// </summary>
        ///
        /// <exception cref="ArgumentException"> Thrown when one or more arguments have unsupported or
        ///                                      illegal values. </exception>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="flags"> The flags to act on. </param>
        ///
        /// <returns>
        /// Flags as an IEnumerable&lt;T&gt;
        /// </returns>
        public static IEnumerable<T> ToValues<T>(this T flags) where T : struct, IConvertible
        {
            CheckIsEnum<T>(true);

            //if (!typeof(T).IsEnum)
            //    throw new ArgumentException("T must be an enumerated type.");

            UInt64 inputInt = Convert.ToUInt64(flags);

            foreach (T value in Enum.GetValues(typeof(T)))
            {
                UInt64 valueInt = Convert.ToUInt64(value);

                if (0 != (valueInt & inputInt))
                {
                    yield return value;
                }
            }
        }

        #endregion Methods
    }
}