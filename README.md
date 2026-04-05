# 🚀 QueryAnalyzer (SQL Server Query Parser)

Herramienta en **C# (.NET)** para analizar queries almacenadas en SQL Server (ej. desde caché o historial) y extraer información estructurada como:

* 📊 Tablas utilizadas
* 🔗 Relaciones (JOINs reales)
* 📅 Uso de fechas (filtros, rangos, funciones)
* ⚠️ Queries sin filtros (potencial riesgo de performance)
* 🔁 Patrones de uso (UNION, CTE, subqueries)

---

# 🧠 Objetivo

Convertir texto SQL en datos analizables para:

* Auditoría de queries
* Optimización de performance
* Detección de malas prácticas
* Análisis histórico de uso

---

# 🏗️ Arquitectura

```
QueryAnalyzer/
│
├── Models/
│   ├── ResultRow.cs
│   └── JoinInfo.cs
│
├── Visitors/
│   ├── TableVisitor.cs
│   ├── WhereVisitor.cs
│   └── JoinVisitor.cs
│
├── Services/
│   └── ParserService.cs
│
├── Data/
│   └── Db.cs
│
├── Utils/
│   └── HashHelper.cs
│
└── Program.cs
```

---

# ⚙️ Requisitos

* .NET 6 o superior
* SQL Server (2016+ recomendado)

---

# 📦 Dependencias

Instalar paquetes NuGet:

```bash
dotnet add package Microsoft.SqlServer.TransactSql.ScriptDom
dotnet add package Microsoft.Data.SqlClient
```

---

# 🗄️ Base de Datos

## 🔹 Tabla origen

```sql
CREATE TABLE HistorialQuerys (
    Id INT IDENTITY PRIMARY KEY,
    QueryText NVARCHAR(MAX),
    Procesado BIT DEFAULT 0,
    FechaProcesado DATETIME NULL,
    Intentos INT DEFAULT 0,
    ErrorMsg NVARCHAR(MAX) NULL
)
```

---

## 🔹 Tabla resultado

```sql
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
```

---

## 🔹 Índices recomendados

```sql
CREATE INDEX IX_AnalisisQuery_IdQuery 
ON AnalisisQuery(IdQuery);

CREATE INDEX IX_AnalisisQuery_Tabla 
ON AnalisisQuery(Tabla);
```

---

# ▶️ Ejecución

1. Configurar conexión en:

```csharp
Db.Conn = "Server=localhost;Database=TuBD;Trusted_Connection=True;TrustServerCertificate=True;";
```

2. Ejecutar:

```bash
dotnet run
```

---

# 🔄 Flujo de procesamiento

```
Leer queries (Procesado = 0)
↓
Parsear (ScriptDom)
↓
Extraer:
   - tablas
   - joins
   - fechas
↓
Insertar resultados (BulkCopy)
↓
Marcar como procesado
↓
Registrar errores (si aplica)
```

---

# 🧪 Casos soportados

### 📅 Fechas

* `'2026-04-03'`
* `20260403`
* `GETDATE()`
* `GETDATE() - 7`
* `DATEADD(DAY, -30, GETDATE())`
* `DATEADD(DAY, n, '19000101')`
* `BETWEEN`
* `DATEDIFF`

---

### 🔗 SQL Complejo

* JOIN (INNER, LEFT, etc)
* UNION
* CTE (`WITH`)
* Subqueries

---

# 🔍 Ejemplo

### Query

```sql
SELECT * 
FROM pedidos 
WHERE fecha >= DATEADD(DAY, -30, GETDATE())
```

### Resultado

| Tabla   | Fecha      | Tipo     |
| ------- | ---------- | -------- |
| pedidos | (hoy - 30) | RELATIVA |

---

# ⚠️ Manejo de errores

* Control por query individual
* Transacciones por registro
* Reintentos (`Intentos < 3`)
* Registro en `ErrorMsg`

---

# 📊 Consultas útiles

### Queries sin fecha

```sql
SELECT *
FROM HistorialQuerys h
LEFT JOIN AnalisisQuery a ON h.Id = a.IdQuery
WHERE a.TieneFecha = 0 OR a.TieneFecha IS NULL
```

---

### Tablas más usadas

```sql
SELECT Tabla, COUNT(*)
FROM AnalisisQuery
GROUP BY Tabla
ORDER BY COUNT(*) DESC
```

---

### Relaciones (joins)

```sql
SELECT Tabla, TablaJoin, COUNT(*)
FROM AnalisisQuery
WHERE TablaJoin IS NOT NULL
GROUP BY Tabla, TablaJoin
```

---

# 🚀 Roadmap

* [ ] Detección de índices faltantes
* [ ] Integración con Power BI
* [ ] Análisis de execution plans
* [ ] Clustering de queries similares
* [ ] Alertas automáticas

---

# 🧠 Notas técnicas

* Se utiliza parser AST (ScriptDom), no regex
* Soporte para expresiones complejas y anidadas
* Preparado para alto volumen (batch + bulk insert)

---

# 👨‍💻 Autor

Proyecto enfocado en análisis de queries SQL.
