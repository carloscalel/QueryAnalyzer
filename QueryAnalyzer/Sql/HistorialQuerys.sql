CREATE TABLE HistorialQuerys (
    Id INT IDENTITY PRIMARY KEY,
    QueryText NVARCHAR(MAX),
    Procesado BIT DEFAULT 0,
    FechaProcesado DATETIME NULL,
    Intentos INT DEFAULT 0,
    ErrorMsg NVARCHAR(MAX) NULL
)

ALTER TABLE HistorialQuerys
ADD 
    FechaProcesado DATETIME NULL,
    Intentos INT DEFAULT 0,
    ErrorMsg NVARCHAR(MAX) NULL;