using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Intefaces;
using Services.Interfaces;
using System.Data;
using System.Globalization;
using System.Net.WebSockets;
using System.Xml.Linq;


using Accord.Statistics.Analysis;
using Accord.Statistics.Filters;
using Accord.IO;
using Accord.Math;
using Accord.MachineLearning;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Services.Implements
{
    public enum PostType
    {
        MatchingPost = 1,
        Blog = 2
    }

    public class TempTransaction
    {
        public int Id { get; set; }
        public decimal? MoneyTrans { get; set; }
        public int? Status { get; set; }
        public List<string> ContentSlots { get; set; }
    }


    public class PostServices : IPostServices
    {
        private readonly IRepositoryManager _repositoryManager;
        private readonly IUserServices _userServices;


        public PostServices(IRepositoryManager repositoryManager,
                            IUserServices userServices)
        {
            _repositoryManager = repositoryManager;
            _userServices = userServices;


        }

        public async Task<string> HandleImg(string base64encodedstring)
        {
            var revertbase64 = base64encodedstring.Replace("data:image/jpeg;base64,", "");
            try
            {
                var bytes = Convert.FromBase64String(revertbase64);
                var contents = new StreamContent(new MemoryStream(bytes));
                Account account = new Account(
                    "dgx21lq5x",
                    "282428924532382",
                    "0WvIKDeB9cy-foUumrFN1dab664");
                Cloudinary cloudinary = new Cloudinary(account);

                string publicId = Guid.NewGuid().ToString();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(publicId, new MemoryStream(bytes)),
                    PublicId = publicId
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                return uploadResult.SecureUrl.ToString();

            }
            catch (Exception ex)
            {
                throw new Exception();
            }

        }

        public async Task<int> CreatePost(int user_id, NewPostInfo info)
        {
            string[] timeSlots = {
            "6h-8h",
            "8h-10h",
            "10h-12h",
            "12h-14h",
            "14h-16h",
            "16h-18h",
            "18h-20h",
            "20h-22h"
        };

            var urls = "";
            foreach (var url in info.ImgUrls)
            {
                try
                {
                    urls += $"{await HandleImg(url)};";
                }
                catch
                {
                    return -1;
                }
            }

            try
            {
                var newPost = new Post
                {
                    CategorySlot = info.CategorySlot,
                    LevelSlot = info.LevelSlot,
                    Title = info.Title,
                    AddressSlot = info.Address,
                    ContentPost = info.Description,
                    SavedDate = DateTime.UtcNow.AddHours(7),
                    ImgUrl = await HandleImg(info.HighlightUrl),
                    ImageUrls = info.ImgUrls[1],
                    IdUserTo = user_id,                  
                    IdType = (int)PostType.MatchingPost,
                    Status = true
                };

                _repositoryManager.Post.Create(newPost);
                await _repositoryManager.SaveAsync();


                foreach (String slotDate in info.SlotInfor.TimeSlot)
                {
                    DateTime parsedDate = DateTime.Parse(slotDate);
                    foreach (String timeSlot in timeSlots)
                    {
                        SlotPost slot = new SlotPost();
                        slot.IdPost = newPost.Id;
                        slot.ContextPost = timeSlot;
                        slot.SlotDate = parsedDate;
                        slot.SlotPrice = info.SlotInfor.Price;
                        _repositoryManager.SlotPost.Create(slot);

                    }
                }           
                await _repositoryManager.SaveAsync();
                
                return newPost.Id;
            }
            catch
            {
                return -1;
            }
        }



        public async Task<bool> DeletePostAsync(int post_id)
        {
            var post =await _repositoryManager.Post.FindByCondition(x => x.Id == post_id && !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, true).FirstOrDefaultAsync();
            if (post != null)
            {
                post.IsDeleted = true;
                await _repositoryManager.SaveAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteBlogAsync(int post_id)
        {
            var post = await _repositoryManager.Post.FindByCondition(x => x.Id == post_id && !x.IsDeleted && x.IdType == (int)PostType.Blog, true).FirstOrDefaultAsync();
            if (post != null)
            {
                post.IsDeleted = true;
                await _repositoryManager.SaveAsync();
                return true;
            }
            return false;
        }

        public List<PostOptional> GetListOptionalPost()
        {
            var optList = _repositoryManager.Post.FindByCondition(x => !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, true)
               .OrderByDescending(x => x.SavedDate)
               .Include(x => x.IdUserToNavigation)
               .ToList();
            var res = new List<PostOptional>();
            foreach (var post in optList)
            {
                if (IsPostValid(post))
                {
                    res.Add(new PostOptional
                    {
                        IdPost = post.Id,
                        Title = post.Title,
                        AddressSlot = post.AddressSlot,
                        ContentPost = post.ContentPost,
                        ImgUrlPost = post.ImgUrl,
                        FullName = post.IdUserToNavigation?.FullName,
                        UserImgUrl = post.IdUserToNavigation?.ImgUrl,
                        HighlightUrl = post.ImgUrl,
                        UserId = post.IdUserTo
                    });
                }
            }

            for (var i = 0; i < res.Count(); i++)
            {
                var cPost = res[i];
                var post = _repositoryManager.Post.FindByCondition(x => x.Id == cPost.IdPost, false).OrderByDescending(x=>x.SavedDate).Include(x => x.Slots).FirstOrDefault();
                if (post != null)
                {
                    GetPostOptional(post, ref cPost);
                }
                res[i] = cPost;
            }

            var returnList = new List<PostOptional>();
            foreach (var postOptional in res)
            {
                if (postOptional.QuantitySlot != null && postOptional.QuantitySlot != 0)
                {
                    returnList.Add(postOptional);
                }
            }
            return returnList;
        }
        public async Task<List<PostOptional>> GetAllPost()
        {
            var listpost = await _repositoryManager.Post.FindByCondition(x => !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, false).OrderByDescending(x => x.SavedDate).Include(x => x.IdUserToNavigation).ToListAsync();
            var res = new List<PostOptional>();
            foreach (var post in listpost)
            {
                if (IsPostValid(post))
                {
                    res.Add(new PostOptional
                    {
                        IdPost = post.Id,
                        Title = post.Title,
                        AddressSlot = post.AddressSlot,
                        ContentPost = post.ContentPost,
                        ImgUrlPost = post.ImgUrl,
                        FullName = post.IdUserToNavigation?.FullName,
                        UserImgUrl = post.IdUserToNavigation?.ImgUrl,
                        HighlightUrl = post.ImgUrl,
                        UserId = post.IdUserTo
                    });
                }
            }

            for (var i = 0; i < res.Count(); i++)
            {
                var cPost = res[i];
                var post = _repositoryManager.Post.FindByCondition(x => x.Id == cPost.IdPost, false).OrderByDescending(x => x.SavedDate).Include(x => x.Slots).FirstOrDefault();

                if (post != null)
                {

                    GetPostOptional(post, ref cPost);
                }
                res[i] = cPost;
            }

            var returnList = new List<PostOptional>();
            foreach (var postOptional in res)
            {
                if (postOptional.QuantitySlot != null && postOptional.QuantitySlot != 0)
                {
                    returnList.Add(postOptional);
                }
            }
      


            return returnList;
        }

        private bool IsPostValid(Post post)
        {
            //var lastSlot = new SlotInfo(post.SlotsInfo.Split(";")[0]);


            //foreach (var slot in post.SlotsInfo.Split(";"))
            //{
            //    if (slot != string.Empty)
            //    {
            //        //var slotInfo = new SlotInfo(slot);

            //        //var joinSlot = _repositoryManager.Slot
            //        //    .FindByCondition(x =>
            //        //    !x.IsDeleted &&
            //        //    x.ContentSlot == slotInfo.StartTime.Value.ToString("dd/MM/yyyy") &&
            //        //    x.IdPost == post.Id, false).Count();


            //        //if (slotInfo.AvailableSlot - joinSlot >= 0)
            //        //{
            //        //    if (slotInfo.StartTime > lastSlot.StartTime)
            //        //    {
            //        //        lastSlot = slotInfo;
            //        //    }
            //        //}
            //        //else
            //        //{
            //        //    if (slotInfo.AvailableSlot - joinSlot <= 0)
            //        //    {
            //        //        return false;
            //        //    }
            //        //    if (slotInfo.StartTime > lastSlot.StartTime)
            //        //    {
            //        //        lastSlot = slotInfo;
            //        //    }
            //        //}


            //    }
            //}

            //if (lastSlot.StartTime < DateTime.UtcNow)
            //{
            //    return false;
            //}
            return true;
        }

        private void GetPostOptional(Post post, ref PostOptional optPost)
        {
            List<SlotInfor> l_slotInfor = new List<SlotInfor>();
            //int joinedSlot = 0;
            //foreach (var slot in post.SlotsInfo.Split(";"))
            //{
            //    var finalInfo = new SlotInfo();
            //    if (slot != string.Empty)
            //    {
            //        var slotInfo = new SlotInfo(slot);
            //        var joinSlot = _repositoryManager.Slot
            //            .FindByCondition(x =>
            //            !x.IsDeleted &&
            //            x.ContentSlot == slotInfo.StartTime.Value.ToString("dd/MM/yyyy") &&
            //            x.IdPost == post.Id, false).Count();
            //        if (slotInfo.AvailableSlot - joinSlot >= finalInfo.AvailableSlot)
            //        {
            //            finalInfo = slotInfo;
            //            joinedSlot = joinSlot;
            //        }
            //    }

            //    SlotInfor slotInfor = new SlotInfor()
            //    {
            //        IdPost = optPost.IdPost,
            //        Days = finalInfo.StartTime.Value.ToString("dd/MM/yyyy"),
            //        StartTime = finalInfo.StartTime.Value.ToString("HH:mm"),
            //        EndTime = finalInfo.EndTime.Value.ToString("HH:mm"),
            //        QuantitySlot = finalInfo.AvailableSlot - joinedSlot,
            //        Price = finalInfo.Price

            //    };
            //    DateTime slotDateTime = DateTime.ParseExact(slotInfor.Days + " " + slotInfor.StartTime, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            //    if (slotDateTime >= DateTime.Now && slotInfor.QuantitySlot > 0 && slotInfor.QuantitySlot != null)
            //    {
            //        l_slotInfor.Add(slotInfor);
            //    }
            //}

            //l_slotInfor = l_slotInfor.OrderBy(s => s.Days).ToList();

            //if (l_slotInfor.Count != 0)
            //{
            //    optPost.Days = l_slotInfor[0].Days;
            //    optPost.StartTime = l_slotInfor[0].StartTime;
            //    optPost.EndTime = l_slotInfor[0].EndTime;
            //    optPost.QuantitySlot = l_slotInfor[0].QuantitySlot;
            //    optPost.Price = l_slotInfor[0].Price;
            //}
        }

        public List<ListPostByAdmin> GetListPostByAdmin()
        {
            return _repositoryManager.Post.FindByCondition(x => x.IdType == (int)PostType.MatchingPost, true)
                .OrderByDescending(x => x.SavedDate)
                .Include(x => x.IdUserToNavigation).ThenInclude(x => x.UserRoleNavigation)
                .Select(x => new ListPostByAdmin
                {
                    CreatedDate = x.SavedDate,
                    FullName = x.IdUserToNavigation.FullName,
                    IdUser = x.IdUserTo,
                    RoleUser = x.IdUserToNavigation.UserRoleNavigation.RoleName,
                    Status = x.Status,
                    TotalViewer = x.TotalViewer.ToString(),
                    IsDeleted = x.IsDeleted
                }).ToList();
        }

        public List<PostInfomation> GetManagedPost(int user_id)
        {
            var listPost = _repositoryManager.Post.FindByCondition(x => x.IdUserTo == user_id && !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, true)
                .OrderByDescending(x => x.SavedDate)
                .Include(x => x.IdUserToNavigation)
                .Include(x => x.Slots)
                .Select(x => new PostInfomation(x)).ToList();
            return listPost;
        }

        public List<PostInfomation> GetManagedPostAdmin(int user_id)
        {
            return _repositoryManager.Post.FindByCondition(x => x.IdUserTo == user_id && !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, true)
                .OrderByDescending(x => x.SavedDate)
                .Include(x => x.IdUserToNavigation)
                .Include(x => x.Slots)
                .Select(x => new PostInfomation(x)).ToList();
        }

        public List<PostOptional> GetPostByPlayGround(string play_ground)
        {
            var listpost = _repositoryManager.Post.FindByCondition(x => x.AddressSlot != null
                && x.AddressSlot.Contains(play_ground)
                && !x.IsDeleted
                && x.IdType == (int)PostType.MatchingPost, false)
                .Include(x => x.IdUserToNavigation)
                .OrderByDescending(x => x.Id)
                .Select(x => new PostOptional
                {
                    IdPost = x.Id,
                    Title = x.Title,
                    AddressSlot = x.AddressSlot,
                    ContentPost = x.ContentPost,
                    ImgUrlPost = x.ImgUrl,
                    FullName = x.IdUserToNavigation.FullName,
                    UserImgUrl = x.IdUserToNavigation.ImgUrl,
                    HighlightUrl = x.ImgUrl,
                    UserId = x.IdUserTo
                })
                .ToList();

            var res = new List<PostOptional>();
            for (var i = 0; i < listpost.Count(); i++)
            {
                var cPost = listpost[i];
                var post = _repositoryManager.Post.FindByCondition(x => x.Id == cPost.IdPost, false).Include(x => x.Slots).FirstOrDefault();
                if (post != null)
                {
                    GetPostOptional(post, ref cPost);
                }
                res.Add(cPost);
            }
            return res;
        }

        public PostDetail GetPostDetail(int id_post)
        {
            var x = _repositoryManager.Post
                .FindByCondition(x =>
                x.Id == id_post
                && !x.IsDeleted
                && x.IdType == (int)PostType.MatchingPost, true)
                .Include(x => x.IdUserToNavigation)
                .Include(x => x.Slots)
                .Include(x => x.SlotsPost)
                .FirstOrDefault();
            if (x != null)
            {
                x.TotalViewer++;
                _repositoryManager.SaveAsync().Wait();
                var postDetail = new PostDetail(x);
                postDetail.ImageUrls = x.ImageUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                return postDetail;
            }
            return new PostDetail();
        }

        public List<PostInfomation> GetSuggestionPost(int user_id)
        {
            var res = new List<PostInfomation>();
            var user = _repositoryManager.User.FindByCondition(x => x.Id == user_id, true).FirstOrDefault();
            if (user != null && user.PlayingArea != null)
            {
                var posts = _repositoryManager.Post.FindByCondition(
                    x => x.AddressSlot != null
                    && user.PlayingArea.Contains(x.AddressSlot)
                    && !x.IsDeleted
                    && x.IdType == (int)PostType.MatchingPost
                        , true)
                    .Include(x => x.IdUserToNavigation)
                    .Include(x => x.Slots)
                    .ToList();

                foreach (var item in posts)
                {
                    if (IsPostValid(item))
                    {
                        res.Add(new PostInfomation(item));
                    }
                }
            }
            return res;
        }

        public async Task<List<JoinedPost>> GetJoined(int user_id)
        {
            var res = new List<JoinedPost>();

            //var transactions = await _repositoryManager.Transaction
            //    .FindByCondition(x => x.IdUser == user_id, false)
            //    .Include(x => x.Slots)
            //    .Select(x => new TempTransaction
            //    {
            //        Id = x.Id,
            //        MoneyTrans = x.MoneyTrans,
            //        Status = x.Status,
            //        ContentSlots = x.Slots.Select(y => y.ContentSlot).ToList()
            //    })
            //    .OrderByDescending(x => x.Id)
            //    .ToListAsync();
            //if (transactions == null || transactions.Count == 0)
            //{
            //    return res;
            //}

            //foreach (var item in transactions)
            //{
            //    var post = await _repositoryManager.Post.FindByCondition(x => x.Slots.Any(y => y.TransactionId == item.Id) && !x.IsDeleted, false)
            //        .Include(x => x.Slots.Where(y => item.ContentSlots.Contains(y.ContentSlot)))
            //        .ThenInclude(x => x.User)
            //        .FirstOrDefaultAsync();

            //    if (post == null)
            //        continue;

            //    var finalInfo = new SlotInfo();
            //    var firstInfo = new SlotInfo()
            //    {
            //        StartTime = DateTime.MaxValue
            //    };
            //    int joinedSlot = 0;
            //    var bookedInfos = new List<BookedSlotInfo>();
            //    foreach (var infoStr in post.SlotsInfo.Split(';'))
            //    {
            //        if (infoStr != string.Empty)
            //        {
            //            var info = new SlotInfo(infoStr);
            //            bookedInfos.Add(new BookedSlotInfo
            //            {
            //                BookedSlot = post.Slots.Where(x => x.ContentSlot == info.StartTime.Value.ToString("dd/MM/yyyy")).Count(),
            //                CreateSlot = info.AvailableSlot,
            //                ImageUrls = post.Slots.Where(x => x.ContentSlot == info.StartTime.Value.ToString("dd/MM/yyyy")).Select(x => x.User.ImgUrl).ToList()
            //            });

            //            var joinSlot = _repositoryManager.Slot
            //                .FindByCondition(x =>
            //                !x.IsDeleted &&
            //                x.ContentSlot == info.StartTime.Value.ToString("dd/MM/yyyy") &&
            //                x.IdPost == post.Id, false).Count();

            //            if (info.StartTime < firstInfo.StartTime)
            //            {
            //                firstInfo = info;
            //            }
            //            if (info.AvailableSlot - joinSlot >= finalInfo.AvailableSlot)
            //            {
            //                finalInfo = info;
            //                joinedSlot = joinSlot;
            //            }
            //        }
            //    }

            //    var isCancel = firstInfo.StartTime.Value.AddHours(-24) >= DateTime.UtcNow.AddHours(7)
            //        && (item.Status == (int)TransactionStatus.Processing || item.Status == (int)TransactionStatus.PaymentSuccess);
            //    var roomId = _repositoryManager.ChatRoom
            //        .FindByCondition(x => x.Code == $"{post.Id}_{finalInfo.StartTime.Value.ToString("dd/MM/yyyy")}", false)
            //        .Select(x => x.Id).FirstOrDefault();

            //    Dictionary<TransactionStatus, string> vietnameseStatus = CreateVietnameseStatusDictionary();

            //    res.Add(new JoinedPost
            //    {
            //        AreaName = post.AddressSlot,
            //        MoneyPaid = item.MoneyTrans,
            //        TransacionId = item.Id,
            //        BookedInfos = bookedInfos,
            //        PostId = post.Id,
            //        Status = vietnameseStatus[(TransactionStatus)item.Status],
            //        PostTitle = post.Title,
            //        AvailableSlot = (finalInfo.AvailableSlot - joinedSlot).ToString(),
            //        EndTime = finalInfo.EndTime.Value.ToString("dd/MM/yyyy hh:mm:ss tt"),
            //        StartTime = finalInfo.StartTime.Value.ToString("dd/MM/yyyy hh:mm:ss tt"),
            //        CoverImage = post.ImgUrl,
            //        CanReport = DateTime.UtcNow.AddHours(7) < finalInfo.EndTime.Value,
            //        ChatRoomUrl = $"https://badminton-matching-24832d1c4b03.herokuapp.com/chatHub?id={roomId}",
            //        ChatRoomId = roomId,
            //        IsCancel = isCancel
            //    });
            //}
            return res;
        }
        public static Dictionary<TransactionStatus, string> CreateVietnameseStatusDictionary()
        {
            Dictionary<TransactionStatus, string> vietnameseStatus = new Dictionary<TransactionStatus, string>
        {
            { TransactionStatus.Processing, "Đang xử lý" },
            { TransactionStatus.PaymentSuccess, "Thanh toán thành công" },
            { TransactionStatus.PaymentFailure, "Thanh toán thất bại" },
            { TransactionStatus.Played, "Đã chơi" },
            { TransactionStatus.Reporting, "Đang báo cáo" },
            { TransactionStatus.ReportResolved, "Báo cáo đã giải quyết" }
        };

            return vietnameseStatus;
        }
        public async Task<List<Room>> GetChatRooms(int post_id)
        {
            var post = await _repositoryManager.Post.FindByCondition(x => x.Id == post_id && x.IdType == (int)PostType.MatchingPost, false).FirstOrDefaultAsync();

            //if (post == null)
            //{
            //    throw new NotImplementedException();
            //}

            var res = new List<Room>();
            //foreach (var infoStr in post.SlotsInfo.Split(';'))
            //{
            //    var info = new SlotInfo(infoStr);
            //    var room = await _repositoryManager.ChatRoom
            //        .FindByCondition(x =>
            //        x.Name == $"{info.StartTime.Value.ToString("dd/MM/yyyy")}" &&
            //        x.Code == $"{post.Id}_{info.StartTime.Value.ToString("dd/MM/yyyy")}"
            //        , false)
            //        .Select(x => new Room
            //        {
            //            Id = x.Id,
            //            PlayDate = x.Name
            //        }).FirstOrDefaultAsync();

            //    if (room != null)
            //        res.Add(room);
            //}
            return res;
        }

        public async Task<bool> CreateBlog(int user_id, NewBlogInfo info)
        {
            var urls = "";
            foreach (var url in info.ImgUrls)
            {
                try
                {
                    urls += $"{await HandleImg(url)};";
                }
                catch
                {
                    return false;
                }
            }
            var post = new Post
            {
                Title = info.Title,
                ContentPost = info.Description,
                IdUserTo = user_id,
                IdType = (int)PostType.Blog,
                SavedDate = DateTime.UtcNow.AddHours(7),
                IsDeleted = false,
                ImageUrls = urls,
                AddressSlot = info.Summary,
                ImgUrl = await HandleImg(info.HighlightImg),
                TotalViewer = 50,
            };

            _repositoryManager.Post.Create(post);
            await _repositoryManager.SaveAsync();
            return true;
        }

        public async Task<List<BlogInList>> GetAllBlogs(int userId)
        {
            if (_userServices.IsAdmin(userId))
            {
                var adminBlog = await _repositoryManager.Post
                    .FindByCondition(x => x.IdType == (int)PostType.Blog && !x.IsDeleted, false)
                    .Select(x => new BlogInList
                    {
                        Id = x.Id,
                        CreateTime = x.SavedDate.ToString("dd/MM/yyyy hh:mm:ss tt"),
                        ShortDescription = x.ContentPost.Substring(0, 100),
                        Title = x.Title,
                        UserCreateName = x.IdUserToNavigation.FullName,
                        Summary = x.AddressSlot,
                        ImgUrl = x.ImgUrl,

                    })/*.OrderByDescending(x => x.CreateTime)*/
                    .ToListAsync();
                return adminBlog;
            }
            var blogs = await _repositoryManager.Post
                    .FindByCondition(x => x.IdType == (int)PostType.Blog && !x.IsDeleted && x.IdUserTo == userId, false)
                    .Select(x => new BlogInList
                    {
                        Id = x.Id,
                        CreateTime = x.SavedDate.ToString("dd/MM/yyyy hh:mm:ss tt"),
                        ShortDescription = x.ContentPost.Substring(0, 100),
                        Title = x.Title,
                        UserCreateName = x.IdUserToNavigation.FullName,
                        Summary = x.AddressSlot,
                        ImgUrl = x.ImgUrl,

                    })/*.OrderByDescending(x => x.CreateTime)*/
                    .ToListAsync();
            return blogs;
        }

        public async Task<BlogDetail> GetBlogDetail(int blog_id)
        {
            var blog = await _repositoryManager.Post
                .FindByCondition(x => x.IdType == (int)PostType.Blog && x.Id == blog_id && !x.IsDeleted, false)
                .Select(x => new BlogDetail
                {
                    Id = x.Id,
                    CreateTime = x.SavedDate.ToString("dd/MM/yyyy HH:mm"),
                    Description = x.ContentPost,
                    Title = x.Title,
                    UserCreateName = x.IdUserToNavigation.FullName,
                    Summary = x.AddressSlot,
                    ImgUrl = x.ImgUrl
                }).FirstOrDefaultAsync();

            return blog;
        }

        public async Task<int> GetUserId(int post_id)
        {
            return await _repositoryManager.Post.FindByCondition(x => x.Id == post_id, false).Select(x => x.IdUserTo.Value).FirstOrDefaultAsync();
        }


        public async Task<bool> CheckPostInMonth(int user_id)
        {
            var cmonth = DateTime.UtcNow.Month;
            var numberPostFree = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == (int)SettingType.NumberPostFree, false).FirstOrDefaultAsync();
            var listpost = await _repositoryManager.Post.FindByCondition(x => x.IdUserTo == user_id && x.SavedDate.Month == cmonth, false).ToListAsync();
            if (listpost.Count() >= numberPostFree.SettingAmount)
            {
                return false;
            }
            return true;
        }

        public async Task<int> UpdateFreePosting(int userId)
        {
            try
            {
                int adminId = 1;
                var SettingBooking = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == ((int)SettingType.PostingSetting), false).FirstOrDefaultAsync();
                var postingFree = SettingBooking.SettingAmount;
                var userWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == userId, true).FirstOrDefaultAsync();

                if (userWallet.Balance - postingFree < 0)
                {
                    return 0;
                }

                var userTrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = userId,
                    MoneyTrans = postingFree,
                    MethodTrans = "posting_free",
                    TypeTrans = "posting_free",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(userTrans);
                await _repositoryManager.SaveAsync();

                var UsertranHistory = new HistoryTransaction
                {
                    IdUserFrom = userId,
                    IdUserTo = adminId,
                    IdTransaction = userTrans.Id,
                    MoneyTrans = postingFree,
                    Status = true,
                    Deadline = null
                };
                _repositoryManager.HistoryTransaction.Create(UsertranHistory);

                var adminWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == adminId, true).FirstOrDefaultAsync();
                //Create admin transaction
                var admintrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = adminId,
                    MoneyTrans = postingFree,
                    MethodTrans = "posting_free",
                    TypeTrans = "posting_free",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(admintrans);
                await _repositoryManager.SaveAsync();

                var adminTranHistory = new HistoryTransaction
                {
                    IdUserFrom = userId,
                    IdUserTo = adminId,
                    IdTransaction = admintrans.Id,
                    MoneyTrans = postingFree,
                    Status = true,
                    Deadline = null
                };
                _repositoryManager.HistoryTransaction.Create(adminTranHistory);

                //Create admin history wallet
                if (adminWallet != null)
                {
                    adminWallet.Balance += postingFree;


                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = postingFree.ToString(),
                        IdUser = adminId,
                        IdWallet = adminWallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Nhận tiền hoa hồng đăng bài của đơn hàng :  " + userTrans.Id,
                    });
                }


                if (userWallet != null)
                {
                    userWallet.Balance -= postingFree;


                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = "-" + postingFree.ToString(),
                        IdUser = userWallet.IdUser,
                        IdWallet = userWallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Thanh toán phí đăng bài"
                    });
                }

                await _repositoryManager.SaveAsync();
                return 1;
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        public async Task<int> UpdateBoost(int userId, int idPost)
        {
            try
            {
                int adminId = 1;
                var SettingBooking = await _repositoryManager.Setting.FindByCondition(x => x.SettingId == ((int)SettingType.BoostPost), false).FirstOrDefaultAsync();
                var boostfee = SettingBooking.SettingAmount;
                var userWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == userId, true).FirstOrDefaultAsync();

                var post = await _repositoryManager.Post.FindByCondition(x => x.Id == idPost, false).FirstOrDefaultAsync();
                if (post == null)
                {
                    return 2;
                }
                var checkPostIsValid = IsPostValid(post);

                if (userWallet.Balance - boostfee < 0)
                {
                    return 0;
                }
                if (!checkPostIsValid)
                {
                    return 2;
                }

                var userTrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = userId,
                    MoneyTrans = boostfee,
                    MethodTrans = "boosting_free",
                    TypeTrans = "boosting_free",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(userTrans);
                await _repositoryManager.SaveAsync();

                var UsertranHistory = new HistoryTransaction
                {
                    IdUserFrom = userId,
                    IdUserTo = adminId,
                    IdTransaction = userTrans.Id,
                    MoneyTrans = boostfee,
                    Status = true,
                    Deadline = null
                };
                _repositoryManager.HistoryTransaction.Create(UsertranHistory);

                var adminWallet = await _repositoryManager.Wallet.FindByCondition(x => x.IdUser == adminId, true).FirstOrDefaultAsync();
                //Create admin transaction
                var admintrans = new Transaction
                {
                    Id = 0,
                    DeadLine = null,
                    IdUser = adminId,
                    MoneyTrans = boostfee,
                    MethodTrans = "boosting_free",
                    TypeTrans = "boosting_free",
                    TimeTrans = DateTime.UtcNow.AddHours(7),
                    Status = (int)TransactionStatus.PaymentSuccess,
                };
                _repositoryManager.Transaction.Create(admintrans);
                await _repositoryManager.SaveAsync();

                var adminTranHistory = new HistoryTransaction
                {
                    IdUserFrom = userId,
                    IdUserTo = adminId,
                    IdTransaction = admintrans.Id,
                    MoneyTrans = boostfee,
                    Status = true,
                    Deadline = null
                };
                _repositoryManager.HistoryTransaction.Create(adminTranHistory);

                //Create admin history wallet
                if (adminWallet != null)
                {
                    adminWallet.Balance += boostfee;


                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = boostfee.ToString(),
                        IdUser = adminId,
                        IdWallet = adminWallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Nhận tiền đẩy bài đăng của đơn hàng :  " + userTrans.Id,
                    });
                }


                if (userWallet != null)
                {
                    userWallet.Balance -= boostfee;


                    _repositoryManager.HistoryWallet.Create(new HistoryWallet
                    {
                        Amount = "-" + boostfee.ToString(),
                        IdUser = userWallet.IdUser,
                        IdWallet = userWallet.Id,
                        Status = (int)HistoryWalletStatus.Success,
                        Time = DateTime.UtcNow.AddHours(7),
                        Type = "Thanh toán phí đẩy bài đăng"
                    });
                }
                post.SavedDate = DateTime.Now.AddHours(7);
                _repositoryManager.Post.Update(post);
                await _repositoryManager.SaveAsync();
                return 1;
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        //public async Task<bool> BoostPost(int idPost)
        //{
        //    try
        //    {
        //        var post =await _repositoryManager.Post.FindByCondition(x => x.Id == idPost, false).FirstOrDefaultAsync();
        //        if (post != null)
        //        {


        //        }
        //    }
        //    catch (Exception e)
        //    {

        //        return false;
        //    }

        //    return true;
        //}

        public async Task<List<PostOptional>> GetPostAiSuggest(int user_id)
        {

            var user = await _repositoryManager.User.FindByCondition(x => x.Id == user_id, false).FirstOrDefaultAsync();
            if (user == null)
            {
                return new List<PostOptional>();
            }
            string playing_area_full = user.PlayingArea;
            int playing_level = user.PlayingLevel;
            string playing_way_raw = user.PlayingWay;

            var playing_area_parts = playing_area_full.Split(',', 2);
            string district_part = (playing_area_parts.Length > 1) ? playing_area_parts[1].Trim().TrimEnd(';') : "";

            int playing_level_category;

            if (playing_level == 0)
                playing_level_category = 0;
            else if (playing_level == 1)
                playing_level_category = 1;
            else if (playing_level == 2 || playing_level == 3)
                playing_level_category = 2;
            else if (playing_level == 4 || playing_level == 5)
                playing_level_category = 3;
            else
                playing_level_category = 4;

            List<string> selected_skills = new List<string>
            {
            "Giành quyền tấn công",
            "Khai thác đường chéo sân",
            "Chiến thuật tấn công cuối sân",
            "Chiến thuật buộc đối thủ đánh cầu trái tay",
            "Chiến thuật ép đối phương đổi hướng liên tục",
            "Chiến thuật đánh vào bốn góc sân",
            "Chiến thuật phòng thủ trước tấn công sau"
             };

            List<string> district_positions = new List<string>
            {
                    "Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 10", "Quận 11", "Quận 12",
                    "Quận Tân Bình", "Quận Bình Tân", "Quận Tân Phú", "Quận Phú Nhuận", "Quận Gò Vấp", "Quận Bình Thạnh",
                    "Hóc Môn", "Củ Chi", "Quận Thủ Đức", "Quận 9", "Quận 2"
               };

            string[] categorySlot = { "Mới chơi", "Nghiệp dư", "Chuyên nghiệp" };

            List<string> play_style = new List<string> { "Đánh đơn", "Đánh đôi", "Hỗn hợp" };

            Dictionary<string, int> district_positions_dict = district_positions.Select((value, index) => new { value, index }).ToDictionary(pair => pair.value, pair => pair.index);
            Dictionary<string, int> play_style_dict = play_style.Select((value, index) => new { value, index }).ToDictionary(pair => pair.value, pair => pair.index);
            Dictionary<string, int> categorySlotDict = categorySlot.Select((value, index) => new { value, index }).ToDictionary(pair => pair.value, pair => pair.index);

            int district_part_index = district_positions_dict.TryGetValue(district_part, out int districtIndex) ? districtIndex : -1;

            List<string> playing_way_list = playing_way_raw.Split(';').Select(skill => skill.Trim()).Where(skill => !string.IsNullOrEmpty(skill)).ToList();

            List<string> matching_skills = playing_way_list.Where(skill => selected_skills.Contains(skill)).ToList();

            string playing_style;

            if (matching_skills.Count == 1)
            {
                int skill_position = selected_skills.IndexOf(matching_skills[0]);
                if (1 <= skill_position && skill_position <= 4)
                    playing_style = "Đánh đơn";
                else if (5 <= skill_position && skill_position <= 7)
                    playing_style = "Đánh đôi";
                else
                    playing_style = "Không xác định";
            }
            else if (matching_skills.Count >= 2)
            {
                playing_style = "Hỗn hợp";
            }
            else
            {
                playing_style = "Không xác định";
            }

            int play_style_index = play_style_dict.TryGetValue(playing_style, out int styleIndex) ? styleIndex : -1;


            var allPost = await _repositoryManager.Post.FindByCondition(x => !x.IsDeleted && x.IdType == (int)PostType.MatchingPost, false).Include(x => x.IdUserToNavigation).ToListAsync();
            var listpost = new List<Post>();
            foreach (var optList in allPost)
            {
                var cPost = new PostOptional
                {
                    IdPost = optList.Id,
                    Title = optList.Title,
                    AddressSlot = optList.AddressSlot,
                    ContentPost = optList.ContentPost,
                    ImgUrlPost = optList.ImgUrl,
                    FullName = optList.IdUserToNavigation?.FullName,
                    UserImgUrl = optList.IdUserToNavigation?.ImgUrl,
                    HighlightUrl = optList.ImgUrl,
                    UserId = optList.IdUserTo
                };

                GetPostOptional(optList, ref cPost);
                if (cPost.QuantitySlot != null && cPost.QuantitySlot != 0)
                {
                    listpost.Add(optList);
                }
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("district_part_post", typeof(int));
            dataTable.Columns.Add("playing_level_post", typeof(int));
            dataTable.Columns.Add("playing_style_post", typeof(int));
            dataTable.Columns.Add("post_id", typeof(int));

            foreach (var post in listpost)
            {
                var Post_area_parts = post.AddressSlot.Split(',')[1].Trim();
                int postDistrict_part_index = district_positions_dict.TryGetValue(Post_area_parts, out int districtIndex1) ? districtIndex1 : -1;
                int postStyle = play_style_dict.TryGetValue(post.CategorySlot, out int postStyleIndex) ? postStyleIndex : -1;
                int postLevel = categorySlotDict.TryGetValue(post.LevelSlot, out int postLevel1) ? postLevel1 : -1;

                dataTable.Rows.Add(postDistrict_part_index, postLevel, postStyle, post.Id);
            }


            double[][] X = dataTable.ToJagged<double>("district_part_post", "playing_level_post", "playing_style_post");
            int[] postIdarr = dataTable.Columns["post_id"].ToArray<int>();
            int[] y = Enumerable.Range(0, postIdarr.Length).OrderBy(i => postIdarr[i]).ToArray(); ;


            // Chuẩn hóa dữ liệu
            var scaler = new Normalization();
            X = scaler.Apply(X);

            var knn = new KNearestNeighbors(k: 8);
            knn.Learn(X, y);


            double[][] new_user = new double[][] { new double[] { district_part_index, playing_level_category, play_style_index } };
            double[][] new_user_scaled = scaler.Apply(new_user);
            double[][] scores = knn.Scores(new_user_scaled);
            int[] predicted_post_ids_knn = scores[0]
              .Select((score, index) => (Score: score, Index: index))
                .OrderByDescending(x => x.Score)
                 .Take(knn.K)
                    .Select(x => x.Index)
                        .ToArray();

            var returnList = new List<PostOptional>();

            foreach (int index in predicted_post_ids_knn)
            {
                int postId = postIdarr[index];
                var optList = await _repositoryManager.Post.FindByCondition(x => x.Id == postId, true)
               .OrderByDescending(x => x.SavedDate)
               .Include(x => x.IdUserToNavigation)
               .FirstOrDefaultAsync();

                var cPost = new PostOptional
                {
                    IdPost = optList.Id,
                    Title = optList.Title,
                    AddressSlot = optList.AddressSlot,
                    ContentPost = optList.ContentPost,
                    ImgUrlPost = optList.ImgUrl,
                    FullName = optList.IdUserToNavigation?.FullName,
                    UserImgUrl = optList.IdUserToNavigation?.ImgUrl,
                    HighlightUrl = optList.ImgUrl,
                    UserId = optList.IdUserTo
                };
                var post = _repositoryManager.Post.FindByCondition(x => x.Id == cPost.IdPost, false).Include(x => x.Slots).FirstOrDefault();
                GetPostOptional(post, ref cPost);
                if (cPost.QuantitySlot != null && cPost.QuantitySlot != 0)
                {
                    returnList.Add(cPost);
                }
            }
            return returnList;


        }

        public async Task<bool> isValidPost(int postId)
        {
            var post = await _repositoryManager.Post.FindByCondition(x => !x.IsDeleted && x.Id == postId && x.IdType == (int)PostType.MatchingPost, true).FirstOrDefaultAsync();

            //var lastSlot = new SlotInfo(post.SlotsInfo.Split(";")[0]);


            //foreach (var slot in post.SlotsInfo.Split(";"))
            //{
            //    if (slot != string.Empty)
            //    {
            //        var slotInfo = new SlotInfo(slot);

            //        var joinSlot = _repositoryManager.Slot
            //            .FindByCondition(x =>
            //            !x.IsDeleted &&
            //            x.ContentSlot == slotInfo.StartTime.Value.ToString("dd/MM/yyyy") &&
            //            x.IdPost == post.Id, false).Count();


            //        if (slotInfo.AvailableSlot - joinSlot > 0)
            //        {
            //            if (slotInfo.StartTime > lastSlot.StartTime)
            //            {
            //                lastSlot = slotInfo;
            //            }
            //        }
            //        else
            //        {
            //            if (slotInfo.AvailableSlot - joinSlot <= 0)
            //            {

            //                post.IsDeleted = true;
            //                _repositoryManager.Post.Update(post);
            //                await _repositoryManager.SaveAsync();
            //                return false;
            //            }
            //            if (slotInfo.StartTime > lastSlot.StartTime)
            //            {
            //                lastSlot = slotInfo;
            //            }
            //        }


            //    }
            //}
            //if (lastSlot.StartTime < DateTime.UtcNow.AddHours(-7))
            //{
            //    return false;
            //    post.Status = true;
            //    _repositoryManager.Post.Update(post);
            //    await _repositoryManager.SaveAsync();

            //}
            return true;
        }

        public async Task<int> UpdatePost(int post_id, NewPostInfo info)
        {


            try
            {
                var isExistPost = await _repositoryManager.Post.FindByCondition(x => x.Id == post_id, false).FirstOrDefaultAsync();
                if (isExistPost == null)
                {
                    return 0;
                }
                else
                {
                    isExistPost.CategorySlot = info.CategorySlot;
                    isExistPost.Title = info.Title;
                    isExistPost.AddressSlot = info.Address;
                    isExistPost.CategorySlot = info.Description;
                    isExistPost.SavedDate = DateTime.UtcNow.AddHours(7);
                    _repositoryManager.Post.Update(isExistPost);
                    _repositoryManager.SaveAsync().Wait();
                }
                return 1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
