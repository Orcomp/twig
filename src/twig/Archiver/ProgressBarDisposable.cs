namespace twig
{
    using System;
    using Spectre.Console;

    public class ProgressBarDisposable : IDisposable
    {
        public ProgressBarDisposable(ProgressTask task)
        {
            Task = task;
            Archiver.ProgressStart += OnProgressStart;
            Archiver.ProgressChanged += OnProgressChanged;
            Archiver.ProgressFinish += OnProgressFinish;
        }
        public void Dispose()
        {
            Archiver.ProgressStart -= OnProgressStart;
            Archiver.ProgressChanged -= OnProgressChanged;
            Archiver.ProgressFinish -= OnProgressFinish;
        }
        public ProgressTask Task { get; private set; }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Task.Value += e.Progress;
        }

        private void OnProgressStart(object sender, ProgressStartEventArgs e)
        {
            Task.MaxValue = e.Size;
        }

        private void OnProgressFinish(object sender, ProgressFinishEventArgs e)
        {

        }
    }
}
