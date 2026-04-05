public class ResultRow
{
    public int IdQuery { get; set; }
    public string Tabla { get; set; }
    public string Columna { get; set; }
    public string Operador { get; set; }
    public DateTime? Fecha { get; set; }
    public string TipoFecha { get; set; }
    public bool TieneFecha { get; set; }

    public string JoinTipo { get; set; }
    public string TablaJoin { get; set; }
    public string ColumnaJoin { get; set; }

    public string HashQuery { get; set; }
}