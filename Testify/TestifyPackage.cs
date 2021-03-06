﻿using EnvDTE;
using EnvDTE80;
using Leem.Testify.SummaryView;
using log4net;
using log4net.Appender;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Linq;
using System.Drawing;

namespace Leem.Testify
{
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(TestifyCoverageWindow))]
    [ProvideToolWindow(typeof(UnitTestSelectorWindow), Style = VsDockStyle.AlwaysFloat, Window = "9197e117-9175-482a-9a0a-44f9af4f11f1")]
    [ProvideToolWindowVisibility(typeof(TestifyCoverageWindow), /*UICONTEXT_SolutionExists*/"f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    [Guid(GuidList.GuidTestifyPkgString)]
    public sealed class TestifyPackage : Package, IVsSolutionEvents3
    {
        public EventArgs E = null;
        private static Timer _timer;
        private DocumentEvents _documentEvents;
        private DTE _dte;
        private ITestifyQueries _queries;
        private UnitTestService _service;
        private IVsSolution _solution = null;
        private uint _solutionCookie;
        private string _solutionDirectory;
        private string _solutionName;
        private volatile int _testRunId;
        private bool _isFirstBuild = true;
        private bool isDatabaseValid = false;
        private readonly ILog _log = LogManager.GetLogger(typeof(TestifyPackage));
        
        private List<string> _fileAndFolderGuids;

        public TestifyPackage()
        {
             _fileAndFolderGuids= new List<string> { "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}", "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}", "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}" };

            _log.DebugFormat(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString());
            try
            {
                FileInfo file;
#if (DEBUG == true)
                file = new FileInfo(Environment.CurrentDirectory.ToString() + @"\log4net.config");
#endif

#if (DEBUG == false)
                file = new FileInfo(Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\log4net.config");
#endif
                _log.DebugFormat("Log4net.config path: " + file.ToString());
                ConfigureLogging(file);

                AppDomain.CurrentDomain.SetData("DataDirectory", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                _log.DebugFormat("DataDirectory = {0}", AppDomain.CurrentDomain.GetData("DataDirectory"));
                _timer = new System.Timers.Timer { Interval = 10000, Enabled = true, AutoReset = true };
                _timer.Elapsed += new ElapsedEventHandler(ProcessTestQueue);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.CurrentCulture, ex.Message, this.ToString()));
            }
        }

        public delegate void CoverageChangedEventHandler(string className, string methodName);
        public delegate void NewSolutionOpenedEventHandler();
        private void CheckForDatabase(string databasePath)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                bool isCompatible;
                _log.DebugFormat("CheckForDatabase: {0}", databasePath);
                if (context.Database.Exists())
                {
                    try
                    {
                        if (context.Database.CompatibleWithModel(throwIfNoMetadata: true))
                        {
                            _log.DebugFormat("Database was found and is compatible");
                            isDatabaseValid = true;
                            return;
                        }
                    }
                    catch
                    {
                        _log.DebugFormat("Database was found and but is not compatible");
                    }
                }

                // Write new database
                string initialDatabasePath = Path.GetDirectoryName(typeof(TestifyPackage).Assembly.Location) + @"\TestifyCE.sdf";
                _log.ErrorFormat("Copying database from {0} to {1}", initialDatabasePath, databasePath);
                File.Copy(initialDatabasePath.ToString(), databasePath, true);
                isDatabaseValid = true;
            }
        }

        private string GetProjectOutputBuildFolder(EnvDTE.Project proj)
        {
            try
            {
                // Get the configuration manager of the project
                var configManager = proj.ConfigurationManager;

                if (configManager == null)
                {
                    _log.DebugFormat("GetProjectOutputBuildFolder - ConfigurationManager is null for Project: {0}", proj.Name);
                    return string.Empty;
                }
                else
                {
                    // Get the active project configuration
                    var activeConfiguration = configManager.ActiveConfiguration;
                    string assemblyName = GetProjectPropertyByName(proj.Properties, "AssemblyName");
                    // Get the output folder
                    string outputPath = activeConfiguration.Properties.Item("OutputPath").Value.ToString();

                    // The output folder can have these patterns:
                    // 1) "\\server\folder"
                    // 2) "drive:\folder"
                    // 3) "..\..\folder"
                    // 4) "folder"

                    string absoluteOutputPath = null;
                    if (outputPath.StartsWith((System.IO.Path.DirectorySeparatorChar + System.IO.Path.DirectorySeparatorChar).ToString()))
                    {
                        // This is the case 1: "\\server\folder"
                        absoluteOutputPath = outputPath;
                    }
                    else if (outputPath.Length >= 2 && outputPath[1] == System.IO.Path.VolumeSeparatorChar)
                    {
                        // This is the case 2: "drive:\folder"
                        absoluteOutputPath = outputPath;
                    }
                    else
                    {
                        string projectFolder = null;
                        if (outputPath.IndexOf("..\\") != -1)
                        {
                            // This is the case 3: "..\..\folder"
                            projectFolder = System.IO.Path.GetDirectoryName(proj.FullName);

                            while (outputPath.StartsWith("..\\"))
                            {
                                outputPath = outputPath.Substring(3);
                                projectFolder = System.IO.Path.GetDirectoryName(projectFolder);
                            }
                            absoluteOutputPath = System.IO.Path.Combine(projectFolder, outputPath);
                        }
                        else
                        {
                            // This is the case 4: "folder"
                            projectFolder = System.IO.Path.GetDirectoryName(proj.FullName);
                            absoluteOutputPath = System.IO.Path.Combine(projectFolder, outputPath);
                        }
                    }
                    return System.IO.Path.Combine(absoluteOutputPath, assemblyName);
                }
            }
            catch (Exception ex)
            {
                _log.DebugFormat("GetProjectOutputBuildFolder could not determine folder name: {0}", ex.Message);
                return string.Empty;
            }
        }

        private void ProcessTestQueue(object source, ElapsedEventArgs e)
        {
            if (_service != null && isDatabaseValid)
            {
                _service.ProcessTestQueue(++_testRunId);
            }
        }

        private async void VerifyProjects(IVsSolution solution, string projectName)
        {
            if (_queries == null)
            {
                object solutionName;
                solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out solutionName);
                _solutionName = solutionName.ToString();
                _queries = TestifyQueries.Instance;
                TestifyQueries.SolutionName = Path.Combine(Path.GetDirectoryName(_solutionName), Path.GetFileNameWithoutExtension(_solutionName));
            }

            var pocoProjects = new List<Poco.Project>();

            foreach (EnvDTE.Project project in _dte.Solution.Projects)
            {
                if (projectName == string.Empty || project.UniqueName.Equals(projectName))
                {
                    _documentEvents = _dte.Events.DocumentEvents;
                    _documentEvents.DocumentSaved += new _dispDocumentEvents_DocumentSavedEventHandler(this.OnDocumentSaved);
                    _documentEvents.DocumentOpening += new _dispDocumentEvents_DocumentOpeningEventHandler(this.OnDocumentOpening);
                    var outputPath = GetProjectOutputBuildFolder(project);
                    var assemblyName = GetProjectPropertyByName(project.Properties, "AssemblyName");

                    _log.DebugFormat("Verify project name: {0}", project.Name);
                    _log.DebugFormat("  outputPath: {0}", outputPath);
                    _log.DebugFormat("  Assembly name: {0}", assemblyName);
                    //var folders = project.ProjectItems..GetType(EnvDTE.Constants.vsProjectItemKindPhysicalFolder);
                    var newProject = new Poco.Project
                    {
                        Name = project.Name,
                        AssemblyName = assemblyName,
                        UniqueName = project.UniqueName,
                    };
                 

                    if (!string.IsNullOrWhiteSpace(outputPath))
                    {
                        // don't overwrite Path with Empty string
                        newProject.Path = outputPath;
                    }
                    pocoProjects.Add(newProject);
                }
                
            }

            _queries.MaintainProjects(pocoProjects);
        }

        private void ConfigureLogging(FileInfo file)
        {
            log4net.Config.XmlConfigurator.Configure(file);
            var appenders = _log.Logger.Repository.GetAppenders();

            var h =
            (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            foreach (var a in h.Root.Appenders)
            {
                if (a is FileAppender)
                {
                    var fa = (FileAppender)a;

                    var fileInfo = new FileInfo(fa.File);
                    var logFileLocation = string.Format(@"{0}\Testify\{1}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileInfo.Name);
                    fa.File = logFileLocation;
                    fa.ActivateOptions();
                    _log.DebugFormat("FileAppender is writing to: " + fa.File);
                    _log.Debug("Log4net is configured");
                    break;
                }
            }
        }

        private void DisableMenuCommandIfNoSolutionLoaded(OleMenuCommand menuCommand)
        {
            uint cookie;
            var monitorSelectionService = (IVsMonitorSelection)GetService(typeof(SVsShellMonitorSelection));
            monitorSelectionService.GetCmdUIContextCookie(new Guid(ContextGuids.vsContextGuidSolutionExists), out cookie);
            int isActive;
            monitorSelectionService.IsCmdUIContextActive(cookie, out isActive);

            menuCommand.Enabled = isActive == 1 ? true : false;
        }

        private string GetProjectPropertyByName(EnvDTE.Properties properties, string name)
        {
            try
            {
                if (properties != null)
                {
                    var item = properties.GetEnumerator();
                    while (item.MoveNext())
                    {
                        var property = item.Current as EnvDTE.Property;

                        if (property.Name == name)
                        {
                            return property.Value.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Error in GetAssemblyName: {0}", ex.Message);
            }

            return string.Empty;
        }

        //private int GetColumnNumber()
        //{
        //    // get the DTE2 object
        //    DTE2 dte2 = GetDTE2();

        //    if (dte2 == null)
        //    {
        //        return 0;
        //    }

        //    // get currently active cursor position
        //    var selection = (TextSelection)dte2.ActiveDocument.Selection;

        //    VirtualPoint point = selection.ActivePoint;

        //    return point.DisplayColumn; // get the column number from the location
        //}

        //private string GetDocumentName()
        //{
        //    // get the DTE2 object
        //    DTE2 dte2 = GetDTE2();

        //    if (dte2 == null)
        //    {
        //        return string.Empty;
        //    }

        //    // get the ActiveDocument name from DTE2 object
        //    return dte2.ActiveDocument.Name;
        //}

        //private DTE2 GetDTE2()
        //{
        //    // get the instance of DTE
        //    var dte = (DTE)GetService(typeof(DTE));

        //    // cast it as DTE2, historical reasons
        //    var dte2 = dte as DTE2;

        //    if (dte2 == null)
        //    {
        //        return null;
        //    }

        //    return dte2;
        //}

        //private int GetLineNumber()
        //{
        //    // get the DTE2 object
        //    var dte2 = GetDTE2();

        //    if (dte2 == null)
        //    {
        //        return 0;
        //    }

        //    // get currently active cursor location
        //    var selection = (TextSelection)dte2.ActiveDocument.Selection;

        //    VirtualPoint point = selection.ActivePoint;

        //    return point.Line; // get the line number from the location
        //}

        

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var clsid = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Testify",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        private void OnDocumentSaved(Document document)
        {
            var project = document.ProjectItem;
            _queries.AddTestsCoveringFileToTestQueue(document.FullName, project.ContainingProject);
        }

        private void OnDocumentOpening(string documentPath, bool isReadOnly)
        {
            //_log.DebugFormat("OnDocumentOpening fired for {0}", documentPath);
            //var project = document.ProjectItem;
            //_queries.AddToTestQueue(project.ContainingProject.UniqueName);
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the
        /// tool window. See the Initialize method to see how the menu item is associated to
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowCoverageToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = FindToolWindow(typeof(TestifyCoverageWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }

            IVsUIShell5 shell5 = (IVsUIShell5)GetService(typeof(SVsUIShell));






            //IconCache.Add("Module", );




           // window.Content = new SummaryViewControl((TestifyCoverageWindow)window, shell5);
            
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }



        public void ShowUnitTestToolWindow(object sender, EventArgs e)
        {
            TEST(sender, e);
            //// Get the instance number 0 of this tool window. This window is single instance so this instance
            //// is actually the only one.
            //// The last flag is set to true so that if the tool window does not exists it will be created.
            //ToolWindowPane window = this.FindToolWindow(typeof(UnitTestSelectorWindow), 0, true);
            //if ((null == window) || (null == window.Frame))
            //{
            //    throw new NotSupportedException(Resources.CanNotCreateWindow);
            //}
            //window.Content = new UnitTestSelector((UnitTestSelectorWindow)window);

            //IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            //Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void TEST(object sender, EventArgs e)
        {
            IVsUIShell vsUIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));

            IVsWindowFrame frame;

            Guid guidToolWindow2 = typeof(UnitTestSelectorWindow).GUID;

            vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref guidToolWindow2, out frame);

            frame.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Float);

            frame.Show();
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        protected override void Dispose(bool disposing)
        {
            UnadviseSolutionEvents();

            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            base.Initialize();
            AdviseSolutionEvents();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the Run All Solution Tests menu item.
                CommandID menuSolutionTestsCommandID = new CommandID(GuidList.GuidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests);
                OleMenuCommand menuSolutionTests = new OleMenuCommand(SolutionTestsCallback, menuSolutionTestsCommandID);
                menuSolutionTests.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                DisableMenuCommandIfNoSolutionLoaded(menuSolutionTests);
                mcs.AddCommand(menuSolutionTests);

                // Create the command for the Run All Project Tests menu item.
                CommandID menuProjectTestsCommandID = new CommandID(GuidList.GuidTestifyCmdSet, (int)PkgCmdIDList.cmdidProjectTests);
                OleMenuCommand menuProjectTests = new OleMenuCommand(ProjectTestsCallback, menuProjectTestsCommandID);
                menuProjectTests.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                DisableMenuCommandIfNoSolutionLoaded(menuProjectTests);
                mcs.AddCommand(menuProjectTests);

                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.GuidTestifyCmdSet, (int)PkgCmdIDList.cmdidTestTool);
                MenuCommand menuToolWin = new MenuCommand(ShowCoverageToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
        }

        private void AdviseSolutionEvents()
        {
            UnadviseSolutionEvents();

            _solution = this.GetService(typeof(SVsSolution)) as IVsSolution;

            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionCookie);
            }
        }

        private void ProjectTestsCallback(object sender, EventArgs e)
        {
            var projectName = _dte.ActiveDocument.ProjectItem.ContainingProject.Name.ToString();
            _queries.AddToTestQueue(projectName);
            ShowCoverageToolWindow(sender, e);
        }

        private void SolutionTestsCallback(object sender, EventArgs e)
        {
            ShowCoverageToolWindow(sender, e);
        }

        private void UnadviseSolutionEvents()
        {
            if (_solution != null)
            {
                if (_solutionCookie != uint.MaxValue)
                {
                    _solution.UnadviseSolutionEvents(_solutionCookie);
                    _solutionCookie = uint.MaxValue;
                }

                _solution = null;
            }
        }

        #endregion Package Members

        #region Interface Methods

        public Project GetProject(string projectUniqueName)
        {
            IVsHierarchy hierarchy;
            _solution.GetProjectOfUniqueName(projectUniqueName, out hierarchy);
            object project;

            ErrorHandler.ThrowOnFailure
                (hierarchy.GetProperty(
                    VSConstants.VSITEMID_ROOT,
                    (int)__VSHPROPID.VSHPROPID_ExtObject,
                    out project));

            return (project as EnvDTE.Project);
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = FindToolWindow(typeof(TestifyCoverageWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsUIShell5 shell5 = (IVsUIShell5)GetService(typeof(SVsUIShell));

            //var themeRespourceKey = new ThemeResourceKey(new System.Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d"), "ToolWindowBackground", 0);

            //var themeColor = VsColors.GetThemedWPFColor(shell5, themeRespourceKey);
            //var colorBrush = new System.Windows.Media.SolidColorBrush(themeColor);

            window.Content = new SummaryViewControl((TestifyCoverageWindow)window, shell5);

           
            return VSConstants.S_OK;
        }

        public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            // Do something
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterMergeSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnAfterOpenProject(IVsHierarchy hierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            // if the user closes a solution and opens another, we need to rebuild the ConnectionString
            if (_queries != null)
            {
                _queries = null;
            }

            IVsSolution solution = SetSolutionValues();
            _dte = (DTE)GetService(typeof(DTE));

            _log.DebugFormat("Solution Opened: {0}", _solutionName);
            _service = new UnitTestService(_dte, _solutionDirectory, _solutionName);
            var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var fullPath = Path.Combine(_solutionDirectory, _solutionName);
            var hashCode = fullPath.GetHashCode();
            hashCode = hashCode > 0 ? hashCode : -hashCode;
            var databasePath = GetDatabasePath(appDataDirectory, hashCode);

            CheckForDatabase(databasePath);

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var solutionTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.GuidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            solutionTestsMenuCommand.Enabled = true;

            var projectTestsMenuCommand = mcs.FindCommand(new CommandID(Testify.GuidList.GuidTestifyCmdSet, (int)PkgCmdIDList.cmdidSolutionTests));
            projectTestsMenuCommand.Enabled = true;

            // Setup Project Build Event Handler

            var projectEvents = ((Events2)_dte.Events).BuildEvents;
            projectEvents.OnBuildProjConfigDone += ProjectBuildEventHandler;

            VerifyProjects(solution, string.Empty);
        
            _queries.SetAllQueuedTestsToNotRunning();
            return VSConstants.S_OK;
        }



        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeCloseSolution(object pUnkReserved)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        { return VSConstants.E_NOTIMPL; }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            //Do something
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        { return VSConstants.E_NOTIMPL; }

        private string GetDatabasePath(string directory, int hashcode)
        {
            var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(_solutionName), hashcode.ToString());

            var appDataExists = Directory.Exists(Path.Combine(path));
            if (!appDataExists)
            {
                Directory.CreateDirectory(path);
            }
            var databasePath = Path.Combine(path, "TestifyCE.sdf");
            return databasePath;
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            DisableMenuCommandIfNoSolutionLoaded(sender as OleMenuCommand);
        }

        private void ProjectBuildEventHandler(string projectName, string projectConfig, string platform, string solutionConfig, bool success)
        {
            var sw = new Stopwatch();
            sw.Restart();

            IVsSolution pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (success)
            {
                pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
                var solution = pSolution as EnvDTE.Solution;
                VerifyProjects(pSolution, projectName);
               
                _isFirstBuild = false;
                _log.DebugFormat("Project Build Successful for project name: {0}", projectName);
                if (projectName.EndsWith("Test.csproj") == false)
                { 
                    var foldermaps = MapProjectFolders(projectName);
                    _queries.UpdateFolders(foldermaps, projectName);
                }
                _queries.AddToTestQueue(projectName);
            }
            sw.Stop();
        }

        private IVsSolution SetSolutionValues()
        {
            var pSolution = GetService(typeof(SVsSolution)) as IVsSolution;
            string solutionDirectory;
            string solutionOptions;
            string solutionFile;
            pSolution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionOptions);
            _solutionDirectory = solutionDirectory;
            _solutionName = solutionFile.Replace(".sln", string.Empty);// Path.GetFileNameWithoutExtension(solutionFile);
            return pSolution;
        }

        #endregion Interface Methods
        public class FileFolderInfo 
        {
            public string ProjectName { get; set; }
            public string ClassName { get; set; }
            public LinkedList<string> Folders { get; set; }
            public FileFolderInfo()
            {
                Folders = new LinkedList<string>();
            
            }

        }

        private List<FileFolderInfo> MapProjectFolders(string projectName)
        {
            var sw = Stopwatch.StartNew();
            var fileList = new List<FileFolderInfo>();
           
            var project = GetProject(projectName);

            var fileFolderInfo = new FileFolderInfo();
            foreach (EnvDTE.ProjectItem projectItem in project.ProjectItems)
            {
                var folders = new LinkedList<string>();
                if (projectItem != null && _fileAndFolderGuids.Contains(projectItem.Kind))
                {


                    var folderPath = GetFileName(new List<FileFolderInfo>(), folders, projectItem);
                    if(folderPath != null)
                    {
                        folderPath.ForEach(x=>x.ProjectName = projectName);
                        fileList.AddRange(folderPath);
                        _log.DebugFormat("MapProjectFolders   File List count: {0}",  fileList.Count);
                    }
                }

            }
            _log.DebugFormat("MapProjectFolders elapsed time: {0}", sw.Elapsed);
            return fileList;
           
        }

        private List<FileFolderInfo> GetFileName( List < FileFolderInfo > fileList, LinkedList<string> folders, EnvDTE.ProjectItem projectItem)
        {
            var localFolders = new LinkedList<string>(folders);
            var localFileList = new List<FileFolderInfo>();
            var x = projectItem.Name;
            var y = projectItem.FileCodeModel;
            if ( projectItem.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}")
            {
                if (projectItem.Name.EndsWith(".cs") || projectItem.Name.EndsWith(".vb"))
                {
                    localFolders = new LinkedList<string>(folders);
                    _log.DebugFormat("GetFileName FileName: {0}  local Folder count: {1}", projectItem.Name,localFolders.Count);
                    
                    
                    var newFolder = new FileFolderInfo { Folders = new LinkedList<string>(localFolders), ClassName = projectItem.Name } ;
                    localFileList.Add(newFolder);
                   

                }

            }
            else if (_fileAndFolderGuids.Contains(projectItem.Kind))
            {
                var nextLevelFolders = new LinkedList<string>(localFolders);
                nextLevelFolders.AddLast(projectItem.Name);
                foreach (EnvDTE.ProjectItem item in projectItem.ProjectItems)
                {
                    _log.DebugFormat("GetFileName Processing ProjectItem: {0}", item.Name);
                  
                    var filesInItem = GetFileName(localFileList, nextLevelFolders, item);
                    if (filesInItem != null)
                    {
                        localFileList.AddRange(filesInItem);
                        _log.DebugFormat("GetFileName localFileList count: {0}", localFileList.Count);
                    }

                }
            }

            return localFileList;
        }
    }
    

    
 public class ProjectItemIterator : IEnumerable<EnvDTE.ProjectItem>
{
    IEnumerable<EnvDTE.Project> projects;

    public ProjectItemIterator(EnvDTE.Solution solution)
    {

        if (solution == null)
            throw new ArgumentNullException("solution");

        projects = solution.Projects.Cast<EnvDTE.Project>();
    }
    
    public ProjectItemIterator(IEnumerable<EnvDTE.Project> projects)
    {
        if (projects == null)
            throw new ArgumentNullException("projects");
 
        this.projects = projects;
    }
 
    public IEnumerator<EnvDTE.ProjectItem> GetEnumerator()
    {
        foreach (EnvDTE.Project currentProject in projects)
            foreach (var currentProjectItem in Enumerate(currentProject.ProjectItems))
                yield return currentProjectItem;
    }
 
    IEnumerable<EnvDTE.ProjectItem> Enumerate(EnvDTE.ProjectItems projectItems)
    {
        if (projectItems != null)
        {
            foreach (EnvDTE.ProjectItem item in projectItems)
            {
                yield return item;
 
                if (item.SubProject != null)
                {
                    foreach (EnvDTE.ProjectItem childItem in Enumerate(item.SubProject.ProjectItems))
                        yield return childItem;
                }
                else
                {
                    foreach (EnvDTE.ProjectItem childItem in Enumerate(item.ProjectItems))
                        yield return childItem;
                }
            }
        }
    }
 
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

 
}