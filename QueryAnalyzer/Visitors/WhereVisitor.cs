using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class WhereVisitor : TSqlFragmentVisitor
{
    public List<(string Columna, string Operador, DateTime? Fecha, string Tipo)> Resultados = new();

    public override void Visit(BooleanComparisonExpression node)
    {
        var operador = node.ComparisonType.ToString();

        //CASO NORMAL (columna vs valor)
        if (node.FirstExpression is ColumnReferenceExpression)
        {
            var columna = ObtenerColumna(node.FirstExpression);
            var fechas = Evaluar(node.SecondExpression);

            foreach (var f in fechas)
                Resultados.Add((columna, operador, f.Fecha, f.Tipo));
        }

        //NUEVO: DATEDIFF
        else if (node.FirstExpression is FunctionCall func)
        {
            if (func.FunctionName.Value.ToLower() == "datediff")
            {
                var resultado = EvaluarDateDiff(func, node.SecondExpression, operador);

                if (resultado != null)
                    Resultados.Add(resultado.Value);
            }
        }
    }

    public override void Visit(BooleanTernaryExpression node)
    {
        if (node.TernaryExpressionType == BooleanTernaryExpressionType.Between)
        {
            var columna = ObtenerColumna(node.FirstExpression);

            var f1 = Evaluar(node.SecondExpression);
            var f2 = Evaluar(node.ThirdExpression);

            foreach (var f in f1.Concat(f2))
                Resultados.Add((columna, "BETWEEN", f.Fecha, f.Tipo));
        }
    }

    private string ObtenerColumna(ScalarExpression expr)
    {
        if (expr is ColumnReferenceExpression col)
            return string.Join(".", col.MultiPartIdentifier.Identifiers.Select(i => i.Value));

        return expr.ToString();
    }

    private List<(DateTime? Fecha, string Tipo)> Evaluar(ScalarExpression expr)
    {
        var list = new List<(DateTime?, string)>();

        switch (expr)
        {
            case StringLiteral str:
                if (DateTime.TryParse(str.Value, out var dt))
                    list.Add((dt, "NORMAL"));
                break;

            case IntegerLiteral i:
                if (i.Value.Length == 8 &&
                    DateTime.TryParseExact(i.Value, "yyyyMMdd",
                    null, DateTimeStyles.None, out var d))
                    list.Add((d, "INT"));
                break;

            case FunctionCall f:
                list.AddRange(EvalFunc(f));
                break;

            case BinaryExpression b:
                var dt2 = EvalBin(b);
                if (dt2.HasValue)
                    list.Add((dt2, "RELATIVA"));
                break;
        }

        return list;
    }

    private List<(DateTime?, string)> EvalFunc(FunctionCall f)
    {
        var list = new List<(DateTime?, string)>();
        var name = f.FunctionName.Value.ToLower();

        if (name == "getdate")
        {
            list.Add((DateTime.Now, "RELATIVA"));
            return list;
        }

        if (name == "dateadd")
        {
            var dt = EvalDateAdd(f);
            if (dt.HasValue)
                list.Add((dt.Value, "RELATIVA"));

            return list;
        }

        return list;
    }
    private DateTime? EvalDateAdd(FunctionCall f)
    {
        if (!string.Equals(f.FunctionName.Value, "dateadd", StringComparison.OrdinalIgnoreCase))
            return null;

        if (f.Parameters == null || f.Parameters.Count != 3)
            return null;

        // 1) datepart
        string? datePart = ObtenerDatePart(f.Parameters[0]);
        if (string.IsNullOrWhiteSpace(datePart))
            return null;

        // 2) número (soporta 30 y -30)
        int? n = ObtenerValorNumericoNullable(f.Parameters[1]);
        if (!n.HasValue)
            return null;

        // 3) fecha base (soporta GETDATE() y literales de fecha)
        DateTime? baseDate = EvaluarFechaBase(f.Parameters[2]);
        if (!baseDate.HasValue)
            return null;

        return datePart switch
        {
            "day" or "dd" or "d" => baseDate.Value.AddDays(n.Value),
            "month" or "mm" or "m" => baseDate.Value.AddMonths(n.Value),
            "year" or "yy" or "yyyy" => baseDate.Value.AddYears(n.Value),
            "hour" or "hh" => baseDate.Value.AddHours(n.Value),
            "minute" or "mi" or "n" => baseDate.Value.AddMinutes(n.Value),
            "second" or "ss" or "s" => baseDate.Value.AddSeconds(n.Value),
            _ => null
        };
    }


    private DateTime? EvalBin(BinaryExpression b)
    {
        if (b.FirstExpression is FunctionCall f &&
            f.FunctionName.Value.ToLower() == "getdate")
        {
            if (b.SecondExpression is IntegerLiteral n)
                return DateTime.Now.AddDays(-int.Parse(n.Value));
        }

        return null;
    }
    private (string Columna, string Operador, DateTime? Fecha, string Tipo)?
    EvaluarDateDiff(FunctionCall func, ScalarExpression right, string operador)
    {
        // DATEDIFF(DAY, fecha, GETDATE()) <= 7

        var parametros = func.Parameters;

        if (parametros.Count != 3)
            return null;

        var columnaExpr = parametros[1];
        var baseExpr = parametros[2];

        var columna = ObtenerColumna(columnaExpr);

        if (baseExpr is FunctionCall baseFunc &&
            baseFunc.FunctionName.Value.ToLower() == "getdate")
        {
            if (right is IntegerLiteral num)
            {
                int dias = int.Parse(num.Value);

                //lógica inversa
                var fecha = DateTime.Now.AddDays(-dias);

                return (columna, "DATEDIFF", fecha, "RELATIVA");
            }
        }

        return null;
    }
    private int ObtenerValorNumerico(ScalarExpression expr)
    {
        // Caso 30
        if (expr is IntegerLiteral num)
            return int.Parse(num.Value);

        // Caso -30
        if (expr is UnaryExpression unary &&
            unary.Expression is IntegerLiteral inner)
        {
            int valor = int.Parse(inner.Value);

            if (unary.UnaryExpressionType == UnaryExpressionType.Negative)
                return -valor;

            return valor;
        }

        return 0;
    }
    private string? ObtenerDatePart(ScalarExpression expr)
    {
        // Ejemplo: DAY, MONTH, YEAR
        if (expr is ColumnReferenceExpression col &&
            col.MultiPartIdentifier != null &&
            col.MultiPartIdentifier.Identifiers.Count > 0)
        {
            return col.MultiPartIdentifier.Identifiers[^1].Value.ToLower();
        }

        // En algunos casos puede venir como identificador textual
        var txt = expr.ToString()?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(txt))
            return null;

        return txt;
    }

    private int? ObtenerValorNumericoNullable(ScalarExpression expr)
    {
        if (expr is IntegerLiteral num)
            return int.Parse(num.Value);

        if (expr is NumericLiteral dec)
        {
            if (int.TryParse(dec.Value, out int nDec))
                return nDec;
        }

        if (expr is UnaryExpression unary)
        {
            var inner = ObtenerValorNumericoNullable(unary.Expression);
            if (!inner.HasValue)
                return null;

            return unary.UnaryExpressionType == UnaryExpressionType.Negative
                ? -inner.Value
                : inner.Value;
        }

        return null;
    }

    private DateTime? EvaluarFechaBase(ScalarExpression expr)
    {
        // GETDATE()
        if (expr is FunctionCall func &&
            string.Equals(func.FunctionName.Value, "getdate", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Now;
        }

        // '19000101' o '2026-04-03'
        if (expr is StringLiteral str)
        {
            if (DateTime.TryParse(str.Value, out var dt))
                return dt;

            if (DateTime.TryParseExact(
                    str.Value,
                    new[] { "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var dt2))
            {
                return dt2;
            }
        }

        return null;
    }
}