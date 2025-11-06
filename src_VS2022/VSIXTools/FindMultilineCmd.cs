using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace VSIXTools
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class FindMultilineCmd
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4136;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("19db246c-f602-457b-af45-cad72791aabd");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindMultilineCmd"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private FindMultilineCmd(AsyncPackage package, OleMenuCommandService commandService)
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
        public static FindMultilineCmd Instance
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
            // Switch to the main thread - the call to AddCommand in FindMultilineCmd's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new FindMultilineCmd(package, commandService);
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

            String fs = getMultilineFindPattern(dte);
            if (fs == null)
                return;

            Find f = dte.Find;
            f.Action = vsFindAction.vsFindActionFind;
            f.Backwards = false;
            f.FindWhat = fs;
            f.MatchWholeWord = false;
            f.MatchCase = false;
            f.MatchInHiddenText = true;
            f.Target = vsFindTarget.vsFindTargetCurrentDocument;
            f.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr;
            f.Execute();

            Window w = (Window)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            w.Visible = true;

            OutputWindowPane owp = GetOutputPane(w);
            owp.Activate();

            owp.OutputString(fs + "\n");

            Clipboard.SetText(fs);
        }

        private OutputWindowPane GetOutputPane(Window w)
        {
            string n = "Multiline Search";
            OutputWindow ow = (OutputWindow)w.Object;
            foreach(OutputWindowPane p in ow.OutputWindowPanes)
            {
                if (p.Name == n)
                    return p;
            }
            return ow.OutputWindowPanes.Add(n);
        }

        string getMultilineFindPattern(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            TextSelection sel = (TextSelection)dte.ActiveDocument.Selection;
            string s = (string)sel.Text;
            if (s == null)
                return null;
            if (s == "")
                return null;

            return avoidRegExprString(s);
        }

        string avoidRegExprString(string src)
        {
            if (src == null)
                return "";

            string r = (string)src.Clone();
            r = r.Replace("\\", "\\\\");

            r = r.Replace("@", "\\@");
            r = r.Replace("!", "\\!");
            r = r.Replace("\"", "\\\"");
            r = r.Replace("#", "\\#");
            r = r.Replace("$", "\\$");
            r = r.Replace("%", "\\%");
            r = r.Replace("&", "\\&");
            r = r.Replace("'", "\\'");
            r = r.Replace(":", "\\:");
            r = r.Replace("{", "\\{");
            r = r.Replace("}", "\\}");
            r = r.Replace("[", "\\[");
            r = r.Replace("]", "\\]");
            r = r.Replace("(", "\\(");
            r = r.Replace(")", "\\)");
            r = r.Replace("<", "\\<");
            r = r.Replace(">", "\\>");
            r = r.Replace("+", "\\+");
            r = r.Replace("-", "\\-");
            r = r.Replace("/", "\\/");
            r = r.Replace("*", "\\*");
            r = r.Replace("=", "\\=");
            r = r.Replace(".", "\\.");
            r = r.Replace("?", "\\?");
            r = r.Replace("^", "\\^");
            r = r.Replace("~", "\\~");
            r = r.Replace("|", "\\|");
            r = r.Replace("\r", "\\r");
            r = r.Replace("\n", "\\r\\n");
            r = r.Replace("\\r\\r\\n", "\\r\\n");

            return r;
        }
    }
}
