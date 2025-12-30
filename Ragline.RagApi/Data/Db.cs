using Microsoft.Data.Sqlite;

namespace Ragline.RagApi.Data;

public static class Db
{
    public static string DbPath = Path.Combine(AppContext.BaseDirectory, "rag.db");

    public static SqliteConnection Open()
    {
        var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();
        return con;
    }

    public static void Init()
    {
        using var con = Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Documents(
  Id TEXT PRIMARY KEY,
  FileName TEXT NOT NULL,
  CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Chunks(
  Id TEXT PRIMARY KEY,
  DocumentId TEXT NOT NULL,
  ChunkIndex INTEGER NOT NULL,
  Page INTEGER NULL,
  Text TEXT NOT NULL,
  VectorJson TEXT NOT NULL,
  FOREIGN KEY(DocumentId) REFERENCES Documents(Id)
);
CREATE INDEX IF NOT EXISTS IX_Chunks_DocumentId ON Chunks(DocumentId);
";
        cmd.ExecuteNonQuery();
    }
}
