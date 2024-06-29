using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Repositories.Intefaces;
using Services.Interfaces;
using System.Globalization;

namespace Services.Implements
{
    public class TransactionServices : ITransactionServices
    {
        private readonly IRepositoryManager _repositoryManager;

        public TransactionServices(IRepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
        }

        public async Task<Transaction> CreateForBuySlot(TransactionCreateInfo info)
        {
            var slots = await _repositoryManager.Slot.FindByCondition(x => info.IdSlot.Contains(x.Id) && !x.IsDeleted, false).ToListAsync();
            if (slots.Count() == 0)
            {
                return null;
            }

            var deadLine = DateTime.MinValue;
            decimal price = 0;

            foreach(var slot in slots)
            {
                var slotTime = DateTime.ParseExact(slot.ContentSlot, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if(slotTime > deadLine)
                {
                    deadLine = slotTime;
                }
                price += slot.Price.Value;
            }

            var tran = new Transaction
            {
                DeadLine = deadLine.AddDays(1),
                IdUser = info.IdUser,
                MoneyTrans = price,
                MethodTrans = "buy_slot",
                TypeTrans = "buy _slot",
                TimeTrans = DateTime.UtcNow.AddHours(7),
                Status = (int)TransactionStatus.Processing,
            };
            _repositoryManager.Transaction.Create(tran);
            await _repositoryManager.SaveAsync();
            if (tran.Id > 0)
            {
                var slotsEnt = _repositoryManager.Slot.FindByCondition(x => info.IdSlot.Contains(x.Id), true).ToList();
                tran.Slots = slotsEnt;
                _repositoryManager.Transaction.Update(tran);
                await _repositoryManager.SaveAsync();
            }
            return tran;
        }

        public async Task DeleteSlot(int transaction_id)
        {
            var slots = _repositoryManager.Slot.FindByCondition(x => x.TransactionId == transaction_id, true).ToList();
            foreach (var slot in slots)
            {
                _repositoryManager.Slot.Delete(slot);
            }
            await _repositoryManager.SaveAsync();
        }

        public async Task DeleteTran(int transaction_id)
        {
            var transaction = await _repositoryManager.Transaction.FindByCondition(x => x.Id == transaction_id, true).FirstOrDefaultAsync();
            if (transaction != null)
            {
                _repositoryManager.Transaction.Delete(transaction);

                var usertrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = transaction.IdUser,
                    MoneyTrans = transaction.MoneyTrans,
                    MethodTrans = "refund_trans",
                    TypeTrans = "refund_trans",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(usertrans);
                await _repositoryManager.SaveAsync();

                var userWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == transaction.IdUser, true).FirstOrDefaultAsync();
                if (userWallet != null)
                {
                    userWallet.Balance += transaction.MoneyTrans;
                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = transaction.MoneyTrans.ToString(),
                        IdUser = transaction.IdUser,
                        IdWallet = userWallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Nhận tiền hoàn giao dịch: "+ transaction.Id
                    });
                }

