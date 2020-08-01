using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("voucher")]
    [ApiController]
    public class Acc_PaymentVoucher : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;


        public Acc_PaymentVoucher(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString(myCompanyID._ConnectionString);
        }


        [HttpGet("list")]
        public ActionResult GetPaymentVoucherList(int? nCompanyId, int nFnYearId, string voucherType)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from vw_AccVoucher_Disp where N_CompanyID=@p1 and N_FnYearID=@p2 and X_TransType=@p3";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            Params.Add("@p3", voucherType);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(dt);
                }
            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }
        [HttpGet("details")]
        public ActionResult GetVoucherDetails(int? nCompanyId, int nFnYearId, string xVoucherNo, string xTransType)
        {
            DataSet dt = new DataSet();
            SortedList Params = new SortedList();

            string sqlCommandText = "Select * from Acc_VoucherMaster left outer join Acc_MastLedger on Acc_VoucherMaster.N_DefLedgerID = Acc_MastLedger.N_LedgerID and Acc_VoucherMaster.N_FnYearID=Acc_MastLedger.N_FnYearID and Acc_VoucherMaster.N_CompanyID=Acc_MastLedger.N_CompanyID    Where  Acc_VoucherMaster.X_VoucherNo=@VoucherNo and Acc_VoucherMaster.N_CompanyID=@CompanyID and X_TransType=@TransType  AND Acc_VoucherMaster.N_FnYearID =@FnYearID Order By D_VoucherDate";
            Params.Add("@CompanyID", nCompanyId);
            Params.Add("@FnYearID", nFnYearId);
            Params.Add("@VoucherNo", xVoucherNo);
            Params.Add("@TransType", xTransType);
            int nFormID = 0;

            if (xTransType.ToLower() == "pv")
                nFormID = 1;
            else if (xTransType.ToLower() == "rv")
                nFormID = 2;
            else if (xTransType.ToLower() == "jv")
                nFormID = 3;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable Voucher = new DataTable();
                    Voucher = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    Voucher = api.Format(Voucher, "Master");
                    dt.Tables.Add(Voucher);
                    int nVoucherID = myFunctions.getIntVAL(Voucher.Rows[0]["N_VoucherID"].ToString());
                    Params.Add("@VoucherID", nVoucherID);

                    string sqlCommandText2 = "Select Acc_VoucherMaster_Details.*,Acc_MastLedger.*,Acc_MastGroup.X_Type,Acc_TaxCategory.X_DisplayName,Acc_TaxCategory.N_Amount,Acc_CashFlowCategory.X_Description AS X_TypeCategory from Acc_VoucherMaster_Details inner join Acc_MastLedger on Acc_VoucherMaster_Details.N_LedgerID = Acc_MastLedger.N_LedgerID and Acc_VoucherMaster_Details.N_CompanyID=Acc_MastLedger.N_CompanyID inner join Acc_MastGroup On Acc_MastLedger.N_GroupID=Acc_MastGroup.N_GroupID and Acc_MastLedger.N_CompanyID=Acc_MastGroup.N_CompanyID  and Acc_MastLedger.N_FnYearID=Acc_MastGroup.N_FnYearID  LEFT OUTER JOIN Acc_TaxCategory ON Acc_VoucherMaster_Details.N_TaxCategoryID1 = Acc_TaxCategory.N_PkeyID LEFT OUTER JOIN Acc_CashFlowCategory on Acc_VoucherMaster_Details.N_TypeID=Acc_CashFlowCategory.N_CategoryID Where N_VoucherID =@VoucherID and Acc_VoucherMaster_Details.N_CompanyID=@CompanyID and Acc_MastGroup.N_FnYearID=@FnYearID";
                    DataTable VoucherDetails = new DataTable();
                    VoucherDetails = dLayer.ExecuteDataTable(sqlCommandText2, Params, connection);
                    VoucherDetails = api.Format(VoucherDetails, "details");
                    dt.Tables.Add(VoucherDetails);

                    DataTable Acc_CostCentreTrans = new DataTable();
                    SortedList ProParams = new SortedList();
                    ProParams.Add("N_CompanyID", nCompanyId);
                    ProParams.Add("N_FnYearID", nFnYearId);
                    ProParams.Add("N_VoucherID", nVoucherID);
                    ProParams.Add("N_Flag", nFormID);

                    Acc_CostCentreTrans = dLayer.ExecuteDataTablePro("SP_Acc_Voucher_Disp", ProParams, connection);
                    Acc_CostCentreTrans = api.Format(Acc_CostCentreTrans, "costCenterTrans");
                    dt.Tables.Add(Acc_CostCentreTrans);


                }
                return Ok(api.Success(dt));

            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }

        //Save....
        [HttpPost("Save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {

            try
            {
                DataTable MasterTable;
                DataTable DetailTable;
                MasterTable = ds.Tables["master"];
                DetailTable = ds.Tables["details"];
                SortedList Params = new SortedList();

                DataRow masterRow = MasterTable.Rows[0];
                var xVoucherNo = masterRow["x_VoucherNo"].ToString();
                var xTransType = masterRow["x_TransType"].ToString();
                var InvoiceNo = masterRow["x_TransType"].ToString();
                int nVoucherId = myFunctions.getIntVAL(masterRow["n_VoucherID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    if (xVoucherNo == "@Auto")
                    {
                        Params.Add("N_CompanyID", masterRow["n_CompanyId"].ToString());
                        Params.Add("N_YearID", masterRow["n_FnYearId"].ToString());
                        Params.Add("N_FormID", 80);
                        Params.Add("N_BranchID", masterRow["n_BranchId"].ToString());
                        xVoucherNo = dLayer.GetAutoNumber("Acc_VoucherMaster", "x_VoucherNo", Params, connection, transaction);
                        if (xVoucherNo == "") { return Ok(api.Error("Unable to generate Invoice Number")); }

                        MasterTable.Rows[0]["x_VoucherNo"] = xVoucherNo;

                        MasterTable.Columns.Remove("N_VoucherId");
                        DetailTable.Columns.Remove("N_VoucherDetailsID");
                        DetailTable.AcceptChanges();
                    }

                    nVoucherId = dLayer.SaveData("Acc_VoucherMaster", "N_VoucherId", nVoucherId, MasterTable, connection, transaction);
                    if (nVoucherId <= 0)
                    {
                        transaction.Rollback();
                    }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["N_VoucherId"] = nVoucherId;
                    }
                    int N_InvoiceDetailId = dLayer.SaveData("Acc_VoucherMaster_Details", "N_VoucherDetailsID", 0, DetailTable, connection, transaction);
                    transaction.Commit();
                }
                return Ok(api.Success("Data Saved"));
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(ex));
            }
        }
        //Delete....
        [HttpDelete()]
        public ActionResult DeleteData(int N_VoucherID)
        {
            int Results = 0;
            try
            {
                dLayer.setTransaction();
                Results = dLayer.DeleteData("Inv_SalesVoucher", "n_quotationID", N_VoucherID, "");
                if (Results <= 0)
                {
                    dLayer.rollBack();
                    return StatusCode(409, api.Response(409, "Unable to delete sales quotation"));
                }
                else
                {
                    dLayer.DeleteData("Inv_SalesVoucherDetails", "n_quotationID", N_VoucherID, "");
                }

                if (Results > 0)
                {
                    dLayer.commit();
                    return StatusCode(200, api.Response(200, "Sales quotation deleted"));
                }
                else
                {
                    dLayer.rollBack();
                    return StatusCode(409, api.Response(409, "Unable to delete sales quotation"));
                }

            }
            catch (Exception ex)
            {
                return StatusCode(404, api.Response(404, ex.Message));
            }


        }

    }
}