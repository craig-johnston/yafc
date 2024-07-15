﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Yafc.UI {
    public abstract class TaskWindow<T> : WindowUtility {
        private TaskCompletionSource<T?>? tcs;

        protected TaskWindow() : base(new Padding(1f)) => tcs = new TaskCompletionSource<T?>();

        public TaskAwaiter<T?> GetAwaiter() => tcs?.Task.GetAwaiter() ?? throw new InvalidOperationException("Cannot await a closed window.");

        protected void CloseWithResult(T result) {
            _ = (tcs?.TrySetResult(result));
            tcs = null;
            Close();
        }

        protected internal override void Close() {
            _ = (tcs?.TrySetResult(default));
            tcs = null;
            base.Close();
        }
    }
}
