CREATE TABLE AnalisisQuery (
    Id INT IDENTITY,
    IdQuery INT,
    Tabla NVARCHAR(200),
    Columna NVARCHAR(200),
    Operador NVARCHAR(50),
    Fecha DATE NULL,
    TipoFecha NVARCHAR(50),
    TieneFecha BIT,
    JoinTipo NVARCHAR(50),
    TablaJoin NVARCHAR(200),
    ColumnaJoin NVARCHAR(200),
    HashQuery NVARCHAR(64),
    FechaProceso DATETIME DEFAULT GETDATE()
)