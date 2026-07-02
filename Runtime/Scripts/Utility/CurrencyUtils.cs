using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// https://github.com/DarkNaku/Number
// https://ConvertNumberToWords.com/
public static class CurrencyUtils
{
    #region Example Usage

    // Compact format:
    // string compact = CurrencyUtils.FormatCompact(1250000); // 1.25M
    // string full = CurrencyUtils.FormatFull(1250000); // 1,250,000
    // string gold = CurrencyUtils.FormatWithSymbol(1250000, "", " gold"); // 1.25M gold
    //
    // Parse compact text:
    // if (CurrencyUtils.TryParseCompact("2.5M", out double value)) { Debug.Log(value); }
    //
    // Animate text:
    // StartCoroutine(CurrencyUtils.FillCurrencyText(goldText, 0, 1250000, "0.##", 0f, 1f));

    #endregion

    #region Constants

    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private static readonly string[] DefaultSuffixes =
    {
        "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc", "Ud", "Dd", "Td", "Qad",
        "Qid", "Sxd", "Spd", "Od", "Nd", "V", "Uv", "Dv", "Tv", "Qav", "Qiv", "Sxv", "Spv", "Ov",
        "Nv", "Tg", "Utg", "Dtg", "Ttg", "Qatg", "Qitg", "Sxtg", "Sptg", "Otg", "Ntg"
    };

    #endregion

    #region Format Compact

    // Formats any numeric struct into a compact currency string.
    public static string FormatCurrency<T>(T number, string format = "0.00") where T : struct
    {
        return FormatCompact(number, format);
    }

    // Formats an int into a compact currency string.
    public static string FormatCurrency(int number)
    {
        return FormatCompact(number, "0.#");
    }

    // Formats a float into a compact currency string.
    public static string FormatCurrency(float number)
    {
        return FormatCompact(number, "0.#");
    }

    // Formats a double into a compact currency string.
    public static string FormatCurrency(double number)
    {
        return FormatCompact(number, "0.##");
    }

    // Formats a long into a compact currency string.
    public static string FormatCurrency(long number)
    {
        return FormatCompact(number, "0.##");
    }

    // Formats any numeric struct into compact suffix form like 1.2K, 3.45M, or 9B.
    public static string FormatCompact<T>(T number, string format = "0.##") where T : struct
    {
        return FormatCompact(ToDouble(number), format);
    }

    // Formats a double into compact suffix form like 1.2K, 3.45M, or 9B.
    public static string FormatCompact(double number, string format = "0.##", string[] suffixes = null)
    {
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            return "0";
        }

        suffixes = suffixes ?? DefaultSuffixes;
        if (suffixes.Length == 0)
        {
            suffixes = DefaultSuffixes;
        }

        double absNumber = Math.Abs(number);
        if (absNumber < 1000d)
        {
            return number.ToString("0.##", InvariantCulture);
        }

        int suffixIndex = 0;
        while (absNumber >= 1000d && suffixIndex < suffixes.Length - 1)
        {
            absNumber /= 1000d;
            number /= 1000d;
            suffixIndex++;
        }

