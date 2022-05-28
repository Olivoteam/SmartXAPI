using AutoMapper;
using SmartxAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Mail;
using System.Text;
using zatca.einvoicing;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Cache;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
namespace SmartxAPI.Controllers
{
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("report")]
    [ApiController]
    public class Reports : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly string reportApi;
        private readonly string TempFilesPath;
        private readonly string reportLocation;
        private readonly IWebHostEnvironment env;
        string RPTLocation = "";
        string ReportName = "";
        string FileName = "";
        string critiria = "";
        string TableName = "";
        string QRurl = "";
        string FormName = "";
        // private string X_CompanyField = "", X_YearField = "", X_BranchField="", X_UserField="",X_DefReportFile = "", X_GridPrevVal = "", X_SelectionFormula = "", X_ProcName = "", X_ProcParameter = "", X_ReprtTitle = "",X_Operator="";
        public Reports(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IWebHostEnvironment envn, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            reportApi = conf.GetConnectionString("ReportAPI");
            TempFilesPath = conf.GetConnectionString("TempFilesPath");
            reportLocation = conf.GetConnectionString("ReportLocation");
            env = envn;
        }
        [HttpGet("list")]
        public ActionResult GetReportList(int? nMenuId, int? nLangId)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "Select vwUserMenus.N_CompanyID, vwUserMenus.N_MenuID, vwUserMenus.X_MenuName, vwUserMenus.X_Caption, vwUserMenus.N_ParentMenuID, vwUserMenus.N_Order, vwUserMenus.N_HasChild ,CAST(MAX(1 * vwUserMenus.B_Visible) AS BIT) as B_Visible, CAST(MAX(1 * vwUserMenus.B_Edit) AS BIT) as B_Edit, CAST(MAX(1 * vwUserMenus.B_Delete) AS BIT) as B_Delete,CAST(MAX(1 * vwUserMenus.B_Save) AS BIT) as B_Save, CAST(MAX(1 * vwUserMenus.B_View) AS BIT) as B_View, vwUserMenus.X_ShortcutKey, vwUserMenus.X_CaptionAr, vwUserMenus.X_FormNameWithTag, vwUserMenus.N_IsStartup, vwUserMenus.B_Show, vwUserMenus.B_ShowOnline, vwUserMenus.X_RouteName, vwUserMenus.B_WShow,Lan_MultiLingual.X_Text from vwUserMenus Inner Join Sec_UserPrevileges On vwUserMenus.N_MenuID=Sec_UserPrevileges.N_MenuID And Sec_UserPrevileges.N_UserCategoryID = vwUserMenus.N_UserCategoryID And  Sec_UserPrevileges.N_UserCategoryID in ( " + myFunctions.GetUserCategoryList(User) + " ) and vwUserMenus.B_Show=1 inner join Lan_MultiLingual on vwUserMenus.N_MenuID=Lan_MultiLingual.N_FormID and Lan_MultiLingual.N_LanguageId=@nLangId and X_ControlNo ='0' Where LOWER(vwUserMenus.X_Caption) <>'seperator' and vwUserMenus.N_ParentMenuID=@nMenuId group by vwUserMenus.N_CompanyID, vwUserMenus.N_MenuID, vwUserMenus.X_MenuName, vwUserMenus.X_Caption, vwUserMenus.N_ParentMenuID, vwUserMenus.N_Order, vwUserMenus.N_HasChild, vwUserMenus.X_ShortcutKey, vwUserMenus.X_CaptionAr, vwUserMenus.X_FormNameWithTag, vwUserMenus.N_IsStartup, vwUserMenus.B_Show, vwUserMenus.B_ShowOnline, vwUserMenus.X_RouteName, vwUserMenus.B_WShow,Lan_MultiLingual.X_Text Order By vwUserMenus.N_Order";
            Params.Add("@nMenuId", nMenuId == 0 ? 318 : nMenuId);
            Params.Add("@nLangId", nLangId);
            Params.Add("@nUserCatID", myFunctions.GetUserCategoryList(User));

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    DataTable dt1 = new DataTable();

                    string sqlCommandText1 = "select n_CompID,n_LanguageId,n_MenuID,x_CompType,x_FieldList,x_FieldType,x_Text,X_FieldtoReturn,X_DefVal1,X_DefVal2,X_Operator,N_LinkCompID,X_LinkField,B_Range from vw_WebReportMenus where N_LanguageId=@nLangId group by n_CompID,n_LanguageId,n_MenuID,x_CompType,x_FieldList,x_FieldType,x_Text,X_FieldtoReturn,X_DefVal1,X_DefVal2,X_Operator,N_ListOrder,N_LinkCompID,X_LinkField,B_Range order by N_ListOrder";
                    dt1 = dLayer.ExecuteDataTable(sqlCommandText1, Params, connection);

                    dt.Columns.Add("ChildMenus", typeof(DataTable));
                    dt.Columns.Add("Filter", typeof(DataTable));
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataTable ChildMenus = new DataTable();
                        DataTable Filter = new DataTable();
                        string N_MenuID = dt.Rows[i]["N_MenuID"].ToString();
                        try
                        {
                            DataRow[] dr = dt1.Select("N_MenuID = " + N_MenuID + " and x_FieldType='RadioButton'");
                            DataRow[] dr1 = dt1.Select("N_MenuID = " + N_MenuID + " and x_FieldType<>'RadioButton'");
                            if (dr != null)
                            {
                                ChildMenus = dr.CopyToDataTable();
                                dt.Rows[i]["ChildMenus"] = ChildMenus;
                            }
                            if (dr1 != null)
                            {
                                Filter = dr1.CopyToDataTable();
                                dt.Rows[i]["Filter"] = Filter;
                            }
                        }
                        catch
                        {

                        }
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else { return Ok(_api.Success(dt)); }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        [HttpGet("dynamiclist")]
        public ActionResult GetDynamicList(int nMenuId, int nCompId, int nLangId, string cval, string bval, string fval, string qry)
        {
            DataTable dt = new DataTable();
            DataTable outTable = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select TOP 1 X_TableName,X_FieldList,X_Criteria from vw_WebReportMenus where N_MenuID=@p1 and N_LanguageId=@p2 and N_CompID=@p3 and X_CompType=@p4";
            //             string sqlCommandText = "Select Sec_ReportsComponents.*, Lan_Multilingual.X_Text from Sec_ReportsComponents Left Outer Join Lan_Multilingual on " +
            //  " Sec_ReportsComponents.N_MenuID = Lan_Multilingual.N_FormID and  Sec_ReportsComponents.X_LangControlNo = Lan_Multilingual.X_ControlNo and  " +
            // " Lan_Multilingual.N_LanguageID=@p2  Where Sec_ReportsComponents.N_MenuID=@p1 AND Sec_ReportsComponents.B_Active =1 and N_CompID=@p3 and X_CompType=@p4 ";

            Params.Add("@p1", nMenuId);
            Params.Add("@p2", nLangId);
            Params.Add("@p3", nCompId);
            Params.Add("@p4", "ListControl");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    if (dt.Rows.Count > 0)
                    {
                        DataRow QueryString = dt.Rows[0];
                        SortedList ListSqlParams = new SortedList();
                        string fields = QueryString["X_FieldList"].ToString();
                        string table = QueryString["X_TableName"].ToString();
                        // if(table=="Acc_VoucherDetails"){
                        //     string a=table;
                        // }
                        string Criteria = QueryString["X_Criteria"].ToString();
                        if (Criteria != "")
                            Criteria = " Where " + QueryString["X_Criteria"].ToString().Replace("'CVal'", "@CVal ").Replace("'BVal'", "@BVal ").Replace("'FVal'", "@FVal ");

                        if (qry != null)
                        {
                            if (Criteria != "")
                            {
                                Criteria = Criteria + " and " + qry;
                            }
                            else
                            {
                                Criteria = " Where " + qry;
                            }
                        }
                        ListSqlParams.Add("@BVal", bval);
                        ListSqlParams.Add("@CVal", cval);
                        ListSqlParams.Add("@FVal", fval);
                        string ListSql = "select " + fields + " from " + table + " " + Criteria + " group by " + fields + " order by " + fields;

                        outTable = dLayer.ExecuteDataTable(ListSql, ListSqlParams, connection);
                    }


                }
                if (outTable.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    //Dictionary<string,Dictionary<string,DataTable>> Menu = new Dictionary<string,Dictionary<string,DataTable>>();
                    outTable = _api.Format(outTable);
                    //Dictionary<string,DataTable> Component = new Dictionary<string,DataTable>();
                    SortedList Component = new SortedList();
                    Component.Add(nCompId.ToString(), outTable);
                    //Menu.Add(nMenuId.ToString(),Component);

                    return Ok(_api.Success(Component));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        [HttpGet("getreport")]
        public IActionResult GetModuleReports(string reportName, string critiria)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                };
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();

                    var client = new HttpClient(handler);
                    var random = RandomString();
                    var dbName = connection.Database;
                    //string URL = reportApi + "api/report?reportName=" + reportName + "&critiria=" + critiria + "&path=" + this.TempFilesPath + "&reportLocation=" + reportLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=&x_Reporttitle=&extention=pdf";
                    string URL = reportApi + "api/report?reportName=" + reportName + "&critiria=" + critiria + "&path=" + this.TempFilesPath + "&reportLocation=" + reportLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=&x_Reporttitle=&extention=pdf&N_FormID=0&QRUrl=&N_PkeyID=0&partyName=&docNumber=&formName=";
                    var path = client.GetAsync(URL);
                    path.Wait();
                    return Ok(_api.Success(new SortedList() { { "FileName", reportName.Trim() + random + ".pdf" } }));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }
        private bool LoadReportDetails(int nFnYearID, int nFormID, int nPkeyID, int nPreview, string xRptname)
        {
            SortedList QueryParams = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            QueryParams.Add("@nCompanyId", nCompanyId);
            QueryParams.Add("@nFnYearID", nFnYearID);
            QueryParams.Add("@nFormID", nFormID);
            RPTLocation = "";
            critiria = "";
            TableName = "";
            ReportName = "";
            //int N_UserCategoryID=myFunctions.GetUserCategory(User);
            bool b_Custom = false;
            string xUserCategoryList = myFunctions.GetUserCategoryList(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();
                    object ObjTaxType = dLayer.ExecuteScalar("SELECT Acc_TaxType.X_RepPathCaption FROM Acc_TaxType LEFT OUTER JOIN Acc_FnYear ON Acc_TaxType.N_TypeID = Acc_FnYear.N_TaxType where Acc_FnYear.N_CompanyID=@nCompanyId and Acc_FnYear.N_FnYearID=@nFnYearID", QueryParams, connection, transaction);
                    if (ObjTaxType == null)
                        ObjTaxType = "";
                    if (ObjTaxType.ToString() == "")
                        ObjTaxType = "none";
                    string TaxType = ObjTaxType.ToString();

                    object ObjPath = dLayer.ExecuteScalar("SELECT X_RptFolder FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID", QueryParams, connection, transaction);
                    if (ObjPath != null)
                    {
                        if (ObjPath.ToString() != "")
                            RPTLocation = reportLocation + "printing/" + ObjPath + "/" + TaxType + "/";
                        else
                            RPTLocation = reportLocation + "printing/";
                    }

                    object Templatecritiria = dLayer.ExecuteScalar("SELECT X_PkeyField FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID", QueryParams, connection, transaction);
                    TableName = Templatecritiria.ToString().Substring(0, Templatecritiria.ToString().IndexOf(".")).Trim();
                    object Custom = dLayer.ExecuteScalar("SELECT isnull(b_Custom,0) FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID and N_UsercategoryID in (" + xUserCategoryList + ")", QueryParams, connection, transaction);
                    int N_Custom = myFunctions.getIntVAL(Custom.ToString());
                    object ObjReportName = dLayer.ExecuteScalar("SELECT X_RptName FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID and N_UsercategoryID in (" + xUserCategoryList + ")", QueryParams, connection, transaction);
                    object objFormName = dLayer.ExecuteScalar("SELECT X_FormName FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID and N_UsercategoryID in (" + xUserCategoryList + ")", QueryParams, connection, transaction);
                    FormName = objFormName.ToString();
                    // object ObjFileName = dLayer.ExecuteScalar("SELECT X_FileName FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID and N_UsercategoryID in (" + xUserCategoryList + ")", QueryParams, connection, transaction);
                    // FileName=ObjFileName.ToString();
                    if (N_Custom == 1)
                    {

                        RPTLocation = RPTLocation + "custom/";
                        ObjReportName = (ObjReportName.ToString().Remove(ObjReportName.ToString().Length - 4)).Trim();
                        ObjReportName = ObjReportName + "_" + myFunctions.GetClientID(User) + "_" + myFunctions.GetCompanyID(User) + "_" + myFunctions.GetCompanyName(User) + ".rpt";
                    }
                    ReportName = ObjReportName.ToString();
                    ReportName = ReportName.Remove(ReportName.Length - 4);

                    if (nPreview == 1)
                    {
                        ReportName = xRptname;
                        if (ReportName.Contains(".rpt"))
                        {
                            ReportName = ReportName.Remove(ReportName.Length - 4);
                        }
                        object pkeyID = dLayer.ExecuteScalar("SELECT max(" + Templatecritiria + ") FROM " + TableName + " WHERE N_CompanyID =@nCompanyId", QueryParams, connection, transaction);
                        if (pkeyID != null)
                            nPkeyID = myFunctions.getIntVAL(pkeyID.ToString());
                    }

                    critiria = "{" + Templatecritiria + "}=" + nPkeyID;
                    object Othercritiria = dLayer.ExecuteScalar("SELECT X_Criteria FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=@nFormID", QueryParams, connection, transaction);

                    if (Othercritiria != null)
                    {
                        if (Othercritiria.ToString() != "")
                            critiria = critiria + " and " + Othercritiria.ToString();

                    }


                    if (nFormID == 64 || nFormID == 894 || nFormID == 372)
                    {
                        //QR Code Generate For Invoice

                        object Total = dLayer.ExecuteScalar("select n_BillAmt+N_taxamtF from inv_sales where N_CompanyID=@nCompanyId and N_SalesID=" + nPkeyID, QueryParams, connection, transaction);
                        object TaxAmount = dLayer.ExecuteScalar("select N_taxamtF from inv_sales where N_CompanyID=@nCompanyId and N_SalesID=" + nPkeyID, QueryParams, connection, transaction);
                        object VatNumber = dLayer.ExecuteScalar("select x_taxregistrationNo from acc_company where N_CompanyID=@nCompanyId", QueryParams, connection, transaction);
                        object SalesDate = dLayer.ExecuteScalar("select D_SalesDate from inv_sales where N_CompanyID=@nCompanyId and N_SalesID=" + nPkeyID, QueryParams, connection, transaction);
                        DateTime dt = DateTime.Parse(SalesDate.ToString());
                        string Amount = Convert.ToDecimal(Total).ToString("0.00");
                        string VatAmount = Convert.ToDecimal(TaxAmount.ToString()).ToString("0.00");
                        string Company = myFunctions.GetCompanyName(User);
                        TLVCls tlv = new TLVCls(Company, VatNumber.ToString(), dt, Convert.ToDouble(Amount), Convert.ToDouble(VatAmount));
                        var plainTextBytes = tlv.ToBase64();

                        QRurl = string.Format(plainTextBytes);
                        // var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chs={1}x{2}&chl={0}", plainTextBytes.Replace("&", "%26"), "500", "500");
                        // WebResponse response = default(WebResponse);
                        // Stream remoteStream = default(Stream);
                        // StreamReader readStream = default(StreamReader);
                        // WebRequest request = WebRequest.Create(url);
                        // response = request.GetResponse();
                        // remoteStream = response.GetResponseStream();
                        // readStream = new StreamReader(remoteStream);
                        // string path = "C://OLIVOSERVER2020/QR/";
                        // DirectoryInfo info = new DirectoryInfo(path);
                        // if (!info.Exists)
                        // {
                        //     info.Create();
                        // }
                        // string pathfile = Path.Combine(path, "QR.png");
                        // using (FileStream outputFileStream = new FileStream(pathfile, FileMode.Create))
                        // {
                        //     remoteStream.CopyTo(outputFileStream);
                        // }
                        //QR End Here

                        // bool SaveDraft = false;
                        // object ObjSaveDraft = dLayer.ExecuteScalar("select b_issavedraft from inv_sales WHERE N_CompanyID =@nCompanyId and N_SalesID=" + nPkeyID, QueryParams, connection, transaction);
                        // if (ObjSaveDraft != null)
                        // {
                        //     SaveDraft = myFunctions.getBoolVAL(ObjSaveDraft.ToString());
                        //     if (SaveDraft == true)
                        //     {
                        //         ObjReportName = dLayer.ExecuteScalar("SELECT X_RptName FROM Gen_PrintTemplates WHERE N_CompanyID =@nCompanyId and N_FormID=644", QueryParams, connection, transaction);
                        //         ReportName = ObjReportName.ToString();
                        //         ReportName = ReportName.Remove(ReportName.Length - 4);
                        //     }
                        // }

                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                return false;

            }

        }
        private string StringToHex(string hexstring)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char t in hexstring)
            {
                //Note: X for upper, x for lower case letters
                sb.Append(Convert.ToInt32(t).ToString("x"));
            }
            return sb.ToString();
        }

        [HttpGet("getscreenprint")]
        public IActionResult GetModulePrint(int nFormID, int nPkeyID, int nFnYearID, int nPreview, string xrptname, string docNumber, string partyName)
        {
            SortedList QueryParams = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();
                    QueryParams.Add("@p1", nCompanyId);
                    QueryParams.Add("@p2", nFormID);
                    QueryParams.Add("@p3", nFnYearID);
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                    };

                    if (LoadReportDetails(nFnYearID, nFormID, nPkeyID, nPreview, xrptname))
                    {

                        var client = new HttpClient(handler);
                        var dbName = connection.Database;
                        var random = RandomString();
                        if (TableName != "" && critiria != "")
                        {
                            critiria = critiria + " and {" + TableName + ".N_CompanyID}=" + myFunctions.GetCompanyID(User);
                        }
                        ReportName = ReportName.Replace("&", "");

                        if (nPreview == 2)
                        {
                            string fileToCopy = RPTLocation + ReportName + ".rpt";
                            string destinationFile = this.TempFilesPath + ReportName + ".rpt";
                            string ZipLocation = this.TempFilesPath + ReportName + ".rpt.zip";
                            if (CopyFiles(fileToCopy, destinationFile, ReportName + ".rpt"))
                                return Ok(_api.Success(new SortedList() { { "FileName", ReportName.Trim() + ".rpt.zip" } }));

                        }

                        if (partyName == "" || partyName == null)
                            partyName = "customer";
                        if (docNumber == "" || docNumber == null)
                            docNumber = "DocNo";
                        partyName = partyName.Replace("&", "");
                        partyName = partyName.ToString().Substring(0, Math.Min(12, partyName.ToString().Length));
                        if (docNumber == null)
                            docNumber = "";
                        docNumber = Regex.Replace(docNumber, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
                        if (!Regex.IsMatch(partyName, @"\p{IsArabic}"))
                            partyName = Regex.Replace(partyName, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);

                        if (docNumber.Contains("/"))
                            docNumber = docNumber.ToString().Substring(0, Math.Min(3, docNumber.ToString().Length));

                        DateTime currentTime;
                        string x_comments = "";
                        //Local Time Checking
                        object TimezoneID = dLayer.ExecuteScalar("select isnull(n_timezoneid,82) from acc_company where N_CompanyID= " + nCompanyId, connection, transaction);
                        object Timezone = dLayer.ExecuteScalar("select X_ZoneName from Gen_TimeZone where n_timezoneid=" + TimezoneID, connection, transaction);
                        if (Timezone != null && Timezone.ToString() != "")
                        {
                            currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(Timezone.ToString()));
                            x_comments = currentTime.ToString();
                        }
                        if (nFormID == 1406)
                        {
                            object ASNdoc = dLayer.ExecuteScalar("select x_asndocno from vw_Wh_AsnMaster_Disp where n_companyid=" + nCompanyId + " and n_asnid=" + nPkeyID, connection, transaction);
                            CreateBarcode(ASNdoc.ToString());
                            DataTable AsnDetails = dLayer.ExecuteDataTable("select X_Barcode from vw_Wh_Asndetails_disp where n_companyid=" + nCompanyId + " and n_asnid=" + nPkeyID, QueryParams, connection, transaction);
                            foreach (DataRow var in AsnDetails.Rows)
                            {
                                CreateBarcode(var["X_Barcode"].ToString());
                            }
                        }
                        if (nFormID == 1411)
                        {
                            object PICKList = dLayer.ExecuteScalar("select X_PickListCode from vw_WhPickListMaster where n_companyid=" + nCompanyId + " and N_PickListID=" + nPkeyID, connection, transaction);
                            CreateBarcode(PICKList.ToString());

                        }

                        string URL = reportApi + "api/report?reportName=" + ReportName + "&critiria=" + critiria + "&path=" + this.TempFilesPath + "&reportLocation=" + RPTLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=" + x_comments + "&x_Reporttitle=&extention=pdf&N_FormID=" + nFormID + "&QRUrl=" + QRurl + "&N_PkeyID=" + nPkeyID + "&partyName=" + partyName + "&docNumber=" + docNumber + "&formName=" + FormName;
                        var path = client.GetAsync(URL);
                        if (nFormID == 80)
                        {
                            SortedList Params = new SortedList();
                            object N_OpportunityID = dLayer.ExecuteScalar("select N_OpportunityID from inv_salesquotation where N_CompanyID =" + myFunctions.GetCompanyID(User) + " and N_QuotationID=" + nPkeyID, Params, connection, transaction);
                            if (N_OpportunityID != null)
                            {
                                if (myFunctions.getIntVAL(N_OpportunityID.ToString()) > 0)
                                {

                                    object Mailsend = dLayer.ExecuteScalar("select B_MailSend from inv_salesquotation where N_CompanyID =" + myFunctions.GetCompanyID(User) + " and N_QuotationID=" + nPkeyID, Params, connection, transaction);
                                    object Mail = dLayer.ExecuteScalar("select X_Email from vw_crmopportunity where N_CompanyID =" + myFunctions.GetCompanyID(User) + " and N_OpportunityID=" + N_OpportunityID, Params, connection, transaction);
                                    if (Mailsend.ToString() == "")
                                    {
                                        if (sendmail(this.TempFilesPath + ReportName + random + ".pdf", Mail.ToString()))
                                        {
                                            dLayer.ExecuteNonQuery("update inv_salesquotation set B_MailSend=1 where N_CompanyID=" + nCompanyId + " and N_QuotationID=" + nPkeyID, Params, connection, transaction);
                                            transaction.Commit();
                                        }
                                    }

                                }
                            }

                        }
                        // ReportName = FormName + "_" + docNumber + "_" + partyName.Trim()+".pdf";
                        ReportName = FormName + "_" + docNumber + "_" + partyName.Trim() + "_" + random + ".pdf";
                        path.Wait();
                        if (env.EnvironmentName != "Development" && !System.IO.File.Exists(this.TempFilesPath + ReportName))
                            return Ok(_api.Error(User, "Report Generation Failed"));
                        else
                            return Ok(_api.Success(new SortedList() { { "FileName", ReportName } }));
                    }
                    else
                    {
                        return Ok(_api.Error(User, "Report Generation Failed"));
                    }
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }

        }
        public bool CreateBarcode(string Data)
        {
            if (Data != "")
            {
                Zen.Barcode.Code128BarcodeDraw barcode = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;
                Image img = barcode.Draw(Data, 50);
                img.Save("C://OLIVOSERVER2020/Barcode/" + Data + ".png", ImageFormat.Png);
            }
            return true;
        }

        public bool sendmail(string url, string mail)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SortedList Params = new SortedList();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int companyid = myFunctions.GetCompanyID(User);
                    string Toemail = "";

                    Toemail = mail;
                    object companyemail = "";
                    object companypassword = "";

                    companyemail = dLayer.ExecuteScalar("select X_Value from Gen_Settings where X_Group='210' and X_Description='EmailAddress' and N_CompanyID=" + companyid, Params, connection, transaction);
                    companypassword = dLayer.ExecuteScalar("select X_Value from Gen_Settings where X_Group='210' and X_Description='EmailPassword' and N_CompanyID=" + companyid, Params, connection, transaction);

                    string Subject = "";
                    if (Toemail.ToString() != "")
                    {
                        if (companyemail.ToString() != "")
                        {
                            object body = null;
                            string MailBody;
                            body = "Hi,<br> please find the attached quotation for your review";
                            if (body != null)
                            {
                                body = body.ToString();
                            }
                            else
                                body = "";

                            string Sender = companyemail.ToString();
                            Subject = "Quotation";
                            MailBody = body.ToString();


                            SmtpClient client = new SmtpClient
                            {
                                Host = "smtp.gmail.com",
                                Port = 587,
                                EnableSsl = true,
                                DeliveryMethod = SmtpDeliveryMethod.Network,
                                Credentials = new System.Net.NetworkCredential(companyemail.ToString(), companypassword.ToString()),
                                Timeout = 10000,
                            };

                            MailMessage message = new MailMessage();
                            message.To.Add(Toemail.ToString()); // Add Receiver mail Address  
                            message.From = new MailAddress(Sender);
                            message.Subject = Subject;
                            message.Body = MailBody;
                            message.From = new MailAddress("sanjay.kv@olivotech.com", "Al Raza Photography");
                            message.IsBodyHtml = true; //HTML email  
                            message.Attachments.Add(new Attachment(url));
                            client.Send(message);

                        }
                    }


                }
                return true;
            }

            catch (Exception ie)
            {
                return false;
            }
        }

        [HttpGet("shiftSchedulePrint")]
        public IActionResult GetshiftSchedulePrint(int nFormID, DateTime dPeriodFrom, DateTime dPeriodTo, int nFnYearID, string xCriteria, int nDepartmentID)
        {
            string RPTLocation = reportLocation;
            string ReportName = "";
            string critiria = "";
            var random = RandomString();
            SortedList QueryParams = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();
                    QueryParams.Add("@p1", nCompanyId);
                    QueryParams.Add("@p3", nFnYearID);


                    string TableName = "";
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                    };

                    object ObjPath = dLayer.ExecuteScalar("SELECT Acc_TaxType.X_RepPathCaption FROM Acc_TaxType LEFT OUTER JOIN Acc_FnYear ON Acc_TaxType.N_TypeID = Acc_FnYear.N_TaxType where Acc_FnYear.N_CompanyID=@p1 and Acc_FnYear.N_FnYearID=@p3", QueryParams, connection, transaction);
                    string TaxType = ObjPath + "/";


                    if (nFormID == 1260)
                    {
                        if (xCriteria == "department")
                        {

                            critiria = "{vw_Pay_Empshiftdetails.D_Date}>=Date('" + dPeriodFrom.Year + "," + dPeriodFrom.Month + "," + dPeriodFrom.Day + "') and {vw_Pay_Empshiftdetails.D_Date}<=Date('" + dPeriodTo.Year + "," + dPeriodTo.Month + "," + dPeriodTo.Day + "') and  {vw_Pay_Empshiftdetails.N_DepartmentID}=" + nDepartmentID + "";
                            TableName = "vw_Pay_Empshiftdetails";


                            RPTLocation = reportLocation + "printing/";
                            ReportName = "Employee_ShiftSchedule";


                        }
                        else if (xCriteria == "all")
                        {
                            critiria = "{vw_Pay_Empshiftdetails.D_Date}>=Date('" + dPeriodFrom.Year + "," + dPeriodFrom.Month + "," + dPeriodFrom.Day + "') and {vw_Pay_Empshiftdetails.D_Date}<=Date('" + dPeriodTo.Year + "," + dPeriodTo.Month + "," + dPeriodTo.Day + "')";
                            TableName = "vw_Pay_Empshiftdetails";


                            RPTLocation = reportLocation + "printing/";
                            ReportName = "Employee_ShiftSchedule";



                        }
                        var client = new HttpClient(handler);
                        var dbName = connection.Database;

                        if (TableName != "" && critiria != "")
                        {
                            critiria = critiria + " and {" + TableName + ".N_CompanyID}=" + myFunctions.GetCompanyID(User);
                        }
                        //string URL = reportApi + "api/report?reportName=" + ReportName + "&critiria=" + critiria + "&path=" + this.TempFilesPath + "&reportLocation=" + RPTLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=&x_Reporttitle=&extention=pdf";
                        string URL = reportApi + "api/report?reportName=" + ReportName + "&critiria=" + critiria + "&path=" + this.TempFilesPath + "&reportLocation=" + RPTLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=&x_Reporttitle=&extention=pdf&N_FormID=0&QRUrl=&N_PkeyID=0&partyName=&docNumber=&formName=";
                        var path = client.GetAsync(URL);
                        path.Wait();

                    }
                    return Ok(_api.Success(new SortedList() { { "FileName", ReportName.Trim() + random + ".pdf" } }));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }


        }
        public bool CopyFiles(string fileToCopy, string destinationFile, string reportName)
        {
            try
            {
                string ZipLocation = destinationFile + ".zip";
                if (System.IO.File.Exists(destinationFile))
                {
                    System.IO.File.Delete(destinationFile);
                }
                if (System.IO.File.Exists(ZipLocation))
                {
                    System.IO.File.Delete(ZipLocation);
                }
                System.IO.File.Copy(fileToCopy, destinationFile);
                using (FileStream fs = new FileStream(ZipLocation, FileMode.Create))
                using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    arch.CreateEntryFromFile(destinationFile, reportName);
                }
                if (System.IO.File.Exists(destinationFile))
                {
                    System.IO.File.Delete(destinationFile);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        public static DateTime GetFastestNISTDate()
        {
            var result = DateTime.MinValue;
            // Initialize the list of NIST time servers
            // http://tf.nist.gov/tf-cgi/servers.cgi
            string[] servers = new string[] {
"nist1-ny.ustiming.org",
"nist1-nj.ustiming.org",
"nist1-pa.ustiming.org",
"time-a.nist.gov",
"time-b.nist.gov",
"nist1.aol-va.symmetricom.com",
"nist1.columbiacountyga.gov",
"nist1-chi.ustiming.org",
"nist.expertsmi.com",
"nist.netservicesgroup.com"
};

            // Try 5 servers in random order to spread the load
            Random rnd = new Random();
            foreach (string server in servers.OrderBy(s => rnd.NextDouble()).Take(5))
            {
                try
                {
                    // Connect to the server (at port 13) and get the response
                    string serverResponse = string.Empty;
                    using (var reader = new StreamReader(new System.Net.Sockets.TcpClient(server, 13).GetStream()))
                    {
                        serverResponse = reader.ReadToEnd();
                    }

                    // If a response was received
                    if (!string.IsNullOrEmpty(serverResponse))
                    {
                        // Split the response string ("55596 11-02-14 13:54:11 00 0 0 478.1 UTC(NIST) *")
                        string[] tokens = serverResponse.Split(' ');

                        // Check the number of tokens
                        if (tokens.Length >= 6)
                        {
                            // Check the health status
                            string health = tokens[5];
                            if (health == "0")
                            {
                                // Get date and time parts from the server response
                                string[] dateParts = tokens[1].Split('-');
                                string[] timeParts = tokens[2].Split(':');

                                // Create a DateTime instance
                                DateTime utcDateTime = new DateTime(
                                    Convert.ToInt32(dateParts[0]) + 2000,
                                    Convert.ToInt32(dateParts[1]), Convert.ToInt32(dateParts[2]),
                                    Convert.ToInt32(timeParts[0]), Convert.ToInt32(timeParts[1]),
                                    Convert.ToInt32(timeParts[2]));

                                // Convert received (UTC) DateTime value to the local timezone
                                result = utcDateTime.ToLocalTime();

                                return result;
                                // Response successfully received; exit the loop

                            }
                        }

                    }

                }
                catch
                {
                    // Ignore exception and try the next server
                }
            }
            return result;
        }

        [HttpPost("getModuleReport")]
        public IActionResult GetModuleReports([FromBody] DataSet ds)
        {
            DataTable MasterTable;
            DataTable DetailTable;

            MasterTable = ds.Tables["master"];
            DetailTable = ds.Tables["details"];
            int nCompanyID = myFunctions.GetCompanyID(User);
            string x_comments = "";
            string x_Reporttitle = "";
            string X_TextforAll = "=all";
            int nUserID = myFunctions.GetUserID(User);
            var random = RandomString();
            DateTime currentTime;

            try
            {
                String Criteria = "";
                String reportName = "";
                String CompanyData = "";
                String YearData = "";
                String BranchData = "";
                String FieldName = "";
                String UserData = "";

                var dbName = "";
                string Extention = "";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //int MenuID = myFunctions.getIntVAL(MasterTable.Rows[0]["moduleID"].ToString());
                    int MainMenuID = myFunctions.getIntVAL(MasterTable.Rows[0]["moduleID"].ToString());

                    int MenuID = myFunctions.getIntVAL(MasterTable.Rows[0]["reportCategoryID"].ToString());
                    int ReportID = myFunctions.getIntVAL(MasterTable.Rows[0]["reportID"].ToString());
                    int FnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["nFnYearID"].ToString());
                    int BranchID = myFunctions.getIntVAL(MasterTable.Rows[0]["nBranchID"].ToString());
                    int ActionID = myFunctions.getIntVAL(MasterTable.Rows[0]["action"].ToString());
                    int SalesmanID = 0;
                    string procParam = "";
                    string xProCode = "";
                    Extention = MasterTable.Rows[0]["extention"].ToString();

                    SortedList Params1 = new SortedList();
                    Params1.Add("@nMenuID", MenuID);
                    Params1.Add("@xType", "RadioButton");
                    Params1.Add("@nCompID", ReportID);


                    reportName = dLayer.ExecuteScalar("select X_rptFile from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID and B_Active=1", Params1, connection).ToString();

                    if (ActionID == 2)
                    {
                        string fileToCopy = reportLocation + reportName;
                        string destinationFile = this.TempFilesPath + reportName;
                        string ZipLocation = this.TempFilesPath + reportName + ".zip";
                        if (CopyFiles(fileToCopy, destinationFile, reportName))
                            return Ok(_api.Success(new SortedList() { { "FileName", reportName.Trim() + ".zip" } }));
                        // if (System.IO.File.Exists(destinationFile))
                        // {
                        //     System.IO.File.Delete(destinationFile);
                        // }
                        // if (System.IO.File.Exists(ZipLocation))
                        // {
                        //     System.IO.File.Delete(ZipLocation);
                        // }
                        // System.IO.File.Copy(fileToCopy, destinationFile);
                        // using (FileStream fs = new FileStream(ZipLocation, FileMode.Create))
                        // using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                        // {
                        //     arch.CreateEntryFromFile(destinationFile, reportName);
                        // }
                        // if (System.IO.File.Exists(destinationFile))
                        // {
                        //     System.IO.File.Delete(destinationFile);
                        // }

                    }
                    reportName = reportName.Substring(0, reportName.Length - 4);
                    SortedList Params = new SortedList();
                    Params.Add("@xMain", "MainForm");
                    Params.Add("@nMenuID", MenuID);
                    CompanyData = dLayer.ExecuteScalar("select X_DataFieldCompanyID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
                    YearData = dLayer.ExecuteScalar("select X_DataFieldYearID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
                    BranchData = dLayer.ExecuteScalar("select X_DataFieldBranchID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();

                    Params.Add("@xType", "");
                    Params.Add("@nCompID", 0);
                    foreach (DataRow var in DetailTable.Rows)
                    {
                        int compID = myFunctions.getIntVAL(var["compId"].ToString());
                        string type = var["type"].ToString();
                        string value = var["value"].ToString();
                        string valueTo = var["valueTo"].ToString();

                        Params["@xType"] = type.ToLower();
                        Params["@nCompID"] = compID;


                        string xFeild = dLayer.ExecuteScalar("select X_DataField from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString();
                        bool bRange = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select isNull(B_Range,0) from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString());
                        string xOperator = dLayer.ExecuteScalar("select isNull(X_Operator,'') from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString();
                        xProCode = dLayer.ExecuteScalar("select X_ProcCode from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
                        string xInstanceCode = dLayer.ExecuteScalar("select isNull(X_DataField,'') from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
                        FieldName = dLayer.ExecuteScalar("select X_Text from vw_WebReportMenus where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID and N_LanguageId=1", Params, connection).ToString();
                        UserData = dLayer.ExecuteScalar("select X_DataFieldUserID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
                        FieldName = FieldName + "=";

                        if (xOperator == null || xOperator == "")
                            xOperator = "=";

                        if (x_Reporttitle != "")
                            x_Reporttitle += ", ";

                        if (type.ToLower() == "datepicker")
                        {
                            DateTime dateFrom = Convert.ToDateTime(value);
                            DateTime dateTo = Convert.ToDateTime(valueTo);
                            x_comments = dateFrom.ToString("dd-MM-yyyy") + " to " + dateTo.ToString("dd-MM-yyyy");

                            if (dateFrom != null && (bRange && dateTo != null))
                            {
                                x_Reporttitle = x_Reporttitle + FieldName + dateFrom.ToString("dd-MMM-yyyy") + " - " + dateTo.ToString("dd-MMM-yyyy");
                                x_comments = dateFrom.ToString("dd-MMM-yyyy") + " to " + dateTo.ToString("dd-MMM-yyyy");
                                procParam = dateFrom.ToString("dd-MMM-yyyy") + "|" + dateTo.ToString("dd-MMM-yyyy") + "|";
                            }
                            else if (dateFrom != null && !bRange)
                            {
                                x_Reporttitle = x_Reporttitle + FieldName + dateFrom.ToString("dd-MMM-yyyy");
                                x_comments = dateFrom.ToString("dd-MMM-yyyy");
                                procParam = dateFrom.ToString("dd-MMM-yyyy");
                            }
                            else if (bRange && dateTo != null)
                            {
                                x_Reporttitle = x_Reporttitle + FieldName + dateTo.ToString("dd-MMM-yyyy");
                                x_comments = dateTo.ToString("dd-MMM-yyyy");
                                procParam = dateTo.ToString("dd-MMM-yyyy") + "|";
                            }

                            string DateCrt = "";
                            if (xFeild != "")
                            {
                                if (bRange)
                                {
                                    DateCrt = xFeild + " >= Date('" + dateFrom.Year + "," + dateFrom.Month + "," + dateFrom.Day + "') And " + xFeild + " <= Date('" + dateTo.Year + "," + dateTo.Month + "," + dateTo.Day + "') ";
                                }
                                else
                                {
                                    DateCrt = xFeild + " " + xOperator + " Date('" + dateFrom.Year + "," + dateFrom.Month + "," + dateFrom.Day + "') ";
                                }
                                Criteria = Criteria == "" ? DateCrt : Criteria + " and " + DateCrt;
                            }
                        }
                        else
                        {
                            if (xFeild != "")
                            {
                                if (xFeild.Contains("#"))
                                    Criteria = Criteria == "" ? xFeild.Replace("#", value) : Criteria + " and " + xFeild.Replace("#", value);
                                else
                                {
                                    if (xFeild == "{Inv_Salesman.N_SalesmanID}")
                                    {
                                        Criteria = Criteria == "" ? xFeild + " " + xOperator + " " + value + " " : Criteria + " and " + xFeild + " " + xOperator + " " + value + " ";
                                        SalesmanID = myFunctions.getIntVAL(value.ToString());
                                    }
                                    else

                                        Criteria = Criteria == "" ? xFeild + " " + xOperator + " '" + value + "' " : Criteria + " and " + xFeild + " " + xOperator + " '" + value + "' ";

                                }
                            }
                            x_Reporttitle = x_Reporttitle + FieldName + value;
                        }



                        //{table.fieldname} in {?Start date} to {?End date}
                    }
                    if (Criteria == "" && CompanyData != "")
                    {
                        Criteria = Criteria + CompanyData + "=" + nCompanyID;
                        if (YearData != "")
                            Criteria = Criteria + " and " + YearData + "=" + FnYearID;
                        if (BranchData != "")
                        {
                            bool mainBranch = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select isnull(B_ShowallData,0) as B_ShowallData from Acc_BranchMaster where N_CompanyID=" + nCompanyID + " and N_BranchID=" + BranchID, Params, connection).ToString());
                            if (mainBranch == false)
                                Criteria = Criteria + " and " + BranchData + "=" + BranchID;

                        }
                    }
                    else if (CompanyData != "")
                    {
                        Criteria = Criteria + " and " + CompanyData + "=" + nCompanyID;
                        if (YearData != "")
                            Criteria = Criteria + " and " + YearData + "=" + FnYearID;
                        if (BranchData != "")
                        {
                            bool mainBranch = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select isnull(B_ShowallData,0) as B_ShowallData from Acc_BranchMaster where N_CompanyID=" + nCompanyID + " and N_BranchID=" + BranchID, Params, connection).ToString());
                            if (mainBranch == false)
                                Criteria = Criteria + " and " + BranchData + "=" + BranchID;
                        }
                    }
                    if (UserData != "")
                    {
                        Criteria = Criteria + " and " + UserData + "=" + nUserID;
                    }
                    if (xProCode != "")
                    {

                        bool mainBranch = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select isnull(B_ShowallData,0) as B_ShowallData from Acc_BranchMaster where N_CompanyID=" + nCompanyID + " and N_BranchID=" + BranchID, Params, connection).ToString());


                        SortedList mParamsList = new SortedList()
                            {
                            {"N_CompanyID",nCompanyID},
                            {"N_FnYearID",FnYearID},
                            {"N_PeriodID",0},
                            {"X_Code",xProCode},
                            {"X_Parameter", procParam },
                            {"N_UserID",myFunctions.GetUserID(User)},
                            {"N_BranchID", mainBranch ? 0 : BranchID},
                            // {"N_SalesmanID",SalesmanID},

                            // {"X_InstanceCode",random},
                            };
                        dLayer.ExecuteDataTablePro("SP_OpeningBalanceGenerate", mParamsList, connection);

                        // if(xInstanceCode!="")
                        // Criteria = Criteria == "" ? xInstanceCode + "='"+random+"' " : Criteria + " and "+xInstanceCode+"='"+random+"' ";

                    }

                    dbName = connection.Database;
                    if (MainMenuID != 340)
                    {
                    //Local Time Checking
                    object TimezoneID = dLayer.ExecuteScalar("select isnull(n_timezoneid,82) from acc_company where N_CompanyID= " + nCompanyID, connection);
                    object Timezone = dLayer.ExecuteScalar("select X_ZoneName from Gen_TimeZone where n_timezoneid=" + TimezoneID, connection);
                    if (Timezone != null && Timezone.ToString() != "")
                    {
                        currentTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(Timezone.ToString()));
                        x_comments = currentTime.ToString();
                    }
                    }
                }


                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                };
                var client = new HttpClient(handler);

                //HttpClient client = new HttpClient(clientHandler);

                var rptArray = reportName.Split(@"\");
                string actReportLocation = reportLocation;
                if (rptArray.Length > 1)
                {
                    reportName = rptArray[1].ToString();
                    actReportLocation = actReportLocation + rptArray[0].ToString() + "/";
                }

                //string URL = reportApi + "api/report?reportName=" + reportName + "&critiria=" + Criteria + "&path=" + this.TempFilesPath + "&reportLocation=" + actReportLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=" + x_comments + "&x_Reporttitle=" + x_Reporttitle + "&extention=" + Extention;
                string URL = reportApi + "api/report?reportName=" + reportName + "&critiria=" + Criteria + "&path=" + this.TempFilesPath + "&reportLocation=" + actReportLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=" + x_comments + "&x_Reporttitle=" + x_Reporttitle + "&extention=" + Extention + "&N_FormID=0&QRUrl=&N_PkeyID=0&partyName=&docNumber=&formName=";
                var path = client.GetAsync(URL);

                path.Wait();
                return Ok(_api.Success(new SortedList() { { "FileName", reportName.Trim() + random + "." + Extention } }));
                //string RptPath = reportPath + reportName.Trim() + ".pdf";
                // var memory = new MemoryStream();

                // using (var stream = new FileStream(RptPath, FileMode.Open))
                // {
                //     await stream.CopyToAsync(memory);
                // }
                // memory.Position = 0;
                // return File(memory, _api.GetContentType(RptPath), Path.GetFileName(RptPath));
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        // [HttpPost("getModuleReport")]
        // public IActionResult GetModuleReports([FromBody] DataSet ds)
        // {
        //     DataTable MasterTable;
        //     DataTable DetailTable;

        //     MasterTable = ds.Tables["master"];
        //     DetailTable = ds.Tables["details"];
        //     int nCompanyID = myFunctions.GetCompanyID(User);
        //     string x_comments = "";
        //     string x_Reporttitle = "";
        //     string X_TextforAll = "=all";
        //     int nUserID = myFunctions.GetUserID(User);

        //     try
        //     {
        //         String Criteria = "";
        //         String reportName = "";
        //         String CompanyData = "";
        //         String YearData = "";
        //         String FieldName = "";
        //         String UserData = "";

        //         var dbName = "";
        //         string Extention = "";
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();
        //             //int MenuID = myFunctions.getIntVAL(MasterTable.Rows[0]["moduleID"].ToString());
        //             int MenuID = myFunctions.getIntVAL(MasterTable.Rows[0]["reportCategoryID"].ToString());
        //             int ReportID = myFunctions.getIntVAL(MasterTable.Rows[0]["reportID"].ToString());
        //             int FnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["nFnYearID"].ToString());
        //             Extention = MasterTable.Rows[0]["extention"].ToString();

        //             SortedList Params1 = new SortedList();
        //             Params1.Add("@nMenuID", MenuID);
        //             Params1.Add("@xType", "RadioButton");
        //             Params1.Add("@nCompID", ReportID);


        //             reportName = dLayer.ExecuteScalar("select X_rptFile from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID and B_Active=1", Params1, connection).ToString();

        //             reportName = reportName.Substring(0, reportName.Length - 4);


        //             foreach (DataRow var in DetailTable.Rows)
        //             {
        //                 int compID = myFunctions.getIntVAL(var["compId"].ToString());
        //                 string type = var["type"].ToString();
        //                 string value = var["value"].ToString();
        //                 string valueTo = var["valueTo"].ToString();

        //                 SortedList Params = new SortedList();
        //                 Params.Add("@nMenuID", MenuID);
        //                 Params.Add("@xType", type.ToLower());
        //                 Params.Add("@nCompID", compID);
        //                 Params.Add("@xMain", "MainForm");
        //                 string xFeild = dLayer.ExecuteScalar("select X_DataField from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString();
        //                 bool bRange = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select isNull(B_Range,0) from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString());
        //                 string xOperator = dLayer.ExecuteScalar("select isNull(X_Operator,'') from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID", Params, connection).ToString();
        //                 string xProCode = dLayer.ExecuteScalar("select X_ProcCode from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
        //                 CompanyData = dLayer.ExecuteScalar("select X_DataFieldCompanyID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
        //                 YearData = dLayer.ExecuteScalar("select X_DataFieldYearID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
        //                 FieldName = dLayer.ExecuteScalar("select X_Text from vw_WebReportMenus where N_MenuID=@nMenuID and X_CompType=@xType and N_CompID=@nCompID and N_LanguageId=1", Params, connection).ToString();
        //                 UserData = dLayer.ExecuteScalar("select X_DataFieldUserID from Sec_ReportsComponents where N_MenuID=@nMenuID and X_CompType=@xMain", Params, connection).ToString();
        //                 FieldName = FieldName + "=";

        //                 if (xOperator == null || xOperator == "")
        //                     xOperator = "=";

        //                 if (x_Reporttitle != "")
        //                     x_Reporttitle += ", ";

        //                 if (type.ToLower() == "datepicker")
        //                 {
        //                     DateTime dateFrom = Convert.ToDateTime(value);
        //                     DateTime dateTo = Convert.ToDateTime(valueTo);
        //                     string procParam = "";
        //                     if (dateFrom != null && (bRange && dateTo != null))
        //                     {
        //                         x_Reporttitle = x_Reporttitle + FieldName + dateFrom.ToString("dd-MMM-yyyy") + " - " + dateTo.ToString("dd-MMM-yyyy");
        //                         x_comments = dateFrom.ToString("dd-MMM-yyyy") + " to " + dateTo.ToString("dd-MMM-yyyy");
        //                         procParam = dateFrom.ToString("dd-MMM-yyyy") + "|" + dateTo.ToString("dd-MMM-yyyy") + "|";
        //                     }
        //                     else if (dateFrom != null && !bRange)
        //                     {
        //                         x_Reporttitle = x_Reporttitle + FieldName + dateFrom.ToString("dd-MMM-yyyy");
        //                         x_comments = dateFrom.ToString("dd-MMM-yyyy");
        //                         procParam = dateFrom.ToString("dd-MMM-yyyy");
        //                     }
        //                     else if (bRange && dateTo != null)
        //                     {
        //                         x_Reporttitle = x_Reporttitle + FieldName + dateTo.ToString("dd-MMM-yyyy");
        //                         x_comments = dateTo.ToString("dd-MMM-yyyy");
        //                         procParam = dateTo.ToString("dd-MMM-yyyy") + "|";
        //                     }

        //                     if (xProCode != "")
        //                     {

        //                         SortedList mParamsList = new SortedList()
        //                     {
        //                     {"N_CompanyID",nCompanyID},
        //                     {"N_FnYearID",FnYearID},
        //                     {"N_PeriodID",0},
        //                     {"X_Code",xProCode},
        //                     {"X_Parameter", procParam },
        //                     {"N_UserID",myFunctions.GetUserID(User)},
        //                     {"N_BranchID",0}
        //                     };
        //                         dLayer.ExecuteDataTablePro("SP_OpeningBalanceGenerate", mParamsList, connection);

        //                     }
        //                     string DateCrt = "";
        //                     if (xFeild != "")
        //                     {
        //                         if (bRange)
        //                         {
        //                             DateCrt = xFeild + " >= Date('" + dateFrom.Year + "," + dateFrom.Month + "," + dateFrom.Day + "') And " + xFeild + " <= Date('" + dateTo.Year + "," + dateTo.Month + "," + dateTo.Day + "') ";
        //                         }
        //                         else
        //                         {
        //                             DateCrt = xFeild + " " + xOperator + " Date('" + dateFrom.Year + "," + dateFrom.Month + "," + dateFrom.Day + "') ";
        //                         }
        //                         Criteria = Criteria == "" ? DateCrt : Criteria + " and " + DateCrt;
        //                     }
        //                 }
        //                 else
        //                 {
        //                     if (xFeild != "")
        //                     {
        //                         if (xFeild.Contains("#"))
        //                             Criteria = Criteria == "" ? xFeild.Replace("#", value) : Criteria + " and " + xFeild.Replace("#", value);
        //                         else
        //                             Criteria = Criteria == "" ? xFeild + " " + xOperator + " '" + value + "' " : Criteria + " and " + xFeild + " " + xOperator + " '" + value + "' ";
        //                     }
        //                     x_Reporttitle = x_Reporttitle + FieldName + value;
        //                 }


        //                 //{table.fieldname} in {?Start date} to {?End date}
        //             }
        //             if (Criteria == "" && CompanyData != "")
        //             {
        //                 Criteria = Criteria + CompanyData + "=" + nCompanyID;
        //                 if (YearData != "")
        //                     Criteria = Criteria + " and " + YearData + "=" + FnYearID;
        //             }
        //             else if (CompanyData != "")
        //             {
        //                 Criteria = Criteria + " and " + CompanyData + "=" + nCompanyID;
        //                 if (YearData != "")
        //                     Criteria = Criteria + " and " + YearData + "=" + FnYearID;
        //             }
        //             if (UserData != "")
        //             {
        //                 Criteria = Criteria + " and " + UserData + "=" + nUserID;
        //             }
        //             dbName = connection.Database;
        //         }


        //         var handler = new HttpClientHandler
        //         {
        //             ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
        //         };
        //         var client = new HttpClient(handler);
        //         var random = RandomString();
        //         //HttpClient client = new HttpClient(clientHandler);

        //         var rptArray = reportName.Split(@"\");
        //         string actReportLocation = reportLocation;
        //         if (rptArray.Length > 1)
        //         {
        //             reportName = rptArray[1].ToString();
        //             actReportLocation = actReportLocation + rptArray[0].ToString() + "/";
        //         }


        //         string URL = reportApi + "api/report?reportName=" + reportName + "&critiria=" + Criteria + "&path=" + this.TempFilesPath + "&reportLocation=" + actReportLocation + "&dbval=" + dbName + "&random=" + random + "&x_comments=" + x_comments + "&x_Reporttitle=" + x_Reporttitle + "&extention=" + Extention;//+ connectionString;
        //         var path = client.GetAsync(URL);

        //         path.Wait();
        //         return Ok(_api.Success(new SortedList() { { "FileName", reportName.Trim() + random + "." + Extention } }));
        //         //string RptPath = reportPath + reportName.Trim() + ".pdf";
        //         // var memory = new MemoryStream();

        //         // using (var stream = new FileStream(RptPath, FileMode.Open))
        //         // {
        //         //     await stream.CopyToAsync(memory);
        //         // }
        //         // memory.Position = 0;
        //         // return File(memory, _api.GetContentType(RptPath), Path.GetFileName(RptPath));
        //     }
        //     catch (Exception e)
        //     {
        //         return Ok(_api.Error(User, e));
        //     }
        // }


        // private void LoadSelectionFormulae(int nFnYearID,int nBranchID,bool bAllBranchData)
        // {

        //     X_SelectionFormula = "";
        //     if (myFunctions.GetCompanyID(User) != 0)
        //     {
        //         X_SelectionFormula = X_CompanyField + " = " + myFunctions.GetCompanyID(User);
        //         if (X_YearField != "")
        //             X_SelectionFormula += " and (" + X_YearField + " = " + nFnYearID + ")";
        //         if (X_UserField != "")
        //             X_SelectionFormula += " and (" + X_UserField + " = " + myFunctions.GetUserID(User) + ")";
        //         if (X_BranchField != "")
        //         {
        //             if (bAllBranchData == false)
        //                 X_SelectionFormula += " and (" + X_BranchField  + " = " + nBranchID + ")";
        //         }


        //     }
        //     else if (X_YearField != "")
        //         X_SelectionFormula = "(" + X_YearField + " = " + nFnYearID + ")";

        //     if (flxListFilter.Rows >= 2)
        //     {
        //         for (int i = 1; i < flxListFilter.Rows; i++)
        //         {
        //             if (flxListFilter.get_TextMatrix(i, mcF_BRange).ToLower() == "true")
        //             {
        //                 if ((flxListFilter.get_TextMatrix(i, mcF_Filter1) == "" && flxListFilter.get_TextMatrix(i, mcF_Filter2) == "") || flxListFilter.get_TextMatrix(i, mcF_DataField) == "")
        //                 {
        //                     if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Date")
        //                     {
        //                         ////X_ProcParameter = MYG.DateToDB(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|" + MYG.DateToDB(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                         ////X_ProcParameter =  myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).ToString(myCompanyID._SystemDateFormat, myCompanyID._EnglishCulture) + "|" +  myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).ToString(myCompanyID._SystemDateFormat, myCompanyID._EnglishCulture) + "|";
        //                         //X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|" + myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter1) == "")
        //                             X_ProcParameter = " |";
        //                         else
        //                         {
        //                             X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|";
        //                             X_Rptcomments = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1));

        //                         }
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter2) == "")
        //                             X_ProcParameter = X_ProcParameter + " |";
        //                         else
        //                         {
        //                             X_ProcParameter = X_ProcParameter + myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                             if (myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) != myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)))
        //                             X_Rptcomments = X_Rptcomments + " to " + myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2));
        //                         }
        //                     }
        //                     continue;
        //                 }
        //                 else
        //                 {
        //                     //if (X_SelectionFormula != "")
        //                     //    X_SelectionFormula += " And ";
        //                     if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Date")
        //                     {
        //                         // //X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + " Date('" + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Year.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Month.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Day.ToString() + "') And " + flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator2) + " Date('" + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Year.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Month.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Day.ToString() + "') ";
        //                         // //X_ProcParameter = MYG.DateToDB(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|" + MYG.DateToDB(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                         //X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + " Date('" + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Year.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Month.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Day.ToString() + "') And " + flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator2) + " Date('" + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Year.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Month.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Day.ToString() + "') ";
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter1) != "")
        //                         {
        //                             if (X_SelectionFormula != "")
        //                                 X_SelectionFormula += " And ";
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + " Date('" + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Year.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Month.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Day.ToString() + "')";
        //                             X_Rptcomments = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1));
        //                         }
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter2) != "")
        //                         {
        //                             if (X_SelectionFormula != "")
        //                                 X_SelectionFormula += " And ";
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator2) + " Date('" + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Year.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Month.ToString() + "," + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).Day.ToString() + "') ";
        //                             if (myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) != myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)))
        //                                 X_Rptcomments = X_Rptcomments + " to " + myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2));

        //                         }

        //                        // //X_ProcParameter = myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).ToString(myCompanyID._SystemDateFormat, myCompanyID._EnglishCulture) + "|" + myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter2)).ToString(myCompanyID._SystemDateFormat, myCompanyID._EnglishCulture) + "|";
        //                         //X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|" + myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter1) != "")
        //                             X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1)) + "|";
        //                         else
        //                             X_ProcParameter = " |";

        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter2) != "")
        //                             X_ProcParameter += myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter2)) + "|";
        //                         else
        //                             X_ProcParameter +=  " |";
        //                     }
        //                     else if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Text")
        //                     {
        //                         if (X_SelectionFormula != "")
        //                             X_SelectionFormula += " And ";
        //                         X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + "'" + flxListFilter.get_TextMatrix(i, mcF_Filter1) + "' And " + flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator2) + " '" + flxListFilter.get_TextMatrix(i, mcF_Filter2) + "' ";
        //                     }
        //                     else
        //                     {
        //                         if (X_SelectionFormula != "")
        //                             X_SelectionFormula += " And ";
        //                         X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + flxListFilter.get_TextMatrix(i, mcF_Operator1) + flxListFilter.get_TextMatrix(i, mcF_Filter1) + " And " + flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator2) + " " + flxListFilter.get_TextMatrix(i, mcF_Filter2) + " ";
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 if (flxListFilter.get_TextMatrix(i, mcF_Filter1) == "" || flxListFilter.get_TextMatrix(i, mcF_DataField) == "")
        //                 {
        //                     if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Date")
        //                     {
        //                         ////X_ProcParameter = MYG.DateToDB(flxListFilter.get_TextMatrix(i, mcF_Filter1));
        //                         ////X_ProcParameter = myFunctions.GetFormatedDate(flxListFilter.get_TextMatrix(i, mcF_Filter1)).ToString(myCompanyID._SystemDateFormat, myCompanyID._EnglishCulture);                                
        //                         //X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1));
        //                         if (flxListFilter.get_TextMatrix(i, mcF_Filter1) != "")
        //                             X_ProcParameter = myFunctions.GetFormatedDate_Ret_string(flxListFilter.get_TextMatrix(i, mcF_Filter1));
        //                     }
        //                     continue;
        //                 }
        //                 else
        //                 {
        //                     if (X_SelectionFormula != "")
        //                         X_SelectionFormula += " And ";
        //                     if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Date")
        //                     {
        //                         if(flxListFilter.get_TextMatrix(i, mcF_Filter1)!="")
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + " Date('" + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Year.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Month.ToString() + "," + Convert.ToDateTime(flxListFilter.get_TextMatrix(i, mcF_Filter1)).Day.ToString() + "') ";
        //                     }
        //                     else if (flxListFilter.get_TextMatrix(i, mcF_FieldType) == "Text")
        //                     {
        //                         if (myFunctions.getBoolVAL(flxListFilter.get_TextMatrix(i, mcF_EnableMultiselect)))
        //                         {
        //                             string[] temp = flxListFilter.get_TextMatrix(i, mcF_Filter1).Split(',');
        //                             X_SelectionFormula += " (";
        //                             for (int w = 0; w < temp.Length; w++)
        //                             {
        //                                 if (w != 0)
        //                                     X_SelectionFormula += " OR ";
        //                                 X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + "'" + temp[w] + "' ";
        //                             }
        //                             X_SelectionFormula += " )";
        //                         }
        //                         else
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + " " + flxListFilter.get_TextMatrix(i, mcF_Operator1) + "'" + flxListFilter.get_TextMatrix(i, mcF_Filter1) + "' ";
        //                     }
        //                     else
        //                     {
        //                         if (flxListFilter.get_TextMatrix(i, mcF_DataField).Contains("#"))
        //                         {
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField).Replace("#", flxListFilter.get_TextMatrix(i, mcF_Filter1));
        //                         }
        //                         else
        //                             X_SelectionFormula += flxListFilter.get_TextMatrix(i, mcF_DataField) + flxListFilter.get_TextMatrix(i, mcF_Operator1) + flxListFilter.get_TextMatrix(i, mcF_Filter1) + " ";
        //                     }
        //                 }
        //             }
        //         }
        //     }

        // }


        private static Random random = new Random();
        public string RandomString(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("sendmessage")]
        public IActionResult SendMessage([FromBody] DataSet ds)
        {
            DataTable dt = new DataTable();
            dt = ds.Tables["master"];
            string x_Mobile = "+" + dt.Rows[0]["x_MobileNo"].ToString();
            int nCompanyId = myFunctions.GetCompanyID(User);
            SortedList QueryParams = new SortedList();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                object Currency = dLayer.ExecuteScalar("select x_currency from acc_company  where n_companyid=" + nCompanyId, QueryParams, connection);
                DateTime deldate = Convert.ToDateTime(dt.Rows[0]["d_Deliverydate"].ToString());
                string body = "Dear " + dt.Rows[0]["x_CustomerName"].ToString() + ",%0A%0AThe *Repair Order* for your Device is *" + dt.Rows[0]["x_ServiceCode"].ToString() + "* opened on " + dt.Rows[0]["d_Entrydate"].ToString() + ".%0A%0AEstimated time of delivery (ETD) is " + deldate.ToString("dd/MM/yyyy") + " and estimated amount is " + dt.Rows[0]["n_BillAmountF"].ToString()+" "+Currency + ". %0A%0ARegards, %0A" + dt.Rows[0]["x_UserName"].ToString();
                var client = new WebClient();
                var content = client.DownloadString("https://api.textmebot.com/send.php?recipient=" + x_Mobile + "&apikey=FmxUWUvgeou2&text=" + body);

            }
            return Ok(_api.Success("Message Sent"));

        }



    }
}