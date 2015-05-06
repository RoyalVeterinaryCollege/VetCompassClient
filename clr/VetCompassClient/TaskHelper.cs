using System;
using System.Threading.Tasks;

namespace VetCompass.Client
{
    /// <summary>
    /// This class implements some helper methods on the base Task library
    /// </summary>
    public static class TaskHelper
    {
      
        /// <summary>
        /// Maps a successfully completed task, else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> MapSuccess<T, U>(this Task<T> task, Func<T, U> f)
        {
            Task<Task<U>> nextTask = task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<U>();

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

                return tcs.Task;
            });
            var unwrapped = nextTask.Unwrap();
            return unwrapped;
        }

        /// <summary>
        /// Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> FlatMapSuccess<U>(this Task task, Func<Task,Task<U>> f)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<Task<U>>();

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
                            Task<U> result = f(task);
                            tcs.SetResult(result);
                        }
                        catch (Exception e)
                        {
                            tcs.SetException(e);
                        }
                        break;
                }

                return tcs.Task;
            }).Unwrap().Unwrap();
        }

        /// <summary>
        /// Flat maps a succesful Task to a new Task[U], else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<U> FlatMapSuccess<T,U>(this Task<T> task, Func<T, Task<U>> f)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<Task<U>>();

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
                            Task<U> nextResult = f(task.Result);
                            tcs.SetResult(nextResult);
                        }
                        catch (Exception e)
                        {
                            tcs.SetException(e);
                        }
                        break;
                }

                return tcs.Task;
            }).Unwrap().Unwrap();
        }

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
    }
}