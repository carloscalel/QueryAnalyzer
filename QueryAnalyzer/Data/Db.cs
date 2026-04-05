using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Data;

public static class Db
{
    public static string Conn = "Server=CALEL;Database=adminDB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

    public static List<(int Id, string QueryText)> Obtener(int top)
    {
        var list = new List<(int, string)>();

        using var cn = new SqlConnection(Conn);
        cn.Open();

        var cmd = new SqlCommand($@"
        SELECT TOP {top} Id, QueryText
        FROM HistorialQuerys
        WHERE Procesado = 0 
            AND Intentos <= 3", cn);

        var dr = cmd.ExecuteReader();

        while (dr.Read())
            list.Add((dr.GetInt32(0), dr.GetString(1)));

        return list;
    }
    public static void BulkInsertYMarcar(DataTable dt, List<int> ids)
    {
        using var cn = new SqlConnection(Conn);
        cn.Open();

        using var tran = cn.BeginTransaction();

        try
        {
            using (var bulk = new SqlBulkCopy(cn, SqlBulkCopyOptions.Default, tran))
            {
                bulk.DestinationTableName = "AnalisisQuery";

                bulk.ColumnMappings.Add("IdQuery", "IdQuery");
                bulk.ColumnMappings.Add("Tabla", "Tabla");
                bulk.ColumnMappings.Add("Columna", "Columna");
                bulk.ColumnMappings.Add("Operador", "Operador");
                bulk.ColumnMappings.Add("Fecha", "Fecha");
                bulk.ColumnMappings.Add("TipoFecha", "TipoFecha");
                bulk.ColumnMappings.Add("TieneFecha", "TieneFecha");
                bulk.ColumnMappings.Add("JoinTipo", "JoinTipo");
                bulk.ColumnMappings.Add("TablaJoin", "TablaJoin");
                bulk.ColumnMappings.Add("ColumnaJoin", "ColumnaJoin");
                bulk.ColumnMappings.Add("HashQuery", "HashQuery");

                bulk.WriteToServer(dt);
            }

            //update seguro
            var idsStr = string.Join(",", ids);

            using var cmd = new SqlCommand($@"
            UPDATE HistorialQuerys
            SET Procesado = 1
            WHERE Id IN ({idsStr})
        ", cn, tran);

            cmd.ExecuteNonQuery();

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
    }
    public static void ProcesarLoteSeguro(
    List<(int Id, string QueryText)> queries,
    ParserService parser)
    {
        using var cn = new SqlConnection(Conn);
        cn.Open();

        foreach (var q in queries)
        {
            using var tran = cn.BeginTransaction();

            try
            {
                //Parsear
                var res = parser.Procesar(q.Id, q.QueryText);

                var dt = ConvertToDataTable(res);

                //Insertar
                using (var bulk = new SqlBulkCopy(cn, SqlBulkCopyOptions.Default, tran))
                {
                    bulk.DestinationTableName = "AnalisisQuery";

                    bulk.ColumnMappings.Add("IdQuery", "IdQuery");
                    bulk.ColumnMappings.Add("Tabla", "Tabla");
                    bulk.ColumnMappings.Add("Columna", "Columna");
                    bulk.ColumnMappings.Add("Operador", "Operador");
                    bulk.ColumnMappings.Add("Fecha", "Fecha");
                    bulk.ColumnMappings.Add("TipoFecha", "TipoFecha");
                    bulk.ColumnMappings.Add("TieneFecha", "TieneFecha");
                    bulk.ColumnMappings.Add("JoinTipo", "JoinTipo");
                    bulk.ColumnMappings.Add("TablaJoin", "TablaJoin");
                    bulk.ColumnMappings.Add("ColumnaJoin", "ColumnaJoin");
                    bulk.ColumnMappings.Add("HashQuery", "HashQuery");

                    bulk.WriteToServer(dt);
                }

                //MARCAR COMO PROCESADO
                using var cmdOk = new SqlCommand(@"
                UPDATE HistorialQuerys
                SET Procesado = 1,
                    FechaProcesado = GETDATE(),
                    Intentos = Intentos + 1,
                    ErrorMsg = NULL
                WHERE Id = @Id", cn, tran);

                cmdOk.Parameters.AddWithValue("@Id", q.Id);
                cmdOk.ExecuteNonQuery();

                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();

                //GUARDAR ERROR
                using var cmdError = new SqlCommand(@"
                UPDATE HistorialQuerys
                SET Intentos = Intentos + 1,
                    ErrorMsg = @Error
                WHERE Id = @Id", cn);

                cmdError.Parameters.AddWithValue("@Id", q.Id);
                cmdError.Parameters.AddWithValue("@Error", ex.Message);

                cmdError.ExecuteNonQuery();
            }
        }
    }
    private static DataTable ConvertToDataTable(List<ResultRow> lista)
    {
        var dt = new DataTable();

        dt.Columns.Add("IdQuery", typeof(int));
        dt.Columns.Add("Tabla", typeof(string));
        dt.Columns.Add("Columna", typeof(string));
        dt.Columns.Add("Operador", typeof(string));
        dt.Columns.Add("Fecha", typeof(DateTime));
        dt.Columns.Add("TipoFecha", typeof(string));
        dt.Columns.Add("TieneFecha", typeof(bool));
        dt.Columns.Add("JoinTipo", typeof(string));
        dt.Columns.Add("TablaJoin", typeof(string));
        dt.Columns.Add("ColumnaJoin", typeof(string));
        dt.Columns.Add("HashQuery", typeof(string));

        foreach (var r in lista)
        {
            dt.Rows.Add(
                r.IdQuery,
                r.Tabla,
                r.Columna,
                r.Operador,
                r.Fecha ?? (object)DBNull.Value,
                r.TipoFecha,
                r.TieneFecha,
                r.JoinTipo,
                r.TablaJoin,
                r.ColumnaJoin,
                r.HashQuery
            );
        }

        return dt;
    }
}