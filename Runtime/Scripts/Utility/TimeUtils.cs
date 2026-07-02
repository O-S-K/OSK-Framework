using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace OSK
{
    public static class TimeUtils
    {
        #region Example Usage

        // Current UTC Unix time:
        // long seconds = TimeUtils.GetCurrentUnixTimeSecondsUtc();
        // double milliseconds = TimeUtils.SystemTimeInMilliseconds;
        //
        // Convert DateTime and Unix time:
        // long savedTime = TimeUtils.DateTimeToUnixSeconds(DateTime.UtcNow);
        // DateTime utcDate = TimeUtils.UnixSecondsToDateTimeUtc(savedTime);
        //
        // Format time:
        // string compact = TimeUtils.SecondsToCompactString(3665f); // 1h 1m
        // string clock = TimeUtils.ConvertIntToTimeHH_MM_SS(3665); // 01:01:05
        //
        // Internet time from Google:
        // StartCoroutine(TimeUtils.GetGoogleInternetTime(
        //     timeUtc => Debug.Log("Internet UTC: " + timeUtc),
        //     error => Debug.LogWarning(error)));

        #endregion

        #region Constants

        public const float SecondsPerMinute = 60f;
        public const float SecondsPerHour = 3600f;
        public const float SecondsPerDay = 86400f;
        public const float SecondsPerYear = 31536000f;

        private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly string[] GoogleTimeUrls =
        {
            "https://www.google.com/generate_204",
            "https://www.google.com",
            "https://clients3.google.com/generate_204"
        };

        private static float secPerYear = SecondsPerYear;

        #endregion

        #region Current Time

        // Gets current Unix time in milliseconds using UTC.
        public static double SystemTimeInMilliseconds => GetCurrentUnixTimeMillisecondsUtc();

        // Gets the current UTC DateTime.
        public static DateTime NowUtc => DateTime.UtcNow;

        // Gets current Unix time in seconds using UTC.
        public static long GetCurrentUnixTimeSecondsUtc()
        {
            return DateTimeToUnixSeconds(DateTime.UtcNow);
        }

        // Gets current Unix time in milliseconds using UTC.
        public static double GetCurrentUnixTimeMillisecondsUtc()
        {
            return DateTimeToUnixMilliseconds(DateTime.UtcNow);
        }

        // Gets current Unix time in days using UTC.
        public static double GetCurrentUnixTimeDaysUtc()
        {
            return (DateTime.UtcNow - UnixEpochUtc).TotalDays;
        }

        // Converts any DateTime value to Unix seconds in UTC.
        public static long DateTimeToUnixSeconds(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - UnixEpochUtc).TotalSeconds;
        }

        // Converts any DateTime value to Unix milliseconds in UTC.
        public static double DateTimeToUnixMilliseconds(DateTime dateTime)
        {
            return (dateTime.ToUniversalTime() - UnixEpochUtc).TotalMilliseconds;
        }

        // Converts Unix seconds to a UTC DateTime.
        public static DateTime UnixSecondsToDateTimeUtc(double unixSeconds)
        {
            return UnixEpochUtc.AddSeconds(unixSeconds);
        }

        // Converts Unix milliseconds to a UTC DateTime.
        public static DateTime UnixMillisecondsToDateTimeUtc(double unixMilliseconds)
        {
            return UnixEpochUtc.AddMilliseconds(unixMilliseconds);
        }

        #endregion

        #region Game Year Conversion

        // Sets how many real seconds equal one in-game year.
        public static void SetSecPerYear(float value)
        {
            if (value <= 0f)
            {
                Debug.LogWarning("TimeUtils.SetSecPerYear ignored because value must be greater than 0.");
                return;
            }

            secPerYear = value;
        }

        // Converts in-game years to seconds.
        public static float YearsToSec(float years)
        {
            return years * secPerYear;
        }

        // Converts seconds to in-game years.
        public static float SecToYears(float value)
        {
            return value / secPerYear;
        }

        // Converts seconds to in-game months.
        public static float SecToMos(float value)
        {
            float _years = value / secPerYear;
            float _mos = _years * 12;
            return _mos;
        }

        #endregion

        #region Unit Conversion

        // Converts minutes to seconds.
        public static float MinutesToSeconds(float value)
        {
            return value * SecondsPerMinute;
        }

        // Converts hours to seconds.
        public static float HoursToSeconds(float value)
        {
            return value * SecondsPerHour;
        }

        // Converts days to seconds.
        public static float DaysToSeconds(float value)
        {
            return value * SecondsPerDay;
        }

        // Converts seconds to minutes.
        public static float SecondsToMinutes(float value)
        {
            return value / SecondsPerMinute;
        }

        // Converts seconds to hours as a numeric value.
        public static float SecondsToHoursValue(float value)
        {
            return value / SecondsPerHour;
        }

        // Converts seconds to days.
        public static float SecondsToDays(float value)
        {
            return value / SecondsPerDay;
        }

        // Gets total elapsed seconds since a local DateTime.
        public static int GetSecondElapsed(DateTime prevDate)
        {
            return Mathf.Max(0, Mathf.FloorToInt((float)(DateTime.Now - prevDate).TotalSeconds));
        }

        // Gets total elapsed seconds since a DateTime, optionally using UTC.
        public static double GetSecondsElapsed(DateTime prevDate, bool useUtc = false)
        {
            DateTime now = useUtc ? DateTime.UtcNow : DateTime.Now;
            return Math.Max(0d, (now - prevDate).TotalSeconds);
        }

        #endregion

        #region Format

        // Formats seconds into a compact text like 1d 2h, 3h 4m, or 5m 6s.
        public static string SecondsToCompactString(float value)
        {
            int days = (int)value / 86400;
            int hours = (int)(value % 86400) / 3600;
            int minutes = (int)(value % 3600) / 60;
            int seconds = (int)value % 60;
            string formattedTime;

            if (days > 0)
                formattedTime = string.Format("{0:D1}d {1:D1}h", days, hours);
            else if (hours > 0)
                formattedTime = string.Format("{0:D1}h {1:D1}m", hours, minutes);
            else
                formattedTime = string.Format("{0:D1}m {1:D1}s", minutes, seconds);
            return formattedTime;
        }

        // Legacy wrapper: formats a seconds value into compact time text.
        public static string MinutesToHours(float value)
        {
            return SecondsToCompactString(value);
        }

        // Formats seconds into a full hour string with optional days.
        public static string SecondsToHours(float value)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(value);
            string timerFormatted;
            if (timeSpan.Days == 0)
            {
                timerFormatted = string.Format("{0:D1}h {1:D1}m {2:D1}s", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
            else timerFormatted = string.Format("{0:D1}d {1:D1}h {2:D1}m {3:D1}s", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            return timerFormatted;
        }

        // Formats a TimeSpan into readable English words.
        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? String.Empty : "s") : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? String.Empty : "s") : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        // Formats a TimeSpan into short units like 1d, 2h, 3m, 4s.
        public static string ToReadableStringShortForm(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0}d, ", span.Days) : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0}h, ", span.Hours) : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0}m, ", span.Minutes) : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0}s", span.Seconds) : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0s";

            return formatted;
        }

        // Formats seconds into MM:SS.HH.
        public static string FormatSeconds(float elapsed)
        {
            int d = Mathf.Max(0, (int)(elapsed * 100.0f));
            int minutes = d / (60 * 100);
            int seconds = (d % (60 * 100)) / 100;
            int hundredths = d % 100;
            return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, hundredths);
        }

        // Converts a duration in seconds to HH:MM:SS.
        public static string ConvertIntToTimeHH_MM_SS(int duration)
        {
            duration = Mathf.Max(0, duration);
            TimeSpan span = TimeSpan.FromSeconds(duration);
            return string.Format("{0:00}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds);
        }

        #endregion

        #region Internet Time

        // Gets current internet time from Google endpoints and returns UTC time.
        public static IEnumerator GetGoogleInternetTime(
            Action<DateTime> onSuccess,
            Action<string> onError = null,
            int timeoutSeconds = 5)
        {
            return GetInternetTime(GoogleTimeUrls, onSuccess, onError, timeoutSeconds);
        }

        // Gets internet time from one URL by reading the HTTP Date header.
        public static IEnumerator GetInternetTime(
            string url,
            Action<DateTime> onSuccess,
            Action<string> onError = null,
            int timeoutSeconds = 5)
        {
            yield return GetInternetTime(new[] { url }, onSuccess, onError, timeoutSeconds);
        }

        // Gets internet time from multiple URLs and falls back until one succeeds.
        public static IEnumerator GetInternetTime(
            string[] urls,
            Action<DateTime> onSuccess,
            Action<string> onError = null,
            int timeoutSeconds = 5)
        {
            if (onSuccess == null)
            {
                Debug.LogWarning("TimeUtils.GetInternetTime requires an onSuccess callback.");
                yield break;
            }

            if (urls == null || urls.Length == 0)
            {
                InvokeInternetTimeError(onError, "Internet time url is null or empty.");
                yield break;
            }

            string lastError = null;
            for (int i = 0; i < urls.Length; i++)
            {
                string url = urls[i];
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                using (UnityWebRequest request = UnityWebRequest.Head(url))
                {
                    request.timeout = Mathf.Max(1, timeoutSeconds);
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        lastError = string.IsNullOrEmpty(request.error) ? request.result.ToString() : request.error;
                        continue;
                    }

                    string dateHeader = request.GetResponseHeader("Date");
                    if (TryParseHttpDateUtc(dateHeader, out DateTime internetTimeUtc))
                    {
                        onSuccess.Invoke(internetTimeUtc);
                        yield break;
                    }

                    lastError = "Cannot parse Date header from: " + url;
                }
            }

            InvokeInternetTimeError(onError, lastError);
        }

        // Parses an HTTP Date header into a UTC DateTime.
        public static bool TryParseHttpDateUtc(string dateHeader, out DateTime dateTimeUtc)
        {
            dateTimeUtc = DateTime.MinValue;

            if (string.IsNullOrEmpty(dateHeader))
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    dateHeader,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset dateTimeOffset))
            {
                return false;
            }

            dateTimeUtc = dateTimeOffset.UtcDateTime;
            return true;
        }

        // Sends an internet time error to callback or logs a warning.
        private static void InvokeInternetTimeError(Action<string> onError, string error)
        {
            string message = string.IsNullOrEmpty(error) ? "Cannot get internet time." : error;
            if (onError != null)
            {
                onError.Invoke(message);
                return;
            }

            Debug.LogWarning("TimeUtils.GetInternetTime failed: " + message);
        }

        #endregion

        #region Legacy Local Time API

        // Legacy local-time API: gets Unix seconds using DateTime.Now.
        public static double GetCurrentTime()
        {
            return (DateTime.Now - UnixEpochUtc.ToLocalTime()).TotalSeconds;
        }

        // Legacy local-time API: gets Unix days using DateTime.Now.
        public static double GetCurrentTimeInDays()
        {
            return (DateTime.Now - UnixEpochUtc.ToLocalTime()).TotalDays;
        }

        // Legacy local-time API: gets Unix milliseconds using DateTime.Now.
        public static double GetCurrentTimeInMills()
        {
            return (DateTime.Now - UnixEpochUtc.ToLocalTime()).TotalMilliseconds;
        }

        #endregion
    }
}
