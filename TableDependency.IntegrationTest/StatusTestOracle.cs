﻿using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.DataAccess.Client;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.Oracle;
using TableDependency.IntegrationTest.Models;
using TableDependency.OracleClient;

namespace TableDependency.IntegrationTest
{
    [TestClass]
    public class StatusTestOracle
    {
        private OracleTableDependency<Item> _tableDependency = null;
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["OracleConnectionString"].ConnectionString;
        private const string TableName = "StatusCheckTest";

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
           
            OracleHelper.DropTable(ConnectionString, TableName);

            using (var connection = new OracleConnection(ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"CREATE TABLE {TableName} (ID number(10), NAME varchar2(50), qty number(10))";
                    command.ExecuteNonQuery();
                }
            }
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            OracleHelper.DropTable(ConnectionString, TableName);
        }

        [TestMethod]
        public void StatusTest()
        {
            try
            {

                this._tableDependency = new OracleTableDependency<Item>(ConnectionString, TableName);
                this._tableDependency.OnChanged += this.TableDependency_Changed;

                Assert.IsTrue(this._tableDependency.Status == TableDependencyStatus.WaitingForStart);

                this._tableDependency.Start();

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                t.Wait(20000);

                this._tableDependency.Stop();
                Assert.IsTrue(this._tableDependency.Status == TableDependencyStatus.StoppedDueToCancellation);
            }
            finally
            {
                this._tableDependency?.Dispose();
            }
        }

        private void TableDependency_Changed(object sender, RecordChangedEventArgs<Item> e)
        {
            Assert.IsTrue(this._tableDependency.Status == TableDependencyStatus.WaitingForNotification || this._tableDependency.Status == TableDependencyStatus.NotificationConsuming || this._tableDependency.Status == TableDependencyStatus.NotificationConsumed);
        }

        private static void ModifyTableContent()
        {
            using (var connection = new OracleConnection(ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"BEGIN INSERT INTO {TableName} (ID, NAME, QTY) VALUES (1, '2', '3'); END;";
                    command.ExecuteNonQuery();
                    Thread.Sleep(2000);

                    command.CommandText = $"BEGIN UPDATE {TableName} SET NAME = '1', QTY = '2'; END;";
                    command.ExecuteNonQuery();
                    Thread.Sleep(2000);

                    command.CommandText = $"BEGIN DELETE FROM {TableName}; END;";
                    command.ExecuteNonQuery();
                    Thread.Sleep(2000);
                }
            }
        }
    }
}