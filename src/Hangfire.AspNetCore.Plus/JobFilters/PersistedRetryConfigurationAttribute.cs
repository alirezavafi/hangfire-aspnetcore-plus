using System;
using System.Linq;
using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public sealed class PersistedRetryConfigurationAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
    {
        private readonly string _key;
        public static readonly int DefaultRetryAttempts = 10;

        private static readonly Func<long, int> DefaultDelayInSecondsByAttemptFunc = attempt =>
        {
            var random = new Random();
            return (int) Math.Round(
                Math.Pow(attempt - 1, 4) + 15 + random.Next(30) * attempt);
        };

        private readonly ILog _logger = LogProvider.For<PersistedRetryConfigurationAttribute>();

        private readonly object _lockObject = new object();
        private int _attempts;
        private int[] _delaysInSeconds;
        private Func<long, int> _delayInSecondsByAttemptFunc;
        private AttemptsExceededAction _onAttemptsExceeded;
        private bool _logEvents;

        public string HashKey => $"job-retry-{_key}";
        public string RetryAttemptPropertyName => "RetryAttempts";
        public string DelayInSecondsPropertyName => "DelayInSeconds";

        public PersistedRetryConfigurationAttribute(string key, int defaultAttempts = -1, int[] delaysInSeconds = null)
        {
            _key = key;
            Attempts = defaultAttempts == -1 ? DefaultRetryAttempts : defaultAttempts;
            if (delaysInSeconds == null)
                DelayInSecondsByAttemptFunc = DefaultDelayInSecondsByAttemptFunc;
            else
                DelaysInSeconds = delaysInSeconds;

            var data = JobStorage.Current.GetConnection().GetAllEntriesFromHash(HashKey);
            if (data != null)
            {
                try
                {
                    if (data.ContainsKey(RetryAttemptPropertyName) && !string.IsNullOrWhiteSpace(data[RetryAttemptPropertyName]))
                        Attempts = int.Parse(data[RetryAttemptPropertyName]);
                    if (data.ContainsKey(DelayInSecondsPropertyName) && !string.IsNullOrWhiteSpace(data[DelayInSecondsPropertyName]))
                        DelaysInSeconds = data[DelayInSecondsPropertyName].Split(",").Select(int.Parse).ToArray();
                    Serilog.Log.Verbose("Loaded {@Attempts}/{@DelayInSeconds} from DB for {@Job}", Attempts, DelaysInSeconds, HashKey);
                }
                catch (Exception e)
                {
                    Serilog.Log.Error(e, "Failed to load configuration from {@Data}", data);
                }
            }
            else
                Serilog.Log.Verbose("No configuration saved in db for {@Job}", HashKey);

            LogEvents = true;
            OnAttemptsExceeded = AttemptsExceededAction.Fail;
            Order = 20;
        }

        public int Attempts
        {
            get
            {
                lock (_lockObject)
                {
                    return _attempts;
                }
            }
            private set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        @"Attempts value must be equal or greater than zero.");
                }

                lock (_lockObject)
                {
                    _attempts = value;
                }
            }
        }

        public int[] DelaysInSeconds
        {
            get
            {
                lock (_lockObject)
                {
                    return _delaysInSeconds;
                }
            }
            private set
            {
                if (value == null || value.Length == 0) throw new ArgumentNullException(nameof(value));
                if (value.Any(delay => delay < 0))
                    throw new ArgumentException(
                        $@"{nameof(DelaysInSeconds)} value must be an array of non-negative numbers.", nameof(value));

                lock (_lockObject)
                {
                    _delaysInSeconds = value;
                }
            }
        }

        public Func<long, int> DelayInSecondsByAttemptFunc
        {
            get
            {
                lock (_lockObject)
                {
                    return _delayInSecondsByAttemptFunc;
                }
            }
            private set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                lock (_lockObject)
                {
                    _delayInSecondsByAttemptFunc = value;
                }
            }
        }

        public AttemptsExceededAction OnAttemptsExceeded
        {
            get
            {
                lock (_lockObject)
                {
                    return _onAttemptsExceeded;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _onAttemptsExceeded = value;
                }
            }
        }

        public bool LogEvents
        {
            get
            {
                lock (_lockObject)
                {
                    return _logEvents;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _logEvents = value;
                }
            }
        }

        /// <inheritdoc />
        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState == null)
            {
                // This filter accepts only failed job state.
                return;
            }

            var retryAttempt = context.GetJobParameter<int>("RetryCount") + 1;

            if (retryAttempt <= Attempts)
            {
                ScheduleAgainLater(context, retryAttempt, failedState);
            }
            else if (retryAttempt > Attempts && OnAttemptsExceeded == AttemptsExceededAction.Delete)
            {
                TransitionToDeleted(context, failedState);
            }
            else
            {
                if (LogEvents)
                {
                    _logger.ErrorException(
                        $"Failed to process the job '{context.BackgroundJob.Id}': an exception occurred.",
                        failedState.Exception);
                }
            }
        }

        /// <inheritdoc />
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is ScheduledState &&
                context.NewState.Reason != null &&
                context.NewState.Reason.StartsWith("Retry attempt"))
            {
                transaction.AddToSet("retries", context.BackgroundJob.Id);
            }
        }

        /// <inheritdoc />
        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.OldStateName == ScheduledState.StateName)
            {
                transaction.RemoveFromSet("retries", context.BackgroundJob.Id);
            }
        }

        private void ScheduleAgainLater(ElectStateContext context, int retryAttempt, FailedState failedState)
        {
            context.SetJobParameter("RetryCount", retryAttempt);

            int delayInSeconds;

            if (_delaysInSeconds != null)
            {
                delayInSeconds = retryAttempt <= _delaysInSeconds.Length
                    ? _delaysInSeconds[retryAttempt - 1]
                    : _delaysInSeconds.Last();
            }
            else
            {
                delayInSeconds = _delayInSecondsByAttemptFunc(retryAttempt);
            }

            var delay = TimeSpan.FromSeconds(delayInSeconds);

            const int maxMessageLength = 50;
            var exceptionMessage = failedState.Exception.Message.Length > maxMessageLength
                ? failedState.Exception.Message.Substring(0, maxMessageLength - 1) + "…"
                : failedState.Exception.Message;

            // If attempt number is less than max attempts, we should
            // schedule the job to run again later.

            var reason = $"Retry attempt {retryAttempt} of {Attempts}: {exceptionMessage}";

            context.CandidateState = delay == TimeSpan.Zero
                ? (IState) new EnqueuedState {Reason = reason}
                : new ScheduledState(delay) {Reason = reason};

            if (LogEvents)
            {
                _logger.WarnException(
                    $"Failed to process the job '{context.BackgroundJob.Id}': an exception occurred. Retry attempt {retryAttempt} of {Attempts} will be performed in {delay}.",
                    failedState.Exception);
            }
        }

        private void TransitionToDeleted(ElectStateContext context, FailedState failedState)
        {
            context.CandidateState = new DeletedState
            {
                Reason = Attempts > 0
                    ? "Exceeded the maximum number of retry attempts."
                    : "Retries were disabled for this job."
            };

            if (LogEvents)
            {
                _logger.WarnException(
                    $"Failed to process the job '{context.BackgroundJob.Id}': an exception occured. Job was automatically deleted because the retry attempt count exceeded {Attempts}.",
                    failedState.Exception);
            }
        }
    }
}