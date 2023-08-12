using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fin.Logger
{
    /// <summary>
    /// Interface Ilogger - client will use its own implementation
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message,Exception e);
        void Fatal(string message , Exception e);

        bool IsDebugEnabled();
        bool IsInfoEnabled();
        bool IsWarnEnabled();
        bool IsErrorEnabled();
    }
}
