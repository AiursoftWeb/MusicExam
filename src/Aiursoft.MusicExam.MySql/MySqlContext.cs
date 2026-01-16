using System.Diagnostics.CodeAnalysis;
using Aiursoft.MusicExam.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : TemplateDbContext(options);
