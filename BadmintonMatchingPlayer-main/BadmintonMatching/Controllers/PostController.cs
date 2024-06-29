using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Intefaces;
using Services.Implements;
using Services.Interfaces;
using System.Text.Json.Nodes;

namespace BadmintonMatching.Controllers
{

    [ApiController]
    [Route("api/posts")]
    public class PostController : ControllerBase
    {
        private List<string> PlaceList = new List<string>
        {

        };
        private readonly IPostServices _postServices;
        private readonly IUserServices _userServices;
        private readonly INotificationServices _notificationServices;
        private readonly IRepositoryManager _repositoryManager;


        public PostController(IPostServices postServices, IUserServices userServices,
            INotificationServices notificationServices, IRepositoryManager repositoryManager
            )
        {
            _postServices = postServices;
            _userServices = userServices;
            _notificationServices = notificationServices;
            _repositoryManager = repositoryManager;

        }

        [HttpGet]
        [Route("user/{user_id}/suggestion")]
        public IActionResult GetSuggestionPost(int user_id)
        {
            if (!_userServices.ExistUserId(user_id))
            {
                return Ok(new SuccessObject<List<PostInfomation?>> { Message = "Không thể tìm thấy người dùng !" });
            }
            var res = _postServices.GetSuggestionPost(user_id);
            return Ok(new SuccessObject<List<PostInfomation>> { Data = res, Message = Message.SuccessMsg });
        }

        [HttpPost]
        [Route("create_by/{user_id}")]
        public async Task<IActionResult> CreatePost(int user_id, NewPostInfo info)
        {
            if (!_userServices.ExistUserId(user_id))
            {
                Ok(new SuccessObject<object> { Message = "Không thể tìm thấy người dùng !" });
            }
            var postId = await _postServices.CreatePost(user_id, info);
            if (postId == -1)
            {
                return Ok(new SuccessObject<object> { Message = "Chuỗi base64 không hợp lệ !" });
            }
            else if (postId != 0)
            {
                var subIds = await _userServices.GetSubcribeUser(user_id);
                await _notificationServices.SendNotification(subIds, "Hoạt động mới", "Một người mà bạn đăng kí vừa đăng bài", NotificationType.Post, postId);

                return Ok(new SuccessObject<object> { Data = new { PostId = postId }, Message = Message.SuccessMsg });
            }
            else
            {
                return Ok(new SuccessObject<object> { Message = "Lưu thất bại !" });
            }

        }


        [HttpPut]
        [Route("update_post/{post_id}")]
        public async Task<IActionResult> UpdatePost(int post_id, NewPostInfo info)
        {
           
            var res = await _postServices.UpdatePost(post_id, info); 
            if (res == -1)
            {
                return Ok(new SuccessObject<object> { Message = "Bài đăng không tồn tại !" });
            }   else if (res == 1)
            {
                return Ok(new SuccessObject<object> { Data=new {post_id},Message = "Bài đăng không tồn tại !" });
            }     
            else
            {
                return Ok(new SuccessObject<object> { Message = "Lưu thất bại !" });
            }
        }


        [HttpPut]
        [Route("{user_id}/create_post_charge")]
        public async Task<IActionResult> CheckAvailableWalletMoney(int user_id)
        {

            var updateWalletCheck = await _postServices.UpdateFreePosting(user_id);
            if (updateWalletCheck == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Số tiền trong ví không đủ  để thanh toán !" });
            }
            else if (updateWalletCheck == -1)
            {
                return Ok(new SuccessObject<object> { Message = $"Thanh toán lỗi !" });
            }
            return Ok(new SuccessObject<CreateChargerResponse>
            {
                Data = new CreateChargerResponse
                {
                    isUser = user_id,
                },
                Message = Message.SuccessMsg
            });
                

        }


