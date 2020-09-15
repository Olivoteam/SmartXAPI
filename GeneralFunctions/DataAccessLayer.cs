using System;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;

namespace SmartxAPI.GeneralFunctions
{
    public class DataAccessLayer : IDataAccessLayer
    {
        private readonly IMyFunctions myFunctions;
        public DataAccessLayer(IConfiguration conf,IMyFunctions myFun)
        {
            myFunctions=myFun;
        }

        public int ExecuteNonQuery(string sqlCommandText, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.Text;
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int ExecuteNonQuery(string sqlCommandText, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Transaction = transaction;
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public int ExecuteNonQuery(string sqlCommandText, SortedList paramList, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.Text;
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public int ExecuteNonQuery(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.Text;
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                sqlCommand.Transaction = transaction;
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        public object ExecuteScalar(string sqlCommandText, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandType = CommandType.Text;
                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public object ExecuteScalar(string sqlCommandText, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Transaction = transaction;
                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public object ExecuteScalar(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.Transaction = transaction;
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public object ExecuteScalar(string sqlCommandText, SortedList paramList, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandType = CommandType.Text;
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public object ExecuteScalarPro(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Transaction = transaction;


                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }

                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public object ExecuteScalarPro(string sqlCommandText, SortedList paramList, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.StoredProcedure;


                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }

                return sqlCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable ExecuteDataTable(string sqlCommandText, SqlConnection connection)
        {
            try
            {
                int recordsReturned;
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = new SqlCommand(sqlCommandText, connection);
                DataTable resultTable = new DataTable();
                recordsReturned = dataAdapter.Fill(resultTable);
                return resultTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable ExecuteDataTable(string sqlCommandText, SortedList paramList, SqlConnection con)
        {
            try
            {
                int recordsReturned;
                SqlCommand Command = new SqlCommand(sqlCommandText, con);
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        Command.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = Command;
                DataTable resTable = new DataTable();
                recordsReturned = dataAdapter.Fill(resTable);
                return resTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable ExecuteDataTable(string sqlCommandText, SortedList paramList, SqlConnection con, SqlTransaction transaction)
        {
            try
            {
                int recordsReturned;
                SqlCommand Command = new SqlCommand(sqlCommandText, con);
                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        Command.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                Command.Transaction = transaction;
                dataAdapter.SelectCommand = Command;
                DataTable resTable = new DataTable();
                recordsReturned = dataAdapter.Fill(resTable);
                return resTable;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        public int ExecuteNonQueryPro(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.StoredProcedure;

                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }


                sqlCommand.Transaction = transaction;
                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int ExecuteNonQueryPro(string sqlCommandText, SortedList paramList, SqlConnection connection)
        {
            try
            {
                SqlCommand sqlCommand = new SqlCommand(sqlCommandText, connection);
                sqlCommand.CommandTimeout = 0;
                sqlCommand.CommandType = CommandType.StoredProcedure;

                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        sqlCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }


                return sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable ExecuteDataTablePro(string sqlCommandText, SortedList paramList, SqlConnection connection)
        {
            try
            {
                int recordsReturned;
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = new SqlCommand(sqlCommandText, connection);
                dataAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                dataAdapter.SelectCommand.CommandText = sqlCommandText;


                if (paramList.Count > 0)
                {
                    ICollection Keys = paramList.Keys;
                    foreach (string key in Keys)
                    {
                        dataAdapter.SelectCommand.Parameters.Add(new SqlParameter(key.ToString(), paramList[key].ToString()));
                    }
                }



                DataTable resultTable = new DataTable();
                recordsReturned = dataAdapter.Fill(resultTable);
                return resultTable;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        public int SaveData(string TableName, string IDFieldName, int IDFieldValue, DataTable DataTable, SqlConnection connection, SqlTransaction transaction)
        {
            string FieldList = "";
            string FieldValues = "";
            int Result = 0;
            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                FieldList = FieldList + "," + DataTable.Columns[i].ColumnName.ToString();
            }
            FieldList = FieldList.Substring(1);

            for (int j = 0; j < DataTable.Rows.Count; j++)
            {
                for (int k = 0; k < DataTable.Columns.Count; k++)
                {
                    var values = DataTable.Rows[j][k].ToString();
                    values = values.Replace("|", " ");
                    FieldValues = FieldValues + "|" + values;

                }
                FieldValues = FieldValues.Substring(1);
                FieldValues = ValidateString(FieldValues);
                SortedList paramList = new SortedList();
                paramList.Add("X_TableName", TableName);
                paramList.Add("X_IDFieldName", IDFieldName);
                paramList.Add("N_IDFieldValue", IDFieldValue);
                paramList.Add("X_FieldList", FieldList);
                paramList.Add("X_FieldValue", FieldValues);
                Result = (int)ExecuteScalarPro("SAVE_DATA", paramList, connection, transaction);
                FieldValues = "";
            }

            return Result;
        }


        public int SaveData(string TableName, string IDFieldName , DataTable DataTable, SqlConnection connection, SqlTransaction transaction)
        {
            string FieldList = "";
            string FieldValues = "";
            int IDFieldValue = 0 ;
            int Result = 0;
            for (int i = 0; i < DataTable.Columns.Count; i++)
            {
                if(DataTable.Columns[i].ColumnName.ToString().ToLower()!=IDFieldName.ToLower())
                FieldList = FieldList + "," + DataTable.Columns[i].ColumnName.ToString();
            }
            FieldList = FieldList.Substring(1);

            for (int j = 0; j < DataTable.Rows.Count; j++)
            {
                for (int k = 0; k < DataTable.Columns.Count; k++)
                {
                    if(DataTable.Columns[k].ColumnName.ToString().ToLower()!=IDFieldName.ToLower()){
                    var values = DataTable.Rows[j][k].ToString();
                    values = values.Replace("|", " ");
                    FieldValues = FieldValues + "|" + values;
                    }
                }

                IDFieldValue = myFunctions.getIntVAL(DataTable.Rows[j][IDFieldName].ToString());

                FieldValues = FieldValues.Substring(1);
                FieldValues = ValidateString(FieldValues);
                SortedList paramList = new SortedList();
                paramList.Add("X_TableName", TableName);
                paramList.Add("X_IDFieldName", IDFieldName);
                paramList.Add("N_IDFieldValue", IDFieldValue);
                paramList.Add("X_FieldList", FieldList);
                paramList.Add("X_FieldValue", FieldValues);
                Result = (int)ExecuteScalarPro("SAVE_DATA", paramList, connection, transaction);
                FieldValues = "";
                if(Result<=0)return 0;
            }

            return Result;
        }

        public string ValidateString(string InputString)
        {
            string OutputString = InputString.Replace("'", "''");
            OutputString = OutputString.Replace("|", "'|'");
            OutputString = "'" + OutputString + "'";
            return OutputString;
        }

        public int DeleteData(string TableName, string IDFieldName, int IDFieldValue, string X_Critieria, SqlConnection connection, SqlTransaction transaction)
        {
            int Result = 0;
            SortedList paramList = new SortedList();
            paramList.Add("X_TableName", TableName);
            paramList.Add("X_IDFieldName", IDFieldName);
            paramList.Add("N_IDFieldValue", IDFieldValue);
            paramList.Add("X_Critieria", X_Critieria);
            Result = (int)ExecuteNonQueryPro("DELETE_DATA", paramList, connection, transaction);
            return Result;
        }

        public int DeleteData(string TableName, string IDFieldName, int IDFieldValue, string X_Critieria, SqlConnection connection)
        {
            int Result = 0;
            SortedList paramList = new SortedList();
            paramList.Add("X_TableName", TableName);
            paramList.Add("X_IDFieldName", IDFieldName);
            paramList.Add("N_IDFieldValue", IDFieldValue);
            paramList.Add("X_Critieria", X_Critieria);
            Result = (int)ExecuteNonQueryPro("DELETE_DATA", paramList, connection);
            return Result;
        }




        public string GetAutoNumber(string TableName, String Coloumn, SortedList Params, SqlConnection connection, SqlTransaction transaction)
        {
            string AutoNumber = "";
            string BranchId = "0";
            if (Params.Contains("N_BranchID")) { BranchId = Params["N_BranchID"].ToString(); }
            SortedList paramList = new SortedList(){
                {"N_CompanyID", Params["N_CompanyID"]},
                {"N_YearID", Params["N_YearID"]},
                {"N_FormID", Params["N_FormID"]},
                {"N_BranchID", BranchId}
                };
            while (true)
            {
                AutoNumber = (string)ExecuteScalarPro("SP_AutoNumberGenerate", paramList, connection, transaction);

                DataTable ResultTable = new DataTable();
                string sqlCommandText = "select 1 from " + TableName + " where " + Coloumn + " = @p1 and N_CompanyID=@p2";
                SortedList SqlParams = new SortedList(){
                    {"@p1",AutoNumber},
                    {"@p2",Params["N_CompanyID"]}};
                object obj = ExecuteScalar(sqlCommandText, SqlParams, connection, transaction);

                if (obj == null)
                    break;

            }
            return AutoNumber;
        }



    }

    public interface IDataAccessLayer
    {
        
        public DataTable ExecuteDataTable(string sqlCommandText, SortedList paramList, SqlConnection con, SqlTransaction transaction);
        public DataTable ExecuteDataTablePro(string sqlCommandText, SortedList paramList, SqlConnection connection);
        public DataTable ExecuteDataTable(string sqlCommandText, SortedList paramList, SqlConnection con);
        public DataTable ExecuteDataTable(string sqlCommandText, SqlConnection connection);

        public object ExecuteScalar(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction);
        public object ExecuteScalar(string sqlCommandText, SqlConnection connection, SqlTransaction transaction);
        public object ExecuteScalar(string sqlCommandText, SortedList paramList, SqlConnection connection);
        public object ExecuteScalar(string sqlCommandText, SqlConnection connection);

        public object ExecuteScalarPro(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction);
        public object ExecuteScalarPro(string sqlCommandText, SortedList paramList, SqlConnection connection);

        public int DeleteData(string TableName, string IDFieldName, int IDFieldValue, string X_Critieria, SqlConnection connection, SqlTransaction transaction);
        public int DeleteData(string TableName, string IDFieldName, int IDFieldValue, string X_Critieria, SqlConnection connection);

        public int ExecuteNonQueryPro(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction);
        public int ExecuteNonQueryPro(string sqlCommandText, SortedList paramList, SqlConnection connection);

        public int ExecuteNonQuery(string sqlCommandText, SortedList paramList, SqlConnection connection, SqlTransaction transaction);
        public int ExecuteNonQuery(string sqlCommandText, SqlConnection connection, SqlTransaction transaction);
        public int ExecuteNonQuery(string sqlCommandText, SortedList paramList, SqlConnection connection);
        public int ExecuteNonQuery(string sqlCommandText, SqlConnection connection);

        public string GetAutoNumber(string TableName, String Coloumn, SortedList Params, SqlConnection connection, SqlTransaction transaction);

        /* Deprecated Method Don't Use */
        [Obsolete("IApiFunctions.SaveData(string TableName, string IDFieldName, int IDFieldValue, DataTable DataTable, SqlConnection connection, SqlTransaction transaction) is deprecated \n please use IApiFunctions.SaveData.(string TableName, string IDFieldName , DataTable DataTable, SqlConnection connection, SqlTransaction transaction) instead. \n\n Deprecate note added by Ratheesh KS-\n\n")]
        public int SaveData(string TableName, string IDFieldName, int IDFieldValue, DataTable DataTable, SqlConnection connection, SqlTransaction transaction);
         /* End Of Deprecated Method */

        public int SaveData(string TableName, string IDFieldName , DataTable DataTable, SqlConnection connection, SqlTransaction transaction);
       

    }

}
