using System.IO;
using MergeGame.App.Saving;
using MergeGame.PlayTests.Support;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(TestSaveFolder))]

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Keeps the PlayMode tests away from the player's own saved game and from each other: the game saves into a
    /// folder of the test run's own, emptied before every test, so each test opens the game from its opening layout
    /// unless it saved something itself. The folder is removed when the run ends.
    /// </summary>
    public sealed class TestSaveFolder : ITestRunCallback
    {
        public static string Directory => Path.Combine(Application.temporaryCachePath, "MergeGameTestSaves");

        public void RunStarted(ITest testsToRun) => Use();

        public void TestStarted(ITest test)
        {
            // Set again per test: entering play mode may have reloaded the domain since the run started.
            Use();
            if (!test.IsSuite)
                Empty();
        }

        public void TestFinished(ITestResult result)
        {
        }

        public void RunFinished(ITestResult testResults)
        {
            Empty();
            GameSaveLocation.DirectoryOverride = null;
        }

        /// <summary>Removes every file the game saved during the test so far.</summary>
        public static void Empty()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }

        private static void Use() => GameSaveLocation.DirectoryOverride = Directory;
    }
}
