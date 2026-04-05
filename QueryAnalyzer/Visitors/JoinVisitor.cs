using Microsoft.SqlServer.TransactSql.ScriptDom;

public class JoinVisitor : TSqlFragmentVisitor
{
    public List<JoinInfo> Joins = new();

    public override void Visit(QualifiedJoin node)
    {
        var tipoJoin = node.QualifiedJoinType.ToString();

        var tablaIzq = ObtenerTabla(node.FirstTableReference);
        var tablaDer = ObtenerTabla(node.SecondTableReference);

        if (node.SearchCondition is BooleanComparisonExpression cond)
        {
            Joins.Add(new JoinInfo
            {
                TablaOrigen = tablaIzq,
                TablaDestino = tablaDer,
                ColumnaOrigen = cond.FirstExpression.ToString(),
                ColumnaDestino = cond.SecondExpression.ToString(),
                TipoJoin = tipoJoin
            });
        }
    }

    private string ObtenerTabla(TableReference table)
    {
        if (table is NamedTableReference t)
            return t.SchemaObject.BaseIdentifier.Value;

        return table.ToString();
    }
}