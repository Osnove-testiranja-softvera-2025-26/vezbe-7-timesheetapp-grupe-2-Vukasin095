using System;
using NUnit.Framework;
using TimesheetApp;
using TimesheetApp.Interfaces;

namespace TimesheetApp.Test
{
    [TestFixture]
    public class TimeLoggerTests
    {
        // Simple stubs for dependencies
        private class FakeTask : ITask
        {
            public int TaskId { get; set; }
            public int Hours { get; set; }
            public int Minutes { get; set; }
            public string Description { get; set; }
            public bool SaveResult { get; set; }
            public bool SaveToDB()
            {
                return SaveResult;
            }
        }

        private class FakeUserLogger : IUserLogger
        {
            public string UserName { get; set; }
            public string UserEmail { get; set; }
            public string GetLoggedUserName()
            {
                return UserName;
            }

            public string GetLoggedUserEmail(string userName)
            {
                return UserEmail;
            }
        }

        private class FakeTaskManager : ITaskManager
        {
            public int TaskIdToReturn { get; set; }
            public int GetTaskId(string loggedUserName, string loggedUserEmail)
            {
                return TaskIdToReturn;
            }
        }

        private class FakeEmailSender : IEmailSender
        {
            public bool WasCalled;
            public string LastTo;
            public string LastTitle;
            public string LastBody;
            public void SendEmail(string to, string title, string body)
            {
                WasCalled = true;
                LastTo = to;
                LastTitle = title;
                LastBody = body;
            }
        }

        private class FakeErrorLogger : IErrorLogger
        {
            public Exception LastError;
            public void LogError(Exception error)
            {
                LastError = error;
            }
        }

        [Test]
        public void LogTime_SuccessfulLogging_SendsEmail()
        {
            var task = new FakeTask();
            task.SaveResult = true;
            var userLogger = new FakeUserLogger();
            userLogger.UserName = "john";
            userLogger.UserEmail = "john@example.com";
            var taskManager = new FakeTaskManager();
            taskManager.TaskIdToReturn = 42;
            var emailSender = new FakeEmailSender();
            var errorLogger = new FakeErrorLogger();

            var sut = new TimeLogger(task, emailSender, errorLogger, userLogger, taskManager);

            sut.LogTime(2, 30, "Worked on feature");

            Assert.IsTrue(emailSender.WasCalled);
            Assert.AreEqual("john@example.com", emailSender.LastTo);
            Assert.AreEqual(42, task.TaskId);
            Assert.AreEqual(2, task.Hours);
            Assert.AreEqual(30, task.Minutes);
            Assert.AreEqual("Worked on feature", task.Description);
            Assert.IsNull(errorLogger.LastError);
        }

        [Test]
        public void LogTime_SaveFails_LogsErrorAndThrows()
        {
            var task = new FakeTask();
            task.SaveResult = false;
            var userLogger = new FakeUserLogger();
            userLogger.UserName = "john";
            userLogger.UserEmail = "john@example.com";
            var taskManager = new FakeTaskManager();
            taskManager.TaskIdToReturn = 100;
            var emailSender = new FakeEmailSender();
            var errorLogger = new FakeErrorLogger();

            var sut = new TimeLogger(task, emailSender, errorLogger, userLogger, taskManager);

            Exception thrown = null;
            try
            {
                sut.LogTime(1, 0, "desc");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual("Failed to save data to database", thrown.Message);
            Assert.IsNotNull(errorLogger.LastError);
            Assert.AreEqual("Failed to save data to database", errorLogger.LastError.Message);
            Assert.IsFalse(emailSender.WasCalled);
        }

        [Test]
        public void LogTime_GetUserEmailThrows_LogsError()
        {
            var task = new FakeTask();
            task.SaveResult = true;
            var userLogger = new FakeUserLoggerThrowsEmail();
            var taskManager = new FakeTaskManager();
            taskManager.TaskIdToReturn = 1;
            var emailSender = new FakeEmailSender();
            var errorLogger = new FakeErrorLogger();

            var sut = new TimeLogger(task, emailSender, errorLogger, userLogger, taskManager);

            Exception thrown = null;
            try
            {
                sut.LogTime(0, 10, "desc");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual("Failed to get user email", thrown.Message);
            Assert.IsNotNull(errorLogger.LastError);
            Assert.AreEqual("Failed to get user email", errorLogger.LastError.Message);
        }

        [Test]
        public void LogTime_GetTaskIdThrows_LogsError()
        {
            var task = new FakeTask();
            task.SaveResult = true;
            var userLogger = new FakeUserLogger();
            userLogger.UserName = "john";
            userLogger.UserEmail = "john@e.com";
            var taskManager = new FakeTaskManagerThrows();
            var emailSender = new FakeEmailSender();
            var errorLogger = new FakeErrorLogger();

            var sut = new TimeLogger(task, emailSender, errorLogger, userLogger, taskManager);

            Exception thrown = null;
            try
            {
                sut.LogTime(3, 15, "desc");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual("Failed to get the task info", thrown.Message);
            Assert.IsNotNull(errorLogger.LastError);
            Assert.AreEqual("Failed to get the task info", errorLogger.LastError.Message);
        }

        [Test]
        public void LogTime_SendEmailThrows_LogsError()
        {
            var task = new FakeTask();
            task.SaveResult = true;
            var userLogger = new FakeUserLogger();
            userLogger.UserName = "john";
            userLogger.UserEmail = "john@e.com";
            var taskManager = new FakeTaskManager();
            taskManager.TaskIdToReturn = 7;
            var emailSender = new FakeEmailSenderThrows();
            var errorLogger = new FakeErrorLogger();

            var sut = new TimeLogger(task, emailSender, errorLogger, userLogger, taskManager);

            Exception thrown = null;
            try
            {
                sut.LogTime(4, 0, "desc");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            Assert.IsNotNull(thrown);
            Assert.AreEqual("Failed to send email", thrown.Message);
            Assert.IsNotNull(errorLogger.LastError);
            Assert.AreEqual("Failed to send email", errorLogger.LastError.Message);
        }

        private class FakeUserLoggerThrowsEmail : IUserLogger
        {
            public string GetLoggedUserName()
            {
                return "john";
            }

            public string GetLoggedUserEmail(string userName)
            {
                throw new Exception("Failed to get user email");
            }
        }

        private class FakeTaskManagerThrows : ITaskManager
        {
            public int GetTaskId(string loggedUserName, string loggedUserEmail)
            {
                throw new Exception("Failed to get the task info");
            }
        }

        private class FakeEmailSenderThrows : IEmailSender
        {
            public void SendEmail(string to, string title, string body)
            {
                throw new Exception("Failed to send email");
            }
        }
    }
}
