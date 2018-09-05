select * from UserVersions;
delete from UserVersions

select * from UserMasters where LoginId='test';
update UserMasters set LoginName='admin' where LoginId='test';

//田中一郎

delete from UserMasters where role='0' ;

select * from UserStatuses ;
update UserStatuses set LoginId='111' where LoginId='test';

delete from WorkScheduleVersions;
select * from WorkScheduleVersions;
select max(id) from WorkScheduleVersions;

select * from WorkScheduleMasters;
delete from WorkScheduleMasters where Version>0;
select * from WorkScheduleMasters where Version=25;
update WorkScheduleMasters set Start='23:00' where Version=10054;

select * from WorkStatusHistories;
select * from SensorStatuses;


select * from HolidayMasters;