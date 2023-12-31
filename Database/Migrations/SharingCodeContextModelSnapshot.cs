﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Migrations
{
    [DbContext(typeof(SharingCodeContext))]
    partial class SharingCodeContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Entities.Models.Match", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<byte>("AnalyzedQuality")
                        .HasColumnType("tinyint unsigned");

                    b.Property<string>("SharingCode")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("Matches");
                });

            modelBuilder.Entity("Entities.Models.Upload", b =>
                {
                    b.Property<int>("UploadId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("InternalMatchId")
                        .HasColumnType("int");

                    b.Property<byte>("Quality")
                        .HasColumnType("tinyint unsigned");

                    b.Property<long>("SteamId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("UploadTime")
                        .HasColumnType("datetime(6)");

                    b.HasKey("UploadId");

                    b.HasIndex("InternalMatchId");

                    b.HasIndex("SteamId");

                    b.ToTable("Uploads");
                });

            modelBuilder.Entity("Entities.Models.User", b =>
                {
                    b.Property<long>("SteamId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<bool>("Invalidated")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LastKnownSharingCode")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("SteamAuthToken")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("SteamId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Entities.Models.Upload", b =>
                {
                    b.HasOne("Entities.Models.Match", "Match")
                        .WithMany("Uploads")
                        .HasForeignKey("InternalMatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Entities.Models.User", "Uploader")
                        .WithMany("Uploads")
                        .HasForeignKey("SteamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
