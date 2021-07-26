using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("genPrintTemplates")]
    [ApiController]
    public class Gen_PrintTemplates : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID = 1374;
        private readonly string reportPath;

        public Gen_PrintTemplates(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            reportPath = conf.GetConnectionString("ReportPath");
        }

        [HttpGet("user")]
        public ActionResult GetUser()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandText = "select * from vw_UserRole_Disp where N_CompanyID=" + nCompanyID + " and Category <> 'Olivo'";
            Params.Add("@nCompanyID", nCompanyID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }
        [HttpGet("module")]
        public ActionResult GetModule(int n_UserCategoryId, int nLanguageID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);

            string sqlCommandText = "select N_CompanyID,N_LanguageID,N_ParentMenuID,X_UserCategory,N_MenuID,X_Text,X_ControlNo,N_UserCategoryID,X_Module from vw_PrintTemplateUserMenus where  N_LanguageID = " + nLanguageID + " and N_ParentMenuID = 0 and N_UserCategoryID=" + n_UserCategoryId + " and N_CompanyID=" + nCompanyID + "and X_ControlNo = '0' group by N_CompanyID,N_LanguageID,N_ParentMenuID,X_UserCategory,N_MenuID,X_Text,X_ControlNo,N_UserCategoryID,X_Module";
            Params.Add("@nCompanyID", nCompanyID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        [HttpGet("screen")]
        public ActionResult GetScreen(int nLanguageID, int n_UserCategoryID, int nModuleID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandText = "select N_UserCategoryID,N_MenuID,X_Text,N_ParentMenuID,x_RptName from vw_PrintSelectDispRpt_Web where  N_LanguageID = " + nLanguageID + " and B_Visible = 'true' and N_UserCategoryID=" + n_UserCategoryID + " and N_CompanyID=" + nCompanyID + "and N_ParentMenuID=" + nModuleID + " group by N_UserCategoryID,N_MenuID,X_Text,N_ParentMenuID,x_RptName";

            Params.Add("@nCompanyID", nCompanyID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }
        [HttpGet("fillData")]
        public ActionResult GetPrintTemp(int reportSelectingScreenID, int languageID, int n_TaxTypeID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string X_ReportFilePath = "";
                    string X_FolderName = "";


                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    Params.Add("@nCompanyID", nCompanyID);

                    //DataTable reportTable = new DataTable();
                    List<SortedList> templates = new List<SortedList>();

                    X_ReportFilePath += reportPath + languageID + @"\printing\";
                    object ObjFolderName = dLayer.ExecuteScalar("SELECT X_RptFolder FROM Gen_PrintTemplates WHERE N_CompanyID = '" + nCompanyID + "' AND N_FormID = " + reportSelectingScreenID, Params, connection);
                    if (ObjFolderName != null)
                        X_FolderName = ObjFolderName.ToString();
                    else
                        //X_FolderName = MYG.ReturnMultiLingualVal(ReportSelectingScreenID.ToString(), "X_ControlNo", "0").Replace(" ", "");
                        X_FolderName = "";
                    if (reportSelectingScreenID > 0)
                    {
                        X_ReportFilePath += X_FolderName + "\\";
                        if (n_TaxTypeID == 1)
                            X_ReportFilePath += @"vat\";
                        else if (n_TaxTypeID == 2)
                            X_ReportFilePath += @"gst\";
                        else if (n_TaxTypeID == 3)
                            X_ReportFilePath += @"gst+cess\";
                        else
                            X_ReportFilePath += @"none\";
                        if (!System.IO.Directory.Exists(X_ReportFilePath))
                            Directory.CreateDirectory(X_ReportFilePath);
                        int index;
                        foreach (var files in Directory.GetFiles(@X_ReportFilePath, "*.rpt"))
                        {
                            SortedList element = new SortedList();
                            index = Path.GetFileName(files).IndexOf(".");
                            if (index > 0)
                            {
                                element.Add("templateName", Path.GetFileName(files).Substring(0, index).ToString());
                            }
                            templates.Add(element);
                        }









                    }
                    return Ok(_api.Success(templates));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }

        }

        // [HttpPost("save")]
        // public ActionResult SaveData([FromBody] DataSet ds)
        // {
        //     try
        //     {
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //              DataTable MasterTable;
        //               MasterTable = ds.Tables["master"];
        //               var x_VoucherNo = MasterTable.Rows[0]["x_VoucherNo"].ToString();
                 
        //                 X_UserCategoryName = txtUserGroup.Text;
        //             object result = 0;
        //             try
        //             {
        //                 dba.SetTransaction();
        //                 dba.ExecuteNonQuery("SP_GeneralDefaults_ins " + myCompanyID._CompanyID + ",'" + ReportSelectingScreenID + "' ,'PrintTemplate',1,'" + txtSelectedRpt.Text + "','" + X_UserCategoryName + "'", "TEXT", new DataTable());
        //                 dba.ExecuteNonQuery("SP_GeneralDefaults_ins " + myCompanyID._CompanyID + ",'" + ReportSelectingScreenID + "' ,'PrintCopy'," + myFunctions.getIntVAL(txtCpyNos.Text) + ",''", "TEXT", new DataTable());
        //                 dba.ExecuteNonQuery("SP_GenPrintTemplatess_ins " + myCompanyID._CompanyID + "," + ReportSelectingScreenID + " ,'" + txtSelectedRpt.Text + "'," + N_UserCategoryId + "," + myFunctions.getIntVAL(txtCpyNos.Text) + "," + myFunctions.getIntVAL(chkClearScreenAfterSave.Checked) + "", "TEXT", new DataTable());
        //                 dba.Commit();
        //                 return true;
        //             }
        //             catch (Exception ex)
        //             {
        //                 dba.Rollback();
        //                 msg.msgError(ex.Message);
        //                 return false;
        //             }
        //         }
            }
}









