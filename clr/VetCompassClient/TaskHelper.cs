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
            return task.ContinueWith(innerTask =>
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
            }).Unwrap();
        }

        /// <summary>
        /// Flat maps a succesful task to a new task, else retains the original cancellation or fault
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Task<T> FlatMapSuccess<T>(this Task task, Func<Task,Task<T>> f)
        {
            return task.ContinueWith(innerTask =>
            {
                var tcs = new TaskCompletionSource<Task<T>>();

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
                            Task<T> result = f(task);
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

        public static Task<T> ActOnFailure<T>(this Task<T> task, Action<AggregateException> a)
        {
            return task.ContinueWith(innerTask =>
            {
                if (innerTask.IsFaulted) a(innerTask.Exception);
                return innerTask;
            }).Unwrap();
        }
    }
}