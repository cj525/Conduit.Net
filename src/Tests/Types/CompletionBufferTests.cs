using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Tests.Types
{
    [TestFixture]
    public class CompletionBufferTests
    {
        [Test]
        public void RemoveHead()
        {
            var c1 = default(ICompletable);
            var c2 = default(ICompletable);
            var c3 = default(ICompletable);
            var buffer = CommonSetup(out c1, out c2, out c3);

            CommonScenario(buffer, () => c1.Complete(),4);

            var aBuffer = buffer.ToArray();

            Assert.AreEqual(aBuffer[0], "Item 2");
            Assert.AreEqual(aBuffer[1], "Item 3");
            Assert.AreEqual(aBuffer[2], "Item 4");
            
            buffer.Dispose();
        }



        [Test]
        public void RemoveFromMiddle()
        {
            var c1 = default(ICompletable);
            var c2 = default(ICompletable);
            var c3 = default(ICompletable);
            var buffer = CommonSetup(out c1, out c2, out c3);

            CommonScenario(buffer, () => c2.Complete(), 4);

            var aBuffer = buffer.ToArray();
            Assert.AreEqual(aBuffer[0], "Item 1");
            Assert.AreEqual(aBuffer[1], "Item 3");
            Assert.AreEqual(aBuffer[2], "Item 4");
        }

        [Test]
        public void RemoveTail()
        {
            var c1 = default(ICompletable);
            var c2 = default(ICompletable);
            var c3 = default(ICompletable);
            var buffer = CommonSetup(out c1, out c2, out c3);

            CommonScenario(buffer, () => c3.Complete(), 4);

            var aBuffer = buffer.ToArray();
            Assert.AreEqual(aBuffer[0], "Item 1");
            Assert.AreEqual(aBuffer[1], "Item 2");
            Assert.AreEqual(aBuffer[2], "Item 4");
        }


        [Test]
        public void BlockTwiceAndRemoveHeadAndTail()
        {
            var c1 = default(ICompletable);
            var c2 = default(ICompletable);
            var c3 = default(ICompletable);
            var buffer = CommonSetup(out c1, out c2, out c3);

            var c4 = CommonScenario(buffer, () => c1.Complete(), 4);
            c3.Complete();
            CommonScenario(buffer, () => c4.Complete(), 5);

            var aBuffer = buffer.ToArray();
            Assert.AreEqual(aBuffer[0], "Item 2");
            Assert.AreEqual(aBuffer[1], "Item 5");
        }

        private static CompletionBuffer<string> CommonSetup(out ICompletable c1, out ICompletable c2, out ICompletable c3)
        {
            var buffer = new CompletionBuffer<string>(3);

            c1 = buffer.Add("Item 1");
            c2 = buffer.Add("Item 2");
            c3 = buffer.Add("Item 3");

            return buffer;
        }

        private static ICompletable CommonScenario(CompletionBuffer<string> buffer, Action action, int value)
        {
            var r = default(ICompletable);
            bool hit = false;
            var t1 = Task.Run(() =>
            {
                bool doCheck = buffer.IsFull;
                r = buffer.Add("Item " + value);

                if (buffer.Count > 3 )
                    Assert.Fail("Too many items");
                if(doCheck && !hit)
                    Assert.Fail("Did not block");
            });
            // Allow t1 to get going
            Thread.Yield();
            var t2 = Task.Run(async () =>
            {
                // Allow t1 to hit the block
                await Task.Delay(1);
                hit = true;
                action();
            });



            Task.WaitAll(t1, t2);
            return r;
        }
    }
}
