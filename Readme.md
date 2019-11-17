NOTES:

Async void methods:
await mailbox cannot be used in an async void method.  The mailbox will be unable to capture the exception which will leak out to the SynchronizationContext of the caller of the async void method.  Async void methods can be called or used with Execute, but note that any exceptions will be propagated as a separate queued action to the mailbox and not occur immediately.