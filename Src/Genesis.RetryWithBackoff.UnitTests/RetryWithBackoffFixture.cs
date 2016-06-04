namespace Genesis.Ensure.UnitTests
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Microsoft.Reactive.Testing;
    using Xunit;

    public sealed class RetryWithBackoffFixture
    {
        [Fact]
        public void retries_indefinitely_if_no_retry_count_specified()
        {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable
                .Defer(
                    () =>
                    {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            var sut = source
                .RetryWithBackoff(scheduler: scheduler)
                .Subscribe(
                    _ => { },
                    ex => { });
            scheduler.AdvanceBy(TimeSpan.FromDays(1));

            Assert.Equal(486, tries);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(42)]
        public void retry_count_determines_how_many_times_to_retry(int retryCount)
        {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable
                .Defer(
                    () =>
                    {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            var sut = source
                .RetryWithBackoff(retryCount, scheduler: scheduler)
                .Subscribe(
                    _ => { },
                    ex => { });
            scheduler.AdvanceUntilEmpty();

            Assert.Equal(retryCount, tries);
        }

        [Fact]
        public void default_strategy_is_exponential_backoff_to_a_maximum_of_three_minutes()
        {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable
                .Defer(
                    () =>
                    {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            var sut = source
                .RetryWithBackoff(100, scheduler: scheduler)
                .Subscribe(
                    _ => { },
                    ex => { });
            Assert.Equal(1, tries);

            var @try = 1;

            for (var i = 0; i < 7; ++i)
            {
                scheduler.AdvanceBy(TimeSpan.FromSeconds(Math.Pow(2, @try)) - TimeSpan.FromMilliseconds(1));
                Assert.Equal(@try, tries);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
                Assert.Equal(++@try, tries);
            }

            // we've reached the 3 minute maximum delay
            for (var i = 0; i < 5; ++i)
            {
                scheduler.AdvanceBy(TimeSpan.FromMinutes(3) - TimeSpan.FromMilliseconds(1));
                Assert.Equal(@try, tries);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
                Assert.Equal(++@try, tries);
            }
        }

        [Fact]
        public void strategy_determines_time_between_retries()
        {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable
                .Defer(
                    () =>
                    {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            var sut = source
                .RetryWithBackoff(100, strategy: n => TimeSpan.FromSeconds(n), scheduler: scheduler)
                .Subscribe(
                    _ => { },
                    ex => { });
            Assert.Equal(1, tries);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(999));
            Assert.Equal(1, tries);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
            Assert.Equal(2, tries);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1999));
            Assert.Equal(2, tries);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
            Assert.Equal(3, tries);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(2999));
            Assert.Equal(3, tries);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
            Assert.Equal(4, tries);
        }

        [Fact]
        public void retry_on_error_determines_whether_a_given_exception_results_in_a_retry()
        {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable
                .Defer(
                    () =>
                    {
                        ++tries;

                        if (tries < 3)
                        {
                            return Observable.Throw<Unit>(new InvalidOperationException());
                        }

                        return Observable.Throw<Unit>(new Exception());
                    });
            var sut = source
                .RetryWithBackoff(100, retryOnError: ex => ex is InvalidOperationException, scheduler: scheduler)
                .Subscribe(
                    _ => { },
                    ex => { });
            Assert.Equal(1, tries);

            scheduler.AdvanceUntilEmpty();
            Assert.Equal(3, tries);
        }
    }
}