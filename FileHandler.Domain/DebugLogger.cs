using System.Diagnostics;

namespace FileHandler.Domain
{
    public abstract class AbstractLogger
    {
        [Conditional("DEBUG")]
        public abstract void WriteLine(string message);
    }
}
