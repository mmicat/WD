using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WitchDoctor.Utils
{
    #region Value to Reference Conversions
    public sealed class VariableReference<T> // Can be used for dictionaries that store by value
    {
        public Func<T> Get { get; private set; }
        public Action<T> Set { get; private set; }

        public T get => Get();
        public void set(T value)
        {
            Set(value);
        }

        public VariableReference(Func<T> getter = null, Action<T> setter = null)
        {
            Get = getter;
            Set = setter;
        }
    }

    public sealed class ActionReference // Can be used for action's list
    {
        public Action Get { get; private set; }

        public Action action;
        public ActionReference()
        {
            Get = action;
        }
    }

    #endregion

    public static class ConversionUtils
    {
        /// <summary>
        /// Used for conversion of float values 
        /// into logarithmic attenuation values. 
        /// Assumes a normalized value
        /// </summary>
        /// <param name="val"> The normalized value to convert </param>
        /// <returns> The resultant logarithm </returns>
        public static float ConvertFloatToLog(float val)
        {
            return Mathf.Log10(val) * 20;
        }

        #region Byte String Conversions
        public static string ConvertByteToString(byte[] _bytes)
        {
            return Encoding.UTF8.GetString(_bytes);
        }

        public static byte[] ConvertStringToByte(string _string)
        {
            return Encoding.UTF8.GetBytes(_string);
        }
        #endregion

        #region Time Conversions
        public static string TimeCalculatorTotalDaysLeft(long gameId)
        {
            DateTime dt2DateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt2DateTime = dt2DateTime.AddMilliseconds(gameId).ToLocalTime();
            TimeSpan timeRemaining = dt2DateTime - DateTime.UtcNow;
            return timeRemaining.Days.ToString();
        }

        public static string TimeCalculatorNumericDays(long c_time)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(c_time).ToLocalTime();
            return dtDateTime.Day.ToString() + ConvertDays(dtDateTime) + " ";
        }

        public static string TimeCalculatorAlphaDays(long c_time)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(c_time).ToLocalTime();
            return dtDateTime.DayOfWeek.ToString();
        }

        public static string TimeCalculatorNumericMonths(long c_time)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(c_time).ToLocalTime();
            return dtDateTime.Month.ToString();
        }

        public static string TimeCalculatorAlphaMonths(long c_time)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(c_time).ToLocalTime();
            return DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(dtDateTime.Month);
        }

        public static string TimeCalculatorNumericYear(long c_time)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(c_time).ToLocalTime();
            return dtDateTime.Year.ToString();
        }

        public static TimeSpan CalculateTimeEndCountDown(long c_time)
        {
            DateTime dt2DateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt2DateTime = dt2DateTime.AddMilliseconds(c_time).ToLocalTime();
            TimeSpan timeRemaining = dt2DateTime - DateTime.UtcNow;
            return timeRemaining;
        }

        private static string ConvertDays(DateTime dtDateTime)
        {
            if (new[] { 11, 12, 13 }.Contains(dtDateTime.Day))
            {
                return "th";
            }
            else if (dtDateTime.Day % 10 == 1)
            {
                return "st";
            }
            else if (dtDateTime.Day % 10 == 2)
            {
                return "nd";
            }
            else if (dtDateTime.Day % 10 == 3)
            {
                return "rd";
            }
            else
            {
                return "th";
            }
        }
        #endregion
    }

    public static class LinqUtils
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                throw new ArgumentNullException("source", "Source is null");
            if (action == null)
                throw new ArgumentNullException("action", "Action is null");

            foreach (T element in source)
            {
                action(element);
            }
        }
    }

    public static class LayerUtils
    {
        public static bool Contains(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static bool Contains(this LayerMask mask, string layerName)
        {
            return mask.Contains(LayerMask.NameToLayer(layerName));
        }
    }

    public static class TextureUtils
    {
        public static Texture2D CopyTexture(Texture2D original)
        {
            Texture2D copied = new Texture2D(original.width, original.height);

            int xN = original.width;
            int yN = original.height;

            for (int i = 0; i < xN; i++)
                for (int j = 0; j < yN; j++)
                    copied.SetPixel(i, j, original.GetPixel(i, j));

            copied.Apply();

            return copied;
        }

        public static Texture2D FlipTexture(Texture2D original)
        {
            Texture2D flipped = new Texture2D(original.width, original.height);

            int xN = original.width;
            int yN = original.height;

            for (int i = 0; i < xN; i++)
                for (int j = 0; j < yN; j++)
                    flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));

            flipped.Apply();

            return flipped;
        }
    }

    public static class UIUtils
    {
        public static float GetWidth(RectTransform rt)
        {
            var w = (rt.anchorMax.x - rt.anchorMin.x) * Screen.width + rt.sizeDelta.x;
            return w;
        }

        public static float GetHeight(RectTransform rt)
        {
            var h = (rt.anchorMax.y - rt.anchorMin.y) * Screen.height + rt.sizeDelta.y;
            return h;
        }

        /// <summary>
        /// Returns the maximum and minimum points 
        /// on a canvas with respect to the world canvas
        /// </summary>
        /// <param name="currRect"></param>
        /// <param name="xMax"></param>
        /// <param name="xMin"></param>
        /// <param name="yMax"></param>
        /// <param name="yMin"></param>
        public static void GetWorldBounds(RectTransform currRect, out float xMax, out float xMin, out float yMax, out float yMin)
        {
            // Get the screen positions
            Vector3[] buttonScreenPositions = new Vector3[4];
            currRect.GetWorldCorners(buttonScreenPositions);

            // find the max and min x and y values
            yMax = Mathf.Max(
                buttonScreenPositions[0].y,
                buttonScreenPositions[1].y,
                buttonScreenPositions[2].y,
                buttonScreenPositions[3].y);

            yMin = Mathf.Min(
                buttonScreenPositions[0].y,
                buttonScreenPositions[1].y,
                buttonScreenPositions[2].y,
                buttonScreenPositions[3].y);

            xMax = Mathf.Max(
                buttonScreenPositions[0].x,
                buttonScreenPositions[1].x,
                buttonScreenPositions[2].x,
                buttonScreenPositions[3].x);

            xMin = Mathf.Min(
                buttonScreenPositions[0].x,
                buttonScreenPositions[1].x,
                buttonScreenPositions[2].x,
                buttonScreenPositions[3].x);
        }
    }
}
