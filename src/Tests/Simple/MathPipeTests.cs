using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Components;

// ReSharper disable BuiltInTypeReferenceStyle
namespace Pipes.Tests.Simple
{
    [TestFixture]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification="XUnit Tests")]
    public class MathPipeTests
    {
        private const string AnswerFormat = "The answer is {0}.";

        private void TestAddMultipleFormatPipe(int input, string output)
        {
            var pipeline = new BasicServicePipe.Add2Times2Formatted {Add = 2, ThenMultiply = 3};
            pipeline.Initialize();
            pipeline.Invoke(input);
            Assert.AreEqual(pipeline.Result, output);
        }



        [Test]
        [Ignore] //Test is invalid atm
        public void That2Plus2Times3_Equals8()
        {
            TestAddMultipleFormatPipe(2, String.Format(AnswerFormat, 12));
        }

        [Test]
        [Ignore] //Test is invalid atm
        public void That5Plus2Times3_Equals14()
        {
            TestAddMultipleFormatPipe(5, String.Format(AnswerFormat, 21));
        }


        // ReSharper disable once ClassNeverInstantiated.Local
        class BasicServicePipe
        {
            internal class Add2Times2Formatted : Pipeline
            {
                public int Add;
                public int ThenMultiply;
                public Action<object> Invoke;

                public string Result { get; private set; }

                public override void Initialize()
                {
                    CreateMessageTap<StringValue>().WhichUnwrapsAndCalls(sz => Result = sz.Value);

                    base.Initialize();
                }

                protected override void Describe(IPipelineBuilder<IOperationContext> thisPipeline)
                {
                    var addTwo = Component;
                    var timesTwo = Component;
                    var toString = Component;

                    thisPipeline
                        .Constructs(() => new AddInt { Value = Add })
                        .Into(ref addTwo);

                    thisPipeline
                        .Constructs(() => new MultiplyInt { Value = ThenMultiply })
                        .Into(ref timesTwo);

                    thisPipeline
                        .Constructs(() => new ToString<object>(AnswerFormat))
                        .Into(ref toString);

                    thisPipeline
                        .IsInvokedBy(ref Invoke)
                        .WhichTransmitsTo(addTwo);

                    addTwo.SendsMessagesTo(timesTwo);
                    timesTwo.SendsMessagesTo(toString);
                    toString.BroadcastsAllMessages();
                }
            }
        }
    }
}
