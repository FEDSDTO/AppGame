using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Configuration;

namespace AppGame
{
    public class DB_Connection
    {
        private string _ConnectionString = WebConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
        private SqlTransaction _Transaction;

        /// <summary>
        /// 取得資料表
        /// </summary>
        /// <param name="SQL">SQL字串</param>
        /// <param name="SQLParameter">參數</param>
        /// <returns>回傳DataTable</returns>
        public DataTable GetDataTable(string SQL, List<SqlParameter> SQLParameter)
        {
            DataTable _DataTable = new DataTable();
            SqlConnection _Connection = new SqlConnection(_ConnectionString);

            if (_Connection.State == ConnectionState.Closed)
                _Connection.Open();

            SqlCommand _Command = new SqlCommand(SQL, _Connection);

            try
            {
                if (SQLParameter != null && SQLParameter.Count > 0)
                {
                    foreach (SqlParameter para in SQLParameter)
                    {
                        if (para.Value == null)
                            para.Value = DBNull.Value;
                        else if (para.SqlDbType == SqlDbType.NVarChar)
                            para.Value = HttpContext.Current.Server.HtmlDecode(para.Value.ToString());

                        _Command.Parameters.Add(para);
                    }

                }

                _DataTable.Load(_Command.ExecuteReader());

                return _DataTable;
            }
            catch
            {
                throw;
            }
            finally
            {
                _Connection.Close();
                _Connection.Dispose();
                _Command.Parameters.Clear();

                if (SQLParameter != null)
                    SQLParameter.Clear();
            }
        }

        /// <summary>
        /// 取得資料表
        /// </summary>
        /// <param name="SQL">SQL字串</param>
        /// <returns>回傳DataTable</returns>
        public DataTable GetDataTable(string SQL)
        {
            return GetDataTable(SQL, null);
        }

        /// <summary>
        /// 取得資料表第一列第一欄的值
        /// </summary>
        /// <param name="SQL">SQL字串</param>
        /// <param name="SQLParameter">參數</param>
        /// <returns>回傳值</returns>
        public string GetValue(string SQL, List<SqlParameter> SQLParameter)
        {
            DataTable _dt = GetDataTable(SQL, SQLParameter);

            return _dt.Rows.Count == 0 ? string.Empty : _dt.Rows[0][0].ToString();
        }

        /// <summary>
        /// 取得資料表第一列第一欄的值
        /// </summary>
        /// <param name="SQL">SQL字串</param>
        /// <returns>回傳值</returns>
        public string GetValue(string SQL)
        {
            DataTable _dt = GetDataTable(SQL, null);

            return _dt.Rows.Count == 0 ? string.Empty : _dt.Rows[0][0].ToString();
        }

        /// <summary>
        /// 執行SQL
        /// </summary>
        /// <param name="SQL">SQL字串</param>
        /// <param name="SQLParameter">參數</param>
        public void SQLExecute(string SQL, List<SqlParameter> SQLParameter)
        {
            SqlConnection _Connection = new SqlConnection(_ConnectionString);

            if (_Connection.State == ConnectionState.Closed)
            {
                _Connection.Open();
                _Transaction = _Connection.BeginTransaction();
            }

            SqlCommand _Command = new SqlCommand(SQL, _Connection);
            _Command.Transaction = _Transaction;

            try
            {
                if (SQLParameter != null && SQLParameter.Count > 0)
                {
                    foreach (SqlParameter para in SQLParameter)
                    {
                        if (para.Value == null)
                            para.Value = DBNull.Value;
                        else if (para.SqlDbType == SqlDbType.NVarChar)
                            para.Value = HttpContext.Current.Server.HtmlDecode(para.Value.ToString());

                        _Command.Parameters.Add(para);
                    }
                }

                _Command.ExecuteNonQuery();
                _Transaction.Commit();
            }
            catch
            {
                _Transaction.Rollback();
                throw;
            }
            finally
            {
                _Connection.Close();
                _Connection.Dispose();
                _Command.Parameters.Clear();

                if (SQLParameter != null)
                    SQLParameter.Clear();
            }
        }
    }
}
