using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.ef
{
    internal class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>().ToTable("Student");
        }
    }
}
