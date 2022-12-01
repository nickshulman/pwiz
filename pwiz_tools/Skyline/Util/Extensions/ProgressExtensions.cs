﻿using pwiz.Common.Progress;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Util.Extensions
{
    public static class ProgressExtensions
    {
        public static void SetProgressValue(this IProgress progress, double value)
        {
            progress.Value = value;
        }

        public static void SetProgressValue(this IProgress progress, int value)
        {
            progress.Value = value;
        }

        public static void SetProgressMessage(this IProgress progress, string message)
        {
            progress.Message = message;
        }
        public static void SetProgressCheckCancel(this IProgress progress, int step, int totalSteps)
        {
            progress.CancellationToken.ThrowIfCancellationRequested();
            progress.SetProgressValue(step * 100.0 / totalSteps);
        }

        public static UpdateProgressResponse UpdateProgress(this IProgress progress, IProgressStatus status)
        {
            progress.Message = status.Message;
            progress.Value = status.PercentComplete;
            if (status.IsError)
            {
                throw status.ErrorException;
            }
            return progress.IsCanceled ? UpdateProgressResponse.cancel : UpdateProgressResponse.normal;
        }

    }
}
