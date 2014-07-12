﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Data;
using log4net;
using ErikEJ.SqlCe;
using System.Data.Entity;
using System.IO;
using Leem.Testify.Model;
using Leem.Testify.Poco;
using System.ComponentModel.Composition;

namespace Leem.Testify
{

    [Export(typeof(ITestifyQueries))]
    public class TestifyQueries : ITestifyQueries
    {
        // static holder for instance, need to use lambda to construct since constructor private
        private static readonly Lazy<TestifyQueries> _instance = new Lazy<TestifyQueries>(() => new TestifyQueries());

        private static string _connectionString;
        private static string _solutionName;
        private static Stopwatch _sw;
        private ILog Log = LogManager.GetLogger(typeof(TestifyQueries));
        // private to prevent direct instantiation.
        private TestifyQueries()
        {
            _sw = new Stopwatch();
        }

        public event EventHandler<ClassChangedEventArgs> ClassChanged;
        // accessor for instance
        public static TestifyQueries Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public static string SolutionName
        {
            set
          {
                _solutionName = value;

                var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                var path = Path.Combine(directory, "Testify", Path.GetFileNameWithoutExtension(value), "TestifyCE.sdf;password=lactose");

                // Set connection string
                _connectionString = string.Format("Data Source={0}", path);
            }
            get 
            {
                return _solutionName;
            }
        }


        public static string ConvertTrackedMethodFormatToUnitTestFormat(string trackedMethodName)
        {
            // Convert This:
            // System.Void UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
            // Into This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
            if (string.IsNullOrEmpty(trackedMethodName))
            {
                return string.Empty;
            }
            else
            {
                int locationOfSpace = trackedMethodName.IndexOf(' ') + 1;

                int locationOfParen = trackedMethodName.IndexOf('(');

                var testMethodName = trackedMethodName.Substring(locationOfSpace, locationOfParen - locationOfSpace);

                testMethodName = testMethodName.Replace("::", ".");

                return testMethodName;
            }

        }

        public static string ConvertUnitTestFormatToFormatTrackedMethod(string testMethodName)
        {
            // Convert This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest.TestIt
            // Into This:
            // UnitTestExperiment.Domain.Test.ThingsThatWereDoneTest::TestIt()
            if (string.IsNullOrEmpty(testMethodName))
            {
                return string.Empty;
            }
            else
            {
                int locationOfLastDot = testMethodName.LastIndexOf(".");

                testMethodName = testMethodName.Remove(locationOfLastDot, 1);

                testMethodName = testMethodName.Insert(locationOfLastDot, "::");

                testMethodName = testMethodName + "()";

                return testMethodName;
            }

        }

        public void AddToTestQueue(string projectName)
        {
            try
            {
                // make sure this is not a test project
                if (!projectName.Contains(".Test"))
                {
                    var projectInfo = GetProjectInfo(projectName);

                    // make sure there is a matching test project
                    if (projectInfo != null && projectInfo.TestProject != null)
                    {
                        var testQueue = new TestQueue
                        {
                            ProjectName = projectName,
                            QueuedDateTime = DateTime.Now
                        };
                        using (var context = new TestifyContext(_solutionName))
                        {
                            context.TestQueue.Add(testQueue);
                            context.SaveChanges();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in AddToTestQueue {0}", ex);
            }
        }

        public void AddToTestQueue(TestQueue testQueue)
        {
            try
            {
                using (var context = new TestifyContext(_solutionName))
                {
                    context.TestQueue.Add(testQueue);

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in AddToTestQueue {0}", ex);
            }
        }

        public IEnumerable<Poco.CoveredLinePoco> GetCoveredLines(TestifyContext context, string className)
        {
            var sw = new Stopwatch();

            sw.Restart();

            var coveredLines = context.CoveredLines
                                        .Include(u => u.UnitTests)
                                        .Include(mo => mo.Module).Include(c =>c.Class)
                                        .Include(me =>me.Method)
                                        .Where(x => x.Class.Name.Equals(className));

            return coveredLines;
        }

        public QueuedTest GetIndividualTestQueue(int testRunId) // List<TestQueueItem> 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                QueuedTest nextItem = null;

                if (context.TestQueue.Where(i => i.IndividualTest != null).All(x => x.TestRunId == 0))// there aren't any Individual tests currently running
                {

                    var individual = (from queueItem in context.TestQueue
                                      where queueItem.IndividualTest != null
                                      group queueItem by queueItem.ProjectName).AsEnumerable().Select(x => new QueuedTest
                                    {
                                        ProjectName = x.Key,
                                        IndividualTests = (from test in context.TestQueue
                                                           where test.ProjectName == x.Key
                                                           && test.IndividualTest != null
                                                           select test.IndividualTest).ToList()
                                    }).OrderBy(o => o.IndividualTests.Count());

                    nextItem = individual.FirstOrDefault();

                }

                var testsToMarkInProgress = new List<TestQueue>();

                if (nextItem != null)
                {
                    testsToMarkInProgress = MarkTestAsInProgress(testRunId, context, nextItem, testsToMarkInProgress);
                }

                return nextItem;
            }

        }

        public ProjectInfo GetProjectInfo(string uniqueName)
        {
            try
            {
                using (var context = new TestifyContext(_solutionName))
                {

                    var result = from project in context.Projects
                                 join testProject in context.TestProjects on project.UniqueName equals uniqueName
                                 where testProject.ProjectUniqueName == project.UniqueName
                                 select new ProjectInfo
                                 {
                                     ProjectName = project.Name,
                                     ProjectAssemblyName = project.AssemblyName,
                                     TestProject = testProject
                                 };

                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error Getting Project Info, error: {0}", ex);
                return null;
            }

        }

        public ProjectInfo GetProjectInfoFromTestProject(string uniqueName)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = from project in context.Projects
                             join testProject in context.TestProjects on project.UniqueName equals testProject.ProjectUniqueName
                             where testProject.UniqueName == uniqueName
                             select new ProjectInfo
                             {
                                 ProjectName = project.Name,
                                 ProjectAssemblyName = project.AssemblyName,
                                 TestProject = testProject
                             };

                return result.FirstOrDefault();
            }
        }

        public IQueryable<Project> GetProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.Projects;
            }
        }

