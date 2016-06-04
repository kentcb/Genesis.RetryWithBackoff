namespace System.Reactive.Linq
{
    using System;
    using Concurrency;

    /// <summary>
    /// Provides the <see cref="RetryWithBackoff"/> extension method.
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        /// The default retry strategy for <see cref="RetryWithBackoff"/>, which waits n^2 seconds between each retry, or 180 seconds, whichever is smaller.
        /// </summary>
        public static readonly Func<int, TimeSpan> DefaultStrategy = n => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, n), 180));

        /// <summary>
        /// Retries an observable upon failure, using the provided strategy to determine how long to wait between retries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This extension method can be used to retry any source pipeline a specified number of times, with a custom
        /// wait period between those retries. The <paramref name="retryCount"/> parameter determines the maximum number of retries. The
        /// default value is <see langword="null"/>, which means there is no maximum (will retry indefinitely). The
        /// <paramref name="strategy"/> parameter dictates the period between retries, and it defaults to <see cref="DefaultStrategy"/>.
        /// </para>
        /// <para>
        /// The <paramref name="retryOnError"/> parameter can be used to determine whether a particular exception should instigate a
        /// retry. By default, all exceptions will.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        /// The source type.
        /// </typeparam>
        /// <param name="this">
        /// The source observable.
        /// </param>
        /// <param name="retryCount">
        /// How many times to retry, or <see langword="null"/> to retry indefinitely.
        /// </param>
        /// <param name="strategy">
        /// The strategy to use when retrying, or <see langword="null"/> to use <see cref="DefaultStrategy"/>.
        /// </param>
        /// <param name="retryOnError">
        /// Predicate to determine whether a given error should result in a retry, or <see langword="null"/> to always retry on error.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler to use for delays, or <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <returns>
        /// An observable that will retry a failing source observable according to the timing dictated by <paramref name="strategy"/>.
        /// </returns>
        public static IObservable<T> RetryWithBackoff<T>(
            this IObservable<T> @this,
            int? retryCount = null,
            Func<int, TimeSpan> strategy = null,
            Func<Exception, bool> retryOnError = null,
            IScheduler scheduler = null)
        {
            strategy = strategy ?? DefaultStrategy;
            scheduler = scheduler ?? DefaultScheduler.Instance;
            retryOnError = retryOnError ?? (e => true);

            var attempt = 0;
            var pipeline = Observable
                .Defer(
                    () =>
                        ((attempt++ == 0) ?
                            @this :
                            @this
                                .DelaySubscription(strategy(attempt - 1), scheduler))
                                .Select(Notification.CreateOnNext)
                                .Catch(
                                    (Exception ex) => retryOnError(ex) ?
                                        Observable.Throw<Notification<T>>(ex) :
                                        Observable.Return(Notification.CreateOnError<T>(ex))));

            if (retryCount.HasValue)
            {
                pipeline = pipeline
                    .Retry(retryCount.Value);
            }
            else
            {
                pipeline = pipeline
                    .Retry();
            }

            return pipeline
                .Dematerialize();
        }
    }
}