                await _repositoryManager.SaveAsync();
            }
        }

        public bool ExistTran(int tran_id)
        {
            return _repositoryManager.Transaction.FindByCondition(x => x.Id == tran_id, false).FirstOrDefault() != null;
        }

        public async Task<TransactionDetail> GetDetail(int transaction_id)
        {
            double dCancelDate = 0;
            var tran = await _repositoryManager.Transaction.FindByCondition(x => x.Id == transaction_id, false)
                .Select(x => new TransactionDetail
                {
                    Id = x.Id,
                    BuyerName = x.IdUserNavigation.FullName,
                    PayTime = x.TimeTrans.Value.ToString("dd/MM/yyyy HH:mm"),
                    SlotCount = x.Slots.Count,
                    Slots = x.Slots.Select(y => new SlotBuy
                    {
                        Id = y.Id,
                        PlayDate = y.ContentSlot
                    }).ToList(),
                    Total = x.MoneyTrans.Value.ToString(),
                    TranStatus = (TransactionStatus)x.Status
                }).FirstOrDefaultAsync();

            var setting = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == (int)SettingType.CancelDate, false).FirstOrDefaultAsync();
            if (setting != null )
            {
                 dCancelDate = Convert.ToDouble(setting.SettingAmount);

                var minPlayDateTime = tran.Slots
                    .Select(x => DateTime.ParseExact(x.PlayDate, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                    .Min();
                var minPlayDateTimeWithCancel = minPlayDateTime.AddHours(dCancelDate);
                tran.IsCancel = (minPlayDateTime < minPlayDateTimeWithCancel && minPlayDateTime > DateTime.Now)
                && (tran.TranStatus == TransactionStatus.Processing || tran.TranStatus == TransactionStatus.PaymentSuccess);
            }
                

            if (tran != null && tran.Slots != null && tran.Slots[0] != null)
            {
                var slotId = tran.Slots[0].Id;
                var slot = await _repositoryManager.Slot.FindByCondition(x => x.Id == slotId, false)
                    .Include(x => x.IdPostNavigation)
                    .ThenInclude(x => x.IdUserToNavigation)
                    .FirstOrDefaultAsync();
                if (slot != null && slot.IdPostNavigation != null)
                {
                    tran.Post = new PostInTransaction
                    {
                        Address = slot.IdPostNavigation.AddressSlot,
                        Id = slot.IdPostNavigation.Id,
                        UserId=slot.IdPostNavigation.IdUserTo,
                        ImageUrls = slot.IdPostNavigation.ImageUrls.Split(";").ToList(),
                        Title = slot.IdPostNavigation.Title,
                        TitleImage = slot.IdPostNavigation.ImgUrl,
                        CategorySlot = slot.IdPostNavigation.CategorySlot,
                        CreateUser = slot.IdPostNavigation.IdUserToNavigation.FullName
                    };
                    foreach (var infoSlot in slot.IdPostNavigation.SlotsInfo.Split(";"))
                    {
                        //var info = new SlotInfo(infoSlot);
                        //if (info.StartTime.Value.ToString("dd/MM/yyyy") == slot.ContentSlot)
                        //{
                        //    tran.Post.StartTime = info.StartTime.Value.ToString("dd/MM/yyyy HH:mm");
                        //    tran.Post.EndTime = info.EndTime.Value.ToString("dd/MM/yyyy HH:mm");
                        //    tran.Post.PricePerSlot = info.Price.ToString();
                        //    break;
                        //}
                    }
                }
            }
            tran.CancelHour = dCancelDate;
            return tran != null ? tran : new TransactionDetail { Id = 0, };
        }

        public async Task<List<TransactionInfo>> GetOfUser(int user_id)
        {
            var trans = await _repositoryManager.Transaction.FindByCondition(x => x.IdUser == user_id, false)
                .Include(x => x.Slots)
                .Select(x => new TransactionInfo
                {
                    Id = x.Id,
                    MoneyPaied = x.MoneyTrans.ToString(),
                    Slots = x.Slots.Select(y => new SlotBuy
                    {
                        Id = y.Id,
                        PlayDate = y.ContentSlot
                    }).ToList()
                }).ToListAsync();
            return trans;
        }

        public async Task<Transaction> GetTransaction(int transaction_id)
        {
            var tran = await _repositoryManager.Transaction.FindByCondition(x => x.Id == transaction_id, false)
                .FirstOrDefaultAsync();
            return tran == null ? new Transaction { Id = 0 } : tran;
        }

        public bool IsFromTwoPost(List<int>? idSlot)
        {
            var idPosts = _repositoryManager.Slot.FindByCondition(x => idSlot.Contains(x.Id), false)
                .Select(x => x.IdPost)
                .ToList();

            var postId = idPosts[0];
            foreach (var id in idPosts)
            {
                if (id != postId)
                    return true;
            }
            return false;
        }

        public async Task UpdateStatus(int tran_id, TransactionStatus tranStatus)
        {
            var tran = await _repositoryManager.Transaction.FindByCondition(x => x.Id == tran_id, true)
                .Include(x => x.IdSlotNavigation)
                .ThenInclude(x => x.IdPostNavigation)
                .FirstOrDefaultAsync();
            int adminId = 1;

            if (tran != null)
            {
                if (tranStatus == TransactionStatus.Reporting)
                {
                    var scheduled = await _repositoryManager.HangfireJob.FindByCondition(x => x.TransactionId == tran.Id, true).FirstOrDefaultAsync();
                    
                    if(scheduled != null)
                    {
                        BackgroundJob.Delete(scheduled.ScheduledId);
                        _repositoryManager.HangfireJob.Delete(scheduled);
                        await _repositoryManager.SaveAsync();
                    }
                }
                else if (tranStatus == TransactionStatus.Played)
                {

                   

                    var idUserTo = await _repositoryManager.Slot
                        .FindByCondition(x => x.TransactionId == tran.Id, false)
                        .Include(x => x.IdPostNavigation)
                        .Select(x => x.IdPostNavigation.IdUserTo)
                        .FirstOrDefaultAsync();
                
                    var UsertranHistory = new HistoryTransaction
                    {
                        IdUserFrom = tran.IdUser,
                        IdUserTo = idUserTo,
                        IdTransaction = tran.Id,
                        MoneyTrans = tran.MoneyTrans,
                        Status = true,
                        Deadline = tran.DeadLine
                    };
                    _repositoryManager.HistoryTransaction.Create(UsertranHistory);

                   

                    
                    var adminWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == adminId, true).FirstOrDefaultAsync();
                    //Create admin transaction
                    var admintrans = new Transaction
                    {
                        Id = 0,
                        DeadLine = null,
                        IdUser = adminId,
                        MoneyTrans = tran.MoneyTrans,
                        MethodTrans = "booking_free",
                        TypeTrans = "booking_free",
                        TimeTrans = DateTime.UtcNow.AddHours(7),
                        Status = (int)TransactionStatus.PaymentSuccess,
                    };
                    _repositoryManager.Transaction.Create(admintrans);
                   await _repositoryManager.SaveAsync();

                    var adminTranHistory = new HistoryTransaction
                    {
                        IdUserFrom = tran.IdUser,
                        IdUserTo = adminId,
                        IdTransaction = admintrans.Id,
                        MoneyTrans = tran.MoneyTrans,
                        Status = true,
                        Deadline = null
                    };
                    _repositoryManager.HistoryTransaction.Create(adminTranHistory);

                    //Create admin history wallet
                    if (adminWallet != null)
                    {
                        adminWallet.Balance += tran.MoneyTrans;


                        _repositoryManager.HistoryWallet.Create(new HistoryWallet
                        {
                            Amount = tran.MoneyTrans.ToString(),
                            IdUser = adminId,
                            IdWallet = adminWallet.Id,
                            Status = (int)HistoryWalletStatus.Success,
                            Time = DateTime.UtcNow.AddHours(7),
                            Type = "Nhận tiền hoa hồng đặt sân của đơn hàng :  "+ tran_id.ToString(),
                        });
                    }

                    var userWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == adminId, true).FirstOrDefaultAsync();
                    if (userWallet != null)
                    {
                        userWallet.Balance += tran.MoneyTrans;
                     

                        _repositoryManager.HistoryWallet.Create(new HistoryWallet
                        {
                            Amount = tran.MoneyTrans.ToString(),
                            IdUser = UsertranHistory.IdUserTo,
                            IdWallet = userWallet.Id,
                            Status = (int)HistoryWalletStatus.Success,
                            Time = DateTime.UtcNow.AddHours(7),
                            Type = "Nhận tiền sân"
                        });
                    }

                    var scheduled = await _repositoryManager.HangfireJob.FindByCondition(x => x.TransactionId == tran.Id, true).FirstOrDefaultAsync();

                    if (scheduled != null)
                    {
                        BackgroundJob.Delete(scheduled.ScheduledId);
                        _repositoryManager.HangfireJob.Delete(scheduled);
                    }
                }
                else if (tranStatus == TransactionStatus.ReportResolved)
                {
                    var tranHistory = new HistoryTransaction
                    {
                        IdUserFrom = tran.IdUser,
                        IdUserTo = tran.IdUser,
                        IdTransaction = tran.Id,
                        MoneyTrans = tran.MoneyTrans,
                        Status = true,
                        Deadline = tran.DeadLine
                    };

                    _repositoryManager.HistoryTransaction.Create(tranHistory);

                    var wallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == adminId, true).FirstOrDefaultAsync();
                    if(wallet != null)
                    {
                        wallet.Balance += tranHistory.MoneyTrans;

                        _repositoryManager.HistoryWallet.Create(new HistoryWallet
                        {
                            Amount = tranHistory.MoneyTrans.ToString(),
                            IdUser = tranHistory.IdUserTo,
                            IdWallet = wallet.Id,
                            Status = (int)HistoryWalletStatus.Success,
                            Time = DateTime.UtcNow.AddHours(7),
                            Type = "Hoàn tiền sân"
                        });
                    }
                }
                tran.Status = (int)tranStatus;
                await _repositoryManager.SaveAsync();
            }
        }

        public async Task<int> CreateWithdrawRequest(CreateWithdrawRequest createWithdrawRequest)
        {
            if (createWithdrawRequest == null)
            {
                return 0;
            }           
            try
            {
                var res = new WithdrawDetail()
                {
                    IdUser = createWithdrawRequest.IdUser,
                    Money = createWithdrawRequest.Money,
                    BankName = createWithdrawRequest.BankName,
                    BankNumber = createWithdrawRequest.BankNumber,
                    AccountName = createWithdrawRequest.AccountName,
                    CreateDate=DateTime.Now,
                    Status=(int)WithdrawStatus.Watting
                };
                _repositoryManager.WithdrawDetail.Create(res);

                var wallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == createWithdrawRequest.IdUser, true).FirstOrDefaultAsync();
                if (wallet.Balance < createWithdrawRequest.Money)
                {
                    return -1;
                }

                if (wallet != null)
                {
                    wallet.Balance -= createWithdrawRequest.Money;

                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = (-createWithdrawRequest.Money).ToString(),
                        IdUser = createWithdrawRequest.IdUser,
                        IdWallet = wallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Rút tiền"
                    });
                }

                var usertrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = createWithdrawRequest.IdUser,
                    MoneyTrans = createWithdrawRequest.Money,
                    MethodTrans = "withdraw_money",
                    TypeTrans = "withdraw_money",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.Processing,
                };
                _repositoryManager.Transaction.Create(usertrans);
                await  _repositoryManager.SaveAsync();
                return res.Id;
            }
            catch (Exception e)
            {

                return 0;
            }         
        }

        public async Task<int> DeniedRequestWithDrawStatus(int requestId)
        {
            var withdrawRequest = await _repositoryManager.WithdrawDetail.FindByCondition(x => x.Id == requestId, false).FirstOrDefaultAsync();
           
            if (withdrawRequest  == null )
            {
                return 0;
            }
            try
            {
     
                withdrawRequest.Status = (int)WithdrawStatus.Denied;
                _repositoryManager.WithdrawDetail.Update(withdrawRequest);
                await _repositoryManager.SaveAsync();



                var wallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == withdrawRequest.IdUser, true).FirstOrDefaultAsync();          
                if (wallet != null)
                {
                    wallet.Balance += withdrawRequest.Money;

                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = (withdrawRequest.Money).ToString(),
                        IdUser = withdrawRequest.IdUser,
                        IdWallet = wallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Hoàn tiền đã rút, yêu cầu rút tiền: "+requestId
                    });
                }
                await _repositoryManager.SaveAsync();
                var usertrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = withdrawRequest.IdUser,
                    MoneyTrans = withdrawRequest.Money,
                    MethodTrans = "refund_trans",
                    TypeTrans = "refund_trans",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(usertrans);
                await _repositoryManager.SaveAsync();
                return 1;
            }
            catch (Exception e)
            {

                return -1;
            }
        }

        public async Task<int> AcceptRequestWithDrawStatus(int requestId)
        {
            var withdrawRequest = await _repositoryManager.WithdrawDetail.FindByCondition(x => x.Id == requestId, false).FirstOrDefaultAsync();
            var withdrawTrans = await _repositoryManager.Transaction.FindByCondition(x => x.IdUser == withdrawRequest.IdUser, false).FirstOrDefaultAsync();
            if (withdrawRequest == null || withdrawTrans == null)
            {
                return 0;
            }
            try
            {
                withdrawTrans.Status = (int)TransactionStatus.PaymentSuccess;
                withdrawRequest.Status = (int)WithdrawStatus.Seccesss;
                _repositoryManager.WithdrawDetail.Update(withdrawRequest);
                await _repositoryManager.SaveAsync();
                return 1;
            }
            catch (Exception e)
            {

                return -1;
            }
        }


        public async Task<List<WithdrawDetailResponse>> GetListWithRequest()
        {
           var withdrawRequest = await _repositoryManager.WithdrawDetail.FindAll(false).Select(x=>new WithdrawDetailResponse()
           {
               Id=x.Id,
               IdUser=x.IdUser,
               Money = x.Money,
               CreateDate = x.CreateDate.ToString("dd/MM/yyyy HH:mm"),
               AcceptDate = x.AcceptDate.ToString("dd/MM/yyyy HH:mm"),
               Status=x.Status,
               BankName=x.BankName,
               AccountName=x.AccountName,
               BankNumber=x.BankNumber,

           }).OrderByDescending(x => x.Id).ToListAsync();
           return withdrawRequest;
        }

    }
}
