using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Ragline.RagApi.Data;

public class RagRepository
{

    // add inside RagRepository class:
    public List<(string fileName, int? page, int chunkIndex, string text, float[] vec)> GetAllChunks()
    {
        using var con = Db.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT d.FileName, c.Page, c.ChunkIndex, c.Text, c.VectorJson
FROM Chunks c
JOIN Documents d ON d.Id = c.DocumentId
";
        using var r = cmd.ExecuteReader();

        var list = new List<(string, int?, int, string, float[])>();
        while (r.Read())
        {
            var fileName = r.GetString(0);
            int? page = r.IsDBNull(1) ? null : r.GetInt32(1);
            var chunkIndex = r.GetInt32(2);
            var text = r.GetString(3);
            var vecJson = r.GetString(4);
            var vec = JsonSerializer.Deserialize<float[]>(vecJson) ?? Array.Empty<float>();
            list.Add((fileName, page, chunkIndex, text, vec));
        }
        return list;
    }
    public string InsertDocument(string fileName)
    {
        var id = Guid.NewGuid().ToString("N");
        using var con = Db.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO Documents(Id, FileName, CreatedUtc) VALUES ($id,$fn,$ts)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$fn", fileName);
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        return id;
    }

    public List<(string Id, string FileName, string CreatedUtc)> ListDocuments()
    {
        using var con = Db.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT Id, FileName, CreatedUtc
FROM Documents
ORDER BY CreatedUtc DESC
";
        using var r = cmd.ExecuteReader();

        var list = new List<(string, string, string)>();
        while (r.Read())
        {
            list.Add((
                r.GetString(0),
                r.GetString(1),
                r.GetString(2)
            ));
        }
        return list;
    }


    public void InsertChunks(string documentId, List<(int chunkIndex, int? page, string text, float[] vec)> chunks)
    {
        using var con = Db.Open();
        using var tx = con.BeginTransaction();

        foreach (var c in chunks)
        {
            using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO Chunks(Id, DocumentId, ChunkIndex, Page, Text, VectorJson)
VALUES($id,$doc,$ci,$pg,$txt,$vec)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
            cmd.Parameters.AddWithValue("$doc", documentId);
            cmd.Parameters.AddWithValue("$ci", c.chunkIndex);
            cmd.Parameters.AddWithValue("$pg", (object?)c.page ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$txt", c.text);
            cmd.Parameters.AddWithValue("$vec", JsonSerializer.Serialize(c.vec));
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public int CountChunks()
    {
        using var con = Db.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM Chunks";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
