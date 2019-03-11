// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Signalr.Client.Internal
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context

    public sealed class TaskQueue
    {
        private readonly object _lockObj = new object();
        private Task _lastQueuedTask;
        private volatile bool _drained;

        public TaskQueue()
            : this(Task.CompletedTask)
        {
        }

        public TaskQueue(Task initialTask)
        {
            _lastQueuedTask = initialTask;
        }

        public bool IsDrained
        {
            get
            {
                return _drained;
            }
        }

        public Task Enqueue(Func<object, Task> taskFunc, object state)
        {
            // Lock the object for as short amount of time as possible
            lock (_lockObj)
            {
                if (_drained)
                {
                    return _lastQueuedTask;
                }

                var newTask = _lastQueuedTask.ContinueWith((t, s1) => taskFunc(s1), state).Unwrap();
                _lastQueuedTask = newTask;
                return newTask;
            }
        }

        public Task Drain()
        {
            lock (_lockObj)
            {
                _drained = true;

                return _lastQueuedTask;
            }
        }
    }
}
