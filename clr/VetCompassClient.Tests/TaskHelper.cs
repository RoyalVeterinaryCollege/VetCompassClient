using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using VetCompass.Client;

namespace VetCompassClient.Tests
{
    [TestFixture]
    public class TaskHelper
    {
        private static Task<string> MakeFaultyTask()
        {
            return Task.Factory.StartNew<string>(() => { throw new Exception("task1 fault"); });
        }

        [Test]
        public void A_middle_failure_is_a_failure()
        {
            var task =
                Task.Factory.StartNew(() => "initial task result")
                    .MapSuccess(x => x.Length)
                    .FlatMapSuccess(x => MakeFaultyTask()) //error 
                    .MapSuccess(x => x.Length);
            try
            {
                task.Wait(); //Wait throws annoyingly...
            }
            catch
            {
            } //ignore this annoying behaviour 
            finally
            {
                task.IsFaulted.Should().BeTrue("error should be propagated to final task");
            }
        }

        [Test]
        public void Antecedant_error_handling_unaffacted_by_error_action()
        {
            var task1 = MakeFaultyTask();
            var task2 = task1.MapSuccess(str => str.Length);
            AggregateException exception = null;
            var task3 = task2.ActOnFailure(e => exception = e);

            try
            {
                task3.Wait(); //Wait throws annoyingly...
            }
            catch
            {
            } //ignore this annoying behaviour 
            finally
            {
                task3.IsFaulted.Should().BeTrue("error should be propagated to final task");
                task3.Exception.Flatten().InnerExceptions.First().Should().Be(task1.Exception.InnerExceptions.First());
                exception.Should().NotBeNull("action on error not called");
            }
        }

        [Test]
        public void Antecedant_failure_is_propagated_to_subsequent_tasks()
        {
            var task1 = MakeFaultyTask();
            var task2 = task1.MapSuccess(str => str.Length);

            try
            {
                task2.Wait(); //Wait throws annoyingly...
            }
            catch
            {
            } //ignore this annoying behaviour 
            finally
            {
                task2.IsFaulted.Should().BeTrue();
                task2.Exception.Flatten().InnerExceptions.First().Should().Be(task1.Exception.InnerExceptions.First());
            }
        }

        [Test]
        public void Chaining_a_few_failures_still_propagates()
        {
            var task =
                MakeFaultyTask()
                    .MapSuccess(x => x.Length)
                    .MapSuccess(x => x*x);
            try
            {
                task.Wait(); //Wait throws annoyingly...
            }
            catch
            {
            } //ignore this annoying behaviour 
            finally
            {
                task.IsFaulted.Should().BeTrue("error should be propagated to final task");
            }
        }

        [Test]
        public void FlatMapping_an_antecedant_error_propagates_the_error()
        {
            var task1 = MakeFaultyTask();
            var task2 = task1.FlatMapSuccess(innerTask => Task.Factory.StartNew(() => "next task result"));

            try
            {
                task2.Wait(); //Wait throws annoyingly...
            }
            catch
            {
            } //ignore this annoying behaviour 
            finally
            {
                task2.IsFaulted.Should().BeTrue("error should be propagated to final task");
                task2.Exception.Flatten().InnerExceptions.First().Should().Be(task1.Exception.InnerExceptions.First());
            }
        }
    }
}