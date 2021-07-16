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
using System.IO;
using System.Threading.Tasks;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("general")]
    [ApiController]



    public class GenDefults : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;


        public GenDefults(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }


        //GET api/Projects/list
        [HttpGet("defults/{type}")]
        public ActionResult GetDefults(string type, string X_TypeCode)
        {
            int id = 0;
            switch (type.ToLower())
            {
                case "locationtype":
                    id = 1;
                    break;
                case "salarytype":
                    id = 2;
                    break;
                case "accrualtype":
                    id = 3;
                    break;
                case "accrualfrequency":
                    id = 4;
                    break;
                case "accrualstartfrom":
                    id = 5;
                    break;
                case "partnertype":
                    id = 25;
                    break;
                case "producttype":
                    id = 36;
                    break;
                case "traveltype":
                    id = 56;
                    break;
                case "activityrelation":
                    id = 91;
                    break;
                case "activitytype":
                    id = 92;
                    break;
                case "customertype":
                    id = 93;
                    break;
                case "timeunit":
                    id = 68;
                    break;
                case "hiretype":
                    id = 56;
                    break;
                case "closingstatus":
                    id = 94;
                    break;
                case "maritalstatus":
                    id = 26;
                    break;
                case "workmode":
                    id = 82;
                    break;
                case "licencepaymentmode":
                    id = 59;
                    break;
                case "licencetype":
                    id = 64;
                    break;
                case "decision":
                    id = 31;
                    break;
                case "employmenttype":
                    id = 6;
                    break;
                case "csv":
                    id = 80;
                    break;
                case "insurancetype":
                    id = 49;
                    break;
                case "assetcurrentstatus":
                    id = 46;
                    break;
                case "assetdepreciationmethod":
                    id = 57;
                    break;
                case "applicationtype":
                    id = 97;
                    break;
                case "defaultunittype":
                    id = 104;
                    break;
                case "media":
                    id = 28;
                    break;
                default: return Ok("Invalid Type");
            }
            string X_Criteria = "N_DefaultId=@p1";
            SortedList param = new SortedList() { { "@p1", id } };
            if (X_TypeCode != "" && X_TypeCode!= null)
            {
                X_Criteria = X_Criteria + " and X_TypeCode=@p2";
                param.Add("@p2", X_TypeCode);
            }

            DataTable dt = new DataTable();

            string sqlCommandText = "select * from Gen_Defaults where " + X_Criteria;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, param, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(dt));
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(e));
            }
        }

        [HttpGet("lookup/{type}")]
        public ActionResult GetLookup(string type)
        {
            int N_FormID = 0;
            switch (type)
            {
                case "VendorType":
                    N_FormID = 52;
                    break;
                case "Stage":
                    N_FormID = 1310;
                    break;
                case "Industry":
                    N_FormID = 1311;
                    break;
                case "LeadSource":
                    N_FormID = 1312;
                    break;
                case "SubSource":
                    N_FormID = 1355;
                    break;
                case "Priority":
                    N_FormID = 1356;
                    break;
                case "CustomerCategory":
                    N_FormID = 1357;
                    break;
                case "CrmItem":
                    N_FormID = 1358;
                    break;
                case "LeadStatus":
                    N_FormID = 1313;
                    break;
                case "District":
                    N_FormID = 1273;
                    break;
                case "Ownership":
                    N_FormID = 1314;
                    break;
                case "BloodGroup":
                    N_FormID = 14;
                    break;
                case "DrivingLicence":
                    N_FormID = 1208;
                    break;
                case "WorkLocation":
                    N_FormID = 1332;
                    break;
                case "EducationType":
                    N_FormID = 28;
                    break;
                case "serviceType":
                    N_FormID = 1151;
                    break;
                case "WorkType":
                    N_FormID = 1121;
                    break;
                case "ProjectType":
                    N_FormID = 1048;
                    break;
                case "EnquiryType":
                    N_FormID = 1100;
                    break;
                case "SubmissionType":
                    N_FormID = 1123;
                    break;
                case "SeparationReason":
                    N_FormID = 455;
                    break;
                case "Rating":
                    N_FormID = 9;
                    break;
                case "Purpose":
                    N_FormID = 51;
                    break;
                case "InsuranceClass":
                    N_FormID = 17;
                    break;
                case "Pricetype":
                    N_FormID = 3;
                    break;
                case "CrmWorkType":
                    N_FormID = 1360;
                    break;
                default: return Ok("Invalid Type");
            }

            string X_Criteria = "N_ReferId=@p1 order by n_Sort ASC";
            SortedList param = new SortedList() { { "@p1", N_FormID } };

            DataTable dt = new DataTable();

            string sqlCommandText = "select * from Gen_LookupTable where " + X_Criteria;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, param, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(dt));
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(e));
            }
        }




        [HttpGet("file")]
        public async Task<IActionResult> Download(string filename)
        {
            if (filename == null)
                return Content("filename not present");

            var path = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SortedList param = new SortedList();
                    param.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    DataTable tblDetails = dLayer.ExecuteDataTable("select ISNULL(X_Value,'') AS X_Value from Gen_Settings where X_Description ='EmpDocumentLocation' and N_CompanyID =@nCompanyID", param, connection);
                    path = tblDetails.Rows[0]["X_Value"].ToString();
                }


            }
            catch (Exception e)
            {
                return Ok(api.Error(e));
            }
            path = path + filename;

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, api.GetContentType(path), Path.GetFileName(path));
        }



    }

}