namespace RQAT
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// An Assert base class mock.
    /// </summary>
    public static partial class Assert
    {
        #region Fields

        /// <summary>
        /// The indent level.
        /// </summary>
        public static Int32 lvl = 2;

        #endregion Fields

        #region Properties

        /// <summary>
        /// The host.
        /// </summary>
        public static IHost Host { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Reports a fail.
        /// </summary>
        ///
        /// <param name="method">     The method. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        private static void ReportFail(String message, params object[] parameters)
        {
            //! Simple, might not work (perfect) in all cases.
            //
            String str = String.IsNullOrEmpty(message)
                ? String.Empty
                : String.Format(message, parameters);

            Report(message, Severity.Warning);
        }

        /// <summary>
        /// Reports a fail.
        /// </summary>
        ///
        /// <param name="message"> The message. </param>
        private static void ReportFail(String message)
        {
            Report(message, Severity.Warning);
        }

        private static void Report(String message, Severity severity)
        {
            StackTrace stackTrace = new StackTrace(false);

            int ndx = stackTrace.GetFrames()
                .ToList()
                .FindLastIndex(p => p.GetMethod().DeclaringType.Name.Equals(typeof(Assert).Name));

            List<String> names = stackTrace.GetFrame(ndx).GetMethod().GetParameters().Select(p => p.Name).ToList();
            List<String> types = stackTrace.GetFrame(ndx).GetMethod().GetParameters().Select(p => p.ParameterType.Name).ToList();

            List<String> parms = new List<String>();
            for (Int32 i = 0; i < names.Count; i++)
            {
                parms.Add($"{types[i]} {names[i]}");
            }

            String assertMethod = stackTrace.GetFrame(ndx).GetMethod().Name;
            String testMethod = stackTrace.GetFrame(ndx + 1).GetMethod().Name;
            Int32 testLine = stackTrace.GetFrame(ndx + 1).GetFileLineNumber();

            Host.AddResult(severity, false, $"{assertMethod}({String.Join(", ", parms)}) Assertion failure in {testMethod}():[{testLine}]{(String.IsNullOrEmpty(message) ? String.Empty : $" - {message}")}".TrimEnd('-', ' '), lvl);
        }

        #endregion Methods
    }

    /// <summary>
    /// An Assert AreEqual class mock.
    /// </summary>
    public static partial class Assert
    {
        #region Methods

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        public static void AreEqual<T>(T expected, T actual)
        {
            AreEqual<T>(expected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreEqual<T>(T expected, T actual, string message)
        {
            AreEqual<T>(expected, actual, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual<T>(T expected, T actual, string message, params object[] parameters)
        {
            if (!object.Equals(expected, actual))
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> An object to process. </param>
        /// <param name="actual"> An object to process. </param>
        public static void AreEqual(object expected, object actual)
        {
            AreEqual(expected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreEqual(object expected, object actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual(object expected, object actual, string message, params object[] parameters)
        {
            AreEqual<object>(expected, actual, message, parameters);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        public static void AreEqual(float expected, float actual, float delta)
        {
            AreEqual(expected, actual, delta, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        /// <param name="message">  The message. </param>
        public static void AreEqual(float expected, float actual, float delta, string message)
        {
            AreEqual(expected, actual, delta, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="delta">      The delta. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual(float expected, float actual, float delta, string message, params object[] parameters)
        {
            if (float.IsNaN(expected) || float.IsNaN(actual) || float.IsNaN(delta))
            {
                ReportFail(message, parameters);
            }
            else if (Math.Abs(expected - actual) > delta)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        public static void AreEqual(double expected, double actual, double delta)
        {
            AreEqual(expected, actual, delta, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        /// <param name="message">  The message. </param>
        public static void AreEqual(double expected, double actual, double delta, string message)
        {
            AreEqual(expected, actual, delta, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="delta">      The delta. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] parameters)
        {
            if (double.IsNaN(expected) || double.IsNaN(actual) || double.IsNaN(delta))
            {
                ReportFail(message, parameters);
            }
            else if (Math.Abs(expected - actual) > delta)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase)
        {
            AreEqual(expected, actual, ignoreCase, CultureInfo.InvariantCulture, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="message">    The message. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message)
        {
            AreEqual(expected, actual, ignoreCase, CultureInfo.InvariantCulture, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            AreEqual(expected, actual, ignoreCase, CultureInfo.InvariantCulture, message, parameters);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="culture">    The culture. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture)
        {
            AreEqual(expected, actual, ignoreCase, culture, String.Empty, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="culture">    The culture. </param>
        /// <param name="message">    The message. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            AreEqual(expected, actual, ignoreCase, culture, message, null);
        }

        /// <summary>
        /// Are equal.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="culture">    The culture. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreEqual(string expected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            if (culture == null)
            {
                ReportFail("CultureInfo parameter not valid.");
            }
            else if (String.Compare(expected, actual, ignoreCase, culture) != 0)
            {
                ReportFail(message, parameters);
            }
        }

        #endregion Methods
    }

    /// <summary>
    /// An Assert AreNoEqual class mock.
    /// </summary>
    public static partial class Assert
    {
        #region Methods

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        public static void AreNotEqual<T>(T notExpected, T actual)
        {
            AreNotEqual<T>(notExpected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreNotEqual<T>(T notExpected, T actual, string message)
        {
            AreNotEqual<T>(notExpected, actual, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual<T>(T notExpected, T actual, string message, params object[] parameters)
        {
            if (object.Equals(notExpected, actual))
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> An object to process. </param>
        /// <param name="actual"> An object to process. </param>
        public static void AreNotEqual(object notExpected, object actual)
        {
            AreNotEqual(notExpected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreNotEqual(object notExpected, object actual, string message)
        {
            AreNotEqual(notExpected, actual, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual(object notExpected, object actual, string message, params object[] parameters)
        {
            AreNotEqual<object>(notExpected, actual, message, parameters);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        public static void AreNotEqual(float notExpected, float actual, float delta)
        {
            AreNotEqual(notExpected, actual, delta, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        /// <param name="message">  The message. </param>
        public static void AreNotEqual(float notExpected, float actual, float delta, string message)
        {
            AreNotEqual(notExpected, actual, delta, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="delta">      The delta. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual(float notExpected, float actual, float delta, string message, params object[] parameters)
        {
            if (Math.Abs(notExpected - actual) <= delta)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        public static void AreNotEqual(double notExpected, double actual, double delta)
        {
            AreNotEqual(notExpected, actual, delta, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="delta">    The delta. </param>
        /// <param name="message">  The message. </param>
        public static void AreNotEqual(double notExpected, double actual, double delta, string message)
        {
            AreNotEqual(notExpected, actual, delta, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="delta">      The delta. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual(double notExpected, double actual, double delta, string message, params object[] parameters)
        {
            if (Math.Abs(notExpected - actual) <= delta)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase)
        {
            AreNotEqual(notExpected, actual, ignoreCase, CultureInfo.InvariantCulture, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="message">    The message. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message)
        {
            AreNotEqual(notExpected, actual, ignoreCase, CultureInfo.InvariantCulture, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, string message, params object[] parameters)
        {
            AreNotEqual(notExpected, actual, ignoreCase, CultureInfo.InvariantCulture, message, parameters);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="culture">    The culture. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture)
        {
            AreNotEqual(notExpected, actual, ignoreCase, culture, String.Empty, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">      The actual. </param>
        /// <param name="ignoreCase">  True to ignore case. </param>
        /// <param name="culture">     The culture. </param>
        /// <param name="message">     The message. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message)
        {
            AreNotEqual(notExpected, actual, ignoreCase, culture, message, null);
        }

        /// <summary>
        /// Are not equal.
        /// </summary>
        ///
        /// <param name="notExpected">   The notExpected. </param>
        /// <param name="actual">     The actual. </param>
        /// <param name="ignoreCase"> True to ignore case. </param>
        /// <param name="culture">    The culture. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreNotEqual(string notExpected, string actual, bool ignoreCase, CultureInfo culture, string message, params object[] parameters)
        {
            if (culture == null)
            {
                ReportFail("CultureInfo parameter not valid.");
            }
            else if (String.Compare(notExpected, actual, ignoreCase, culture) == 0)
            {
                ReportFail(message, parameters);
            }
        }

        #endregion Methods
    }

    /// <summary>
    /// An Assert AreNotSame class mock.
    /// </summary>
    public static partial class Assert
    {
        #region Methods

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="notExpected"> An object to process. </param>
        /// <param name="actual"> An object to process. </param>
        public static void AreNotSame(object notExpected, object actual)
        {
            AreNotSame(notExpected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="notExpected"> The notExpected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreNotSame(object notExpected, object actual, string message)
        {
            AreNotSame(notExpected, actual, message, null);
        }

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="notExpected"> An object to process. </param>
        /// <param name="actual">      An object to process. </param>
        /// <param name="message">     The message. </param>
        /// <param name="parameters">  A variable-length parameters list containing parameters. </param>
        public static void AreNotSame(object notExpected, object actual, string message, params object[] parameters)
        {
            if (notExpected == actual)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="expected"> An object to process. </param>
        /// <param name="actual">   An object to process. </param>
        public static void AreSame(object expected, object actual)
        {
            AreSame(expected, actual, String.Empty, null);
        }

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="expected"> The expected. </param>
        /// <param name="actual">   The actual. </param>
        /// <param name="message">  The message. </param>
        public static void AreSame(object expected, object actual, string message)
        {
            AreSame(expected, actual, message, null);
        }

        /// <summary>
        /// Are not same.
        /// </summary>
        ///
        /// <param name="expected">   The expected. </param>
        /// <param name="actual">     An object to process. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void AreSame(object expected, object actual, string message, params object[] parameters)
        {
            if (expected != actual)
            {
                ReportFail(message, parameters);
            }
        }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        /// <summary>
        /// Tests if two object objects are considered equal.
        /// </summary>
        ///
        /// <param name="a"> Object to be compared. </param>
        /// <param name="b"> Object to be compared. </param>
        ///
        /// <returns>
        /// True if the objects are considered equal, false if they are not.
        /// </returns>
        public static bool Equals(System.Object a, System.Object b)
        {
            Report("The Asset.Equal() methode is obsolete and should not be used.", Severity.Error);

            return a == b;
        }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        /// <summary>
        /// Fails.
        /// </summary>
        public static void Fail()
        {
            Fail(String.Empty, null);
        }

        /// <summary>
        /// Fails.
        /// </summary>
        ///
        /// <param name="message"> The message. </param>
        public static void Fail(string message)
        {
            Fail(message, null);
        }

        /// <summary>
        /// Fails.
        /// </summary>
        ///
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void Fail(string message, params object[] parameters)
        {
            ReportFail(message, parameters);
        }

        /// <summary>
        /// Inconclusives this object.
        /// </summary>
        public static void Inconclusive()
        {
            Inconclusive(String.Empty, null);
        }

        /// <summary>
        /// Inconclusives this object.
        /// </summary>
        ///
        /// <param name="message"> The message. </param>
        public static void Inconclusive(string message)
        {
            Inconclusive(message, null);
        }

        /// <summary>
        /// Inconclusives this object.
        /// </summary>
        ///
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void Inconclusive(string message, params object[] parameters)
        {
            ReportFail(message, parameters);
        }

        /// <summary>
        /// Is false.
        /// </summary>
        ///
        /// <param name="condition"> True to condition. </param>
        public static void IsFalse(bool condition)
        {
            IsFalse(condition, string.Empty, null);
        }

        /// <summary>
        /// Is false.
        /// </summary>
        ///
        /// <param name="condition"> True to condition. </param>
        /// <param name="message">   The message. </param>
        public static void IsFalse(bool condition, string message)
        {
            IsFalse(condition, message, null);
        }

        /// <summary>
        /// Is false.
        /// </summary>
        ///
        /// <param name="condition">  True to condition. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void IsFalse(bool condition, string message, params object[] parameters)
        {
            if (condition)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">        The value. </param>
        /// <param name="expectedType"> Type of the expected. </param>
        public static void IsInstanceOfType(object value, Type expectedType)
        {
            IsInstanceOfType(value, expectedType, String.Empty, null);
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">        The value. </param>
        /// <param name="expectedType"> Type of the expected. </param>
        /// <param name="message">      The message. </param>
        public static void IsInstanceOfType(object value, Type expectedType, string message)
        {
            IsInstanceOfType(value, expectedType, message, null);
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">        The value. </param>
        /// <param name="expectedType"> Type of the expected. </param>
        /// <param name="message">      The message. </param>
        /// <param name="parameters">   A variable-length parameters list containing parameters. </param>
        public static void IsInstanceOfType(object value, Type expectedType, string message, params object[] parameters)
        {
            if (expectedType == null)
            {
                ReportFail("Expected Type parameter not valid.");
            }

            if (!expectedType.IsInstanceOfType(value))
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">     The value. </param>
        /// <param name="wrongType"> Type of the expected. </param>
        public static void IsNotInstanceOfType(object value, Type wrongType)
        {
            IsNotInstanceOfType(value, wrongType, String.Empty, null);
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">     The value. </param>
        /// <param name="wrongType"> Type of the expected. </param>
        /// <param name="message">   The message. </param>
        public static void IsNotInstanceOfType(object value, Type wrongType, string message)
        {
            IsNotInstanceOfType(value, wrongType, message, null);
        }

        /// <summary>
        /// Is instance of.
        /// </summary>
        ///
        /// <param name="value">      The value. </param>
        /// <param name="wrongType">  Type of the expected. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void IsNotInstanceOfType(object value, Type wrongType, string message, params object[] parameters)
        {
            if (wrongType == null)
            {
                ReportFail("Wrong Type parameter not valid.");
            }

            if (value != null && wrongType.IsInstanceOfType(value))
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Is not null.
        /// </summary>
        ///
        /// <param name="value"> The value. </param>
        public static void IsNotNull(object value)
        {
            IsNotNull(value, string.Empty, null);
        }

        /// <summary>
        /// Is not null.
        /// </summary>
        ///
        /// <param name="value">   The value. </param>
        /// <param name="message"> The message. </param>
        public static void IsNotNull(object value, string message)
        {
            IsNotNull(value, message, null);
        }

        /// <summary>
        /// Is not null.
        /// </summary>
        ///
        /// <param name="value">      The value. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void IsNotNull(object value, string message, params object[] parameters)
        {
            if (value == null)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Is null.
        /// </summary>
        ///
        /// <param name="value"> The value. </param>
        public static void IsNull(object value)
        {
            IsNull(value, string.Empty, null);
        }

        /// <summary>
        /// Is null.
        /// </summary>
        ///
        /// <param name="value">   The value. </param>
        /// <param name="message"> The message. </param>
        public static void IsNull(object value, string message)
        {
            IsNull(value, message, null);
        }

        /// <summary>
        /// Is null.
        /// </summary>
        ///
        /// <param name="value">      The value. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void IsNull(object value, string message, params object[] parameters)
        {
            if (value != null)
            {
                ReportFail(message, parameters);
            }
        }

        /// <summary>
        /// Is true.
        /// </summary>
        ///
        /// <param name="condition"> True to condition. </param>
        public static void IsTrue(bool condition)
        {
            IsTrue(condition, string.Empty, null);
        }

        /// <summary>
        /// Is true.
        /// </summary>
        ///
        /// <param name="condition"> True to condition. </param>
        /// <param name="message">   The message. </param>
        public static void IsTrue(bool condition, string message)
        {
            IsTrue(condition, message, null);
        }

        /// <summary>
        /// Is true.
        /// </summary>
        ///
        /// <param name="condition">  True to condition. </param>
        /// <param name="message">    The message. </param>
        /// <param name="parameters"> A variable-length parameters list containing parameters. </param>
        public static void IsTrue(bool condition, string message, params object[] parameters)
        {
            if (!condition)
            {
                ReportFail(message, parameters);
            }
        }

        #endregion Methods
    }
}