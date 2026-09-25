using System.Text.Json;
using Fitlog.Models;
using Microsoft.Data.Sqlite;

namespace Fitlog.Data;

public sealed class FitnessRepository
{
    public string DatabasePath { get; }
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public FitnessRepository(string path)
    {
        DatabasePath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS records (kind TEXT NOT NULL, id TEXT NOT NULL, payload TEXT NOT NULL, PRIMARY KEY(kind,id));
            PRAGMA user_version=1;
            """;
        cmd.ExecuteNonQuery();
    }
    private SqliteConnection Open()
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString());
        db.Open();
        return db;
    }
    public List<T> Read<T>(string kind)
    {
        using var db = Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT payload FROM records WHERE kind=$kind ORDER BY id";
        cmd.Parameters.AddWithValue("$kind", kind);
        using var reader = cmd.ExecuteReader();
        var result = new List<T>();
        while (reader.Read()) result.Add(JsonSerializer.Deserialize<T>(reader.GetString(0))!);
        return result;
    }
    public void Save<T>(string kind, string id, T value)
    {
        Validate(kind, value);
        using var db = Open();
        Put(db, null, kind, id, value);
    }
    private static void Put<T>(SqliteConnection db, SqliteTransaction? tx, string kind, string id, T value)
    {
        using var cmd = db.CreateCommand(); cmd.Transaction = tx;
        cmd.CommandText = "INSERT INTO records(kind,id,payload) VALUES($kind,$id,$payload) ON CONFLICT(kind,id) DO UPDATE SET payload=excluded.payload";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(value));
        cmd.ExecuteNonQuery();
    }
    public void Delete(string kind, string id)
    {
        using var db = Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM records WHERE kind=$kind AND id=$id";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); cmd.ExecuteNonQuery();
    }
    public Backup Snapshot() => new()
    {
        Logs = Read<DailyLog>("log"), Weights = Read<WeightEntry>("weight"), Measurements = Read<Measurement>("measurement"),
        Goals = Read<Goal>("goal"), Photos = Read<ProgressPhoto>("photo"), Albums = Read<PhotoAlbum>("album"), Routines = Read<WorkoutRoutine>("routine"), Preferences = Read<Preferences>("preferences").FirstOrDefault() ?? new()
    };
    public string Export() => JsonSerializer.Serialize(Snapshot(), JsonOptions);
    public void Import(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object || new[] { "Version", "Logs", "Weights", "Measurements", "Goals", "Photos", "Preferences" }.Any(key => !document.RootElement.TryGetProperty(key, out _)))
            throw new InvalidDataException("This is not a complete fitlog backup.");
        var data = JsonSerializer.Deserialize<Backup>(json) ?? throw new InvalidDataException("This backup is empty.");
        if (data.Version is not (1 or 2) || data.Logs == null || data.Weights == null || data.Measurements == null || data.Goals == null || data.Photos == null || data.Albums == null || data.Routines == null || data.Preferences == null)
            throw new InvalidDataException("Unsupported or incomplete backup.");
        foreach (var x in data.Logs) Validate("log", x);
        foreach (var x in data.Weights) Validate("weight", x);
        foreach (var x in data.Measurements) Validate("measurement", x);
        foreach (var x in data.Goals) Validate("goal", x);
        foreach (var x in data.Photos) Validate("photo", x);
        foreach (var x in data.Albums) Validate("album", x);
        foreach (var x in data.Routines) Validate("routine", x);
        if (data.Photos.Any(x => x.AlbumId != null && !data.Albums.Any(a => a.Id == x.AlbumId))) throw new InvalidDataException("A photo refers to a missing album.");
        Validate("preferences", data.Preferences);
        using var db = Open(); using var tx = db.BeginTransaction();
        foreach (var x in data.Logs) Put(db, tx, "log", x.Date.ToString("yyyy-MM-dd"), x);
        foreach (var x in data.Weights) Put(db, tx, "weight", x.Date.ToString("yyyy-MM-dd"), x);
        foreach (var x in data.Measurements) Put(db, tx, "measurement", x.Date.ToString("yyyy-MM-dd"), x);
        foreach (var x in data.Goals) Put(db, tx, "goal", x.Id, x);
        foreach (var x in data.Photos) Put(db, tx, "photo", x.Id, x);
        foreach (var x in data.Albums) Put(db, tx, "album", x.Id, x);
        foreach (var x in data.Routines) Put(db, tx, "routine", x.Id, x);
        Put(db, tx, "preferences", "main", data.Preferences); tx.Commit();
    }
    private static void Validate<T>(string kind, T value)
    {
        bool valid = value switch
        {
            DailyLog x => x.Date != default && (x.Status == null || Enum.IsDefined(x.Status.Value)) && (x.WorkoutCompletion == null || x.WorkoutCompletion is >= 0 and <= 100) && (x.DietCompletion == null || x.DietCompletion is >= 0 and <= 100) && x.Minutes is >= 0 and <= 1440 && x.Steps is >= 0 and <= 200000 && x.Water is >= 0 and <= 30 && x.Sleep is >= 0 and <= 24 && x.Energy is >= 1 and <= 5 && x.Notes != null && x.Workout != null,
            WeightEntry x => x.Date != default && double.IsFinite(x.Kilograms) && x.Kilograms is >= 20 and <= 500,
            Measurement x => x.Date != default && x.Waist is > 0 and <= 500 && x.Chest is > 0 and <= 500 && x.Hips is > 0 and <= 500,
            Goal x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Title) && x.Unit != null && double.IsFinite(x.Target) && x.Target > 0 && double.IsFinite(x.Current) && x.Current >= 0,
            ProgressPhoto x => !string.IsNullOrWhiteSpace(x.Id) && x.Date != default && x.Caption != null && ValidPhoto(x.Base64),
            PhotoAlbum x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Title) && x.Title.Length <= 100 && x.Date != default,
            WorkoutRoutine x => !string.IsNullOrWhiteSpace(x.Id) && !string.IsNullOrWhiteSpace(x.Title) && x.Title.Length <= 100 && x.Notes != null && x.Days != null && x.Days.Count > 0 && x.Days.All(Enum.IsDefined) && x.Exercises != null && x.Exercises.Count is > 0 and <= 100 && x.Exercises.All(e => e != null && !string.IsNullOrWhiteSpace(e.Name) && e.Name.Length <= 100 && e.Sets is >= 1 and <= 50 && !string.IsNullOrWhiteSpace(e.Reps) && e.Reps.Length <= 30 && e.WeightKg is >= 0 and <= 1000 && e.RestSeconds is >= 0 and <= 3600),
            Preferences x => x.Language is "en" or "ar" && x.WeeklyGoal is >= 1 and <= 7 && x.MonthlyGoal is >= 1 and <= 31 && x.TargetWeight is >= 20 and <= 500 && x.DietGoal is >= 1 and <= 100 && new[] { "Overall", "Gym", "Diet", "Weight logging" }.Contains(x.Heatmap),
            _ => false
        };
        if (!valid) throw new InvalidDataException($"Invalid {kind} values. Check the entered ranges.");
    }
    public void SavePhotos(IEnumerable<ProgressPhoto> photos)
    {
        var items = photos.ToList(); foreach (var photo in items) Validate("photo", photo);
        var albums = Read<PhotoAlbum>("album").Select(x => x.Id).ToHashSet();
        if (items.Any(x => x.AlbumId != null && !albums.Contains(x.AlbumId))) throw new InvalidDataException("A photo refers to a missing album.");
        using var db = Open(); using var tx = db.BeginTransaction();
        foreach (var photo in items) Put(db, tx, "photo", photo.Id, photo);
        tx.Commit();
    }
    private static bool ValidPhoto(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 14_000_000) return false;
        try { var bytes = Convert.FromBase64String(text); return bytes.Length >= 8 && ((bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47) || (bytes[0] == 0xff && bytes[1] == 0xd8)); }
        catch (FormatException) { return false; }
    }
}
