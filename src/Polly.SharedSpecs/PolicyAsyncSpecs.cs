﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Polly.Specs.Helpers;
using Polly.Utilities;
using Xunit;

namespace Polly.Specs
{
    public class PolicyAsyncSpecs
    {
        [Fact]
        public async Task Executing_the_policy_action_should_execute_the_specified_async_action()
        {
            bool executed = false;

            Policy policy = Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { });

            await policy.ExecuteAsync(() =>
            {
                executed = true;
                return TaskHelper.EmptyTask;
            });

            executed.Should()
                .BeTrue();
        }

        [Fact]
        public async Task Executing_the_policy_function_should_execute_the_specified_async_function_and_return_the_result()
        {
            Policy policy = Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { });

            int result = await policy.ExecuteAsync(() => Task.FromResult(2));

            result.Should()
                .Be(2);
        }

        [Fact]
        public async Task Executing_the_policy_action_successfully_should_return_success_result()
        {
            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync(() => Task.FromResult(0) as Task);

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Successful,
                FinalException = (Exception)null,
                ExceptionType = (ExceptionType?)null,
            });
        }

        [Fact]
        public async Task Executing_the_policy_action_and_failing_with_a_defined_exception_type_should_return_failure_result_indicating_that_exception_type_is_one_handled_by_this_policy()
        {
            var definedException = new DivideByZeroException();

            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync(() =>
                {
                    throw definedException;
                });

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Failure,
                FinalException = definedException,
                ExceptionType = ExceptionType.HandledByThisPolicy
            });
        }

        [Fact]
        public async Task Executing_the_policy_action_and_failing_with_an_undefined_exception_type_should_return_failure_result_indicating_that_exception_type_is_unhandled_by_this_policy()
        {
            var undefinedException = new Exception();

            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync(() =>
                {
                    throw undefinedException;
                });

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Failure,
                FinalException = undefinedException,
                ExceptionType = ExceptionType.Unhandled
            });
        }

        [Fact]
        public async Task Executing_the_policy_function_successfully_should_return_success_result()
        {
            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync(() => Task.FromResult(Int32.MaxValue));

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Successful,
                FinalException = (Exception)null,
                ExceptionType = (ExceptionType?)null,
                FaultType = (FaultType?)null,
                FinalHandledResult = default(int),
                Result = Int32.MaxValue
            });
        }

        [Fact]
        public async Task Executing_the_policy_function_and_failing_with_a_defined_exception_type_should_return_failure_result_indicating_that_exception_type_is_one_handled_by_this_policy()
        {
            var definedException = new DivideByZeroException();

            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync<int>(() =>
                {
                    throw definedException;
                });

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Failure,
                FinalException = definedException,
                ExceptionType = ExceptionType.HandledByThisPolicy,
                FaultType = FaultType.ExceptionHandledByThisPolicy,
                FinalHandledResult = default(int),
                Result = default(int)
            });
        }

        [Fact]
        public async Task Executing_the_policy_function_and_failing_with_an_undefined_exception_type_should_return_failure_result_indicating_that_exception_type_is_unhandled_by_this_policy()
        {
            var undefinedException = new Exception();

            var result = await Policy
                .Handle<DivideByZeroException>()
                .RetryAsync((_, __) => { })
                .ExecuteAndCaptureAsync<int>(() =>
                {
                    throw undefinedException;
                });

            result.ShouldBeEquivalentTo(new
            {
                Outcome = OutcomeType.Failure,
                FinalException = undefinedException,
                ExceptionType = ExceptionType.Unhandled,
                FaultType = FaultType.UnhandledException,
                FinalHandledResult = default(int),
                Result = default(int)
            });
        }

        [Theory, MemberData("SyncPolicies")]
        public void Executing_the_synchronous_policies_using_the_asynchronous_execute_should_throw_an_invalid_operation_exception(Policy syncPolicy, string description)
        {
            syncPolicy
                .Awaiting(async x => await x.ExecuteAsync(() => TaskHelper.EmptyTask))
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("Please use asynchronous-defined policies when calling asynchronous ExecuteAsync (and similar) methods.");
        }

        [Theory, MemberData("SyncPolicies")]
        public void Executing_the_synchronous_policies_using_the_asynchronous_execute_and_capture_should_throw_an_invalid_operation_exception(Policy syncPolicy, string description)
        {
            syncPolicy
                .Awaiting(async x => await x.ExecuteAndCaptureAsync(() => TaskHelper.EmptyTask))
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("Please use asynchronous-defined policies when calling asynchronous ExecuteAsync (and similar) methods.");
        }

        public static IEnumerable<object[]> SyncPolicies
        {
            get
            {
                return new[]
                {
                    new object[] {RetryPolicy(), "retry"},
                    new object[] {RetryForeverPolicy(), "retry forever"},
                    new object[] {WaitAndRetryPolicy(), "wait and retry"},
                    new object[] {CircuitBreakerPolicy(), "circuit breaker"}
                };
            }
        }

        private static Policy RetryPolicy()
        {
            return Policy
                .Handle<DivideByZeroException>()
                .Retry((_, __) => { });
        }

        private static Policy RetryForeverPolicy()
        {
            return Policy
                .Handle<DivideByZeroException>()
                .RetryForever((_) => { });
        }

        private static Policy WaitAndRetryPolicy()
        {
            return Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetry(new TimeSpan[] { });
        }

        private static Policy CircuitBreakerPolicy()
        {
            return Policy
                .Handle<DivideByZeroException>()
                .CircuitBreaker(1, new TimeSpan());
        }
    }
}