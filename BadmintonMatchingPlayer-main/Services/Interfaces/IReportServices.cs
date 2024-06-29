using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IReportServices
    {
        Task<int> CreateFromPost(int user_id, int post_id, ReportContent info);
        Task<int> CreateFromTransaction(int tran_id, ReportContent info);
        Task<List<Reports>> GetByStatus(ReportStatus status);
        Task<List<Reports>> GetReportByType(ReportCreateType report_type);
        ReportIncomeModel GetIncomeByInMonth(string month,string year);
        ReportIncomeModel GetIncomeByMonth(string startDate,string endDate);
        Task<ReportDetail> ReportDetail(int idReport, int reportType);
        Task<bool> UpdateReportStatus(int idReport, int reportStatus);
    }
}
