using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace Updater
{
    public static class Updater
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "" };
        public static void Patch(AssemblyDefinition assembly) { }

        public static string pluginsFolder = Path.Combine(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName, "plugins");
        public static void Initialize()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("-UpdateYARGSpy"))
                return;

            bool includePDB = false;
            if (!Directory.Exists(pluginsFolder))
                return;
            if (File.Exists(Path.Combine(pluginsFolder, "YARGSpy.dll")))
                File.Delete(Path.Combine(pluginsFolder, "YARGSpy.dll"));
            if (File.Exists(Path.Combine(pluginsFolder, "YARGSpy.pdb")))
            {
                File.Delete(Path.Combine(pluginsFolder, "YARGSpy.pdb"));
                includePDB = true;
            }
            if (File.Exists(Path.Combine(Path.Combine(pluginsFolder, "YARGSpy"), "YARGSpy.pdb")))
                includePDB = true;
            if (Directory.Exists(Path.Combine(pluginsFolder, "YARGSpy")))
                Directory.Delete(Path.Combine(pluginsFolder, "YARGSpy"), true);
                
            Directory.CreateDirectory(Path.Combine(pluginsFolder, "YARGSpy"));

            WebClient client = new WebClient();
            client.DownloadFileAsync(new Uri("https://github.com/gingerphoenix10/YARGSpy-Client/releases/latest/download/YARGSpy.dll"), Path.Combine(Path.Combine(pluginsFolder, "YARGSpy"), "YARGSpy.dll"));
            // idk how to wait for the download to finish before launching the game
        }
    }
}
