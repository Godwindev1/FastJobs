using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace FastJobs.Persistence
{
    [Serializable]
    public class DistributedLockException : Exception
    {
        public DistributedLockException(string message)
            : base(message)
        {
        }
        
    }
}