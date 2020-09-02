using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RQAT
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Cd_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += Cd_AssemblyLoad;
            AppDomain.CurrentDomain.TypeResolve += Cd_TypeResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += Cd_ReflectionOnlyAssemblyResolve;

            //foreach (Assembly asm in cd.GetAssemblies())
            //{
            //    Debug.Print("<APPDOMAIN>" + asm.FullName);
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            //AppDomain.Unload(cd);
        }

        /// <summary>
        /// Event handler. Called by CD for type resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_TypeResolve(object sender, ResolveEventArgs args)
        {
            // Debug.Print("<TYPE>" + args.Name);

            return args.RequestingAssembly;
        }

        /// <summary>
        /// Event handler. Called by CD for assembly load events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Assembly load event information. </param>
        private static void Cd_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            Debug.Print("<LOAD>" + args.LoadedAssembly.FullName);

            if (!cache.ContainsKey(args.LoadedAssembly.FullName))
            {
                Debug.Print("<CACHING>" + args.LoadedAssembly.FullName);
                Assembly asm = args.LoadedAssembly;
                cache[asm.FullName] = asm;
            }
        }

        static Dictionary<String, Assembly> cache = new Dictionary<String, Assembly>();

        /// <summary>
        /// Event handler. Called by CD for assembly resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Debug.Print("<RESOLVE>" + args.Name);

            String fn = args.Name;

            if (!cache.ContainsKey(args.Name))
            {

                if (File.Exists(fn))
                {

                    FileStream fs = new FileStream(fn, FileMode.Open);

                    Byte[] buffer = new Byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    fs.Close();

                    cache.Add(args.Name, AppDomain.CurrentDomain.Load(buffer));

                    //! Works
                    //cache.Add(args.Name, Assembly.LoadFile(fn));
                }
                else
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= Cd_AssemblyResolve;

                    cache.Add(args.Name, Assembly.Load(args.Name));

                    AppDomain.CurrentDomain.AssemblyResolve += Cd_AssemblyResolve;
                }
            }

            return cache[args.Name];
        }

        /// <summary>
        /// Event handler. Called by CD for reflection only assembly resolve events.
        /// </summary>
        ///
        /// <param name="sender"> Source of the event. </param>
        /// <param name="args">   Resolve event information. </param>
        ///
        /// <returns>
        /// An Assembly.
        /// </returns>
        private static Assembly Cd_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            //! See https://blogs.msdn.microsoft.com/jasonz/2004/05/31/why-isnt-there-an-assembly-unload-method/
            //! See https://stackoverflow.com/questions/123391/how-to-unload-an-assembly-from-the-primary-appdomain
            //! See https://blogs.msdn.microsoft.com/raulperez/2010/03/04/net-reflection-and-unloading-assemblies/
            //! See https://stackoverflow.com/questions/5783228/effect-of-loaderoptimizationattribute
            // 
            Debug.Print("<ORIGIN>" + args.RequestingAssembly.GetName());
            Debug.Print("<RESOLVE>" + args.Name);

            String fn = Path.Combine(
                Path.GetDirectoryName(args.RequestingAssembly.CodeBase),
                Path.ChangeExtension(args.Name.Substring(0, args.Name.IndexOf(',')), ".dll"));
            if (File.Exists(fn))
            {
                return Assembly.ReflectionOnlyLoadFrom(fn);
            }
            else
            {
                return Assembly.ReflectionOnlyLoad(args.Name);
            }
        }

    }
}
