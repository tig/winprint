using System;
using System.Globalization;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;


namespace WinPrint.Console {

    /// <summary>
    ///		Base class for Cmdlets that run asynchronously.
    /// </summary>
    /// <remarks>
    ///		Inherit from this class if your Cmdlet needs to use <c>async</c> / <c>await</c> functionality.
    /// </remarks>
    public abstract class AsyncCmdlet
        : PSCmdlet, IDisposable {
        /// <summary>
        ///		The source for cancellation tokens that can be used to cancel the operation.
        /// </summary>
        readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();


        /// <summary>
        ///		Initialise the <see cref="AsyncCmdlet"/>.
        /// </summary>
        protected AsyncCmdlet() {
        }


        /// <summary>
        ///		Finaliser for <see cref="AsyncCmdlet"/>.
        /// </summary>
        ~AsyncCmdlet() {
            Dispose(false);
        }


        /// <summary>
        ///		Dispose of resources being used by the Cmdlet.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///		Dispose of resources being used by the Cmdlet.
        /// </summary>
        /// <param name="disposing">
        ///		Explicit disposal?
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _cancellationSource.Dispose();
            }
        }

        public int ProcessingCount { get; set; } = 0;

        /// <summary>
        ///		Asynchronously perform Cmdlet pre-processing.
        /// </summary>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task BeginProcessingAsync() {
            return BeginProcessingAsync(_cancellationSource.Token);
        }


        /// <summary>
        ///		Asynchronously perform Cmdlet pre-processing.
        /// </summary>
        /// <param name="cancellationToken">
        ///		A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task BeginProcessingAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        ///		Asynchronously perform Cmdlet processing.
        /// </summary>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task ProcessRecordAsync() {
            return ProcessRecordAsync(_cancellationSource.Token);
        }


        /// <summary>
        ///		Asynchronously perform Cmdlet processing.
        /// </summary>
        /// <param name="cancellationToken">
        ///		A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task ProcessRecordAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        ///		Asynchronously perform Cmdlet post-processing.
        /// </summary>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task EndProcessingAsync() {
            return EndProcessingAsync(_cancellationSource.Token);
        }


        /// <summary>
        ///		Asynchronously perform Cmdlet post-processing.
        /// </summary>
        /// <param name="cancellationToken">
        ///		A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
        /// </param>
        /// <returns>
        ///		A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        protected virtual Task EndProcessingAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        ///		Perform Cmdlet pre-processing.
        /// </summary>
        protected sealed override void BeginProcessing() {
            ++ProcessingCount;
            ThreadAffinitiveSynchronizationContext.RunSynchronized(
                () => BeginProcessingAsync()
            );
        }


        /// <summary>
        ///		Perform Cmdlet processing.
        /// </summary>
        protected sealed override void ProcessRecord() {
            ThreadAffinitiveSynchronizationContext.RunSynchronized(
                () => ProcessRecordAsync()
            );
        }


        /// <summary>
        ///		Perform Cmdlet post-processing.
        /// </summary>
        protected sealed override void EndProcessing() {
            ThreadAffinitiveSynchronizationContext.RunSynchronized(
                () => EndProcessingAsync()
            );
            ProcessingCount--;
        }


        /// <summary>
        ///		Interrupt Cmdlet processing (if possible).
        /// </summary>
        protected sealed override void StopProcessing() {
            _cancellationSource.Cancel();


            base.StopProcessing();
        }


        /// <summary>
        ///		Write a progress record to the output stream, and as a verbose message.
        /// </summary>
        /// <param name="progressRecord">
        ///		The progress record to write.
        /// </param>
        protected void WriteVerboseProgress(ProgressRecord progressRecord) {
            if (progressRecord == null) {
                throw new ArgumentNullException(nameof(progressRecord));
            }

            WriteProgress(progressRecord);
            WriteVerbose(progressRecord.StatusDescription);
        }


        /// <summary>
        ///		Write a progress record to the output stream, and as a verbose message.
        /// </summary>
        /// <param name="progressRecord">
        ///		The progress record to write.
        /// </param>
        /// <param name="messageOrFormat">
        ///		The message or message-format specifier.
        /// </param>
        /// <param name="formatArguments">
        ///		Optional format arguments.
        /// </param>
        protected void WriteVerboseProgress(ProgressRecord progressRecord, string messageOrFormat, params object[] formatArguments) {
            if (progressRecord == null) {
                throw new ArgumentNullException(nameof(progressRecord));
            }

            if (String.IsNullOrWhiteSpace(messageOrFormat)) {
                throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'messageOrFormat'.", nameof(messageOrFormat));
            }

            if (formatArguments == null) {
                throw new ArgumentNullException(nameof(formatArguments));
            }

            progressRecord.StatusDescription = string.Format(CultureInfo.CurrentCulture, messageOrFormat, formatArguments);
            WriteVerboseProgress(progressRecord);
        }


        /// <summary>
        ///		Write a completed progress record to the output stream.
        /// </summary>
        /// <param name="progressRecord">
        ///		The progress record to complete.
        /// </param>
        /// <param name="completionMessageOrFormat">
        ///		The completion message or message-format specifier.
        /// </param>
        /// <param name="formatArguments">
        ///		Optional format arguments.
        /// </param>
        protected void WriteProgressCompletion(ProgressRecord progressRecord, string completionMessageOrFormat, params object[] formatArguments) {
            if (progressRecord == null) {
                throw new ArgumentNullException(nameof(progressRecord));
            }

            if (String.IsNullOrWhiteSpace(completionMessageOrFormat)) {
                throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'completionMessageOrFormat'.", nameof(completionMessageOrFormat));
            }

            if (formatArguments == null) {
                throw new ArgumentNullException(nameof(formatArguments));
            }

            progressRecord.StatusDescription = string.Format(CultureInfo.CurrentCulture, completionMessageOrFormat, formatArguments);
            progressRecord.PercentComplete = 100;
            progressRecord.RecordType = ProgressRecordType.Completed;
            WriteProgress(progressRecord);
            WriteVerbose(progressRecord.StatusDescription);
        }
    }
}
