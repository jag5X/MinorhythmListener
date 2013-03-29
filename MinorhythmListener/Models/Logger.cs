using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinorhythmListener.Models
{
    static class Logger
    {
        private const string ErrorLogFile = "error.log";
        private static StringBuilder text = new StringBuilder();

        public static void WriteError(Exception ex)
        {
            text.Clear();
            text.AppendLine(DateTime.Now.ToString());
            text.AppendLine();
            WriteException(ex);
            text.AppendLine("********************************************************************************************************************************");
            text.AppendLine();

            File.AppendAllText(ErrorLogFile, text.ToString());
        }

        private static void WriteException(Exception ex)
        {
            text.AppendFormat("[{0}]", ex.GetType());
            text.AppendLine();
            text.AppendFormat("Message : ");
            text.AppendLine(ex.Message);
            text.AppendLine();
            text.AppendLine(ex.StackTrace);
            text.AppendLine();
            if (ex.InnerException != null)
            {
                text.AppendLine("* InnerException");
                WriteException(ex.InnerException);
            }
        }
    }
}
