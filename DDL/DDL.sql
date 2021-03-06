USE [gpnavi_db]
GO
/****** Object:  Table [dbo].[HolidayMaster]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HolidayMaster](
	[Holiday] [date] NOT NULL,
 CONSTRAINT [PK_HolidayMaster_Holiday] PRIMARY KEY CLUSTERED 
(
	[Holiday] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SensorMonitor]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SensorMonitor](
	[SensorId] [varchar](4) NOT NULL,
	[LastReceiveTime] [datetime] NULL,
 CONSTRAINT [PK_SensorMonitor_SensorId] PRIMARY KEY CLUSTERED 
(
	[SensorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SensorStatus]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SensorStatus](
	[SensorId] [varchar](4) NOT NULL,
	[SensorType] [varchar](8) NULL,
	[DisplayName] [varchar](20) NULL,
	[OccurrenceDate] [datetime] NULL,
	[StartDate] [datetime] NULL,
	[Status] [varchar](8) NULL,
	[LoginId] [varchar](13) NULL,
 CONSTRAINT [PK_SensorStatus_SensorId] PRIMARY KEY CLUSTERED 
(
	[SensorId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserMaster]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserMaster](
	[LoginId] [varchar](13) NOT NULL,
	[Password] [varchar](64) NOT NULL,
	[LoginName] [nvarchar](16) NOT NULL,
	[Role] [char](1) NOT NULL,
	[IsValid] [bit] NOT NULL,
	[RemoveDate] [datetime] NULL,
 CONSTRAINT [PK_UserMaster_LoginId] PRIMARY KEY CLUSTERED 
(
	[LoginId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserStatus]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserStatus](
	[LoginId] [varchar](13) NOT NULL,
	[SessionKey] [varchar](36) NOT NULL,
	[DeviceType] [varchar](8) NOT NULL,
	[DeviceToken] [varchar](4096) NOT NULL,
 CONSTRAINT [PK_UserStatus_LoginId] PRIMARY KEY CLUSTERED 
(
	[LoginId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserVersion]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserVersion](
	[Version] [bigint] NOT NULL,
	[RegisterDate] [datetime] NULL,
	[ExpirationDate] [datetime] NULL,
 CONSTRAINT [PK_UserVersion_Version] PRIMARY KEY CLUSTERED 
(
	[Version] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WorkScheduleMaster]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WorkScheduleMaster](
	[Version] [bigint] NOT NULL,
	[Start] [char](5) NOT NULL,
	[Name] [varchar](40) NOT NULL,
	[ShortName] [varchar](20) NULL,
	[Priority] [char](1) NULL,
	[IconId] [char](4) NULL,
	[Time] [varchar](2) NULL,
	[Holiday] [char](1) NOT NULL,
	[Row] [bigint] NULL,
 CONSTRAINT [PK_WorkScheduleMaster] PRIMARY KEY CLUSTERED 
(
	[Version] ASC,
	[Start] ASC,
	[Name] ASC,
	[Holiday] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WorkScheduleStatus]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WorkScheduleStatus](
	[Version] [bigint] NOT NULL,
	[Start] [varchar](5) NOT NULL,
	[Name] [varchar](40) NOT NULL,
	[Holiday] [char](1) NOT NULL,
	[Status] [varchar](8) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[StatusUpdateDate] [datetime] NULL,
	[LoginId] [varchar](13) NOT NULL,
 CONSTRAINT [PK_WorkScheduleStatus] PRIMARY KEY CLUSTERED 
(
	[Version] ASC,
	[Start] ASC,
	[Name] ASC,
	[Holiday] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WorkScheduleVersion]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WorkScheduleVersion](
	[Version] [bigint] NOT NULL,
	[RegisterDate] [datetime] NULL,
	[ExpirationDate] [datetime] NOT NULL,
 CONSTRAINT [PK_WorkScheduleVersion] PRIMARY KEY CLUSTERED 
(
	[Version] ASC,
	[ExpirationDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[WorkStatusHistory]    Script Date: 2018/08/13 13:50:02 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[WorkStatusHistory](
	[Version] [bigint] NOT NULL,
	[Start] [varchar](5) NOT NULL,
	[Name] [varchar](40) NOT NULL,
	[Holiday] [varchar](1) NOT NULL,
	[SessionId] [varchar](4) NOT NULL,
	[DisplayName] [varchar](20) NULL,
	[LoginId] [varchar](13) NULL,
	[LoginName] [varchar](16) NULL,
	[Status] [varchar](8) NULL,
	[StartDate] [datetime] NOT NULL,
	[RegisterDate] [datetime] NOT NULL,
 CONSTRAINT [PK_WorkStatusHistory] PRIMARY KEY CLUSTERED 
(
	[Version] ASC,
	[Start] ASC,
	[Name] ASC,
	[Holiday] ASC,
	[SessionId] ASC,
	[StartDate] ASC,
	[RegisterDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
