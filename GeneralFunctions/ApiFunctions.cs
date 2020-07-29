using System;
using System.Data;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;

namespace SmartxAPI.GeneralFunctions
{
    public class ApiFunctions : IApiFunctions
    {
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment env;
        public ApiFunctions(IMapper mapper,IWebHostEnvironment envn)
        {
            _mapper = mapper;
            env = envn;
        }

        public object Response(int Code, string ResMessage)
        {
            return (new { StatusCode = Code, Message = ResMessage, Data = "" });
        }

        public object Error(string message)
        {
            return (new { type = "error", Message = message, Data = "" });
        }
        public object Success(DataTable dataTable)
        {
            return (new { type = "success", Message = "null", Data = dataTable });
        }
        public object Success(DataSet dataSet)
        {
            return (new { type = "success", Message = "null", Data = dataSet });
        }
        public object Success(string message)
        {
            return (new { type = "success", Message = message, Data = "" });
        }
        public object Notice(string message)
        {
            return (new { type = "notice", Message = message, Data = "" });
        }
        public object Warning(string message)
        {
            return (new { type = "warning", Message = message, Data = "" });
        }
        public object ErrorResponse(Exception ex)
        {
            string Msg = "";
            string subString = ex.Message.Substring(8, ex.Message.Length - 8);

            switch (ex.Message.Substring(0, 8))
            {
                case "Column '":
                    Msg = ex.Message.Substring(7, subString.IndexOf("'") + 1) + " is required";
                    break;
                case "Error co":
                    Msg = ex.Message.Substring(0, 42);
                    break;
                default:
                    if (env.EnvironmentName=="Development")
                        Msg = ex.Message;
                        else
                        Msg = "Internal Server Error";
                    break;
            }


            return (new { type = "error", Message = Msg , Data = "" });


        }

        public object Error(Exception ex)
        {
            string Msg = "";
            string subString = ex.Message.Substring(8, ex.Message.Length - 8);

            switch (ex.Message.Substring(0, 8))
            {
                case "Column '":
                    Msg = ex.Message.Substring(7, subString.IndexOf("'") + 1) + " is required";
                    break;
                case "Error co":
                    Msg = ex.Message.Substring(0, 42);
                    break;
                default:
                    if (env.EnvironmentName=="Development")
                        Msg = ex.Message;
                        else
                        Msg = "Internal Server Error";
                    break;
            }


            return (new { type = "error", Message = Msg , Data = "" });


        }

        public DataTable Format(DataTable dt, string tableName)
        {
            foreach (DataColumn c in dt.Columns)
            {
                c.ColumnName = String.Join("", c.ColumnName.Split());
            }
            dt.TableName = tableName;
            return dt;
        }
        public DataTable Format(DataTable dt)
        {
            foreach (DataColumn c in dt.Columns)
            {
                c.ColumnName = String.Join("", c.ColumnName.Split());
            }
            return dt;
        }

    }
    public interface IApiFunctions
    {
        public object Response(int Code, string Response);
        public object ErrorResponse(Exception ex);
        public object Error(Exception ex);
        public DataTable Format(DataTable table, string tableName);
        public DataTable Format(DataTable dt);
        public object Error(string message);
        public object Success(DataTable dataTable);
        public object Success(DataSet dataSet);
        public object Success(string message);
        public object Notice(string message);
        public object Warning(string message);
    }
}