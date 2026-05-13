using System.Linq;
using TableEditor.Clipboard;

namespace TableEditor.DataGrid;

// General purpose data class that covers off everything needed to communicate to the
// main dgv and header dgv's.
public class DgvData
{
    public string[] RowHeadersText { get; set; }
    public string[] ColHeadersText { get; set; }
    public double[] RowHeaders { get; set; }
    public double[] ColHeaders { get; set; }
    public double[,] TableData { get; set; }
    public string RowHeaderFormat { get; set; }
    public string ColHeaderFormat { get; set; }
    public string TableDataFormat { get; set; }
    public Paste.eMode CopyPasteMode { get; set; }
    public DgvData Empty { get { return new DgvData(); } }

    public DgvData()
    { }

    public DgvData(DgvData dgvData)
    {
        this.RowHeadersText = dgvData.RowHeadersText;
        this.ColHeadersText = dgvData.ColHeadersText;
        this.RowHeaders     = dgvData.RowHeaders;
        this.ColHeaders     = dgvData.ColHeaders;
        this.TableData      = dgvData.TableData;
        this.RowHeaderFormat = dgvData.RowHeaderFormat;
        this.ColHeaderFormat = dgvData.ColHeaderFormat;
        this.TableDataFormat = dgvData.TableDataFormat;
    }

    public bool Equals(DgvData dgvData)
    {
        // Returns true if equal

        // Row and column length comparison
        if (RowHeaders.Length != dgvData.RowHeaders.Length || ColHeaders.Length != dgvData.ColHeaders.Length)
            return false;

        // Comparison on the row header values
        if (!RowHeaders.SequenceEqual(dgvData.RowHeaders))
            return false;

        // Comparison on the column header values
        if (!ColHeaders.SequenceEqual(dgvData.ColHeaders))
            return false;

        // Table data comparison
        if (TableData.Length != dgvData.TableData.Length)
            return false;

        for (int i = 0; i < TableData.GetLength(0); i++)
            for (int j = 0; j < TableData.GetLength(1); j++)
                if (TableData[i, j] != dgvData.TableData[i, j])
                    return false;

        return true;
    }

    public bool HeadersEqual(DgvData dgvData)
    {
        // Returns true if equal

        if (dgvData == null)
            return false;

        // Row and column length comparison
        if (RowHeaders.Length != dgvData.RowHeaders.Length || ColHeaders.Length != dgvData.ColHeaders.Length)
            return false;

        // Comparison on the row header values
        if (!RowHeaders.SequenceEqual(dgvData.RowHeaders))
            return false;

        // Comparison on the column header values
        if (!ColHeaders.SequenceEqual(dgvData.ColHeaders))
            return false;

        return true;
    }

    public DgvData Copy()
    {
        DgvData dataOut = new DgvData();

        if (RowHeadersText != null)
            dataOut.RowHeadersText = (string[])RowHeadersText.Clone();

        if (ColHeadersText != null)
            dataOut.ColHeadersText = (string[])ColHeadersText.Clone();

        if (RowHeaders != null)
            dataOut.RowHeaders = (double[])RowHeaders.Clone();

        if (ColHeaders != null)
            dataOut.ColHeaders = (double[])ColHeaders.Clone();

        if (TableData != null)
            dataOut.TableData = (double[,])TableData.Clone();

        dataOut.RowHeaderFormat = RowHeaderFormat;
        dataOut.ColHeaderFormat = ColHeaderFormat;
        dataOut.TableDataFormat = TableDataFormat;

        return dataOut;
    }

    public string[] FormatHeaderText(string[] s, string format)
    {
        if (s == null)
            return new string[0];

        for (int i = 0; i < s.Length; i++)
        {
            if (double.TryParse(s[i], out double number))
            {
                s[i] = number.ToString(format);
            }
        }

        return s;
    }

    public string[] FormatHeaderText(double[] d, string format)
    {
        if (d == null)
            return new string[0];

        string[] s = new string[d.Length];

        for (int i = 0; i < d.Length; i++)
        {
            s[i] = d[i].ToString(format);
        }

        return s;
    }

    public void FormatHeaderText()
    {
        RowHeadersText = FormatHeaderText(RowHeadersText, RowHeaderFormat);
        ColHeadersText = FormatHeaderText(ColHeadersText, ColHeaderFormat);
    }

    public static string[] ConvertNumericHeadersToText(double[] d)
    {
        string[] s = new string[d.Length];

        for (int i = 0; i < d.GetLength(0); i++)
            s[i] = d[i].ToString();

        return s;
    }
}
