﻿// <auto-generated />
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Migrations
{
    [DbContext(typeof(SharingCodeContext))]
    [Migration("20191215091129_AddUserInvalidated")]
    partial class AddUserInvalidated
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Entities.Models.Match", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("SharingCode")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("Matches");
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
#pragma warning restore 612, 618
        }
    }
}
