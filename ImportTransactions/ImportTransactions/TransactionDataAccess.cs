using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ImportTransactions
{
    public class TransactionDataAccess
    {
        public static SqlConnection Connect()
        {
            var connString = ConfigurationManager.ConnectionStrings["DatabaseConnection"].ConnectionString;
            SqlConnection conn = new SqlConnection { ConnectionString = connString };
            return conn;
        }

        public static void InsertTransactions(string values)
        {
            var tableName = ConfigurationManager.AppSettings.Get("TransactionsTableName");
            var insertStatement =string.Format("INSERT INTO {0} ([Account],[Description],[CurrencyCode],[Value]) Values {1}",tableName, values);

            var conn = Connect();
            conn.Open();
            var command = conn.CreateCommand();
            command.CommandText = insertStatement;
            command.CommandType = CommandType.Text;
            SqlDataAdapter sda = new SqlDataAdapter(command);
            DataTable dt = new DataTable();
            sda.Fill(dt);
        }

        public static void InsertInvalidLines(string values)
        {
            var tableName = ConfigurationManager.AppSettings.Get("InvalidTableName");
            string insertStatement = string.Format("INSERT INTO {0} (InvalidLine) Values ('{1}')", tableName,values);
            var conn = Connect();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = insertStatement;
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            sda.Fill(dt);
        }

        // TODO: Implement Bulk Insert
        //public static void BulkInsertTransactions(string sql)
        //{
        //}

        public static void InsertErrors(IEnumerable<Exception> exceptions)
        {
            var tableName = ConfigurationManager.AppSettings.Get("ErrorsTableName");
            var exceptionList = exceptions.Select(exception => string.Format("('{0}', '{1}')", exception.Message, exception.StackTrace));
            var valueString = string.Join(" , ", exceptionList);

            var insertStatement = string.Format("INSERT INTO {0} (Message, StackTrace) Values {1}", tableName, valueString);
            var conn = Connect();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = insertStatement;
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            sda.Fill(dt);
        }
    }
}