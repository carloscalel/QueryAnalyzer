using Microsoft.SqlServer.TransactSql.ScriptDom;

public class ParserService
{
    public List<ResultRow> Procesar(int idQuery, string query)
    {
        var parser = new TSql150Parser(false);
        IList<ParseError> errors;

        var fragment = parser.Parse(new StringReader(query), out errors);

        var t = new TableVisitor();
        fragment.Accept(t);

        var w = new WhereVisitor();
        fragment.Accept(w);

        var j = new JoinVisitor();
        fragment.Accept(j);

        var hash = HashHelper.GetHash(query);
        var tieneFecha = w.Resultados.Any();

        var res = new List<ResultRow>();

        foreach (var tabla in t.Tablas)
        {
            foreach (var cond in w.Resultados.DefaultIfEmpty())
            {
                foreach (var join in j.Joins.DefaultIfEmpty())
                {
                    res.Add(new ResultRow
                    {
                        IdQuery = idQuery,
                        Tabla = tabla.Tabla,
                        Columna = cond.Columna,
                        Operador = cond.Operador,
                        Fecha = cond.Fecha,
                        TipoFecha = cond.Tipo,
                        TieneFecha = tieneFecha,
                        JoinTipo = join?.TipoJoin,
                        TablaJoin = join?.TablaDestino,
                        ColumnaJoin = join?.ColumnaDestino,
                        HashQuery = hash
                    });
                }
            }
        }

        return res;
    }
}