        return number.ToString(format, InvariantCulture) + suffixes[suffixIndex];
    }

    // Formats a number with comma separators, useful when compact suffix is not wanted.
    public static string FormatFull<T>(T number, string format = "N0") where T : struct
    {
        return ToDouble(number).ToString(format, InvariantCulture);
    }

    // Formats a number with a custom prefix and suffix.
    public static string FormatWithSymbol<T>(T number, string prefix = "", string suffix = "", string format = "0.##") where T : struct
    {
        return prefix + FormatCompact(number, format) + suffix;
    }

    #endregion

    #region Parse

    // Tries to parse plain numbers or compact values like 1K, 2.5M, and 3B.
    public static bool TryParseCompact(string text, out double value)
    {
        value = 0d;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string normalized = text.Trim().Replace(",", string.Empty);
        if (double.TryParse(normalized, NumberStyles.Float, InvariantCulture, out value))
        {
            return true;
        }

        for (int i = DefaultSuffixes.Length - 1; i > 0; i--)
        {
            string suffix = DefaultSuffixes[i];
            if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string numberPart = normalized.Substring(0, normalized.Length - suffix.Length);
            if (!double.TryParse(numberPart, NumberStyles.Float, InvariantCulture, out double baseValue))
            {
                return false;
            }

            value = baseValue * Math.Pow(1000d, i);
            return true;
        }

        return false;
    }

    #endregion

    #region Text Animation

    // Animates a TMP text number from current to target.
    public static IEnumerator FillCurrencyText<T>(
        TMP_Text text,
        T current,
        T target,
        string format = "0.00",
        float delay = 0f,
        float time = 1f,
        Action onCompleted = null) where T : struct
    {
        if (text == null)
        {
            yield break;
        }

        yield return FillTextTo(
            elapsedTime =>
            {
                T value = LerpValue(current, target, GetProgress(elapsedTime, time));
                text.text = FormatCompact(value, format);
            },
            delay,
            time,
            onCompleted);
    }

    // Animates a UI Text number from current to target.
    public static IEnumerator FillCurrencyText<T>(
        Text text,
        T current,
        T target,
        string format = "0.00",
        float delay = 0f,
        float time = 1f,
        Action onCompleted = null) where T : struct
    {
        if (text == null)
        {
            yield break;
        }

        yield return FillTextTo(
            elapsedTime =>
            {
                T value = LerpValue(current, target, GetProgress(elapsedTime, time));
                text.text = FormatCompact(value, format);
            },
            delay,
            time,
            onCompleted);
    }

    // Animates a raw number update callback from current to target.
    public static IEnumerator FillCurrencyValue<T>(
        T current,
        T target,
        Action<T> onValueChanged,
        float delay = 0f,
        float time = 1f,
        Action onCompleted = null) where T : struct
    {
        if (onValueChanged == null)
        {
            yield break;
        }

        yield return FillTextTo(
            elapsedTime =>
            {
                T value = LerpValue(current, target, GetProgress(elapsedTime, time));
                onValueChanged.Invoke(value);
            },
            delay,
            time,
            onCompleted);
    }

    // Runs delayed/timed update callbacks for text and value animation helpers.
    private static IEnumerator FillTextTo(Action<float> updateText, float delay, float time, Action onCompleted)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (time <= 0f)
        {
            updateText.Invoke(1f);
            onCompleted?.Invoke();
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            updateText.Invoke(elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        updateText.Invoke(time);
        onCompleted?.Invoke();
    }

    #endregion

    #region Helpers

    // Converts supported numeric values to double.
    private static double ToDouble<T>(T value) where T : struct
    {
        try
        {
            return Convert.ToDouble(value, InvariantCulture);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("CurrencyUtils.ToDouble failed: " + exception.Message);
            return 0d;
        }
    }

    // Lerps supported numeric values while preserving the requested return type.
    private static T LerpValue<T>(T a, T b, float t) where T : struct
    {
        t = Mathf.Clamp01(t);
        Type type = typeof(T);

        if (type == typeof(int))
        {
            return (T)(object)Mathf.RoundToInt(Mathf.Lerp(Convert.ToSingle(a), Convert.ToSingle(b), t));
        }

        if (type == typeof(float))
        {
            return (T)(object)Mathf.Lerp(Convert.ToSingle(a), Convert.ToSingle(b), t);
        }

        if (type == typeof(double))
        {
            double start = Convert.ToDouble(a, InvariantCulture);
            double end = Convert.ToDouble(b, InvariantCulture);
            return (T)(object)LerpDouble(start, end, t);
        }

        if (type == typeof(long))
        {
            double start = Convert.ToDouble(a, InvariantCulture);
            double end = Convert.ToDouble(b, InvariantCulture);
            return (T)(object)(long)Math.Round(LerpDouble(start, end, t));
        }

        throw new ArgumentException("Unsupported type for CurrencyUtils.LerpValue: " + type.Name);
    }

    // Lerps two double values.
    private static double LerpDouble(double a, double b, float t)
    {
        return a + (b - a) * t;
    }

    // Calculates safe progress for animation time.
    private static float GetProgress(float elapsedTime, float time)
    {
        return time <= 0f ? 1f : Mathf.Clamp01(elapsedTime / time);
    }

    #endregion
}
