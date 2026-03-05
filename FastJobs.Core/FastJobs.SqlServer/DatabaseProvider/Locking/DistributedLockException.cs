using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace FastJobs.SqlServer
{
    [Serializable]
    internal class DistributedLockException : Exception
    {
        public DistributedLockException(string message)
            : base(message)
        {
        }
        
    }
}