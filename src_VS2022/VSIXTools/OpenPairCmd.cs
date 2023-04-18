using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSIXTools
{
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
            string srcExt = System.IO.Path.GetExtension(path).ToLower();
            string basePath = System.IO.Path.GetDirectoryName(path) + "\\" + System.IO.Path.GetFileNameWithoutExtension(path);
            List<string> exts = getPairExts(srcExt);
            if (exts == null)
                return null;

            foreach (string ext in exts)
            {
                string d = basePath + ext;
                if (System.IO.File.Exists(d))
                    return d;
            }
            return null;
        }

        private List<string> getPairExts(string srcExt)
        {
            List<string> l = new List<string>();
            if (srcExt == ".cpp" || srcExt == ".c")
            {
                l.Add(".h");
                l.Add(".hpp");
                return l;
            }
            if (srcExt == ".h" || srcExt == ".hpp")
            {
                l.Add(".cpp");
                l.Add(".c");
                return l;
            }
            if (srcExt == ".frag")
            {
                l.Add(".vert");
                return l;
            }
            if (srcExt == ".vert")
            {
                l.Add(".frag");
                return l;
            }
            return null;
        }
    }
}
