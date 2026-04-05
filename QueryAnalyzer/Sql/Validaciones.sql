--Tabla Analisis
SELECT*
FROM AnalisisQuery

--Tabla Historial
SELECT *
FROM HistorialQuerys

--Validar duplicados
SELECT HashQuery, COUNT(*)
FROM AnalisisQuery
GROUP BY HashQuery
HAVING COUNT(*) > 1

--Tablas más usadas
SELECT Tabla, COUNT(*)
FROM AnalisisQuery
GROUP BY Tabla
ORDER BY COUNT(*) DESC

--Queries sin fecha
SELECT IdQuery, COUNT(*)
FROM AnalisisQuery
WHERE TieneFecha = 0
GROUP BY IdQuery

--Uso de fechas por tipo
SELECT TipoFecha, COUNT(*)
FROM AnalisisQuery
GROUP BY TipoFecha

--Join con tabla Historial-Analisis
SELECT 
    h.Id,
    h.QueryText
FROM HistorialQuerys h
LEFT JOIN AnalisisQuery a 
    ON h.Id = a.IdQuery
WHERE a.TieneFecha = 0 OR a.TieneFecha IS NULL

