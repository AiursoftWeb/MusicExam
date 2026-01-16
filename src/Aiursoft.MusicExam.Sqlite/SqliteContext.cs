using System.Diagnostics.CodeAnalysis;
using Aiursoft.MusicExam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
