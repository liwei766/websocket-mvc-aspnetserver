using GPnaviServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Data
{
    /// <summary>
    /// DBコンテキスト
    /// </summary>
    public class GPnaviServerContext : DbContext
    {
        public GPnaviServerContext(DbContextOptions<GPnaviServerContext> options) : base(options)
        {

        }
        /// <summary>
        /// 担当者マスタ
        /// </summary>
        public DbSet<UserMaster> UserMasters { get; set; }
        /// <summary>
        /// 担当者ステータス
        /// </summary>
        public DbSet<UserStatus> UserStatuses { get; set; }
        /// <summary>
        /// WSマスタバージョン管理
        /// </summary>
        public DbSet<WorkScheduleVersion> WorkScheduleVersions { get; set; }
        /// <summary>
        /// 担当者マスタバージョン管理
        /// </summary>
        public DbSet<UserVersion> UserVersions { get; set; }
        /// <summary>
        /// WSマスタ
        /// </summary>
        public DbSet<WorkScheduleMaster> WorkScheduleMasters { get; set; }
        /// <summary>
        /// WS作業状態
        /// </summary>
        public DbSet<WorkScheduleStatus> WorkScheduleStatuses { get; set; }
        /// <summary>
        /// 突発作業状態
        /// </summary>
        public DbSet<SensorStatus> SensorStatuses { get; set; }
        /// <summary>
        /// 祝日マスタ
        /// </summary>
        public DbSet<HolidayMaster> HolidayMasters { get; set; }
        /// <summary>
        /// センサー死活監視
        /// </summary>
        public DbSet<SensorMonitor> SensorMonitors { get; set; }
        /// <summary>
        /// 作業状況履歴
        /// </summary>
        public DbSet<WorkStatusHistory> WorkStatusHistories { get; set; }
        /// <summary>
        /// センサーマスタ
        /// </summary>
        public DbSet<SensorMaster> SensorMasters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // WSマスタに複合キーを設定する
            modelBuilder.Entity<WorkScheduleMaster>().HasKey(e => new { e.Version, e.Start, e.Name, e.Holiday });

            // WS作業状態に複合キーを設定する
            modelBuilder.Entity<WorkScheduleStatus>().HasKey(e => new { e.Version, e.Start, e.Name, e.Holiday });

            // 作業状況履歴に複合キーを設定する
            modelBuilder.Entity<WorkStatusHistory>().HasKey(e => new { e.Version, e.Start, e.Name, e.Holiday, e.SensorId, e.RegisterDate });
            // 作業状況履歴にインデックスを設定する
            modelBuilder.Entity<WorkStatusHistory>().HasIndex(e => e.StartDate);
        }
    }
}
