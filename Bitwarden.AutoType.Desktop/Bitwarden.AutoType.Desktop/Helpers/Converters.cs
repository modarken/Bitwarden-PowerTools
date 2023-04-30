using System;

namespace Bitwarden.AutoType.Desktop.Helpers
{
    public static class Converters
    {
        public static DateTimeOffset ConvertEpochStringToDateTimeOffset(string epochTimeString)
        {
            long milliseconds;
            if (long.TryParse(epochTimeString, out milliseconds))
            {
                DateTimeOffset epochStart = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
                return epochStart.AddMilliseconds(milliseconds);
            }
            else
            {
                throw new ArgumentException("Invalid epoch time string format.");
            }
        }
    }
}