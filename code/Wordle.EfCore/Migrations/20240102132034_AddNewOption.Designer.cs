﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Wordle.EfCore;

#nullable disable

namespace Wordle.EfCore.Migrations
{
    [DbContext(typeof(WordleContext))]
    [Migration("20240102132034_AddNewOption")]
    partial class AddNewOption
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "round_state", new[] { "active", "inactive", "terminated" });
            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "session_state", new[] { "inactive", "active", "success", "fail", "terminated" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Wordle.Model.Guess", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<Guid>("RoundId")
                        .HasColumnType("uuid")
                        .HasColumnName("roundid");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uuid")
                        .HasColumnName("sessionid");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("timestamp");

                    b.Property<string>("User")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("user");

                    b.Property<string>("Word")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("word");

                    b.HasKey("Id")
                        .HasName("pk_guesses");

                    b.ToTable("guesses", (string)null);
                });

            modelBuilder.Entity("Wordle.Model.Options", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<bool>("AllowGuessesAfterRoundEnd")
                        .HasColumnType("boolean")
                        .HasColumnName("allowguessesafterroundend");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("createdat");

                    b.Property<string>("DictionaryName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("dictionaryname");

                    b.Property<int>("InitialRoundLength")
                        .HasColumnType("integer")
                        .HasColumnName("initialroundlength");

                    b.Property<int>("MaximumRoundExtensions")
                        .HasColumnType("integer")
                        .HasColumnName("maximumroundextensions");

                    b.Property<int>("MinimumAnswersRequired")
                        .HasColumnType("integer")
                        .HasColumnName("minimumanswersrequired");

                    b.Property<int>("NumberOfRounds")
                        .HasColumnType("integer")
                        .HasColumnName("numberofrounds");

                    b.Property<int>("RoundEndToleranceSeconds")
                        .HasColumnType("integer")
                        .HasColumnName("roundendtoleranceseconds");

                    b.Property<int>("RoundExtensionLength")
                        .HasColumnType("integer")
                        .HasColumnName("roundextensionlength");

                    b.Property<int>("RoundExtensionWindow")
                        .HasColumnType("integer")
                        .HasColumnName("roundextensionwindow");

                    b.Property<int>("RoundVotesPerUser")
                        .HasColumnType("integer")
                        .HasColumnName("roundvotesperuser");

                    b.Property<Guid?>("SessionId")
                        .HasColumnType("uuid")
                        .HasColumnName("sessionid");

                    b.Property<string>("TenantId")
                        .HasColumnType("text")
                        .HasColumnName("tenantid");

                    b.Property<int>("TiebreakerStrategy")
                        .HasColumnType("integer")
                        .HasColumnName("tiebreakerstrategy");

                    b.Property<int>("WordLength")
                        .HasColumnType("integer")
                        .HasColumnName("wordlength");

                    b.HasKey("Id")
                        .HasName("pk_options");

                    b.ToTable("options", (string)null);
                });

            modelBuilder.Entity("Wordle.Model.Round", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("createdat");

                    b.Property<string>("Guess")
                        .HasColumnType("text")
                        .HasColumnName("guess");

                    b.Property<int[]>("Result")
                        .IsRequired()
                        .HasColumnType("integer[]")
                        .HasColumnName("result");

                    b.Property<Guid>("SessionId")
                        .HasColumnType("uuid")
                        .HasColumnName("sessionid");

                    b.Property<int>("State")
                        .HasColumnType("integer")
                        .HasColumnName("state");

                    b.HasKey("Id")
                        .HasName("pk_rounds");

                    b.ToTable("rounds", (string)null);
                });

            modelBuilder.Entity("Wordle.Model.Session", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset?>("ActiveRoundEnd")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("activeroundend");

                    b.Property<Guid?>("ActiveRoundId")
                        .HasColumnType("uuid")
                        .HasColumnName("activeroundid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("createdat");

                    b.Property<int>("State")
                        .HasColumnType("integer")
                        .HasColumnName("state");

                    b.Property<string>("Tenant")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("tenant");

                    b.Property<List<string>>("UsedLetters")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("usedletters");

                    b.Property<string>("Word")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("word");

                    b.HasKey("Id")
                        .HasName("pk_sessions");

                    b.ToTable("sessions", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}