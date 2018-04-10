namespace SCTP
{
    using System;
    using System.Threading;

    /// <summary>
    /// Provides a base async result implementation.
    /// </summary>
    /// <typeparam name="T">The async operations return type.</typeparam>
    public abstract class AsyncResultBase<T>
        : IAsyncResult
    {
        /// <summary>
        /// The callers callback.
        /// </summary>
        private AsyncCallback callback;

        /// <summary>
        /// An exception if the operation failed.
        /// </summary>
        private Exception exception;

        /// <summary>
        /// A value indicating whether or not the operation has completed.
        /// </summary>
        private bool complete;

        /// <summary>
        /// 
        /// </summary>
        private ManualResetEvent waitHandle;

        /// <summary>
        /// A lock to handle the critical completion scenario.
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// Initialises a new instance of the <see cref="AsyncResultBase"/> class.
        /// </summary>
        /// <param name="callback">Optional callback method.</param>
        /// <param name="state">Optional state data.</param>
        public AsyncResultBase(AsyncCallback callback = null, object state = null)
        {
            this.callback = callback;
            this.AsyncState = state;
        }

        /// <summary>
        /// Marks the operation as complete.
        /// </summary>
        /// <param name="completedSynchronously">A value indicatig whether or not the operation completed synchronously.</param>
        /// <param name="exception">An exception if the call failed.</param>
        protected virtual void SetComplete(bool completedSynchronously, Exception exception = null)
        {
            lock (this.lockObject)
            {
                if (this.complete == false)
                {
                    this.complete = true;
                    this.CompletedSynchronously = completedSynchronously;
                    this.exception = exception;

                    if (this.waitHandle != null)
                    {
                        this.waitHandle.Set();
                    }

                    this.InvokeCallback();
                }
            }
        }

        /// <summary>
        /// Invokes the callback if one has been set.
        /// </summary>
        protected virtual void InvokeCallback()
        {
            this.callback?.Invoke(this);
        }

        /// <summary>
        /// Completes the asynchronous operation.
        /// </summary>
        /// <returns>The result.</returns>
        public virtual T EndInvoke()
        {
            WaitHandle wait = null;
            lock (this.lockObject)
            {
                if (this.complete == false)
                {
                    wait = this.AsyncWaitHandle;
                }
            }

            if (wait != null)
            {
                wait.WaitOne();
            }

            if (this.exception != null)
            {
                throw this.exception;
            }

            return this.Result;
        }

        /// <summary>
        /// Gets or sets the result of the operation.
        /// </summary>
        protected T Result { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public object AsyncState { get; }

        /// <summary>
        /// Gets a handle to wait for completion.
        /// </summary>
        public System.Threading.WaitHandle AsyncWaitHandle
        {
            get 
            {
                if (this.waitHandle == null)
                {
                    lock (this.lockObject)
                    {
                        if (this.waitHandle == null)
                        {
                            this.waitHandle = new ManualResetEvent(this.complete);
                        }
                    }
                }

                return this.waitHandle;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the operation completed synchronously.
        /// </summary>
        public bool CompletedSynchronously { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.complete;
                }
            }
        }
    }
}