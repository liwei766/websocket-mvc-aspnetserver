using GPnaviServer.Data;
using GPnaviServer.Utilities;
using GPnaviServer.WebSockets.APIs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPnaviServer.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new GPnaviServerContext(serviceProvider.GetRequiredService<DbContextOptions<GPnaviServerContext>>()))
            {
                bool isDirty = false;

                if (!context.UserMasters.Any())
                {
                    // 担当者マスタの初期設定
                    context.UserMasters.AddRange(
                        new UserMaster
                        {
                            LoginId = "999",
                            Password = PasswordUtility.Hash("999"),
                            LoginName = "初期設定管理者",
                            Role = ApiConstant.ROLE_ADMIN,
                            IsValid = true,
                        },
                        new UserMaster
                        {
                            LoginId = "111",
                            Password = PasswordUtility.Hash("1234"),
                            LoginName = "山田太郎",
                            Role = ApiConstant.ROLE_WORK,
                            IsValid = true,
                        },
                        new UserMaster
                        {
                            LoginId = "222",
                            Password = PasswordUtility.Hash("5678"),
                            LoginName = "鈴木次郎",
                            Role = ApiConstant.ROLE_WORK,
                            IsValid = true,
                        },
                        new UserMaster
                        {
                            LoginId = "333",
                            Password = PasswordUtility.Hash("0000"),
                            LoginName = "佐藤三郎",
                            Role = ApiConstant.ROLE_WORK,
                            IsValid = true,
                        }
                    );

                    isDirty = true;
                }

                if (!context.WorkScheduleMasters.Any())
                {
                    // WSマスタの初期設定
                    context.WorkScheduleMasters.AddRange(
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "チルド２便納品",
                            ShortName = "チルド納品",
                            Priority = "H",
                            IconId = "0003",
                            Time = "30",
                            Holiday = "0",
                            Row = 1
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "ヤマザキ２便検収・納品",
                            ShortName = "ヤマザキ検収・納品",
                            Priority = "H",
                            IconId = "0003",
                            Time = "30",
                            Holiday = "0",
                            Row = 2
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "店頭ガラス清掃",
                            ShortName = "ガラス清掃",
                            Priority = "L",
                            IconId = "0008",
                            Time = "15",
                            Holiday = "0",
                            Row = 3
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "入口ドア清掃",
                            ShortName = "ドア清掃",
                            Priority = "L",
                            IconId = "0008",
                            Time = "10",
                            Holiday = "0",
                            Row = 4
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "チルド２便納品",
                            ShortName = "チルド納品",
                            Priority = "H",
                            IconId = "0003",
                            Time = "30",
                            Holiday = "1",
                            Row = 5
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "雑誌検収・納品",
                            ShortName = "検収・納品",
                            Priority = "M",
                            IconId = "0003",
                            Time = "15",
                            Holiday = "1",
                            Row = 6
                        },
                        new WorkScheduleMaster
                        {
                            Version = 0,
                            Start = "7:00",
                            Name = "入口ドア清掃",
                            ShortName = "ドア清掃",
                            Priority = "L",
                            IconId = "0008",
                            Time = "10",
                            Holiday = "1",
                            Row = 7
                        }
                    );

                    isDirty = true;
                }

                if (!context.HolidayMasters.Any())
                {
                    // 休日マスタの初期設定
                    context.HolidayMasters.AddRange(
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 9, 17)
                        },
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 9, 23)
                        },
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 10, 8)
                        },
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 11, 3)
                        },
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 11, 23)
                        },
                        new HolidayMaster
                        {
                            Holiday = new DateTime(2018, 12, 23)
                        }
                    );

                    isDirty = true;
                }

                if (!context.SensorMasters.Any())
                {
                    // センサーマスタの初期設定
                    context.SensorMasters.AddRange(
                        new SensorMaster
                        {
                            SensorId = "9001",
                            SensorType = ApiConstant.SENSOR_TYPE_POT,
                            DisplayName = "ポット１"
                        },
                        new SensorMaster
                        {
                            SensorId = "9002",
                            SensorType = ApiConstant.SENSOR_TYPE_POT,
                            DisplayName = "ポット２"
                        },
                        new SensorMaster
                        {
                            SensorId = "9003",
                            SensorType = ApiConstant.SENSOR_TYPE_TRASH,
                            DisplayName = "ごみ箱１"
                        },
                        new SensorMaster
                        {
                            SensorId = "9004",
                            SensorType = ApiConstant.SENSOR_TYPE_TRASH,
                            DisplayName = "ごみ箱２"
                        }
                    );

                    isDirty = true;
                }

                if (isDirty)
                {
                    context.SaveChanges();
                }
            }
        }
    }
}
