namespace SCTP
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a timer.
    /// </summary>
    public class Timer
    {
        /// <summary>
        /// A value indicating whether or not the instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The initial timeout value.
        /// </summary>
        private TimeSpan initialTimeout;

        /// <summary>
        /// A value indicating whether or not the timer has expired.
        /// </summary>
        private volatile bool expired;

        /// <summary>
        /// A value indicating whether or not the timer has been started.
        /// </summary>
        private volatile bool started;

        /// <summary>
        /// The method to call when the timer expires.
        /// </summary>
        private WaitCallback callback;

        /// <summary>
        /// The state to pass to the callback method.
        /// </summary>
        private object state;

        /// <summary>
        /// A cancellation token
        /// </summary>
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initialises a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="name">The name of the timer</param>
        /// <param name="initialTimeout">The initial timeout value.</param>
        /// <param name="callback">The function to call when the timer expires.</param>
        /// <param name="state">State to pass through to the timer callback.</param>
        public Timer(string name, TimeSpan initialTimeout, WaitCallback callback, object state = null)
        {
            Utility.ThrowIfArgumentNull("name", name);
            Utility.ThrowIfArgumentNull("callback", callback);

            this.Name = name;
            this.initialTimeout = initialTimeout;
            this.callback = callback;
            this.state = state;
        }

        /// <summary>
        /// Gets the name of the timer.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the time has expired.
        /// </summary>
        public bool HasExpired => this.expired;

        /// <summary>
        /// Gets the timeout value for the timer.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> timeout value.</param>
        /// <returns>A <see cref="uint"/> timeout value.</returns>
        private uint GetTimerValue(TimeSpan timeout)
        {
            return timeout.TotalMilliseconds >= 0 ? (uint)timeout.TotalMilliseconds : 0;
        }

        /// <summary>
        /// Starts the timer with the initial timeout, if its not already running.
        /// </summary>
        /// <returns>True if the timer was started; otherwise false.</returns>
        public bool Start()
        {
            return this.Start(this.initialTimeout);
        }

        /// <summary>
        /// Starts the timer if its not already running.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>True if the timer was started; otherwise false.</returns>
        public bool Start(TimeSpan timeout)
        {
            if (this.started == false)
            {
                this.started = true;
                this.StartTask(timeout);
            }

            return this.started;
        }

        /// <summary>
        /// Starts the timer task.
        /// </summary>
        /// <param name="timeout">The timeout value.</param>
        /// <returns>True if the timer had expired before starting the task.</returns>
        private bool StartTask(TimeSpan timeout)
        {
            bool hadExpired = this.expired;

            this.expired = false;
            this.cancellationTokenSource = new CancellationTokenSource();

            Task.Delay(timeout, this.cancellationTokenSource.Token)
                .ContinueWith(
                    (t) =>
                    {
                        if (t.IsCanceled == false)
                        {
                            this.TimerCallback();
                        }
                    });

            return hadExpired;
        }

        /// <summary>
        /// Restarts the timer with the initial timeout value.
        /// </summary>
        /// <returns>True if the previous timer had expired; otherwise false.</returns>
        public bool Restart()
        {
            return this.Restart(this.initialTimeout);
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for the timer to expire.</param>
        /// <returns>True if the previous timer had expired; otherwise false.</returns>
        public bool Restart(TimeSpan timeout)
        {
            this.cancellationTokenSource.Cancel();
            return this.StartTask(timeout);
        }

        /// <summary>
        /// Restarts the timer with different timeouts depending on whether it is currently running or not.
        /// </summary>
        /// <param name="alreadyStartedTimeout">The timeout value to use if the timer is already running.</param>
        /// <param name="notStartedTimeout">The timeout value to use if the timer is NOT already running.</param>
        /// <returns>True if the previous timer had expired; otherwise false.</returns>
        public bool Restart(TimeSpan alreadyStartedTimeout, TimeSpan notStartedTimeout)
        {
            this.cancellationTokenSource.Cancel();

            TimeSpan timeout = notStartedTimeout;
            if (this.started == true &&
                this.expired == false)
            {
                timeout = alreadyStartedTimeout;
            }

            return this.StartTask(timeout);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        /// <returns>True if the timer had already expired; otherwise false.</returns>
        public bool Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.started = false;
            return this.expired;
        }

        /// <summary>
        /// The timer callback function.
        /// </summary>
        private void TimerCallback()
        {
            this.expired = true;
            this.callback(this.state);
        }
    }
}
