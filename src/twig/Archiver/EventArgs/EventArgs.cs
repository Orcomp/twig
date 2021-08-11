namespace twig
{
    using System;

    public class ProgressChangedEventArgs : EventArgs
    {
        public ProgressChangedEventArgs(long progress)
        {
            Progress = progress;
        }

        public long Progress { get; }
    }

    public class ProgressStartEventArgs : EventArgs
    {
        public ProgressStartEventArgs(long size)
        {
            Size = size;
        }
        public long Size { get; }
    }

    public class ProgressFinishEventArgs : EventArgs
    {

    }
}