        public QueuedTest GetProjectTestQueue(int testRunId) // List<TestQueueItem> 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                QueuedTest nextItem = null;

                if (context.TestQueue.Where(i => i.IndividualTest == null).All(x => x.TestRunId == 0))// there aren't any Project tests currently running
                {

                    var query = (from queueItem in context.TestQueue
                                 where queueItem.IndividualTest == null
                                 orderby queueItem.QueuedDateTime
                                 group queueItem by queueItem.ProjectName).AsEnumerable().Select(x => new QueuedTest { ProjectName = x.Key });

                    nextItem = query.FirstOrDefault();

                }

                var testsToMarkInProgress = new List<TestQueue>();

                if (nextItem != null)
                {
                    testsToMarkInProgress = MarkTestAsInProgress(testRunId, context, nextItem, testsToMarkInProgress);
                }

                return nextItem;
            }

        }

        public IList<TestProject> GetTestProjects()
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.TestProjects.ToList();
            }
        }

        public List<Poco.UnitTest> GetUnitTestByName(string name)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return SelectUnitTestByName(name, context);
            }
        }

        public void GetUnitTestsCoveringMethod(string modifiedMethod)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var query = from unitTest in context.TrackedMethods
                            where unitTest.Name.Contains(modifiedMethod)
                            select unitTest.UnitTestId;

            }
        }

        public List<string> GetUnitTestsThatCoverLines(string className, string methodName, int lineNumber)
        {
            string methodNameFragment = className + "::" + methodName;

            List<UnitTest> tests = new List<UnitTest>();

            using (var context = new TestifyContext(_solutionName))
            {
                var query = (from line in context.CoveredLines.Include(x => x.UnitTests)

                             where line.Method.Name.Contains(methodNameFragment)
                             select line.UnitTests);

                tests = query.SelectMany(x => x).ToList();
            }

            List<string> testNames = new List<string>();

            testNames = tests.Select(x => x.TestMethodName).Distinct().ToList();

            return testNames;
        }

        public async void MaintainProjects(IList<Project> projects)
        {

            using (var context = new TestifyContext(_solutionName))
            {
                try
                {
                    UpdateProjects(projects, context);

                    UpdateTestProjects(projects, context);
                }
                catch (Exception ex)
                {
                    Log.DebugFormat(ex.Message);
                }

                try
                {
                    context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in MaintainProjects Message: {0}", ex.InnerException);
                }

                _sw.Stop();

                Log.DebugFormat("MaintainProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
        }

        public void RemoveFromQueue(QueuedTest testQueueItem)
        {
            Log.DebugFormat("Test Completed: {0} Elapsed Time {1}", testQueueItem.ProjectName, DateTime.Now - testQueueItem.TestStartTime);

            var testsToDelete = new List<TestQueue>();

            using (var context = new TestifyContext(_solutionName))
            {
                testsToDelete = context.TestQueue.Where(x => x.TestRunId == testQueueItem.TestRunId).ToList();

                foreach (var test in testsToDelete.ToList())
                {
                    context.TestQueue.Remove(test);
                }

                context.SaveChanges();
            }
        }

        public async Task RunTestsThatCoverLine(string projectName, string className, string methodName, int lineNumber)
        {
            try
            {
                var unitTestNames = GetUnitTestsThatCoverLines(className.Substring(0, className.IndexOf('.')), methodName, lineNumber);

                var projectInfo = GetProjectInfo(projectName);

                if (projectInfo != null && projectInfo.TestProject != null)
                {
                    var testQueueItem = new QueuedTest { ProjectName = projectName, IndividualTests = unitTestNames };

                    using (var context = new TestifyContext(_solutionName))
                    {
                        if (testQueueItem.IndividualTests.Any())
                        {
                            foreach (var test in testQueueItem.IndividualTests)
                            {
                                var testQueue = new TestQueue { ProjectName = testQueueItem.ProjectName, IndividualTest = test };

                                context.TestQueue.Add(testQueue);
                            }
                        }
                        else
                        {
                            var testQueue = new TestQueue { ProjectName = testQueueItem.ProjectName };

                            context.TestQueue.Add(testQueue);
                        }

                        context.SaveChanges();
                    }

                }
                else
                {
                    Log.DebugFormat("GetProjectInfo returned a null TestProject for {0}", projectName);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error in RunTestsThatCoverLine {0}", ex);
            }


        }

        public async Task<List<string>> SaveCoverageSessionResults(CoverageSession coverageSession, ProjectInfo projectInfo, List<string> individualTests)
        {
            var coverageService = CoverageService.Instance;

            coverageService.SolutionName = _solutionName;

            var changedClasses = new List<string>();

            IList<LineCoverageInfo> newCoveredLineInfos = new List<LineCoverageInfo>();

            var sessionModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.ProjectAssemblyName));
           
            var moduleName = sessionModule != null ? sessionModule.FullName : string.Empty;

            var testModule = coverageSession.Modules.FirstOrDefault(x => x.ModuleName.Equals(projectInfo.TestProject.AssemblyName));
            UpdateModulesClassesMethodsSummaries(sessionModule);

            try
            {
                if (individualTests == null || !individualTests.Any())
                {
                    // Tests have been run on the whole module, so any line not in CoverageSession is not "Covered"

                    newCoveredLineInfos = coverageService.GetCoveredLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName);

                    var newCoveredLineList = new List<Poco.CoveredLinePoco>();
                    Log.DebugFormat("SaveCoverageSessionResults for ModuleNane {0} ", moduleName);

                    UpdateUnitTests(sessionModule, testModule);

                    using (var context = new TestifyContext(_solutionName))
                    {
                        try
                        {

                            var outerSW = new Stopwatch();


                            outerSW.Restart();

                            var existingCoveredLines = GetCoveredLinesForModule(sessionModule.ModuleName, context);

                            var unitTests = context.UnitTests.Where(x => x.AssemblyName.Contains(projectInfo.TestProject.Path));

                            foreach (var line in newCoveredLineInfos)
                            {
                                line.Module = context.CodeModule.FirstOrDefault(x => x.Name == line.ModuleName);
                                line.Class = context.CodeClass.FirstOrDefault(x => x.Name == line.ClassName);
                                line.Method = context.CodeMethod.FirstOrDefault(x => x.Name == line.MethodName);

                                var existingLine = GetCoveredLinesByClassAndLine(existingCoveredLines, line);

                                foreach (var trackedMethod in line.TrackedMethods)
                                {

                                    var unitTest = SelectUnitTestByName(trackedMethod.NameInUnitTestFormat, context);
                                    line.UnitTests.Add(unitTest.FirstOrDefault());
                                }
                                if (existingLine != null)
                                {
                                    if (existingLine.IsCode != line.IsCode)
                                        existingLine.IsCode = line.IsCode;

                                    if (existingLine.IsCovered != line.IsCovered)
                                        existingLine.IsCovered = line.IsCovered;

                                }
                                else
                                {
                                    var newCoverage = ConstructCoveredLine(line);

                                    context.CoveredLines.Add(newCoverage);
                                }

                                // Todo Profile and refactor to improve performance
                                var sw = new Stopwatch();
                                sw.Restart();
                                foreach (var coveringTest in line.TrackedMethods)
                                {

                                    var matchingUnitTest = unitTests
                                                                  .FirstOrDefault(x => coveringTest.NameInUnitTestFormat
                                                                      .Equals(x.TestMethodName));
                                    if (matchingUnitTest != null && existingLine != null)
                                    {
                                        if (!existingLine.UnitTests.Any(x => x.UnitTestId == matchingUnitTest.UnitTestId))
                                        {
                                            existingLine.UnitTests.Add(matchingUnitTest);
                                        }
                                    }


                                }
                                ///todo remove  deleted Unit Tests
                                ///
                                //var listOfCurrentTests = newCoveredLineInfos.SelectMany(x=>x.UnitTests);
                                //var testsToRemove = from currentTest in listOfCurrentTests
                                //                    join existingTest in context.UnitTests
                                //                         on currentTest.TestMethodName equals existingTest.TestMethodName
                                //                    into test
                                //                    from testLeft in test.DefaultIfEmpty()

                                //                    select new { Orphan = existingTest,};

                                // Log.DebugFormat("Inner Loop in SaveCoverageSessionResults for ModuleNane {0} Elapsed Time: {1} ms",moduleName, sw.ElapsedMilliseconds);

                            }

                            outerSW.Stop();

                            Log.DebugFormat("Outer Loop in SaveCoverageSessionResults Elapsed Time: {0} ms", outerSW.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                        }
                        //foreach (var newLine in newCoveredLineInfos)
                        //{
                        //    context.CoveredLines.Attach(ConstructCoveredLine(newLine));
                        //}
                       
                        DoBulkCopy("CoveredLinePoco", newCoveredLineList, context);
                        // var isDirty = CheckForChanges(context);


                        try
                        {
                            context.SaveChanges();
                        }
                        catch (DbEntityValidationException dbEx)
                        {
                            foreach (var validationErrors in dbEx.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    Log.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                        }


                    }
                }
                else
                {
                    // Only a single unit test was run, so only the lines in the CoverageSession will be updated
                    // Get the MetadataTokens for the unitTests we just ran

                    var individualTestUniqueIds = new List<int>();

                    foreach (var test in individualTests)
                    {
                        var testMethodName = ConvertUnitTestFormatToFormatTrackedMethod(test);
                        individualTestUniqueIds.Add((int)testModule.TrackedMethods
                                                                 .Where(x => x.Name.Contains(testMethodName))
                                                                 .FirstOrDefault().UniqueId);
                    }

                    // GetMetadatTokenForUnitTest(individualTests);                     
                    List<int> unitTestIds;

                    using (var context = new TestifyContext(_solutionName))
                    {
                        var unitTests = context.UnitTests.Where(x => individualTests.Contains(x.TestMethodName));

                        unitTestIds = unitTests.Select(x => x.UnitTestId).ToList();

                        if (unitTestIds != null)
                        {
                            newCoveredLineInfos = coverageService.GetRetestedLinesFromCoverageSession(coverageSession, projectInfo.ProjectAssemblyName, individualTestUniqueIds);
                            changedClasses = newCoveredLineInfos.Select(x => x.Class.Name).Distinct().ToList();
                        }
                        context.SaveChanges();
                    }


                }

                RefreshUnitTestIds(newCoveredLineInfos, sessionModule, testModule);

                OnClassChanged(changedClasses);

                return changedClasses;

            }

            catch (Exception ex)
            {

                Log.ErrorFormat("Error in SaveCoverageSessionResults Inner Exception: {0} Message: {1} StackTrace {2}", ex.InnerException, ex.Message, ex.StackTrace);
                return new List<string>();

            }
        }

        public void SaveUnitTest(UnitTest test)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                context.UnitTests.Add(test);

                try
                {
                    context.SaveChanges();
                }

                catch (Exception ex)
                {
                    Log.ErrorFormat("Error SaveUnitTest, InnerException:{0}, UnitTest Name: {1}", ex.InnerException, test.TestMethodName);
                }
            }
        }
        public void SaveUnitTestResults(resultType testOutput)
        {

            string runDate = testOutput.date;

            string runTime = testOutput.time;

            string fileName = testOutput.name;

            var x = testOutput.testsuite;

            var unitTests = GetUnitTests(testOutput.testsuite);

            foreach (var test in unitTests)
            {
                test.LastRunDatetime = runDate + " " + runTime;

                test.AssemblyName = fileName;

                if (test.IsSuccessful)
                {
                    test.LastSuccessfulRunDatetime = DateTime.Parse(test.LastRunDatetime);

                }

                if (test.TestMethodName.Contains("("))
                {
                    Debug.WriteLine("test.TestMethodName= {0}", test.TestMethodName);
                }

            }

            using (var context = new TestifyContext(_solutionName))
            {

                try
                {
                    foreach (var test in unitTests)
                    {
                        var existingTest = context.UnitTests.FirstOrDefault(y => y.TestMethodName == test.TestMethodName);

                        if (existingTest == null)
                        {
                            // todo get the actual line number from the FileCodeModel for this unit test, to be used in the bookmark
                            test.LineNumber = "1";

                            context.UnitTests.Add(test);

                            Log.DebugFormat("Added UnitTest to Context: Name: {0}, IsSucessful : {1}", test.TestMethodName, test.IsSuccessful);

                        }
                        else
                        {
                            test.UnitTestId = existingTest.UnitTestId;

                            existingTest.LastSuccessfulRunDatetime = test.LastSuccessfulRunDatetime;

                            context.Entry(existingTest).CurrentValues.SetValues(test);
                        }

                    }

                    context.SaveChanges();

                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in SaveUnitTestResults Message: {0}, InnerException {1}", ex.Message, ex.InnerException);
                }
            }
        }

        public void SetAllQueuedTestsToNotRunning()
        {
            using (var context = new TestifyContext(_solutionName))
            {

                foreach (var test in context.TestQueue)
                {
                    test.TestRunId = 0;
                }

                context.SaveChanges();
            }
        }

        public void UpdateTrackedMethods(IList<Poco.TrackedMethod> trackedMethods)
        {
            using (var context = new TestifyContext(_solutionName))
            {

                foreach (var currentTrackedMethod in trackedMethods)
                {
                    //var existingTrackedMethod = context.TrackedMethods.Find(currentTrackedMethod.MetadataToken);
                    var existingTrackedMethod = context.TrackedMethods.Where(x => x.Name == currentTrackedMethod.Name).FirstOrDefault();

                    if (existingTrackedMethod == null)
                    {
                        context.TrackedMethods.Add(currentTrackedMethod);
                    }

                }

                _sw.Stop();

                Log.DebugFormat("UpdateTrackedMethods Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);

                try
                {
                    context.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Log.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error in UpdateTrackedMethods Message: {0}", ex.InnerException);
                }
            }
        }

        protected virtual void OnClassChanged(List<string> changedClasses)
        {

            var args = new ClassChangedEventArgs();

            args.ChangedClasses = changedClasses;

            if (ClassChanged != null)
            {
                ClassChanged(this, args);
            }
        }

        private static CoveredLinePoco ConstructCoveredLine(LineCoverageInfo line)
        {

            var newCoverage = new CoveredLinePoco
            {
                LineNumber = line.LineNumber,
                Method = line.Method,
                Class = line.Class,
                Module = line.Module,
                IsCode = line.IsCode,
                IsCovered = line.IsCovered,
                TrackedMethods = line.TrackedMethods,
                UnitTests = line.UnitTests
            };

            return newCoverage;
        }

        private static void DoBulkCopy(string tableName, List<Poco.CoveredLinePoco> coveredLines, TestifyContext context)
        {
            SqlCeBulkCopyOptions options = new SqlCeBulkCopyOptions();

            using (SqlCeBulkCopy bc = new SqlCeBulkCopy(context.Database.Connection.ConnectionString))
            {
                bc.DestinationTableName = tableName;
                bc.WriteToServer(coveredLines);
            }
        }

        private static Poco.CoveredLinePoco GetCoveredLinesByClassAndLine(ILookup<int, Poco.CoveredLinePoco> existingCoveredLines, LineCoverageInfo line)
        {
            var existingLine = existingCoveredLines[line.LineNumber].FirstOrDefault(x => x.Method.Equals(line.Method)
                                                && x.Class.Equals(line.Class));

            return existingLine;
        }

        private static ILookup<int, Poco.CoveredLinePoco> GetCoveredLinesForModule(string moduleName, TestifyContext context)
        {
            var existingCoveredLines = (from line in context.CoveredLines
                                        where line.Module.Name.Equals(moduleName)
                                        select line).ToLookup(x => x.LineNumber);

            return existingCoveredLines;
        }

        private static List<TestQueue> MarkTestAsInProgress(int testRunId, TestifyContext context, QueuedTest nextItem, List<TestQueue> testsToMarkInProgress)
        {
            nextItem.TestRunId = testRunId;

            // if the queued item has individual tests, we will remove all of these individual tests from queue.
            if (nextItem.IndividualTests == null)
            {
                testsToMarkInProgress = context.TestQueue.Where(x => x.ProjectName == nextItem.ProjectName).ToList();
            }
            else if (nextItem.IndividualTests.Any())
            {
                // if we are running all the tests for the project, we can remove all the individual and Project tests 
                foreach (var testToRun in nextItem.IndividualTests)
                {
                    testsToMarkInProgress = context.TestQueue.Where(x => x.IndividualTest == testToRun).ToList();
                }
            }

            foreach (var test in testsToMarkInProgress.ToList())
            {
                test.TestRunId = testRunId;
            }

            context.SaveChanges();

            return testsToMarkInProgress;
        }

        private static List<Poco.UnitTest> SelectUnitTestByName(string name, TestifyContext context)
        {
            var query = (from test in context.UnitTests
                         where test.TestMethodName.Equals(name)
                         select test);

            return query.ToList();
        }

        private Poco.UnitTest ConstructUnitTest(testcaseType testcase)
        {

            var unitTest = new Poco.UnitTest
            {
                TestDuration = testcase.time,
                TestMethodName = testcase.name,
                Executed = testcase.executed == bool.TrueString,
                Result = testcase.result,
                NumberOfAsserts = Convert.ToInt32(testcase.asserts),
                IsSuccessful = testcase.success == bool.TrueString
            };

            if (testcase.success == Boolean.TrueString)
            {
                unitTest.LastSuccessfulRunDatetime = DateTime.Now;
            }

            return unitTest;
        }

        private List<string> GetChangedMethods(TestifyContext context)
        {
            List<string> changedMethods = new List<string>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State != System.Data.EntityState.Unchanged)
                {
                    var methodName = entry.CurrentValues.GetValue<string>("Method");

                    if (!changedMethods.Contains(methodName))
                    {
                        changedMethods.Add(methodName);
                    }

                }
            }

            return changedMethods;
        }

        private List<string> GetChangedMethods(List<Poco.CoveredLinePoco> coveredLines)
        {
            var changedMethods = coveredLines.GroupBy(i => i.Method.Name)
                                                           .Select(i => i.Key)
                                                           .ToList();
            return changedMethods;
        }

        private List<Poco.UnitTest> GetUnitTests(object element)
        {
            var unitTests = new List<Poco.UnitTest>();

            if (element.GetType() == typeof(testcaseType))
            {
                testcaseType testcase = (testcaseType)element;

                var unitTest = ConstructUnitTest(testcase);

                unitTest.TestMethodName = testcase.name;

                unitTests.Add(unitTest);
            }
            else
            {

                if (element is testsuiteType)
                {
                    testsuiteType testsuite = (testsuiteType)element;

                    foreach (var item in testsuite.results.Items)
                    {
                        unitTests.AddRange(GetUnitTests(item));
                    }
                }
            }

            return unitTests;
        }

        private void RefreshUnitTestIds(IList<LineCoverageInfo> newCoveredLineInfos, Module module, Module testModule)
        {
            var trackedMethodLists = (from testInfo in newCoveredLineInfos
                                      where testInfo.TrackedMethods != null
                                      select testInfo.TrackedMethods);

            var trackedMethods = trackedMethodLists.SelectMany(x => x).ToList();

            UpdateModulesClassesMethodsSummaries(module);

            if (trackedMethods.Any())
            {
                var distinctTrackedMethods = trackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();

                UpdateUnitTests(module, testModule);

                UpdateTrackedMethods(distinctTrackedMethods);

                UpdateCoveredLines(module.FullName, distinctTrackedMethods, newCoveredLineInfos);


            }
        }

        private void UpdateModulesClassesMethodsSummaries(Module module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var codeModule = context.CodeModule.FirstOrDefault(x=> x.Name == module.ModuleName);
                if (codeModule != null)
                {
                    codeModule.Summary.BranchCoverage = module.Summary.BranchCoverage;
                    codeModule.Summary.MaxCyclomaticComplexity = module.Summary.MaxCyclomaticComplexity;
                    codeModule.Summary.MinCyclomaticComplexity = module.Summary.MinCyclomaticComplexity;
                    codeModule.Summary.NumBranchPoints = module.Summary.NumBranchPoints;
                    codeModule.Summary.NumSequencePoints = module.Summary.NumSequencePoints;
                    codeModule.Summary.SequenceCoverage = module.Summary.SequenceCoverage;
                    codeModule.Summary.VisitedBranchPoints = module.Summary.VisitedBranchPoints;
                    codeModule.Summary.VisitedSequencePoints = module.Summary.VisitedSequencePoints;
                }
                else 
                {
                    codeModule = new Poco.CodeModule(module);
                    context.CodeModule.Add(codeModule);
                }

                UpdateCodeClasses(module, codeModule, context);

                context.SaveChanges();

            }
        }

        private void UpdateCodeClasses(Module module, CodeModule codeModule, TestifyContext context)
        {
            foreach (var moduleClass in module.Classes)
            {
                var pocoCodeClass = context.CodeClass.FirstOrDefault(x => x.Name == moduleClass.FullName);
                if (!moduleClass.FullName.Contains("__"))
                {
                    if (pocoCodeClass != null)
                    {
                        pocoCodeClass.Summary.BranchCoverage = moduleClass.Summary.BranchCoverage;
                        pocoCodeClass.Summary.MaxCyclomaticComplexity = moduleClass.Summary.MaxCyclomaticComplexity;
                        pocoCodeClass.Summary.MinCyclomaticComplexity = moduleClass.Summary.MinCyclomaticComplexity;
                        pocoCodeClass.Summary.NumBranchPoints = moduleClass.Summary.NumBranchPoints;
                        pocoCodeClass.Summary.NumSequencePoints = moduleClass.Summary.NumSequencePoints;
                        pocoCodeClass.Summary.SequenceCoverage = moduleClass.Summary.SequenceCoverage;
                        pocoCodeClass.Summary.VisitedBranchPoints = moduleClass.Summary.VisitedBranchPoints;
                        pocoCodeClass.Summary.VisitedSequencePoints = moduleClass.Summary.VisitedSequencePoints;

                    }
                    else
                    {
                        pocoCodeClass = new Poco.CodeClass(moduleClass);
                        codeModule.Classes.Add(pocoCodeClass);
                    }

                    UpdateCodeMethods(moduleClass, pocoCodeClass, context);
                }
            }
        }

        private void UpdateCodeMethods(Class codeClass, Poco.CodeClass pocoCodeClass, TestifyContext context)
        {
            foreach (var moduleMethod in codeClass.Methods)
            {
                var codeMethod = context.CodeMethod.FirstOrDefault(x => x.Name == moduleMethod.Name);

                if (!moduleMethod.Name.Contains("__"))
                { 

                    if (codeMethod != null)
                    {
                        codeMethod.Summary.BranchCoverage = moduleMethod.Summary.BranchCoverage;
                        codeMethod.Summary.MaxCyclomaticComplexity = moduleMethod.Summary.MaxCyclomaticComplexity;
                        codeMethod.Summary.MinCyclomaticComplexity = moduleMethod.Summary.MinCyclomaticComplexity;
                        codeMethod.Summary.NumBranchPoints = moduleMethod.Summary.NumBranchPoints;
                        codeMethod.Summary.NumSequencePoints = moduleMethod.Summary.NumSequencePoints;
                        codeMethod.Summary.SequenceCoverage = moduleMethod.Summary.SequenceCoverage;
                        codeMethod.Summary.VisitedBranchPoints = moduleMethod.Summary.VisitedBranchPoints;
                        codeMethod.Summary.VisitedSequencePoints = moduleMethod.Summary.VisitedSequencePoints;
                    }
                    else
                    {

                        var pocoCodeMethod = new Poco.CodeMethod(moduleMethod);
                        pocoCodeClass.Methods.Add(pocoCodeMethod);
                    }
                }
            }
        }

        private void UpdateCoveredLines(string moduleName, List<Poco.TrackedMethod> trackedMethods, IList<LineCoverageInfo> newCoveredLineInfos)
        {
            var newCoveredMethodIds = newCoveredLineInfos.GroupBy(g => g.Method)
                                                        .Select(m => m.First().Method.CodeMethodId)
                                                        .ToList();

            using (var context = new TestifyContext(_solutionName))
            {

                var coveredLines = context.CoveredLines.Include(x=>x.Class).Include(y=>y.Module).Include(z=>z.Method);//.Where(x => newCoveredMethods.Contains(x.Method.Name));

                foreach (var coveredLine in coveredLines)
                {
                    var line = newCoveredLineInfos.FirstOrDefault(x => x.Method.CodeMethodId == coveredLine.Method.CodeMethodId && x.LineNumber == coveredLine.LineNumber);
                    //var existingLine = from aline in context.CoveredLines
                    //                   where aline.LineNumber == coveredLine.LineNumber
                    //           &&  aline.Method.CodeMethodId == coveredLine.Method.CodeMethodId
                    //           select aline;
                    // line = existingLine;
                    string testMethodName = string.Empty;

                    if (line != null && line.TrackedMethods.Any())
                    {
                        testMethodName = line.TrackedMethods.FirstOrDefault().NameInUnitTestFormat;

                    }

                    var unitTests = context.UnitTests.Where(x => x.TestMethodName == testMethodName);

                    foreach (var test in unitTests)
                    {
                        coveredLine.UnitTests.Add(test);
                    }

                    if (unitTests.Any(x => x.IsSuccessful == true))
                    {
                        coveredLine.IsSuccessful = true;
                    }

                    if (unitTests.Any(x => x.IsSuccessful == false))
                    {
                        coveredLine.IsSuccessful = false;
                    }


                }

                context.SaveChanges();
            }
        }

        private void UpdateProjects(IList<Project> projects, TestifyContext context)
        {
            _sw.Restart();

            // Existing projects
            foreach (var currentProject in projects)
            {
                var existingProject = context.Projects.Find(currentProject.UniqueName);
                if (existingProject != null)
                {

                    // update the path
                    if (currentProject.Path != existingProject.Path
                        && !string.IsNullOrEmpty(currentProject.Path))
                    {
                        existingProject.Path = currentProject.Path;
                    }

                    // update the assembly name
                    if (currentProject.AssemblyName != existingProject.AssemblyName
                        && !string.IsNullOrEmpty(currentProject.AssemblyName))
                    {
                        existingProject.AssemblyName = currentProject.AssemblyName;
                    }
                }
            }

            try
            {
                // Add new projects
                var newProjects = (from currentProject in projects
                                   where !(currentProject.UniqueName.Contains(".Test."))
                                   where !(currentProject.UniqueName.Contains("Solution Items"))
                                   where !(currentProject.UniqueName.Contains("Miscellaneous Files"))
                                   where !(from existing in context.Projects
                                           select existing.UniqueName).Contains(currentProject.UniqueName)
                                   select currentProject).ToList();
                newProjects.ForEach(p => context.Projects.Add(p));

                /// Todo - Delete projects from database that no longer exist in solution.

                context.SaveChanges();

                _sw.Stop();

                Log.DebugFormat("UpdateProjects Elapsed Time {0} milliseconds", _sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Log.DebugFormat("Error in UpdateProjects Message: {0}", ex);
                throw;
            }

        }
        private void UpdateTestProjects(IList<Project> projects, TestifyContext context)
        {

            // Existing projects
            foreach (var currentProject in projects)
            {
                Log.DebugFormat("Project Name: {0}, AssemblyName: {1}, UniqueName: {2}", currentProject.Name, currentProject.AssemblyName, currentProject.UniqueName);

                var existingProject = context.TestProjects.Find(currentProject.UniqueName);

                if (existingProject != null)
                {
                    if (currentProject.Path != existingProject.Path
                        && !string.IsNullOrEmpty(currentProject.Path))
                    {
                        existingProject.Path = currentProject.Path;
                    }

                    if (string.IsNullOrEmpty(existingProject.AssemblyName)
                        && !string.IsNullOrEmpty(currentProject.AssemblyName))
                    {
                        existingProject.AssemblyName = currentProject.AssemblyName;
                    }
                }

            }

            // Add new projects
            var newProjects = (from currentProject in projects
                               where (currentProject.Name.Contains(".Test"))
                               && !(from existing in context.TestProjects
                                    select existing.UniqueName).Contains(currentProject.UniqueName)
                               select currentProject).ToList();

            foreach (var newProject in newProjects)
            {
                Log.DebugFormat("New Project Name: {0}, UniqueName: {1}", newProject.Name, newProject.UniqueName);
                var targetProjectName = newProject.Name.Replace(".Test", string.Empty);
                var targetProject = context.Projects.FirstOrDefault(x => x.Name.Equals(targetProjectName));
                var existingProject = context.Projects.FirstOrDefault(x => x.Name.Contains(newProject.Name));

                if (targetProject != null)
                {
                    if (existingProject != null)
                    {
                        existingProject.Name = newProject.Name;
                        existingProject.UniqueName = newProject.UniqueName;
                        existingProject.Path = newProject.Path;
                        existingProject.AssemblyName = newProject.AssemblyName;
                    }
                    else
                    {
                        var newTestProject = new Poco.TestProject
                        {
                            Name = newProject.Name,
                            UniqueName = newProject.UniqueName,
                            Path = newProject.Path,
                            ProjectUniqueName = targetProject.UniqueName,
                            OutputPath = newProject.Path,
                            AssemblyName = newProject.AssemblyName
                        };
                        context.TestProjects.Add(newTestProject);
                    }
                }

            }
        }
        private void UpdateUnitTests(Module codeModule, Module testModule)
        {
            _sw.Restart();

            var distinctTrackedMethods = testModule.TrackedMethods.GroupBy(x => x.MetadataToken).Select(y => y.First()).ToList();

            try
            {
                if (testModule != null)
                {
                    using (var context = new TestifyContext(_solutionName))
                    {
                        var testProjectUniqueName = context.TestProjects.Where(x => x.AssemblyName.Equals(testModule.ModuleName)).First().UniqueName;

                        //Create Unit Test objects
                        var unitTests = new List<Poco.UnitTest>();
                        foreach (var trackedMethod in distinctTrackedMethods)
                        {
                            var testMethodName = ConvertTrackedMethodFormatToUnitTestFormat(trackedMethod.Name);

                            //Todo modify the next line to properly handle TestCases, The Tracking method
                            // The TrackedMethod contains the argument Type in parenthesis
                            //"System.Void Quad.QuadMed.QMedClinicalTools.Domain.Test.Services.PatientMergeServiceTest::CanGetHealthAssessmentsByQMedPidNumber(System.String)"
                            // The UnitTest is saved with the actual value of the argument in parenthesis
                            // Quad.QuadMed.QMedClinicalTools.Domain.Test.Services.PatientMergeServiceTest.CanGetHealthAssessmentsByQMedPidNumber("110989")
                            // The Unit test doesn't match because  (System.String) <> ("110989")

                            var matchingUnitTest = context.UnitTests.FirstOrDefault(x => x.TestMethodName.Equals(testMethodName));

                            if (matchingUnitTest != null)
                            {
                                matchingUnitTest.TestProjectUniqueName = testProjectUniqueName;
                                trackedMethod.UnitTestId = matchingUnitTest.UnitTestId;

                            }
                            else
                            {
                                Log.DebugFormat("ERROR: Could not find Unit test that matched Tracking Method: {0}", trackedMethod.Name);
                            }

                        }

                        context.SaveChanges();

                    }

                }
                else
                {
                    Log.DebugFormat("UpdateUnitTests was called with a Null TestModule");
                }

            }

            catch (Exception ex)
            {
                Log.ErrorFormat("Error in UpdateUnitTests Message: {0} Message: {1}", ex.InnerException, ex.Message);

            }
        }


        public CodeModule[] GetModules()
        {
            using (var context = new TestifyContext(_solutionName) )
            {
                return context.CodeModule.Include(x=>x.Summary).ToArray();
            }

        }


        public CodeClass[] GetClasses(CodeModule module)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.CodeClass.Where(x => x.CodeModule.CodeModuleId == module.CodeModuleId).Include(x => x.Summary).ToArray();
            }
        }


        public CodeMethod[] GetMethods(CodeClass _class)
        {
            using (var context = new TestifyContext(_solutionName))
            {
                return context.CodeMethod.Where(x => x.CodeClassId == _class.CodeClassId).Include(x => x.Summary).ToArray();
            }
        }

        public CodeModule[] GetSummaries() 
        {
            using (var context = new TestifyContext(_solutionName))
            {
                var result = context.CodeModule
                    .Include(x => x.Summary)
                    .Include(y => y.Classes.Select(c => c.Summary))
                    .Include(y => y.Classes.Select(m => m.Methods))
                    .Include(y => y.Classes.Select(mm => mm.Methods.Select(s=>s.Summary)))
                    
                    .Include(z => z.Summary).ToArray();
                return result;
            }
        }
    }

}