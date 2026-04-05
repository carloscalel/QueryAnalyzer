using System.Data;

class Program
{
    static void Main()
    {
        var parser = new ParserService();

        var queries = Db.Obtener(1000);

        Db.ProcesarLoteSeguro(queries, parser);

        Console.WriteLine("FIN");
    }
}