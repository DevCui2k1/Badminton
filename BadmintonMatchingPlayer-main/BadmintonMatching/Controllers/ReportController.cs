using System.Text;
using CloudinaryDotNet;
using System.Web;
using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace BadmintonMatching.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _reportServices;
        private readonly IUserServices _userServices;

        public ReportController(IReportServices reportServices, IUserServices userServices)
        {
            _reportServices = reportServices;
            _userServices = userServices;
        }

        [HttpGet]
        [Route("status/{status}/{admin_id}")]
        [ProducesResponseType(typeof(SuccessObject<List<Reports>>), 200)]
        public async Task<IActionResult> GetReportByStatus(int admin_id, int status)
        {
            if (!_userServices.IsAdmin(admin_id))
            {
                return Ok(new SuccessObject<List<Reports?>> { Message = "Bạn không có quyền truy cập !" });
            }
            var reports = await _reportServices.GetByStatus((ReportStatus)status);
            return Ok(new SuccessObject<List<Reports>> { Data = reports, Message = Message.SuccessMsg });
        }

        [HttpPost]
        [Route("from_tran/{tran_id}")]
        public async Task<IActionResult> CreateFromTransaction(int tran_id, ReportContent info)
        {
            var report_id = await _reportServices.CreateFromTransaction(tran_id, info);
            if (report_id == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Tạo giao dịch thất bại !" });
            }
            else
            {
                return Ok(new SuccessObject<object> { Data = new { ReportId = report_id }, Message = Message.SuccessMsg });
            }
        }

        [HttpPost]
        [Route("sender/{user_id}/from_post/{post_id}")]
        public async Task<IActionResult> CreateFromPost(int user_id, int post_id, ReportContent info)
        {
            var report_id = await _reportServices.CreateFromPost(user_id, post_id, info);
            if (report_id == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Tạo giao dịch thất bại !" });
            }
            else
            {
                return Ok(new SuccessObject<object> { Data = new { ReportId = report_id }, Message = Message.SuccessMsg });
            }
        }

        [HttpPut]
        [Route("update_report_status/{id_report}&{report_status}")]
        public async Task<IActionResult> CreateFromPost(int id_report, int report_status)
        {
            var report_id = await _reportServices.UpdateReportStatus(id_report, report_status);
            if (report_id == false)
            {
                return Ok(new SuccessObject<object> { Message = "Báo cáo không tồn tại !" });
            }
            else
            {
                return Ok(new SuccessObject<object> { Data = new { ReportId = report_id }, Message = Message.SuccessMsg });
            }
        }


        [HttpGet]
        [Route("type/{report_type}")]
        public async Task<IActionResult> GetReportByType(int report_type)
        {
            var reports = await _reportServices.GetReportByType((ReportCreateType)report_type);
            return Ok(new SuccessObject<List<Reports>> { Data = reports, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{month}${year}/report_income_inMonth")]
        public async Task<IActionResult> GetReportIncomeInMonth(string month,string year)
        {
            var reportIncomeModel =  _reportServices.GetIncomeByInMonth(month, year);
            return Ok(new SuccessObject<ReportIncomeModel> { Data = reportIncomeModel, Message = Message.SuccessMsg });
        }
        [HttpGet]
        [Route("{startDate}&{endDate}/report_income_Month")]
        public async Task<IActionResult> GetReportIncomeMonth(string startDate, string endDate)


        {
            startDate = HttpUtility.UrlDecode((startDate));
            endDate= HttpUtility.UrlDecode((endDate));

            var reportIncomeModel = _reportServices.GetIncomeByMonth( startDate, endDate);
            return Ok(new SuccessObject<ReportIncomeModel> { Data = reportIncomeModel, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{idReport}&{reportType}/report_detail")]
        public async Task<IActionResult> ReportDetail(int idReport, int reportType)


        {
          
            var reportDetail =await _reportServices.ReportDetail(idReport, reportType);
            if(reportDetail == null)
            {
                 return Ok(new SuccessObject<object> { Data = null, Message = "Lấy thông tin chi tiết báo cáo thất bại !" });
            }
            return Ok(new SuccessObject<ReportDetail> { Data = reportDetail, Message = Message.SuccessMsg });
        }
    }
}
