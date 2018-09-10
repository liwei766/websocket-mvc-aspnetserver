select * from UserVersions;
delete from UserVersions

select * from UserMasters where LoginId='test';
update UserMasters set LoginName='admin',IsValid='1' where LoginId='test';


update UserMasters set LoginName=(select LoginName from UserMasters where LoginId='333') where LoginId='test';

select LoginName from UserMasters where LoginId='333';
--田中一郎

delete from UserMasters where role='0' ;

select * from UserMasters

select * from UserStatuses ;
update UserStatuses set SessionKey='3ca995b6-391f-429f-90a1-6524f1a07563' where LoginId='111';

delete from WorkScheduleVersions;
select * from WorkScheduleVersions order by id desc;
select max(id) from WorkScheduleVersions;

select * from WorkScheduleMasters order by version desc;
delete from WorkScheduleMasters where Version>0;
select * from WorkScheduleMasters where Version=25;
update WorkScheduleMasters set Start='23:00' where Version=10054;

select * from WorkStatusHistories;
select * from SensorStatuses;
delete from WorkStatusHistories;

insert into WorkStatusHistories(Version,Start,Name,Holiday,SensorId,StartDate,DisplayName,LoginId,LoginName,RegisterDate,Status,RfType) 
values(0,'','','','9002','2018-09-09 11:27:03.9930000',N'ポット２','001',N'東 達也','2018-09-09 11:27:03.9930000',N'開始','');
insert into WorkStatusHistories(Version,Start,Name,Holiday,SensorId,StartDate,DisplayName,LoginId,LoginName,RegisterDate,Status,RfType) 
values(118,'13:00',N'乳製品補充','1','','2018-09-09 11:20:03.9930000',N'乳製品補充','001',N'東 達也a','2018-09-09 11:27:03.9930000',N'開始','');

update SensorStatuses set OccurrenceDate='2018-09-09 10:46:15.0083002' where SensorId='9002'
insert into SensorStatuses(SensorId,DisplayName,LoginId,OccurrenceDate,SensorType,StartDate,Status)
values(9002,N'ポット２',NULL,'2018-09-09 10:46:15.0083002','POT','0001-01-01 00:00:00.0000000',NULL);


INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'111', 1, N'山田太郎', N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'9999-12-31 23:59:59', N'0')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'222', 1, N'鈴木次郎', N'6b56cb3bacea5eb11b954e72a9b0342dee3e208a5601fdb79c9256e622630982', N'9999-12-31 23:59:59', N'0')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'333', 1, N'佐藤三郎', N'9af15b336e6a9619928537df30b2e6a2376569fcf9d7e773eccede65606529a0', N'9999-12-31 23:59:59', N'0')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'444', 1, N'tanaka', N'1038e0b72d98745fac0fb015fd9c56704862adf11392936242a2ff5a65629f50', N'9999-12-31 23:59:59', N'0')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'555', 1, N'suzuki', N'1038e0b72d98745fac0fb015fd9c56704862adf11392936242a2ff5a65629f50', N'9999-12-31 23:59:59', N'0')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'999', 1, N'初期設定管理者', N'999', N'0001-01-01 00:00:00', N'1')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'test', 1, N'佐藤三郎', N'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3', N'0001-01-01 00:00:00', N'1')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'test2', 0, N'test2', N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'0001-01-01 00:00:00', N'1')
INSERT INTO [dbo].[UserMasters] ([LoginId], [IsValid], [LoginName], [Password], [RemoveDate], [Role]) VALUES (N'test3', 0, N'test2', N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'0001-01-01 00:00:00', N'1')



select * from HolidayMasters;