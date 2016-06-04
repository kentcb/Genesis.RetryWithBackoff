![Logo](Art/Logo150x150.png "Logo")

# Genesis.RetryWithBackoff

[![Build status](https://ci.appveyor.com/api/projects/status/6xahxjp1ac5ly0g2?svg=true)](https://ci.appveyor.com/project/kentcb/genesis-retrywithbackoff)

## What?

> All Genesis.* projects are formalizations of small pieces of functionality I find myself copying from project to project. Some are small to the point of triviality, but are time-savers nonetheless. They have a particular focus on performance with respect to mobile development, but are certainly applicable outside this domain.
 
**Genesis.RetryWithBackoff** adds a `RetryWithBackoff` extension method to observables (based on [this work](https://gist.github.com/atifaziz/c6776b936a36a98a8153) by @niik). As the name suggests, the `RetryWithBackoff` method makes it simple to retry a failing observable with a variable delay between retries.

**Genesis.RetryWithBackoff** is delivered as a PCL targeting a wide range of platforms, including:

* .NET 4.5
* Windows 8
* Windows Store
* Windows Phone 8
* Xamarin iOS
* Xamarin Android

## Why?

When using Rx to model asynchrony, it's often desirable to retry a pipeline when it fails. But if that pipeline represents, for example, a web service invocation, we usually want to delay before the retry. Moreover, we want to tailor that delay according to the number of times we've retried. The built-in `Retry` and `Delay` operators are not sufficient to achieve this.

## Where?

The easiest way to get **Genesis.RetryWithBackoff** is via [NuGet](http://www.nuget.org/packages/Genesis.RetryWithBackoff/):

```PowerShell
Install-Package Genesis.RetryWithBackoff
```

## How?

**Genesis.RetryWithBackoff** adds a single `RetryWithBackoff` extension method to your observable sequences. It's defined in the `System.Reactive.Linq` namespace, so you'll generally have access to it if you're already using LINQ to Rx.

Here are some examples:

```C#
// retry any number of times, backing off exponentially
someObservable
    .RetryWithBackoff();

// retry up to 3 times, backing off exponentially
someObservable
    .RetryWithBackoff(retryCount: 3);

// retry up to 3 times, with 3 seconds between each retry
someObservable
    .RetryWithBackoff(
        retryCount: 3,
        strategy: n => TimeSpan.FromSeconds(3));

// retry up to 10 times, but only for InvalidOperationExceptions
someObservable
    .RetryWithBackoff(
        retryCount: 10,
        retryOnError: ex => ex is InvalidOperationException);

// retry with a custom scheduler (useful for tests)
someObservable
    RetryWithBackoff(scheduler: s);
``` 

## Who?

**Genesis.RetryWithBackoff** is created and maintained by [Kent Boogaart](http://kent-boogaart.com). However, it is based on [original code](https://gist.github.com/atifaziz/c6776b936a36a98a8153) is by @niik. Issues and pull requests are welcome.