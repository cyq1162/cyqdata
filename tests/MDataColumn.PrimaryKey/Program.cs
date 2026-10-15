using System;
using System.Data;
using CYQ.Data.Table;

internal static class Program
{
    private static void Main()
    {
        var table = new MDataTable("Records");
        table.Columns.Add("Name", SqlDbType.NVarChar);
        table.Columns.Add("ID", SqlDbType.Int, false, false, -1, true, null);
        var row = table.NewRow(true);
        row["Name"].Value = "Record seventeen";
        row["ID"].Value = 17;

        // Scalar conditions infer the primary key, which need not be the first column.
        if (table.Select(17).Rows.Count != 1)
        {
            throw new Exception("Select(17) did not find the row using the explicitly declared ID primary key.");
        }
        if (!table.Columns["ID"].IsPrimaryKey || table.Columns.FirstPrimary.ColumnName != "ID")
        {
            throw new Exception("The seven-argument Add overload lost the primary-key flag.");
        }

        var columns = new MDataColumn();
        columns.Add("Optional", SqlDbType.NVarChar, false, true, 30, false, "default name");
        if (columns["Optional"].IsPrimaryKey || !columns["Optional"].IsCanNull
            || columns["Optional"].MaxSize != 30
            || Convert.ToString(columns["Optional"].DefaultValue) != "default name")
        {
            throw new Exception("Non-primary column options changed.");
        }
        columns.Add("AutoID", SqlDbType.Int, true);
        if (!columns["AutoID"].IsPrimaryKey || !columns["AutoID"].IsAutoIncrement)
        {
            throw new Exception("The auto-increment overload must retain its primary-key declaration.");
        }
        Console.WriteLine("PASS: declared primary-key lookup and column options.");
    }
}
