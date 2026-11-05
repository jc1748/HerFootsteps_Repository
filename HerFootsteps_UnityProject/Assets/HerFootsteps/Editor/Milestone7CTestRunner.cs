using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class Milestone7CTestRunner
    {
        private static TestRunnerApi runner;
        static Milestone7CTestRunner(){if(SessionState.GetBool("HF.M7C.Tests",false))Register();}
        private static void Register(){runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());}
        [MenuItem("Her Footsteps/Presentation/Test Milestone 7C")]
        public static void Run(){SessionState.SetBool("HF.M7C.Tests",true);Register();runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"HerFootsteps.Tests.Milestone7CPlayTests","HerFootsteps.Tests.Milestone7BPlayTests"}}));}
        private sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor test){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor test){}
            public void RunFinished(ITestResultAdaptor result){Directory.CreateDirectory("Logs");TestRunnerApi.SaveResultToFile(result,"Logs/Milestone7C-tests.xml");File.WriteAllText("Logs/Milestone7C-tests.txt",$"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}\n{result.Message}");SessionState.SetBool("HF.M7C.Tests",false);runner.UnregisterCallbacks(this);Object.DestroyImmediate(runner);}
        }
    }
}
