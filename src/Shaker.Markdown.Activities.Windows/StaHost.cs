using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Shaker.Markdown.Activities.Windows
{
    /// <summary>
    /// Runs WPF work on a thread of its own and waits for the answer.
    /// </summary>
    /// <remarks>
    /// Neither WPF nor WebView2 will start on the thread a Robot runs an activity on: both need a single
    /// threaded apartment and a running message pump, and the Robot promises neither. So each activity that
    /// needs a window gets a thread with a dispatcher, and blocks until it is finished with — which is what
    /// an activity is supposed to do anyway, since the step after it must not start early.
    /// </remarks>
    internal static class StaHost
    {
        /// <summary>Runs <paramref name="work"/> on an STA thread with a dispatcher, and returns its result.</summary>
        /// <param name="work">The work, which may await.</param>
        /// <param name="timeout">
        /// How long to wait before giving up. A window nobody ever closes would otherwise hang the process
        /// for good.
        /// </param>
        internal static T Run<T>(Func<Task<T>> work, TimeSpan timeout)
        {
            T result = default;
            Exception failure = null;
            var finished = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                try
                {
                    Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

                    // Without this, an await inside the work resumes on the thread pool and every WPF object
                    // it then touches throws for being on the wrong thread.
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherSynchronizationContext(dispatcher));

                    dispatcher.InvokeAsync(async () =>
                    {
                        try
                        {
                            result = await work().ConfigureAwait(true);
                        }
                        catch (Exception exception)
                        {
                            failure = exception;
                        }
                        finally
                        {
                            dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                        }
                    });

                    Dispatcher.Run();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    finished.Set();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            if (!finished.Wait(timeout))
            {
                throw new TimeoutException(
                    "The Markdown window did not finish within " + timeout.TotalSeconds + " seconds. " +
                    "Raise the Timeout property, or check that nothing is waiting on a dialog nobody can see.");
            }

            if (failure != null)
                throw new MarkdownWindowException(failure.Message, failure);

            return result;
        }
    }

    /// <summary>Thrown when the window or the print could not be done.</summary>
    [Serializable]
    public class MarkdownWindowException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public MarkdownWindowException()
        {
        }

        /// <summary>Creates the exception.</summary>
        public MarkdownWindowException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception.</summary>
        public MarkdownWindowException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>Creates the exception during deserialization.</summary>
        protected MarkdownWindowException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
