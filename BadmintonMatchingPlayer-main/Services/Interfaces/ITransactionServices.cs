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
    public interface ITransactionServices
    {
        Task<Transaction> CreateForBuySlot(TransactionCreateInfo info);
        Task DeleteSlot(int transaction_id);
        Task DeleteTran(int transaction_id);
        bool ExistTran(int tran_id);
        Task<TransactionDetail> GetDetail(int transaction_id);
        Task<List<TransactionInfo>> GetOfUser(int user_id);
        Task<Transaction> GetTransaction(int transaction_id);
        bool IsFromTwoPost(List<int>? idSlot);
        Task UpdateStatus(int tran_id, TransactionStatus tranStatus);
        Task<int> CreateWithdrawRequest(CreateWithdrawRequest createWithdrawRequest);
        Task<List<WithdrawDetailResponse>> GetListWithRequest();
        Task<int> AcceptRequestWithDrawStatus(int requestId);
        Task<int> DeniedRequestWithDrawStatus(int requestId);
    }
}
