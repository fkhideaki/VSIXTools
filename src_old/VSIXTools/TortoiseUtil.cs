using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSIXTools
{
    public enum EngineType
    {
        None,
        Git,
        Svn,
    }

    class TortoiseUtil
    {
        public static EngineType getEngineType(string dir)
        {
            if (dir == null)
                return EngineType.None;

            while (dir.Length >= 0)
            {
                if (System.IO.Directory.Exists(dir + "\\" + ".git"))
                    return EngineType.Git;
                if (System.IO.Directory.Exists(dir + "\\" + ".svn"))
                    return EngineType.Svn;

                System.IO.DirectoryInfo di = System.IO.Directory.GetParent(dir);
                if (di == null)
                    return EngineType.None;

                string s = di.ToString();
                if (s.Equals(dir))
                    return EngineType.None;
                dir = s;
            }
            return EngineType.None;
        }

        public static string createCmd(string cmd, string path)
        {
            string tp = getTPName(path);
            if (tp == null)
                return null;

            string sc = " /command:" + cmd;
            string sp = " /path:" + "\"" + path + "\"";
            return tp + sc + sp + " /notempfile";
        }


        public static string getTPName(string path)
        {
            EngineType t = getEngineType(path);
            switch (t)
            {
                case EngineType.Git:
                    return "TortoiseGitProc.exe";
                case EngineType.Svn:
                    return "TortoiseProc.exe";
                default:
                    return null;
            }
        }

        public static void execPathCmd(string cmd, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (path == null)
                return;
            string cl = createCmd(cmd, path);
            if (cl == null)
                return;

            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            dte.ExecuteCommand("Tools.Shell", cl);
        }

        public static void execDiff(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            execPathCmd("diff", path);
        }

        public static void execLog(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            execPathCmd("log", path);
        }
    }
}
