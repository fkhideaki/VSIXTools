using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Windows.Forms;

namespace VSIXTools
{
    class ExtPair
    {
        public string src;
        public string dst;

        public ExtPair(string src_, string dst_)
        {
            src = src_;
            dst = dst_;
        }
    }

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenPairCmd
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4133;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("19db246c-f602-457b-af45-cad72791aabd");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenPairCmd"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenPairCmd(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenPairCmd Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in OpenPairCmd's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenPairCmd(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            Document doc = dte.ActiveDocument;
            if (doc == null)
                return;

            string path = doc.FullName;
            string pair = getPair(path);
            if (pair == null)
                return;

            dte.ItemOperations.OpenFile(pair);
        }

        private string getPair(string path)
        {
            List<ExtPair> exts = new List<ExtPair>();
            exts.Add(new ExtPair(".cpp", ".h"));
            exts.Add(new ExtPair(".cpp", ".hpp"));
            exts.Add(new ExtPair(".h", ".cpp"));
            exts.Add(new ExtPair(".c", ".h"));
            exts.Add(new ExtPair(".h", ".c"));
            exts.Add(new ExtPair(".frag", ".vert"));
            exts.Add(new ExtPair(".vert", ".frag"));
            exts.Add(new ExtPair(".xaml", ".xaml.cs"));
            exts.Add(new ExtPair(".xaml.cs", ".xaml"));

            string p = path.ToLower();

            foreach (ExtPair ext in exts)
            {
                string s = GetReplace(p, ext.src, ext.dst);
                if (s != null)
                    return s;
            }

            return null;
        }

        private static string GetReplace(string path, string srcEnd, string dstEnd)
        {
            if (!IsMatchEnd(path, srcEnd))
            {
                return null;
            }

            return path.Substring(0, path.Length - srcEnd.Length) + dstEnd;
        }

        private static bool IsMatchEnd(string fp, string e)
        {
            if (fp.Length < e.Length)
                return false;
            return fp.Substring(fp.Length - e.Length) == e;
        }

        private static string GetExistsPath(string basePath, string ext)
        {
            string d = basePath + ext;
            if (File.Exists(d))
                return d;
            return null;
        }
    }
}
