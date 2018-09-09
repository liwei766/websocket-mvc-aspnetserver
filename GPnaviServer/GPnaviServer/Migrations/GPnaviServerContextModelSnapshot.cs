﻿// <auto-generated />
using GPnaviServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace GPnaviServer.Migrations
{
    [DbContext(typeof(GPnaviServerContext))]
    partial class GPnaviServerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.3-rtm-10026")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GPnaviServer.Models.HolidayMaster", b =>
                {
                    b.Property<DateTime>("Holiday");

                    b.HasKey("Holiday");

                    b.ToTable("HolidayMasters");
                });

            modelBuilder.Entity("GPnaviServer.Models.SensorMaster", b =>
                {
                    b.Property<string>("SensorId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(4);

                    b.Property<string>("DisplayName")
                        .HasMaxLength(20);

                    b.Property<string>("SensorType")
                        .HasMaxLength(8);

                    b.HasKey("SensorId");

                    b.ToTable("SensorMasters");
                });

            modelBuilder.Entity("GPnaviServer.Models.SensorMonitor", b =>
                {
                    b.Property<string>("SensorId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(4);

                    b.Property<DateTime>("LastReceiveTime");

                    b.HasKey("SensorId");

                    b.ToTable("SensorMonitors");
                });

            modelBuilder.Entity("GPnaviServer.Models.SensorStatus", b =>
                {
                    b.Property<string>("SensorId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(4);

                    b.Property<string>("DisplayName")
                        .HasMaxLength(20);

                    b.Property<string>("LoginId")
                        .HasMaxLength(13);

                    b.Property<DateTime>("OccurrenceDate");

                    b.Property<string>("SensorType")
                        .HasMaxLength(8);

                    b.Property<DateTime>("StartDate");

                    b.Property<string>("Status")
                        .HasMaxLength(8);

                    b.HasKey("SensorId");

                    b.ToTable("SensorStatuses");
                });

            modelBuilder.Entity("GPnaviServer.Models.UserMaster", b =>
                {
                    b.Property<string>("LoginId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(13);

                    b.Property<bool>("IsValid");

                    b.Property<string>("LoginName")
                        .HasMaxLength(16);

                    b.Property<string>("Password")
                        .HasMaxLength(64);

                    b.Property<DateTime>("RemoveDate");

                    b.Property<string>("Role")
                        .HasMaxLength(1);

                    b.HasKey("LoginId");

                    b.ToTable("UserMasters");
                });

            modelBuilder.Entity("GPnaviServer.Models.UserStatus", b =>
                {
                    b.Property<string>("LoginId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(13);

                    b.Property<string>("DeviceToken")
                        .HasMaxLength(4096);

                    b.Property<string>("DeviceType")
                        .HasMaxLength(8);

                    b.Property<string>("SessionKey")
                        .HasMaxLength(36);

                    b.HasKey("LoginId");

                    b.ToTable("UserStatuses");
                });

            modelBuilder.Entity("GPnaviServer.Models.UserVersion", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<DateTime>("RegisterDate");

                    b.HasKey("Id");

                    b.ToTable("UserVersions");
                });

            modelBuilder.Entity("GPnaviServer.Models.WorkScheduleMaster", b =>
                {
                    b.Property<long>("Version");

                    b.Property<string>("Start")
                        .HasMaxLength(5);

                    b.Property<string>("Name")
                        .HasMaxLength(40);

                    b.Property<string>("Holiday")
                        .HasMaxLength(1);

                    b.Property<string>("IconId")
                        .HasMaxLength(4);

                    b.Property<string>("Priority")
                        .HasMaxLength(1);

                    b.Property<long>("Row");

                    b.Property<string>("ShortName")
                        .HasMaxLength(20);

                    b.Property<string>("Time")
                        .HasMaxLength(2);

                    b.HasKey("Version", "Start", "Name", "Holiday");

                    b.ToTable("WorkScheduleMasters");
                });

            modelBuilder.Entity("GPnaviServer.Models.WorkScheduleStatus", b =>
                {
                    b.Property<long>("Version");

                    b.Property<string>("Start")
                        .HasMaxLength(5);

                    b.Property<string>("Name")
                        .HasMaxLength(40);

                    b.Property<string>("Holiday")
                        .HasMaxLength(1);

                    b.Property<string>("LoginId")
                        .HasMaxLength(13);

                    b.Property<DateTime>("StartDate");

                    b.Property<string>("Status")
                        .HasMaxLength(8);

                    b.Property<DateTime>("StatusUpdateDate");

                    b.HasKey("Version", "Start", "Name", "Holiday");

                    b.ToTable("WorkScheduleStatuses");
                });

            modelBuilder.Entity("GPnaviServer.Models.WorkScheduleVersion", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<DateTime>("RegisterDate");

                    b.HasKey("Id");

                    b.ToTable("WorkScheduleVersions");
                });

            modelBuilder.Entity("GPnaviServer.Models.WorkStatusHistory", b =>
                {
                    b.Property<long>("Version");

                    b.Property<string>("Start")
                        .HasMaxLength(5);

                    b.Property<string>("Name")
                        .HasMaxLength(40);

                    b.Property<string>("Holiday")
                        .HasMaxLength(1);

                    b.Property<string>("SensorId")
                        .HasMaxLength(4);

                    b.Property<DateTime>("RegisterDate");

                    b.Property<string>("DisplayName")
                        .HasMaxLength(20);

                    b.Property<string>("LoginId")
                        .HasMaxLength(13);

                    b.Property<string>("LoginName")
                        .HasMaxLength(16);

                    b.Property<string>("RfType")
                        .HasMaxLength(5);

                    b.Property<DateTime>("StartDate");

                    b.Property<string>("Status")
                        .HasMaxLength(8);

                    b.HasKey("Version", "Start", "Name", "Holiday", "SensorId", "RegisterDate");

                    b.HasIndex("StartDate");

                    b.HasIndex("Status");

                    b.ToTable("WorkStatusHistories");
                });
#pragma warning restore 612, 618
        }
    }
}
