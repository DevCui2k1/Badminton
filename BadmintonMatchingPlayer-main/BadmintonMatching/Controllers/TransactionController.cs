using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Services.Interfaces;

namespace BadmintonMatching.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionServices _transactionRepository;
        private readonly IUserServices _userServices;

        public TransactionController(ITransactionServices transactionRepository, IUserServices userServices)
        {
            _transactionRepository = transactionRepository;
            _userServices = userServices;
        }

        [HttpPost]
        [Route("buy_slot")]
        public async Task<IActionResult> CreateTransactionBuyingSlot(TransactionCreateInfo info)
        {
            if (_transactionRepository.IsFromTwoPost(info.IdSlot))
            {
                return Ok(new SuccessObject<object> { Message = "Không thể tạo từ các vị trí có nhiều hơn 1 bài đăng !" });
            }
            var tranId = await _transactionRepository.CreateForBuySlot(info);
            if (tranId.Id == 0)
            {
                return Ok(new SuccessObject<object>{ Message = "Tạo giao dịch thất bại !" });
            }
            else
            {
                return Ok(new SuccessObject<object> { Data = new { TranSactionId = tranId }, Message = Message.SuccessMsg });
            }
        }

       

        [HttpPut]
        [Route("{tran_id}/status_info/{status_info}")]
        [ProducesResponseType(typeof(SuccessObject<List<Reports>>), 200)]
        public async Task<IActionResult> SuccessPayment(int tran_id, int status_info)
        {
            if (_transactionRepository.ExistTran(tran_id))
            {
                await _transactionRepository.UpdateStatus(tran_id, (TransactionStatus)status_info);               
                return Ok(new SuccessObject<object> { Message = "Cập nhật thành công", Data = true });
            }
            else
            {
                return Ok(new SuccessObject<object> { Message = "Giao dịch khôn tồn tại !" });
            }
        }

        [HttpGet]
        [Route("user/{user_id}")]
        [ProducesResponseType(typeof(SuccessObject<List<TransactionInfo>>), 200)]
        public async Task<IActionResult> GetTransactionOfUser(int user_id)
        {
            if (!_userServices.ExistUserId(user_id))
            {
                return Ok(new SuccessObject<List<TransactionInfo?>> { Message = "Người dùng không tồn tại !" });
            }

            var data = await _transactionRepository.GetOfUser(user_id);
            return Ok(new SuccessObject<List<TransactionInfo>> { Data= data, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{transaction_id}/detail")]
        [ProducesResponseType(typeof(SuccessObject<TransactionDetail>), 200)]
        public async Task<IActionResult> GetTransactionDetail(int transaction_id)
        {
            var data = await _transactionRepository.GetDetail(transaction_id);
            return Ok(new SuccessObject<TransactionDetail> { Data= data, Message = Message.SuccessMsg });
        }

        [HttpDelete]
        [Route("{transaction_id}/discard")]
        public async Task<IActionResult> DiscardTransaction(int transaction_id)
        {
            var transaction = await _transactionRepository.GetTransaction(transaction_id);
            if(transaction.Id > 0)
            {
                if(transaction.Status != (int)TransactionStatus.ReportResolved && transaction.Status != (int)TransactionStatus.Played)
                {
                    await _transactionRepository.DeleteSlot(transaction_id);
                    await _transactionRepository.DeleteTran(transaction_id);
                    return Ok(new SuccessObject<object> { Message = Message.SuccessMsg, Data = true });
                }
                else
                {
                    return Ok(new SuccessObject<object> { Message = "Giao dịch đã hoàn tất không được phép xóa !" });
                }
            }
            else
            {
                return Ok(new SuccessObject<object> { Message = "Giao dịch khôn tồn tại ! id" });
            }
        }


        [HttpPut]
        [Route("create_withdraw_request")]
        public async Task<IActionResult> WithdawRequest(CreateWithdrawRequest createWithdrawRequest)
        {
            var transaction = await _transactionRepository.CreateWithdrawRequest(createWithdrawRequest);

            if (transaction == 0 )
            {
                return Ok(new SuccessObject<object> { Message = "Tạo yêu cầu rút tiền thất bại !" });
            }else if(transaction == -1)
            {
                return Ok(new SuccessObject<object> { Message = "Số tiền trong ví không đủ để rút !" });
            }
            return Ok(new SuccessObject<object> { Message = Message.SuccessMsg, Data = new { id=transaction} });
        }

        [HttpGet]
        [Route("withdraw_request")]
        public async Task<IActionResult> GetListWithdrawRequest()
        {
            var listRequest =await _transactionRepository.GetListWithRequest();
            if (listRequest==null)
            {
                return Ok(new SuccessObject<List<WithdrawDetailResponse?>> { Message = "Không có yêu cầu rút tiền nào !" });
            }
         
            return Ok(new SuccessObject<List<WithdrawDetailResponse>> { Data = listRequest, Message = Message.SuccessMsg });
        }

        [HttpPut]
        [Route("{id_request}/accept_withdraw_request")]
        public async Task<IActionResult> AcceptWithdawRequest(int id_request)
        {
            var transaction = await _transactionRepository.AcceptRequestWithDrawStatus(id_request);

            if (transaction == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Yêu cầu rút tiền không hợp lệ !" });
            }
            else if (transaction == -1)
            {
                return Ok(new SuccessObject<object> { Message = "Thanh toán thất bại !" });
            }
            return Ok(new SuccessObject<object> { Message = Message.SuccessMsg, Data = new { id = transaction } });
        }

        [HttpPut]
        [Route("{id_request}/denied_withdraw_request")]
        public async Task<IActionResult> DeninWithdawRequest(int id_request)
        {
            var transaction = await _transactionRepository.DeniedRequestWithDrawStatus(id_request);

            if (transaction == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Từ chối yêu cầu thất bại !" });
            }
            else if (transaction == -1)
            {
                return Ok(new SuccessObject<object> { Message = "Hoàn tiền thất bại !" });
            }
            return Ok(new SuccessObject<object> { Message = Message.SuccessMsg, Data = new { id = transaction } });
        }
    }
}
