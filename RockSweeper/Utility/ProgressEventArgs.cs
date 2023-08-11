using System;

namespace RockSweeper.Utility
{
    public class ProgressEventArgs : EventArgs
    {
        public Guid OperationId { get; }

        public double? Progress { get; }

        public string Message { get; }

        public ProgressEventArgs( Guid operationId, double? progress, string message )
        {
            OperationId = operationId;
            Progress = progress;
            Message = message;
        }
    }
}
