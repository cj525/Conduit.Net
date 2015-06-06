using Pipes.Tests.Simple;

namespace Pipes.Tests
{
    static class EntryPoint
    {
        static void Main(string[] args)
        {
            var mathPipeTests = new MathPipeTests();
            mathPipeTests.That2Plus2Times3_Equals8();
            mathPipeTests.That5Plus2Times3_Equals14();
        }
    }
}
