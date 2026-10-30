using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace HerFootsteps.Editor
{
    [InitializeOnLoad]
    public static class Milestone7BTestRunner
    {
        private static TestRunnerApi runner;
        static Milestone7BTestRunner(){if(SessionState.GetBool("HF.M7B.Tests",false))Register();}
        private static void Register(){runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());}
        [MenuItem("Her Footsteps/Presentation/Test Milestone 7B")]
        public static void Run(){SessionState.SetBool("HF.M7B.Tests",true);Register();runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"HerFootsteps.Tests.Milestone7BPlayTests"}}));}
        private sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor test){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor test){}
            public void RunFinished(ITestResultAdaptor result){Directory.CreateDirectory("Logs");TestRunnerApi.SaveResultToFile(result,"Logs/Milestone7B-tests.xml");File.WriteAllText("Logs/Milestone7B-tests.txt",$"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}\n{result.Message}");SessionState.SetBool("HF.M7B.Tests",false);runner.UnregisterCallbacks(this);Object.DestroyImmediate(runner);}
        }
    }
}
