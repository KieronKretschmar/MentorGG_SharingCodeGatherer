using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class SharingCodeContext : DbContext
    {
        public SharingCodeContext()
        {
        }

        public SharingCodeContext(DbContextOptions<SharingCodeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Match> Matches { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Upload> Uploads { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x=>x.Id).ValueGeneratedOnAdd();

            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => new { e.SteamId });
            });

            modelBuilder.Entity<Upload>(entity =>
            {
                entity.HasKey(e => new { e.UploadId });
                entity.Property(x=>x.UploadId).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Uploader)
                    .WithMany(p => p.Uploads)
                    .HasForeignKey(d => d.SteamId)
                    .IsRequired();

                entity.HasOne(d => d.Match)
                    .WithMany(p => p.Uploads)
                    .HasForeignKey(d => d.InternalMatchId)
                    .IsRequired();
            });
        }
    }
}
