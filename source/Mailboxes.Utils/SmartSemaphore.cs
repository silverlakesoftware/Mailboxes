using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mailboxes.Utils
{
    public class SmartSemaphore<TResult>
    {
        readonly Mailbox _mailbox = new ConcurrentMailbox();
        readonly Queue<WorkUnit> _workQueue = new Queue<WorkUnit>();

        int _executionLimit = int.MaxValue;
        int _maxCapacity = int.MaxValue;
        int _executionCount;

        public void SetExecutionLimit(int executionLimit) => _mailbox.Execute(limit => _executionLimit = limit, executionLimit);

        public Task<TResult> ExecuteAsync(Func<Task<TResult>> action) => _mailbox.ExecuteAsync(() => SafeExecuteAsync(action));

        Task<TResult> SafeExecuteAsync(Func<Task<TResult>> action)
        {
            if (_workQueue.Count >= _maxCapacity)
            {
                throw new OperationCanceledException("Semaphore buffer capacity reached.");
            }

            // We don't want to get caught in a continuation within the mailbox context when the execution is complete.
            var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var workUnit = new WorkUnit(tcs, action);

            if (_executionCount < _executionLimit)
            {
                SafeExecutionBegin(workUnit);
            }
            else
            {
                _workQueue.Enqueue(workUnit);
            }

            return tcs.Task;
        }

        void SafeExecutionBegin(WorkUnit workUnit)
        {
            ++_executionCount;

            Task.Run(workUnit.Action).ContinueWith(
                t => _mailbox.Execute(t => SafeExecutionComplete(workUnit.TaskCompleteSource, t!), t),
                TaskContinuationOptions.ExecuteSynchronously);
        }

        void SafeExecutionComplete(TaskCompletionSource<TResult> tcs, Task<TResult> task)
        {
            _executionCount--;

            if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else if (task.IsFaulted)
            {
                tcs.TrySetException(task.Exception!);
            }
            else
            {
                tcs.TrySetResult(task.Result);
            }

            // If there's still work remaining kick it off
            if (_executionCount < _executionLimit && _workQueue.Count > 0)
            {
                SafeExecutionBegin(_workQueue.Dequeue());
            }
        }

        readonly struct WorkUnit
        {
            public WorkUnit(TaskCompletionSource<TResult> tcs, Func<Task<TResult>> action)
            {
                TaskCompleteSource = tcs;
                Action = action;
            }

            public TaskCompletionSource<TResult> TaskCompleteSource{ get; }

            public Func<Task<TResult>> Action { get; }
        }
    }
}
