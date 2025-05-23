﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TelegramDigest.Backend.Db;

#nullable disable

namespace TelegramDigest.Backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250219164435_DigestPostOneToMany")]
    partial class DigestPostOneToMany
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("TelegramDigest.Backend.Database.ChannelEntity", b =>
                {
                    b.Property<string>("TgId")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("TEXT");

                    b.Property<string>("ImageUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("TgId");

                    b.HasIndex("IsDeleted");

                    b.ToTable("Channels", (string)null);
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.DigestEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Digests", (string)null);
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.DigestSummaryEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("TEXT");

                    b.Property<double>("AverageImportance")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateFrom")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTo")
                        .HasColumnType("TEXT");

                    b.Property<int>("PostsCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PostsSummary")
                        .IsRequired()
                        .HasMaxLength(8192)
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("DigestSummaries", (string)null);
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.PostSummaryEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("ChannelTgId")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<Guid>("DigestId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Importance")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("PublishedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Summary")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ChannelTgId");

                    b.HasIndex("DigestId");

                    b.ToTable("PostSummaries", (string)null);
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.DigestSummaryEntity", b =>
                {
                    b.HasOne("TelegramDigest.Backend.Database.DigestEntity", "DigestNav")
                        .WithOne("SummaryNav")
                        .HasForeignKey("TelegramDigest.Backend.Database.DigestSummaryEntity", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DigestNav");
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.PostSummaryEntity", b =>
                {
                    b.HasOne("TelegramDigest.Backend.Database.ChannelEntity", "ChannelNav")
                        .WithMany()
                        .HasForeignKey("ChannelTgId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("TelegramDigest.Backend.Database.DigestEntity", "DigestNav")
                        .WithMany("PostsNav")
                        .HasForeignKey("DigestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ChannelNav");

                    b.Navigation("DigestNav");
                });

            modelBuilder.Entity("TelegramDigest.Backend.Database.DigestEntity", b =>
                {
                    b.Navigation("PostsNav");

                    b.Navigation("SummaryNav");
                });
#pragma warning restore 612, 618
        }
    }
}
