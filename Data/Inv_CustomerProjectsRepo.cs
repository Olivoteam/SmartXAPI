using System;
using System.Collections.Generic;
using System.Linq;
using SmartxAPI.Models;

namespace SmartxAPI.Data
{
    public class Inv_CustomerProjectsRepo : IInvCustomerProjectsRepo
    {
        private readonly SmartxContext _context;

        public Inv_CustomerProjectsRepo(SmartxContext context)
        {
            _context = context;
        }
        public IEnumerable<VwInvCustomerProjects> GetAllProjects(int? nCompanyID,int? nFnYearID)
        {
            //return _context.AccCompany.ToList();
            return _context.VwInvCustomerProjects
             .Where(V => V.NCompanyId==nCompanyID && V.NFnYearId==nFnYearID)
            .ToList();
        }

    }

    public interface IInvCustomerProjectsRepo
    {
        IEnumerable<VwInvCustomerProjects> GetAllProjects(int? nCompanyID,int? nFnYearID);
    }
}