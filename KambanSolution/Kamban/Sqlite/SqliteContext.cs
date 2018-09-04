using Kamban.SqliteLocalStorage.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kamban.SqliteLocalStorage.Context
{
    public class SqliteContext : DbContext
    {
        private readonly string baseConnstr;

        public SqliteContext(string baseConnstr) 
        {
            this.baseConnstr = baseConnstr;
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public DbSet<RowInfo> Row { get; set; }
        public DbSet<ColumnInfo> Column { get; set; }
        public DbSet<BoardInfo> Board { get; set; }
        public DbSet<SqliteIssue> Issue    { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.EnableSensitiveDataLogging();
            builder.UseSqlite(baseConnstr);
        }
    }
}
