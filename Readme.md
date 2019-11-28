# Mailboxes

[![Build Status](https://img.shields.io/azure-devops/build/silverlakesoftware/mailboxes/1)](https://dev.azure.com/silverlakesoftware/Mailboxes/_build/latest?definitionId=1)
[![NuGet package](https://img.shields.io/nuget/v/Mailboxes.svg)](https://nuget.org/packages/Mailboxes)

## Overview

Mailboxes is a low-level implementation of an actor-like computation model.
- high performance
- support usage of C# idioms (i.e. async/await)
- concurency model focused (i.e. local communication only)
- integrate will with existing code
- strongly typed
- provide building blocks for a more complete "pay as you go" actor system

Mailboxes is a project I developed after working with another actor system and soon realized I wanted something worked at a lower level of code and to be able to write idiomatic C# but still take advantage of the performance and safety of the actor computation model in how it approaches concurrency.  It's called mailboxes because it implements only the core metaphor of how actor messages are passed.

By focusing on a single-process implementation we can simplify some aspects of actor systems to more direct advantage of the C# language.  The following are parallel concepts:
- Actor address = Mailbox object reference (i.e. mailbox)
- Send message = mailbox.Execute() or await mailbox
- Process message = A mailbox execution unit.  Only one execution unit can be processed at a time.
- Actor state = The set of data exclusively modified when executing the scope of a mailbox (like most actor system equivalents this can only be enforced through good coding practices)

This allows C# idioms like await/async, Task, and CancellationToken to be used. Code running in a mailbox execution unit:
- Can be awaited by returning a Task.
- Can await non-actor code and the continuation will be called on an excution unit.
- Await the excution unit of another mailbox and the continuation will be called on an execution unit of the calling mailbox.
- Can use a Task to capture success, an exception, or cancellation result of an execution unit.  (i.e. treated like a message sent back to the caller.)
- CancellationTokens can be transformed to be mailbox safe with state that can only be modified as part of it's own execution unit.

## Sample

```C#
public class ActorLike
{
    Mailbox _mailbox = new ConcurrentMailbox();
    int _counter = 0;

    Task<int> Add() 
    {
        await _mailbox;
        ++_counter;
        return _counter;
    }

    Task<int> Subtract()
    {
        await _mailbox;
        --_counter;
        return _counter;;
    }
}

```

In the above example, Add and Subtract are both non-blocking method calls that will execute sequentially to modify the "actor" state in a thread safe way.

## Execution units

This is a term this project is using to describe a single unit of execution within a mailbox.  For await/async, these execution units are defined by the continuation provided to the awaiter by the compiler.  For example:

```C#

Task<string> GetWebsite()
{
    await _mailbox;
    // Execution Unit 1 (this will continue until the HttpClient waits on IO)
    var body = await _httpClient.GetStringAsync("http://example.com");
    // Execution Unit 2    
    body.Replace("A","B");
}

```

It is important to understand that the mailbox will enforce one execution unit at a time, but when using await/async a single method can contain more then one execution unit.  This means any state that is "protected" by the mailbox can change between execution units.  There is no guarantee Execution Unit 2 will be called immediately after 1.  It's likely that it won't.

## Things to avoid:
`.ConfigureAwait(false)`
When awaiting a mailbox execution unit this can extend the execution unit of the mailbox but not forcing a context switch back to the calling context. In an a mailbox execution unit when awaiting a task this can cause code that changes state outside of an execution unit.

`async void`  methods:
await mailbox cannot be used in an async void method.  The mailbox will be unable to capture the exception which will leak out to the SynchronizationContext of the caller of the async void method.  Async void methods can be called or used with Execute, but note that any exceptions will be propagated as a separate queued action to the mailbox and not occur immediately.

Using a `CancellationToken` directly:
A CancellationToken can change it's state at any time which violates the concurrency model of a mailbox.  The .Include method on Mailbox will return safe CancellationToken that will observe the original CancellationToken and only propegate a state change on it's own execution unit.

## Project Structure

| Project | Description |
|---------|-------------|
| Mailboxes | Core project  |
| Mailboxes.Benchmarks | Benchmarks |
| Mailboxes.Tests | Unit tests |
| Mailboxes.Example | Nothing really yet :) |
| ThirdParty.Benchmarks | Comparison benchmarks to some Actor projects |


## Contributions

Please create an issue to discuss any possible contributions.