using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections.Tests
{
    internal class CollectionsTestCommonBase
    {
        [SetUp]
        public virtual void Setup()
        {
#if UNITY_DOTSRUNTIME
            Unity.Runtime.TempMemoryScope.EnterScope();
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
#if UNITY_DOTSRUNTIME
            Unity.Runtime.TempMemoryScope.ExitScope();
#endif
        }
    }

    /// <summary>
    /// Collections test fixture to do setup and teardown.
    /// </summary>
    /// <remarks>
    /// Jobs debugger and safety checks should always be enabled when running collections tests. This fixture verifies
    /// those are enabled to prevent crashing the editor.
    /// </remarks>
    internal abstract class CollectionsTestFixture : CollectionsTestCommonBase
    {
        static string SafetyChecksMenu = "Jobs > Burst > Safety Checks";
        private bool JobsDebuggerWasEnabled;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            JobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
#if !UNITY_DOTSRUNTIME
            Assert.IsTrue(BurstCompiler.Options.EnableBurstSafetyChecks, $"Collections tests must have Burst safety checks enabled! To enable, go to {SafetyChecksMenu}");
#endif
        }

        [TearDown]
        public override void TearDown()
        {
            JobsUtility.JobDebuggerEnabled = JobsDebuggerWasEnabled;

            base.TearDown();
        }
    }
}
