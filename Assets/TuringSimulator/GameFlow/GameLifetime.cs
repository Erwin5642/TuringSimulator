namespace TuringSimulator.GameFlow
{
    using System;
    using System.Threading;

    public sealed class GameLifetime : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        public CancellationToken Token => _cts.Token;

        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();

            _cts.Dispose();
        }
    }
}