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
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Pdb;

    /// <summary>
    /// A project reader plugin.
    /// </summary>
    [Export]
    [Export(typeof(ITestPlugin))]
    [Export(typeof(IPlugin))]
    public class TestPlugin : ITestPlugin
    {
        #region Fields

        /// <summary>
        /// The current job.
        /// </summary>
        private IJob currentJob;

        /// <summary>
        /// The host.
        /// </summary>
        private IHost host;

        /// <summary>
        /// The tests.
        /// </summary>
        private TestClasses testAsm = new TestClasses();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TestPlugin()
        {
            // Nothing
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the description.
        /// </summary>
        ///
        /// <value>
        /// The description.
        /// </value>
        public string Description => "This plugin is work in progress, should load the assembly and run setup/test/teardown cycles.";

        /// <summary>
        /// Gets a value indicating whether this object is leaf.
        /// </summary>
        ///
        /// <value>
        /// True if this object is leaf, false if not.
        /// </value>
        public bool IsLeaf => true;

        /// <summary>
        /// Gets or sets the maturity.
        /// </summary>
        ///
        /// <value>
        /// The maturity.
        /// </value>
        public Maturity Maturity
        {
            get
            {
                return Maturity.alpha;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        ///
        /// <value>
        /// The name.
        /// </value>
        public String Name
        {
            get
            {
                return "Test Project Runner";
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        ///
        /// <value>
        /// The version.
        /// </value>
        public Version Version => new Version(1, 0, 0, 0);

        #endregion Properties

        #region Methods

        /// <summary>
        /// Executes.
        /// </summary>
        ///
        /// <param name="job"> The parameter. </param>
        ///
        /// <returns>
        /// A Dictionary&lt;string,bool&gt;
        /// </returns>
        public Boolean Execute(IJob job)
        {
            currentJob = job;

            host.AddResult(Severity.Debug, true, $"{GetType().Name}.Execute('{job.Parm}')");

            host.AddResult(Severity.Info, true, $"[Examining and Executing Unit Test in {Path.GetFileNameWithoutExtension(job.Parm)} Project]");

            Utils.TestTypes tt = Utils.TestTypes.Unknown;

            if (File.Exists(job.Parm))
            {
                host.AddResult(Severity.Info, true, $"'{job.Parm}' exists.");

                //! Only needed for Microsoft Test.
                List<String> Unpatched = new List<String>();

                //! Only needed for Microsoft Test.
                AssemblyDefinition assertAsmDef = AssemblyDefinition.ReadAssembly(GetType().Assembly.Location);

                AssemblyDefinition testAsmDef = AssemblyDefinition.ReadAssembly(job.Parm);

                //! ------------------------------------------------------------
                //! Enumerate and process all classes with a TestClassAttribute.
                //! ------------------------------------------------------------
                // 
                foreach (TypeDefinition td in testAsmDef.MainModule.Types)
                {
                    //! ------------------------------------------------------------
                    //! Microsoft Test.
                    // 
                    //! Needs a fixup for references to the Assert class that resides in a pair of non-distributable assemblies.
                    //! Here the references are changed to a Assert class defined in RQAT. 
                    // 
                    // [TestClass]
                    // [TestInitialize]
                    // [TestMethod]
                    // [TestCleanup]
                    //! ------------------------------------------------------------
                    // 
                    if (td.CustomAttributes.Any(q => q.AttributeType.Name.Equals("TestClassAttribute")) ||
                        td.Methods.Any(q => q.CustomAttributes.Any(p => p.AttributeType.Name.Equals("TestMethodAttribute"))))
                    {
                        tt = Utils.TestTypes.Microsoft;

                        //! ------------------------------------------------------------
                        //! Weave Start...
                        //! ------------------------------------------------------------
                        //
                        //! https://www.simplethread.com/static-method-interception-in-net-with-c-and-monocecil/
                        //
                        //! Patch all methods where the Assert class is used (so these end up in this code).
                        //
                        foreach (MethodDefinition foundMethod in td.Methods.OfType<MethodDefinition>())
                        {
                            //! https://stackoverflow.com/questions/25077830/replace-references-to-a-type-namespace-using-mono-cecil
                            //! https://blog.elishalom.com/2012/02/04/monitoring-execution-using-mono-cecil/
                            //! https://gist.github.com/7H3LaughingMan/311662c07b8bf8f8d2c6
                            //
                            ILProcessor ilProcessor = foundMethod.Body.GetILProcessor();

                            Debug.WriteLine($"\r\n-----------------");
                            Debug.WriteLine($"Weaving: {foundMethod.Name}");

                            //! Examine and patch Call statements to the Visual Studio UnitTesting.Assert class.
                            // 
                            for (Int32 pc = 0; pc < ilProcessor.Body.Instructions.Count; pc++)
                            {
                                Instruction il = ilProcessor.Body.Instructions[pc];

                                if (il.OpCode.Code == Code.Call)
                                {
                                    Debug.WriteLine($"Examening: {il.OpCode.Code} {il.Operand}");

                                    //! Methods with generics need special handling.
                                    //
                                    if (il.Operand is GenericInstanceMethod && ((GenericInstanceMethod)il.Operand).DeclaringType.FullName.Equals("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
                                    {
                                        Debug.WriteLine(il.Operand.ToString());
                                        GenericInstanceMethod gim = ((GenericInstanceMethod)il.Operand);

                                        Debug.WriteLine($"Method: {gim.Name}");
                                        Debug.WriteLine($"Signature: {gim}");

                                        //! Check if there is a matching non-generic method.
                                        //
                                        MethodDefinition md = FindReplacementMethod(assertAsmDef, gim.ToString());

                                        if (md != null)
                                        {
                                            //! Replace generic method call by a non generic one.
                                            //
                                            Debug.WriteLine($"Replacement Definition: {md}");

                                            //! Find MethodInfo from MethodDefinition by matching MetadataToken value.
                                            //
                                            MethodInfo mi = typeof(Assert).GetMethods().First(p => p.MetadataToken == md.MetadataToken.ToInt32());
                                            Debug.WriteLine($"Replacement Info: {mi}");

                                            MethodReference mri = testAsmDef.MainModule.ImportReference(mi);
                                            Debug.WriteLine($"Replacement Import: {mri}");

                                            //! Patch.
                                            //
                                            ilProcessor.Replace(foundMethod.Body.Instructions[pc], ilProcessor.Create(OpCodes.Call, mri));
                                        }
                                        else
                                        {
                                            //! Check if there is a matching generic method.
                                            //
                                            md = FindGenericReplacementMethod(assertAsmDef, gim.ToString());

                                            if (md != null)
                                            {
                                                //! Replace generic method call by a generic one.
                                                //
                                                Debug.WriteLine($"Replacement Definition: {md}");

                                                //! Find MethodInfo from MethodDefinition by matching MetadataToken value.
                                                //
                                                MethodInfo mi = typeof(Assert).GetMethods().First(p => p.MetadataToken == md.MetadataToken.ToInt32());
                                                Debug.WriteLine($"Replacement Info: {mi}");

                                                //! Adjust Generic method to match the one to be replaced.
                                                //
                                                GenericInstanceMethod mri = new GenericInstanceMethod(testAsmDef.MainModule.ImportReference(mi));
                                                foreach (TypeReference tr in gim.GenericArguments)
                                                {
                                                    mri.GenericArguments.Add(tr);
                                                }

                                                Debug.WriteLine($"Replacement Import: {mri}");

                                                //! Patch.
                                                //
                                                ilProcessor.Replace(foundMethod.Body.Instructions[pc], ilProcessor.Create(OpCodes.Call, mri));

                                            }
                                            else
                                            {
                                                if (!Unpatched.Contains(foundMethod.Name))
                                                {
                                                    Unpatched.Add(foundMethod.Name);
                                                }
                                                Debug.WriteLine($"Failed to weave: {gim}");
                                            }
                                        }
                                    }
                                    else if (il.Operand is MethodReference && ((MethodReference)il.Operand).DeclaringType.FullName.Equals("Microsoft.VisualStudio.TestTools.UnitTesting.Assert"))
                                    {
                                        Debug.WriteLine(il.Operand.ToString());
                                        MethodReference mr = ((MethodReference)il.Operand);

                                        Debug.WriteLine($"Method: {mr.Name}");
                                        Debug.WriteLine($"Signature: {mr}");

                                        //! Check if there is a matching non-generic method.
                                        //
                                        MethodDefinition md = FindReplacementMethod(assertAsmDef, mr.ToString());

                                        if (md != null)
                                        {
                                            //! Replace non-generic method call by a non-generic one.
                                            //
                                            Debug.WriteLine($"Replacement Definition: {md}");

                                            //! Find MethodInfo from MethodDefinition by matching MetadataToken value.
                                            //
                                            MethodInfo mi = typeof(Assert).GetMethods().First(p => p.MetadataToken == md.MetadataToken.ToInt32());
                                            Debug.WriteLine($"Replacement Info: {mi}");

                                            MethodReference mri = testAsmDef.MainModule.ImportReference(mi);
                                            Debug.WriteLine($"Replacement Import: {mri}");

                                            //! Patch.
                                            //
                                            ilProcessor.Replace(foundMethod.Body.Instructions[pc], ilProcessor.Create(OpCodes.Call, mri));
                                        }
                                        else
                                        {
                                            if (!Unpatched.Contains(foundMethod.Name))
                                            {
                                                Unpatched.Add(foundMethod.Name);
                                            }
                                            Debug.WriteLine($"Failed to weave: {mr}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (td.CustomAttributes.Any(q => q.AttributeType.Name.Equals("TestFixtureAttribute")) ||
                             td.Methods.Any(q => q.CustomAttributes.Any(p => p.AttributeType.Name.Equals("TestAttribute"))))
                    {
                        tt = Utils.TestTypes.Nunit;

                    }

                }

                //! ------------------------------------------------------------
                //! Finish up and save the resulting Weaved Assembly.
                //! ------------------------------------------------------------
                // 
                //https://stackoverflow.com/questions/13499384/is-it-possible-to-debug-assemblies-compiled-with-mono-xbuild-with-visual-studi

                switch (tt)
                {
                    case Utils.TestTypes.Microsoft:
                        {
                            CustomAttribute debuggableAttribute = new CustomAttribute(testAsmDef.MainModule.ImportReference(
                                    typeof(DebuggableAttribute).GetConstructor(new[] { typeof(bool), typeof(bool) })));

                            debuggableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
                                testAsmDef.MainModule.ImportReference(typeof(bool)), true));

                            debuggableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
                                testAsmDef.MainModule.ImportReference(typeof(bool)), true));

                            // !Only one DebuggableAttribute is allowed, so replace an existing one.

                            Int32 ndx = testAsmDef.CustomAttributes.ToList().FindIndex(p => p.AttributeType.Name.Equals("DebuggableAttribute"));
                            if (ndx != -1)
                            {
                                testAsmDef.CustomAttributes.RemoveAt(ndx);
                            }
                            testAsmDef.CustomAttributes.Add(debuggableAttribute);

                            //!Remove references to Visual Studio Assembly.
                            //!Removing leads to failing to enumerate the test methods further along.
                            //ndx = assemblyDef.MainModule.AssemblyReferences.ToList().FindIndex(p => p.Name.Equals("Microsoft.VisualStudio.QualityTools.UnitTestFramework"));
                            //if (ndx != -1)
                            //{
                            //    assemblyDef.MainModule.AssemblyReferences.RemoveAt(ndx);
                            //}

                            //! Create a method call:
                            // See https://stackoverflow.com/questions/35948733/mono-cecil-method-and-instruction-insertion
                            //
                            testAsmDef.Write(Path.Combine(Path.GetDirectoryName(job.Parm), "weaved.dll"),
                                new WriterParameters()
                                {
                                    SymbolWriterProvider = new PdbWriterProvider(),
                                    WriteSymbols = true
                                });

                            //! ------------------------------------------------------------
                            //! Weave End
                            //! ------------------------------------------------------------
                        }
                        break;

                    case Utils.TestTypes.Nunit:
                        {
                            File.Copy(job.Parm, Path.Combine(Path.GetDirectoryName(job.Parm), "weaved.dll"));
                        }
                        break;

                    default:
                        return false;
                }

                String dll = Path.Combine(Path.GetDirectoryName(job.Parm), "weaved.dll");
                String pdb = Path.ChangeExtension(dll, ".pdb");

                //AppDomain currentDomain = AppDomain.CurrentDomain;
                //currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);
                //tests.TestAssembly = Assembly.Load(File.ReadAllBytes(dll), File.ReadAllBytes(pdb));

                testAsm.TestAssembly = Assembly.LoadFrom(dll);

                host.AddResult(Severity.Info, true, $"{tt} test class found in {Path.GetFileNameWithoutExtension(job.Parm)} assembly.", 1);

                //! ------------------------------------------------------------
                //! Enumerate all patched tests and build a list.
                //! ------------------------------------------------------------
                //
                foreach (Type type in testAsm.TestAssembly.GetTypes())
                {
                    switch (tt)
                    {
                        case Utils.TestTypes.Microsoft:
                            {
                                //! Microsoft Test needs a TestClass Attribute.
                                // 
                                foreach (Attribute att1 in type.GetCustomAttributes(false))
                                {
                                    if (att1.GetType().Name.Equals("TestClassAttribute"))
                                    {
                                        testAsm.Add(new TestClass());

                                        testAsm.Last().TestType = type;

                                        host.AddResult(Severity.Info, true, $"{att1.GetType().Name} found on {testAsm.Last().TestType.Name} class.", 1);

                                        foreach (MethodInfo method in type.GetMethods())
                                        {
                                            foreach (Attribute att2 in method.GetCustomAttributes(false))
                                            {
                                                String attname = att2.GetType().Name;

                                                //! See https://stackoverflow.com/questions/933613/how-do-i-use-assert-to-verify-that-an-exception-has-been-thrown
                                                // [ExpectedException(typeof(ArgumentException), "A userId of null was inappropriately allowed.")]

                                                if (attname.Equals("TestInitializeAttribute"))
                                                {
                                                    testAsm.Last().Initialize = method;
                                                    host.AddResult(Severity.Info, true, $"Initialize found in {testAsm.Last().TestType.Name}: {method.Name}() is {att2.GetType().Name}", 2);
                                                }
                                                else if (attname.Equals("TestCleanupAttribute"))
                                                {
                                                    testAsm.Last().Cleanup = method;
                                                    host.AddResult(Severity.Info, true, $"Cleanup found in {testAsm.Last().TestType.Name}: {method.Name}() is {att2.GetType().Name}", 2);
                                                }
                                                else if (attname.Equals("TestMethodAttribute"))
                                                {
                                                    //! Skip unpatched methods
                                                    //
                                                    if (!Unpatched.Contains(method.Name))
                                                    {
                                                        testAsm.Last().Methods.Add(method);
                                                    }
                                                }
                                            }
                                        }

                                        host.AddResult(Severity.Info, true, $"Patched Assert Class {testAsm.Last().TestType.Name} method calls in TestMethods: {testAsm.Last().Methods.Count}", 2);

                                        host.AddResult(Severity.Info, true, $"Unpatched TestMethods in {testAsm.Last().TestType.Name}: {Unpatched.Count}", 2);
                                    }
                                }
                            }
                            break;

                        case Utils.TestTypes.Nunit:
                            {
                                //! Nunit Test might lack a TestFixture Attribute.
                                // 
                                if (type.GetCustomAttributes(false).Any(q => q.GetType().Name.Equals("TestFixtureAttribute")) ||
                                    type.GetMethods().Any(q => q.GetCustomAttributes(false).Any(p => p.GetType().Name.Equals("TestAttribute"))))
                                {
                                    testAsm.Add(new TestClass());

                                    testAsm.Last().TestType = type;

                                    foreach (MethodInfo method in type.GetMethods())
                                    {
                                        foreach (Attribute att2 in method.GetCustomAttributes(false))
                                        {
                                            String attname = att2.GetType().Name;

                                            //! See https://stackoverflow.com/questions/933613/how-do-i-use-assert-to-verify-that-an-exception-has-been-thrown
                                            // [ExpectedException(typeof(ArgumentException), "A userId of null was inappropriately allowed.")]

                                            if (attname.Equals("SetupAttribute"))
                                            {
                                                testAsm.Last().Initialize = method;
                                                host.AddResult(Severity.Info, true, $"Setup found in {testAsm.Last().TestType.Name}: {method.Name}() is {att2.GetType().Name}", 2);
                                            }
                                            else if (attname.Equals("TearDownAttribute"))
                                            {
                                                testAsm.Last().Cleanup = method;
                                                host.AddResult(Severity.Info, true, $"TearDown found in {testAsm.Last().TestType.Name}: {method.Name}() is {att2.GetType().Name}", 2);
                                            }
                                            else if (attname.Equals("TestAttribute"))
                                            {
                                                //! Skip unpatched methods
                                                //
                                                if (!Unpatched.Contains(method.Name))
                                                {
                                                    testAsm.Last().Methods.Add(method);
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                            break;
                    }
                }

                //! ------------------------------------------------------------
                //! Run Patched Method from (last added) Weaved Test Class.
                //! ------------------------------------------------------------
                //
                foreach (TestClass testclass in testAsm)
                {
                    foreach (MethodInfo test in testclass.Methods)
                    {
                        host.AddResult(Severity.Info, true, $"Invoking Test: {testclass.TestType.Name}.{test.Name}", 1);

                        Object cls = Activator.CreateInstance(testclass.TestType);

                        //! Initialize Test
                        //
                        testclass.Initialize?.Invoke(cls, new Object[] { });

                        //! Execute Test
                        //
                        try
                        {
                            test?.Invoke(cls, new Object[] { });
                        }
                        catch (Exception e)
                        {
                            host.AddResult(Severity.Error, false, $"{test.Name} - {e.GetType().Name} - {e.Message}.");
                        }

                        //! Cleanup Test
                        //
                        testclass.Cleanup?.Invoke(cls, new Object[] { });
                    }
                }

                testAsm.TestAssembly = null;
            }
            else
            {
                host.AddResult(Severity.Warning, false, $"Failed to Locate UnitTest Assembly.");
            }

            return true;
        }

        /// <summary>
        /// Initializes this object.
        /// </summary>
        ///
        /// <param name="host"> The host. </param>
        public void Initialize(IHost host)
        {
            this.host = host;

            Assert.Host = host;
        }

        /// <summary>
        /// Supports.
        /// </summary>
        ///
        /// <param name="parm"> The parameter. </param>
        ///
        /// <returns>
        /// True if it succeeds, false if it fails.
        /// </returns>
        public bool Supports(String parm)
        {
            return Path.GetExtension(parm).Equals(".dll", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Searches for the first generic replacement method.
        /// </summary>
        ///
        /// <param name="ad">  The ad. </param>
        /// <param name="sig"> The signal. </param>
        ///
        /// <returns>
        /// The found generic replacement method.
        /// </returns>
        private MethodDefinition FindGenericReplacementMethod(AssemblyDefinition ad, String sig)
        {
            foreach (ModuleDefinition md in ad.Modules)
            {
                if (md.Types.Any(p => p.FullName.Equals(typeof(Assert).FullName)))
                {
                    return FindGenericReplacementMethod(md.Types.First(p => p.FullName.Equals(typeof(Assert).FullName)), sig);
                }
            }

#warning TODO Retry with a generic parameter.
            //! Example: System.Void Microsoft.VisualStudio.TestTools.UnitTesting.Assert::AreEqual<AssetPackage.Node>(!!0, !!0)
            //! Patched: System.Void RQAT.Assert::AreEqual(T,T)
            return null;
        }

        /// <summary>
        /// Searches for the first generic replacement method.
        /// </summary>
        ///
        /// <param name="typedef"> The typedef. </param>
        /// <param name="sig">     The signal. </param>
        ///
        /// <returns>
        /// The found generic replacement method.
        /// </returns>
        private MethodDefinition FindGenericReplacementMethod(TypeDefinition typedef, String sig)
        {
            String newSig = sig
                .Replace("Microsoft.VisualStudio.TestTools.UnitTesting.Assert", "RQAT.Assert");

            if (newSig.IndexOf("<") < newSig.IndexOf(">"))
            {
                String gp = newSig.Substring(newSig.IndexOf("<") + 1, newSig.IndexOf(">") - newSig.IndexOf("<") - 1);

                newSig = newSig.Replace($"<{gp}>", "");
                newSig = newSig.Replace($"!!0", "T");
            }

            //! Support for a single generic parameter!!
            //
            if (typedef.Methods.Any(p => p.ToString().Equals(newSig)))
            {
                return typedef.Methods.First(p => p.ToString().Equals(newSig));
            }

            return null;
        }

        ///// <summary>
        ///// Handler, called when my resolve event.
        ///// </summary>
        /////
        ///// <param name="sender"> Source of the event. </param>
        ///// <param name="args">   Resolve event information. </param>
        /////
        ///// <returns>
        ///// An Assembly.
        ///// </returns>
        //private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        //{
        //    String basePath = Path.GetDirectoryName(currentJob.Parm);
        //    return Assembly.LoadFrom(Path.Combine(basePath, args.Name));
        //}
        /// <summary>
        /// Searches for the first replacement method.
        /// </summary>
        ///
        /// <param name="ad">  The ad. </param>
        /// <param name="sig"> The signal. </param>
        ///
        /// <returns>
        /// The found replacement method.
        /// </returns>
        private MethodDefinition FindReplacementMethod(AssemblyDefinition ad, String sig)
        {
            foreach (ModuleDefinition md in ad.Modules)
            {
                if (md.Types.Any(p => p.FullName.Equals(typeof(Assert).FullName)))
                {
                    return FindReplacementMethod(md.Types.First(p => p.FullName.Equals(typeof(Assert).FullName)), sig);
                }
            }

#warning TODO Retry with a generic parameter.
            //! Example: System.Void Microsoft.VisualStudio.TestTools.UnitTesting.Assert::AreEqual<AssetPackage.Node>(!!0, !!0)
            //! Patched: System.Void RQAT.Assert::AreEqual(T,T)
            return null;
        }

        /// <summary>
        /// Searches for the first replacement method.
        /// </summary>
        ///
        /// <param name="typedef"> The typedef. </param>
        /// <param name="sig">     The signal. </param>
        ///
        /// <returns>
        /// The found replacement method.
        /// </returns>
        private MethodDefinition FindReplacementMethod(TypeDefinition typedef, String sig)
        {
            String newSig = sig
                .Replace("Microsoft.VisualStudio.TestTools.UnitTesting.Assert", "RQAT.Assert");
            String gp = String.Empty;

            //! Support for a single generic parameter!!
            //
            if (newSig.IndexOf("<") < newSig.IndexOf(">"))
            {
                gp = newSig.Substring(newSig.IndexOf("<") + 1, newSig.IndexOf(">") - newSig.IndexOf("<") - 1);

                // Replace generic parameters
                //
                newSig = newSig.Replace("!!0", gp);

                // Remove Generic Definition
                //
                newSig = newSig.Remove(newSig.IndexOf("<"), newSig.IndexOf(">") - newSig.IndexOf("<") + 1);
            }

            if (typedef.Methods.Any(p => p.ToString().Equals(newSig)))
            {
                return typedef.Methods.First(p => p.ToString().Equals(newSig));
            }

            return null;
        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// The tests classes.
        /// </summary>
        internal class TestClasses : List<TestClass>
        {
            #region Fields

            /// <summary>
            /// The assembly.
            /// </summary>
            internal Assembly TestAssembly;

            #endregion Fields

            #region Constructors

            /// <summary>
            /// Default constructor.
            /// </summary>
            public TestClasses()
            {
                TestAssembly = null;
            }

            #endregion Constructors
        }

        internal class TestClass
        {
            #region Fields

            /// <summary>
            /// The cleanup.
            /// </summary>
            public MethodInfo Cleanup;
            public Boolean Enabled;

            /// <summary>
            /// The initialize.
            /// </summary>
            public MethodInfo Initialize;

            /// <summary>
            /// The methods.
            /// </summary>
            public List<MethodInfo> Methods;
            public Type TestType;

            #endregion Fields

            #region Constructors

            /// <summary>
            /// Default constructor.
            /// </summary>
            public TestClass()
            {
                Enabled = true;
                TestType = null;
                Initialize = null;
                Cleanup = null;
                Methods = new List<MethodInfo>();
            }

            #endregion Constructors
        }

        #endregion Nested Types
    }
}