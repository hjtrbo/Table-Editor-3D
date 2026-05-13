﻿using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace TableEditor.Common;

// Determines the most compact "Nn" format string that still accurately represents a
// given scalar, array, or table of doubles.  The logic inspects the actual decimal
// precision of the data and trims trailing zeros from the candidate format before
// returning, so the result is always the narrowest format that loses no information.
public static class NumberFormatter
{
    // Chooses the best-fit "Nn" format string for a single double value.
    public static string FormatDouble(double d)
    {
        int minDp = int.MaxValue;
        int maxDp = int.MinValue;
        double minValue = double.PositiveInfinity;
        double maxValue = double.NegativeInfinity;
        string numberFormat = "N0";
        string[] parts;

        if (d == double.NaN)
            return string.Empty;

        double value = d;
        int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];
        minDp = System.Math.Min(minDp, decimalPlaces);
        maxDp = System.Math.Max(maxDp, decimalPlaces);

        if (value != 0)
        {
            minValue = System.Math.Min(minValue, System.Math.Abs(value));
            maxValue = System.Math.Max(maxValue, System.Math.Abs(value));
        }

        numberFormat = PickFormat(minDp, maxDp, minValue, maxValue, d);

        // Trim trailing-zero decimal places from the chosen format.
        parts = numberFormat.Split('N');
        int formatDigits = int.Parse(parts[1]);
        formatDigits = TrimTrailingZerosSingle(d, formatDigits);
        return $"N{formatDigits}";
    }

    // Chooses the best-fit "Nn" format string for a 1-D array of doubles.
    public static string FormatDouble(double[] d)
    {
        if (d == null)
            return string.Empty;

        int length = d.Length;
        int minDp = int.MaxValue;
        int maxDp = int.MinValue;
        double minValue = double.PositiveInfinity;
        double maxValue = double.NegativeInfinity;

        for (int i = 0; i < length; i++)
        {
            if (d[i] == double.NaN)
                break;

            double value = d[i];
            int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];
            minDp = System.Math.Min(minDp, decimalPlaces);
            maxDp = System.Math.Max(maxDp, decimalPlaces);

            if (value != 0)
            {
                minValue = System.Math.Min(minValue, System.Math.Abs(value));
                maxValue = System.Math.Max(maxValue, System.Math.Abs(value));
            }
        }

        string numberFormat = PickFormatWithLeadingZeros1D(minDp, maxDp, minValue, maxValue, d);

        string[] parts = numberFormat.Split('N');
        int formatDigits = int.Parse(parts[1]);
        formatDigits = TrimTrailingZerosArray1D(d, formatDigits);
        return $"N{formatDigits}";
    }

    // Chooses the best-fit "Nn" format string for a 2-D array of doubles.
    public static string FormatDouble(double[,] d)
    {
        if (d == null)
            return string.Empty;

        int rows = d.GetLength(0);
        int columns = d.GetLength(1);
        int minDp = int.MaxValue;
        int maxDp = int.MinValue;
        double minValue = double.PositiveInfinity;
        double maxValue = double.NegativeInfinity;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (d[i, j] == double.NaN)
                    break;

                double value = d[i, j];
                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];
                minDp = System.Math.Min(minDp, decimalPlaces);
                maxDp = System.Math.Max(maxDp, decimalPlaces);

                if (value != 0)
                {
                    minValue = System.Math.Min(minValue, System.Math.Abs(value));
                    maxValue = System.Math.Max(maxValue, System.Math.Abs(value));
                }
            }
        }

        string numberFormat = PickFormatWithLeadingZeros2D(minDp, maxDp, minValue, maxValue, d);

        string[] parts = numberFormat.Split('N');
        int formatDigits = int.Parse(parts[1]);
        formatDigits = TrimTrailingZerosArray2D(d, formatDigits);
        return $"N{formatDigits}";
    }

    // Chooses the best-fit "Nn" format string for a DataTable whose cells are all numeric.
    // DBNull and NaN cells are skipped rather than throwing.
    public static string FormatDouble(DataTable dt)
    {
        if (dt == null)
            return string.Empty;

        int rows = dt.Rows.Count;
        int columns = dt.Columns.Count;
        int minDp = int.MaxValue;
        int maxDp = int.MinValue;
        double minValue = double.PositiveInfinity;
        double maxValue = double.NegativeInfinity;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (dt.Rows[i][j] == DBNull.Value)
                    continue;

                double value = Convert.ToDouble(dt.Rows[i][j]);

                if (value.Equals(double.NaN))
                    continue;

                int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)value)[3])[2];
                minDp = System.Math.Min(minDp, decimalPlaces);
                maxDp = System.Math.Max(maxDp, decimalPlaces);

                if (value != 0)
                {
                    minValue = System.Math.Min(minValue, System.Math.Abs(value));
                    maxValue = System.Math.Max(maxValue, System.Math.Abs(value));
                }
            }
        }

        string numberFormat = PickFormatWithLeadingZerosDt(minDp, maxDp, minValue, maxValue, dt);

        string[] parts = numberFormat.Split('N');
        int formatDigits = int.Parse(parts[1]);
        formatDigits = TrimTrailingZerosDataTable(dt, formatDigits);
        return $"N{formatDigits}";
    }

    // Returns the format string already stored on a DataGridViewCell, or falls back to
    // the column's inherited format, or "N0" when neither is set.
    public static string GetNumberFormat(DataGridViewCell cell)
    {
        if (cell.Value != null && cell.Value is IFormattable)
        {
            IFormatProvider formatProvider =
                cell.InheritedStyle.FormatProvider
                ?? cell.OwningColumn.InheritedStyle.FormatProvider;

            string formatString =
                cell.InheritedStyle.Format
                ?? cell.OwningColumn.InheritedStyle.Format;

            if (!string.IsNullOrEmpty(formatString))
                return formatString;

            if (formatProvider != null)
            {
                var nfi = (NumberFormatInfo)formatProvider.GetFormat(typeof(NumberFormatInfo));
                return $"N{nfi.NumberDecimalDigits}";
            }
        }

        return "N0";
    }

    // Infers the number of decimal places from the text representation of a numeric string
    // and returns the corresponding "Nn" format token.
    public static string GetNumberFormat(string s)
    {
        string[] parts = s.Split('.');
        if (parts.Length == 1)
            return "N0";

        return "N" + parts[1].Length.ToString();
    }

    // ---------------------------------------------------------------------------------
    // Private helpers shared by all FormatDouble overloads
    // ---------------------------------------------------------------------------------

    // Core format-selection logic for a single scalar (no leading-zero path needed).
    private static string PickFormat(int minDp, int maxDp, double minValue, double maxValue,
                                     double scalar)
    {
        if (minValue > 100 || (maxDp == 0 && minDp == 0))
            return "N0";
        if (minValue >= 10 && maxDp >= 2)
            return "N1";
        if (minValue >= 10 && maxDp >= 1)
            return "N1";
        if (minValue >= 0.01)
        {
            if (System.Math.Abs(maxValue) < 1 && maxDp >= 3) return "N3";
            if (maxDp >= 2) return "N2";
            if (maxDp >= 1) return "N1";
            return "N0";
        }
        if (minValue < 0.01)
        {
            if (maxValue > 1) return "N2";
            if (maxDp <= 3)   return "N" + maxDp.ToString();

            int maxLeadingZeros = CountLeadingZerosSingle(scalar);
            maxLeadingZeros = (maxLeadingZeros + 3 < maxDp) ? maxLeadingZeros + 3 : maxDp;
            return "N" + maxLeadingZeros.ToString();
        }

        return "N0";
    }

    private static string PickFormatWithLeadingZeros1D(int minDp, int maxDp,
                                                        double minValue, double maxValue,
                                                        double[] d)
    {
        if (minValue > 100 || (maxDp == 0 && minDp == 0)) return "N0";
        if (minValue >= 10 && maxDp >= 2) return "N1";
        if (minValue >= 10 && maxDp >= 1) return "N1";
        if (minValue >= 0.01)
        {
            if (System.Math.Abs(maxValue) < 1 && maxDp >= 3) return "N3";
            if (maxDp >= 2) return "N2";
            if (maxDp >= 1) return "N1";
            return "N0";
        }
        if (minValue < 0.01)
        {
            if (maxValue > 1) return "N2";
            if (maxDp <= 3)   return "N" + maxDp.ToString();

            int maxLeadingZeros = 0;
            for (int i = 0; i < d.Length; i++)
                maxLeadingZeros = System.Math.Max(maxLeadingZeros, CountLeadingZerosSingle(d[i]));

            maxLeadingZeros = (maxLeadingZeros + 3 < maxDp) ? maxLeadingZeros + 3 : maxDp;
            return "N" + maxLeadingZeros.ToString();
        }

        return "N0";
    }

    private static string PickFormatWithLeadingZeros2D(int minDp, int maxDp,
                                                        double minValue, double maxValue,
                                                        double[,] d)
    {
        if (minValue > 100 || (maxDp == 0 && minDp == 0)) return "N0";
        if (minValue >= 10 && maxDp >= 2) return "N1";
        if (minValue >= 10 && maxDp >= 1) return "N1";
        if (minValue >= 0.01)
        {
            if (System.Math.Abs(maxValue) < 1 && maxDp >= 3) return "N3";
            if (maxDp >= 2) return "N2";
            if (maxDp >= 1) return "N1";
            return "N0";
        }
        if (minValue < 0.01)
        {
            if (maxValue > 1) return "N2";
            if (maxDp <= 3)   return "N" + maxDp.ToString();

            int maxLeadingZeros = 0;
            for (int i = 0; i < d.GetLength(0); i++)
                for (int j = 0; j < d.GetLength(1); j++)
                    maxLeadingZeros = System.Math.Max(maxLeadingZeros, CountLeadingZerosSingle(d[i, j]));

            maxLeadingZeros = (maxLeadingZeros + 3 < maxDp) ? maxLeadingZeros + 3 : maxDp;
            return "N" + maxLeadingZeros.ToString();
        }

        return "N0";
    }

    private static string PickFormatWithLeadingZerosDt(int minDp, int maxDp,
                                                        double minValue, double maxValue,
                                                        DataTable dt)
    {
        if (minValue > 100 || (maxDp == 0 && minDp == 0)) return "N0";
        if (minValue >= 10 && maxDp >= 2) return "N1";
        if (minValue >= 10 && maxDp >= 1) return "N1";
        if (minValue >= 0.01)
        {
            if (System.Math.Abs(maxValue) < 1 && maxDp >= 3) return "N3";
            if (maxDp >= 2) return "N2";
            if (maxDp >= 1) return "N1";
            return "N0";
        }
        if (minValue < 0.01)
        {
            if (maxValue > 1) return "N2";
            if (maxDp <= 3)   return "N" + maxDp.ToString();

            int maxLeadingZeros = 0;
            for (int i = 0; i < dt.Rows.Count; i++)
                for (int j = 0; j < dt.Columns.Count; j++)
                    maxLeadingZeros = System.Math.Max(maxLeadingZeros,
                        CountLeadingZerosSingle(Convert.ToDouble(dt.Rows[i][j])));

            maxLeadingZeros = (maxLeadingZeros + 3 < maxDp) ? maxLeadingZeros + 3 : maxDp;
            return "N" + maxLeadingZeros.ToString();
        }

        return "N0";
    }

    // Counts leading zeros in the fractional part of a value less than 1 (e.g. 0.00123 → 2).
    // Returns 0 for zero or values >= 1.
    private static int CountLeadingZerosSingle(double value)
    {
        if (value == 0)
            return 0;

        string stringValue = value.ToString("F20");
        string[] parts = stringValue.Split('.');
        if (parts.Length < 2)
            return 0;

        double integerPart = Convert.ToDouble(parts[0]);
        if (integerPart >= 1)
            return 0;

        return parts[1].TakeWhile(c => c == '0').Count();
    }

    // Decrements formatDigits while ALL elements of d still format with a trailing zero,
    // stopping at N0.  This removes unnecessary decimal places from the candidate format.
    private static int TrimTrailingZerosSingle(double d, int formatDigits)
    {
        while (formatDigits > 0)
        {
            if (!d.ToString($"N{formatDigits}").EndsWith("0"))
                break;
            formatDigits--;
        }
        return formatDigits;
    }

    private static int TrimTrailingZerosArray1D(double[] d, int formatDigits)
    {
        while (formatDigits > 0)
        {
            bool allEndWithZero = true;
            for (int i = 0; i < d.Length; i++)
            {
                if (!d[i].ToString($"N{formatDigits}").EndsWith("0"))
                {
                    allEndWithZero = false;
                    break;
                }
            }
            if (!allEndWithZero)
                break;
            formatDigits--;
        }
        return formatDigits;
    }

    private static int TrimTrailingZerosArray2D(double[,] d, int formatDigits)
    {
        int rows = d.GetLength(0);
        int cols = d.GetLength(1);
        while (formatDigits > 0)
        {
            bool allEndWithZero = true;
            for (int i = 0; i < rows && allEndWithZero; i++)
            {
                for (int j = 0; j < cols && allEndWithZero; j++)
                {
                    if (!d[i, j].ToString($"N{formatDigits}").EndsWith("0"))
                        allEndWithZero = false;
                }
            }
            if (!allEndWithZero)
                break;
            formatDigits--;
        }
        return formatDigits;
    }

    private static int TrimTrailingZerosDataTable(DataTable dt, int formatDigits)
    {
        int rows = dt.Rows.Count;
        int cols = dt.Columns.Count;
        while (formatDigits > 0)
        {
            bool allEndWithZero = true;
            for (int i = 0; i < rows && allEndWithZero; i++)
            {
                for (int j = 0; j < cols && allEndWithZero; j++)
                {
                    if (dt.Rows[i][j] == DBNull.Value)
                    {
                        allEndWithZero = false;
                        break;
                    }
                    double value = Convert.ToDouble(dt.Rows[i][j]);
                    if (!value.ToString($"N{formatDigits}").EndsWith("0"))
                        allEndWithZero = false;
                }
            }
            if (!allEndWithZero)
                break;
            formatDigits--;
        }
        return formatDigits;
    }
}