        [HttpGet]
        [Route("play_ground/{play_ground}")]
        public IActionResult GetPostByPlayGround(string play_ground)
        {
            List<PostOptional> res = _postServices.GetPostByPlayGround(play_ground);
            return Ok(new SuccessObject<List<PostOptional>> { Data = res, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{user_id}/managed_all_post")]
        public IActionResult GetPostByPlayGround(int user_id)
        {
            if (!_userServices.ExistUserId(user_id))
            {
                return Ok(new SuccessObject<List<PostInfomation?>> { Message = "Không thể tìm thấy người dùng !" });
            }

            List<PostInfomation> res = new List<PostInfomation>();

            if (_userServices.IsAdmin(user_id))
            {
                res = _postServices.GetManagedPostAdmin(user_id);
            }
            else
            {
                res = _postServices.GetManagedPost(user_id);
            }


            return Ok(new SuccessObject<List<PostInfomation>> { Data = res, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{post_id}/details")]
        public IActionResult GetDetailPost(int post_id)
        {
            var res = _postServices.GetPostDetail(post_id);
            return Ok(new SuccessObject<PostDetail> { Data = res, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("{user_id}/post_suggestion")]
        public IActionResult GetListOptionalPost()
        {
            var res = _postServices.GetListOptionalPost();
            return Ok(new SuccessObject<List<PostOptional>> { Data = res, Message = Message.SuccessMsg });
        }

        [HttpGet]
        [Route("GetListPost")]
        public async Task<IActionResult> GetListPost()
        {
            var res = await _postServices.GetAllPost();
            return Ok(new SuccessObject<List<PostOptional>> { Data = res, Message = Message.SuccessMsg });
        }

        #region List Post at role Admin
        [HttpGet]
        [Route("{admin_id}/post")]
        public IActionResult GetListPostByAdmin(int admin_id)
        {

            var checkadmin = _userServices.IsAdminAndStaff(admin_id);
            if (checkadmin)
            {
                var res = _postServices.GetListPostByAdmin();
                return Ok(new SuccessObject<List<ListPostByAdmin>> { Data = res, Message = Message.SuccessMsg });
            }
            else
            {

                return Ok(new SuccessObject<List<ListPostByAdmin?>> { Message = "Not admin" });
            }
        }
        #endregion

        #region Delete Post byAdmin
        [HttpPut]
        [Route("{admin_id}/delete/{post_id}")]
        public async Task<IActionResult> DeletePost(int post_id, int admin_id)
        {
            if (!_userServices.IsAdmin(admin_id))
            {
                if (!_userServices.IsPostOwner(admin_id, post_id))
                {
                    return Ok(new SuccessObject<object> { Message = "Bạn không có quyền xóa" });
                }
            }
          
            var res =await _postServices.DeletePostAsync(post_id);
            if (!res)
            {

                return Ok(new SuccessObject<object> { Message = "Bài viết đã có người đặt chổ, bạn cần liên hệ với người đặt để xóa !" });
            }

            return Ok(res ? new SuccessObject<object> { Data = true, Message = Message.SuccessMsg } : new SuccessObject<object> { Message = "Cập nhật thất bại" });
        }
        #endregion

        [HttpGet]
        [Route("user/{user_id}/joined")]
        public async Task<IActionResult> GetJoinedPost(int user_id)
        {
            List<JoinedPost> res = await _postServices.GetJoined(user_id);
           
            if (res != null)
            {
                return Ok(new SuccessObject<List<JoinedPost>> { Data = res, Message = Message.SuccessMsg });
            }
            else
            {
                return Ok(new SuccessObject<List<JoinedPost>?> { Message = "Yêu cầu không tồn tại !" });
            }
        }

        [HttpGet]
        [Route("{post_id}/chat_rooms")]
        public async Task<IActionResult> GetChatRooms(int post_id)
        {
            try
            {
                List<Room> rooms = await _postServices.GetChatRooms(post_id);
                return Ok(new SuccessObject<List<Room>> { Data = rooms, Message = Message.SuccessMsg });
            }
            catch (NotImplementedException)
            {
                return Ok(new SuccessObject<object> { Data = null, Message = "Bài đăng không tồn tại !" });
            }
        }

        [HttpPost]
        [Route("notice/to/{post_id}")]
        public async Task<IActionResult> SendNotices(int post_id, NoticesRequest info)
        {
            var user_id = await _postServices.GetUserId(post_id);
            await _notificationServices.SendNotification(user_id, "Nhắc nhở từ Admin", info.Message, NotificationType.User, post_id);
            return Ok(new SuccessObject<object> { Message = "Nhắc nhở thành công", Data = true });
        }

        [HttpPut]
        [Route("{user_id}/check_user_post")]
        public async Task<IActionResult> CheckUserPostInMonth(int user_id)
        {
            var setting = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == (int)SettingType.NumberPostFree,false).FirstOrDefaultAsync();
            
            var free = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == (int)SettingType.PostingSetting, false).FirstOrDefaultAsync();        
            try
            {             
                    return Ok(new SuccessObject<CreateChargerResponse> {
                        Data = new CreateChargerResponse()
                        {
                           isUser = user_id,
                        }, Message = Message.SuccessMsg
                    } );

               

            }
            catch (NotImplementedException)
            {
                return Ok(new SuccessObject<object> { Data = null, Message = "Bài đăng không tồn tại" });
            }
        }

 
        [HttpPut]
        [Route("{user_id}&{post_id}/create_boost_charge")]
        public async Task<IActionResult> CheckAvailableWalletMoneyForBoost(int user_id,int post_id)
        {

            var updateWalletCheck = await _postServices.UpdateBoost(user_id, post_id);
             if (updateWalletCheck == -1)
            {
                return Ok(new SuccessObject<object> { Message = $"Đẩy tin lỗi !" });
            }
            else if (updateWalletCheck == 0)
            {
                return Ok(new SuccessObject<object> { Message = "Sô dư trong ví không đủ  !" });
            }
            else if (updateWalletCheck == 2)
            {
                return Ok(new SuccessObject<object> { Message = "Bài viết không còn hoạt động  !" });
            }
            return Ok(new SuccessObject<CreateChargerResponse>
            {
                Data = new CreateChargerResponse
                {
                    isUser = user_id,
                },
                Message = Message.SuccessMsg
            });
        }

        [HttpGet]
        [Route("{user_id}/post_ai_suggestion")]
        public async Task<IActionResult> GetAiIdPost(int user_id)
        {
            var res =await _postServices.GetPostAiSuggest(user_id);
            return Ok(new SuccessObject<List<PostOptional>> { Data = res, Message = Message.SuccessMsg });
        }

        //[HttpPut]
        //[Route("{postId}/boost_post")]
        //public async Task<IActionResult> BoostPost(int postId)
        //{
        //    var isSeccess = await _postServices.BoostPost(postId);
        //        if (!isSeccess)
        //        {
        //            return Ok(new SuccessObject<object> { Message = "Boost post fail !" });
        //       }
        //    return Ok(new SuccessObject<CreateChargerResponse>
        //    {
        //        Data = new CreateChargerResponse()
        //        {
        //            isUser = postId,
        //        },
        //        Message = Message.SuccessMsg
        //    });
        //}
    }
}
