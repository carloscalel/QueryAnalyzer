using Microsoft.SqlServer.TransactSql.ScriptDom;

public class TableVisitor : TSqlFragmentVisitor
{
    public List<(string Tabla, string Alias)> Tablas = new();

    public override void Visit(NamedTableReference node)
    {
        var tabla = node.SchemaObject.BaseIdentifier.Value;
        var alias = node.Alias?.Value;

        Tablas.Add((tabla, alias));
    }
}