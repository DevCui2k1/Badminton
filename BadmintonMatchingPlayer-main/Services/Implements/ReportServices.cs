using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Repositories.Intefaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{

    public class ReportServices : IReportServices
    {
        private readonly IRepositoryManager _repositoryManager;

        public ReportServices(IRepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
        }

        public async Task<int> CreateFromPost(int user_id, int post_id, ReportContent info)
        {
            var post = await _repositoryManager.Post.FindByCondition(x => x.Id == post_id, false)
                .FirstOrDefaultAsync();
            if (post == null)
            {
                return 0;
            }

            var report = new Report
            {
                IdUserFrom = user_id,
                IdUserTo = post.IdUserTo,
                reportContent = info.reportContent,
                ReportTitle = info.ReportTitle,
                Status = (int)ReportStatus.Pending,
                TimeReport = DateTime.UtcNow.AddHours(7),
                IdPost = post.Id
            };
            _repositoryManager.Report.Create(report);
            await _repositoryManager.SaveAsync();
            return report.Id;
        }

        public async Task<int> CreateFromTransaction(int tran_id, ReportContent info)
        {
            var tran = await _repositoryManager.Transaction.FindByCondition(x => x.Id == tran_id, false)
                .Include(x => x.IdSlotNavigation)
                .ThenInclude(x => x.IdPostNavigation)
                .FirstOrDefaultAsync();
            if (tran == null)
            {
                return 0;
            }
            else
            {
                int? idUserTo=1;
                if (tran.IdSlotNavigation != null)
                {
                    idUserTo = tran.IdSlotNavigation.IdPostNavigation.IdUserTo;
                }
                var report = new Report
                {
                    IdTransaction = tran_id,
                    IdUserFrom = tran.IdUser,
                    IdUserTo = idUserTo,
                    reportContent = info.reportContent,
                    ReportTitle = info.ReportTitle,
                    Status = (int)ReportStatus.Pending,
                    TimeReport = DateTime.UtcNow.AddHours(7),
                };
                _repositoryManager.Report.Create(report);
                await _repositoryManager.SaveAsync();
                return report.Id;
            }
        }

        public async Task<List<Reports>> GetByStatus(ReportStatus status)
        {
            switch (status)
            {
                case ReportStatus.Pending:
                case ReportStatus.Reviewing:
                    {
                        var reports = await _repositoryManager.Report.FindByCondition(x => x.Status == (int)status, false)
                            .Select(
                                x => new Reports
                                {
                                    Content = x.reportContent,
                                    Status = status.ToString(),
                                    DateReceive = x.TimeReport.Value.ToString("d"),
                                    Id = x.Id,
                                    Title = x.ReportTitle
                                })
                            .OrderBy(x => x.DateReceive)
                            .ToListAsync();
                        return reports;
                    }
                case ReportStatus.Resolved:
                    {
                        var reports = await _repositoryManager.Report.FindByCondition(x => x.Status == (int)status, false)
                            .Select(
                                x => new Reports
                                {
                                    Content = x.reportContent,
                                    Status = status.ToString(),
                                    DateReceive = x.TimeReport.Value.ToString("d"),
                                    Id = x.Id,
                                    Title = x.ReportTitle
                                })
                            .OrderByDescending(x => x.DateReceive)
                            .ToListAsync();
                        return reports;
                    }
                default:
                    {
                        var reports = await _repositoryManager.Report.FindAll(false)
                            .Select(
                                x => new Reports
                                {
                                    Content = x.reportContent,
                                    Status = ((ReportStatus)x.Status).ToString(),
                                    DateReceive = x.TimeReport.Value.ToString("d"),
                                    Id = x.Id,
                                    Title = x.ReportTitle
                                })
                            .OrderBy(x => x.DateReceive)
                            .ToListAsync();
                        return reports;
                    }
            }
        }

        public async Task<List<Reports>> GetReportByType(ReportCreateType report_type)
        {
            var res = new List<Reports>();

            switch (report_type)
            {
                case ReportCreateType.Post:
                    {
                        res = await _repositoryManager.Report.FindByCondition(x => x.Post != null, false)
                            .Select(x => new Reports
                            {
                                Content = x.reportContent,
                                Status = ((ReportStatus)x.Status).ToString(),
                                DateReceive = x.TimeReport.Value.ToString("d"),
                                Id = x.Id,
                                Title = x.ReportTitle,
                                NavigationId = x.Id,
                                ObjectNavigation = "Post"
                            }).ToListAsync();
                        break;
                    }
                case ReportCreateType.User:
                    {
                        res = await _repositoryManager.Report.FindByCondition(x => x.Post == null && x.IdTransaction == null, false)
                            .Select(x => new Reports
                            {
                                Content = x.reportContent,
                                Status = ((ReportStatus)x.Status).ToString(),
                                DateReceive = x.TimeReport.Value.ToString("d"),
                                Id = x.Id,
                                Title = x.ReportTitle,
                                NavigationId = x.Id,
                                ObjectNavigation = "User"
                            }).ToListAsync();
                        break;
                    }
                default:
                    {
                        res = await _repositoryManager.Report.FindByCondition(x => x.IdTransaction != null, false)
                            .Select(x => new Reports
                            {
                                Content = x.reportContent,
                                Status = ((ReportStatus)x.Status).ToString(),
                                DateReceive = x.TimeReport.Value.ToString("d"),
                                Id = x.Id,
                                Title = x.ReportTitle,
                                NavigationId = x.Id,
                                ObjectNavigation = "Transaction"
                            }).ToListAsync();
                        break;
                    }
            }

            return res;
        }

        public ReportIncomeModel GetIncomeByInMonth(string month, string year)
        {
            var adminId = 1;
            var listHistoryWallet = _repositoryManager.HistoryWallet.FindByCondition(x => x.Time.Value.Month.ToString().Equals(month)
            && x.Time.Value.Year.ToString().Equals(year) && x.IdUser == adminId, false)
                .Select(x => new HistoryWalletModel
                {
                    IdWallet = x.IdWallet,
                    Id = x.Id,
                    IdUser = x.IdUser,
                    Amount = x.Amount,
                    Status = ((HistoryWalletStatus)x.Status).ToString(),
                    Time = x.Time.Value.ToString("dd/MM/yyyy HH:mm"),
                    Type = x.Type
                }).ToList();

       
            var total = listHistoryWallet.Sum(x => Convert.ToDecimal(x.Amount));

            var reportIncome = new ReportIncomeModel();
            reportIncome.historyWalletModels = listHistoryWallet.OrderByDescending(x => x.Id).ToList(); ;
            reportIncome.Total = total;

            return reportIncome;
        }


        public ReportIncomeModel GetIncomeByMonth(string startDate, string endDate)
        {
            var adminId = 1;
            var dStartDate = DateTime.Parse(startDate);
            var dEndDate = DateTime.Parse(endDate);

            var listHistoryWallet = _repositoryManager.HistoryWallet.FindByCondition(x => x.Time >= dStartDate && x.Time <= dEndDate && x.IdUser == adminId, false)
                .Select(x => new HistoryWalletModel
                {
                    IdWallet = x.IdWallet,
                    Id = x.Id,
                    IdUser = x.IdUser,
                    Amount = x.Amount,
                    Status = ((HistoryWalletStatus)x.Status).ToString(),
                    Time = x.Time.Value.ToString("dd/MM/yyyy HH:mm"),
                    Type = x.Type
                }).ToList();

            var total = listHistoryWallet.Sum(x => Convert.ToDecimal(x.Amount));

            var reportIncome = new ReportIncomeModel();
            reportIncome.historyWalletModels = listHistoryWallet.OrderByDescending(x=>x.Id).ToList();
            reportIncome.Total = total;

            return reportIncome;
        }


        public async Task<ReportDetail> ReportDetail(int idReport, int reportType)
        {
            try
            {
                var report = await _repositoryManager.Report.FindByCondition(x => x.Id == idReport, false).FirstOrDefaultAsync();
                var userFrom = await _repositoryManager.User.FindByCondition(x => x.Id == report.IdUserFrom, false).FirstOrDefaultAsync();
                var userTo = await _repositoryManager.User.FindByCondition(x => x.Id == report.IdUserTo, false).FirstOrDefaultAsync();
                var reportDetail = new ReportDetail()
                {
                    ReportId = idReport,
                    ContentReport = report.reportContent,
                    ReportStatus = report.Status,
                    UserReportId = report.IdUserTo,
                    UserSendId=report.IdUserFrom,
                    SendUserName = userFrom.FullName,
                    reportUserName = userTo.FullName,
                    ReportType = reportType,
                    TitleReport=report.ReportTitle,
                    IsBan=userTo.IsBanFromLogin,
                    
                };


                switch (reportType)
                {
                    case (int)ReportCreateType.Post:
                        {
                            var post = await _repositoryManager.Post.FindByCondition(x => x.Id == report.IdPost, false).FirstOrDefaultAsync();

                            reportDetail.reportPost.PostId = post.Id;
                            reportDetail.reportPost.PostName = post.Title;
                            reportDetail.reportPost.PostContent = post.ContentPost;
                            reportDetail.reportPost.PostDate = post.SavedDate.ToString();
                            reportDetail.reportPost.PostAddress = post.AddressSlot;
                            reportDetail.reportPost.PostImage = post.ImgUrl;
                            break;
                        }

                    case (int)ReportCreateType.Transaction:
                        {
                            var trans = await _repositoryManager.Transaction.FindByCondition(x => x.Id == report.IdTransaction, false).FirstOrDefaultAsync();

                            reportDetail.reportTrans.TransId = trans.Id;
                            reportDetail.reportTrans.TransDate = trans.TimeTrans.ToString();
                            reportDetail.reportTrans.TransMoney = trans.MoneyTrans;
                            
                            
                            break;
                        }

                }

                return reportDetail;
            }
            catch (Exception e)
            {

            }
            return new ReportDetail();

        }

        public async Task<bool> UpdateReportStatus(int idReport, int reportStatus)
        {
            var report =await _repositoryManager.Report.FindByCondition(x => x.Id == idReport, false).FirstOrDefaultAsync();
            if (report == null)
            {
                return false;
            }
            report.Status = reportStatus;
            _repositoryManager.Report.Update(report);
           await _repositoryManager.SaveAsync();
            return true;
        }
    }
}
