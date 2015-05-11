using System;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace VetCompass.Client
{
    /// <summary>
    ///     This class implements some helper methods on the base Task library
    /// </summary>
    public static class TaskHelper
    {
        /// <summary>
        ///     Maps a successfully completed task, else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> MapSuccess<T, U>(this Task<T> task, Func<T, U> f)
        {
            return MapSuccess(task, f, new CancellationTokenSource().Token);
        }

        /// <summary>
        ///  Maps a successfully completed task, else retains the original cancellation or fault.  This overload permits cancellation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<U> MapSuccess<T, U>(this Task<T> task, Func<T, U> f, CancellationToken ct)
        {
            var nextTask = task.ContinueWith(innerTask =>
            {
               
                var tcs = new TaskCompletionSource<U>();
                if (ct.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    switch (innerTask.Status)
                    {
                        case TaskStatus.Canceled:
                            tcs.SetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            tcs.SetException(task.Exception);
                            break;

                        case TaskStatus.RanToCompletion:
                            try
                            {
                                var result = f(task.Result);
                                tcs.SetResult(result);
                            }
                            catch (Exception e)
                            {
                                tcs.SetException(e);
                            }
                            break;
                    }
                }

                return tcs.Task;
            }, ct);
            var unwrapped = nextTask.Unwrap();
            return unwrapped;
        }

        /// <summary>
        ///     Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> FlatMapSuccess<U>(this Task task, Func<Task, Task<U>> f, CancellationToken ct)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<Task<U>>();

                if (ct.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    switch (innerTask.Status)
                    {
                        case TaskStatus.Canceled:
                            tcs.SetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            tcs.SetException(task.Exception);
                            break;

                        case TaskStatus.RanToCompletion:
                            try
                            {
                                var result = f(task);
                                tcs.SetResult(result);
                            }
                            catch (Exception e)
                            {
                                tcs.SetException(e);
                            }
                            break;
                    }
                }

                return tcs.Task;
            },ct).Unwrap().Unwrap();
        }

        /// <summary>
        ///     Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> FlatMapSuccess<T, U>(this Task<T> task, Func<T, Task<U>> f)
        {
            return FlatMapSuccess(task, f, new CancellationTokenSource().Token);
        }

        /// <summary>
        ///     Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<T> FlatMapSuccess<T>(this Task task, Func<Task, Task<T>> f)
        {
            return FlatMapSuccess(task, f, new CancellationTokenSource().Token);
        }

        /// <summary>
        ///     Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault.  This overload permits cancellation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> FlatMapSuccess<T, U>(this Task<T> task, Func<T, Task<U>> f, CancellationToken ct)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<Task<U>>();
                if (ct.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    switch (innerTask.Status)
                    {
                        case TaskStatus.Canceled:
                            tcs.SetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            tcs.SetException(task.Exception);
                            break;

                        case TaskStatus.RanToCompletion:
                            try
                            {
                                var nextResult = f(task.Result);
                                tcs.SetResult(nextResult);
                            }
                            catch (Exception e)
                            {
                                tcs.SetException(e);
                            }
                            break;
                    }
                }
               
                return tcs.Task;
            },ct).Unwrap().Unwrap();
        }

        /// <summary>
        ///     Permits a state altering side effect conditioned on an antecedent task failure
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Task<T> ActOnFailure<T>(this Task<T> task, Action<AggregateException> a)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<T>();

                switch (innerTask.Status)
                {
                    case TaskStatus.Canceled:
                        tcs.SetCanceled();
                        break;

                    case TaskStatus.Faulted:
                        a(innerTask.Exception);
                        tcs.SetException(task.Exception);
                        break;

                    case TaskStatus.RanToCompletion:
                        tcs.SetResult(task.Result);
                        break;
                }

                return tcs.Task;
            }).Unwrap();
        }

#if NET35
        /// <summary>
        ///     Adds a helper method for creating tasks directly from results which is missing in the .net 3.5 TPL library
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Task<T> FromResult<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        /// <summary>
        /// A method which mimics the .net 4.5 Task.Delay task
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static Task Delay(int timeoutMilliseconds)
        {
            var tcs = new TaskCompletionSource<object>();
            var timer = new System.Timers.Timer(timeoutMilliseconds);

            timer.Elapsed += (sender, e) =>
            {
                timer.Stop();
                timer.Close();
                tcs.SetResult(new object());
            };
            timer.Start();
            return tcs.Task;
        }
#endif
#if NET45
        
        /// <summary>
        /// Wrapper function for Task.Delay
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static Task Delay(int timeoutMilliseconds)
        {
            return Task.Delay(timeoutMilliseconds);
        }
#endif
        /// <summary>
        /// Returns either the results of the original task, or a cancellation after a timeout.  Which ever is first
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="originalTask"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <param name="timeMilliseconds"></param>
        /// <returns></returns>
        public static Task<T> CancelAfter<T>(this Task<T> originalTask, CancellationTokenSource cancellationTokenSource,
            int timeMilliseconds)
        {
            var delayTask = Delay(timeMilliseconds);
            delayTask.ContinueWith(_ =>
            {
                if (!originalTask.IsCompleted)
                {
                    cancellationTokenSource.Cancel();
                }
            });

            return Task.Factory
                .ContinueWhenAny(new Task[] {delayTask, originalTask}, completedTask => completedTask)
                .FlatMapSuccess(completedTask =>
            {
                if (completedTask == originalTask) return originalTask;
                else
                {
                    //timeout occurred
                    var tcs = new TaskCompletionSource<T>();
                    tcs.SetCanceled();
                    return tcs.Task;
                }
            });
        }
    }
}