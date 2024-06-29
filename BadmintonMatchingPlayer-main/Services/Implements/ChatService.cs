using Entities.Models;
using Entities.RequestObject;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Repositories.Implements;
using Repositories.Intefaces;
using Services.Interfaces;
using System;

namespace Services.Implements
{
    public class ChatService : IChatServices
    {
        private readonly IRepositoryManager _repositoryManager;

        public ChatService(IRepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
        }

        public async Task<int> CreateRoom(int admin_id, int reportId)
        {
            var report = await _repositoryManager.Report.FindByCondition(x => x.Id == reportId, false)
                .Include(x => x.Transaction)
                .FirstOrDefaultAsync();

            if(report == null)
            {
                throw new Exception("Report not found");
            }

            if(report.IdTransaction == null)
            {
                throw new Exception("Report not about transaction to open chat");
            }

            var chatRoom = new ChatRoom
            {
                Code = $"Transaction-{report.IdTransaction}",
                Name = $"{report.Transaction.TimeTrans.Value.ToString("dd/MM HH:mm")}",
                UpdateTime = DateTime.UtcNow.AddHours(7),
                CoverImage = "https://res.cloudinary.com/dbjvirvym/image/upload/v1702103595/98b0ecf8-7461-4e4e-bb02-5174d035d88a.png"
            };
            _repositoryManager.ChatRoom.Create(chatRoom);
            await _repositoryManager.SaveAsync();

            if (chatRoom.Id == 0)
                throw new Exception("Không thể tạo chat");

            await JoinRoom(admin_id, chatRoom.Id);
            await JoinRoom(report.IdUserFrom.Value, chatRoom.Id);
            await JoinRoom(report.IdUserTo.Value, chatRoom.Id);

            return chatRoom.Id;
        }

        public async Task<List<ChatInfos>> GetChatRoom(int transactionId)
        {
            var slots = await _repositoryManager.Slot
                .FindByCondition(x => x.TransactionId == transactionId, false)
                .Include(x => x.IdPostNavigation)
                .ToListAsync();

            var playDate = string.Empty;
            var res = new List<ChatInfos>();
            foreach (var slot in slots)
            {
                if (playDate != slot.ContentSlot)
                {
                    var room = await _repositoryManager.ChatRoom
                        .FindByCondition(x => x.Code == $"{slot.IdPost}_{slot.ContentSlot}", false)
                        .Select(x => new ChatInfos
                        {
                            PlayDate = slot.ContentSlot,
                            RoomId = x.Id
                        })
                        .FirstOrDefaultAsync();

                    if (room == null)
                    {
                        var roomEnt = new Entities.Models.ChatRoom
                        {
                            Code = $"{slot.IdPost}_{slot.ContentSlot}",
                            Name = $"{slot.ContentSlot}",
                            CoverImage = slot.IdPostNavigation.ImgUrl
                        };
                        _repositoryManager.ChatRoom.Create(roomEnt);
                        await _repositoryManager.SaveAsync();

                        room = new ChatInfos
                        {
                            PlayDate = slot.ContentSlot,
                            RoomId = roomEnt.Id
                        };
                    }
                    else
                    {
                        var userJoined = await _repositoryManager.ChatRoomUser
                            .FindByCondition(x => x.UserId == slot.IdUser && x.RoomId == room.RoomId, false)
                            .FirstOrDefaultAsync();

                        if (userJoined == null)
                        {
                            _repositoryManager.ChatRoomUser.Create(new Entities.Models.UserChatRoom
                            {
                                RoomId = room.RoomId,
                                UserId = slot.IdUser
                            });

                            await _repositoryManager.SaveAsync();
                            var user = await _repositoryManager.User
                             .FindByCondition(x => x.Id == slot.IdUser, false)
                             .FirstOrDefaultAsync();
                            var msg = new SendMessageRequest
                            {
                                Message = user.FullName + " đã tham gia hội thoại.",
                                RoomId = room.RoomId,
                            };
                            await JoinOrleaveChat(msg);
                        }
                    }
                    res.Add(room);
                }
            }
            return res;
        }

        public async Task<List<MessageDetail>> GetRoomDetail(int roomIds, int pageSize, int pageNum)
        {
            var ss = await _repositoryManager.Message.FindByCondition(x => x.RoomId == roomIds, false).ToListAsync();
            var msg = await _repositoryManager.Message.FindByCondition(x => x.RoomId == roomIds, false)
                .OrderByDescending(x => x.Id)
                .Skip((pageNum - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.User)
                .Select(x => new MessageDetail
                {
                    Message = x.Message,
                    MessageId = x.Id,
                    SendTime = x.SendTime.ToString("ddd HH:mm"),
                    SendUserName = x.User.FullName,
                    UserId = x.UserId.Value
                })
                .OrderBy(x => x.MessageId)
                .ToListAsync();
            return msg;
        }

        public async Task<List<JoinedChatRoom>> GetRoomOfUser(int user_id)
        {
            var joinedRoom = await _repositoryManager.ChatRoomUser.FindByCondition(x => x.UserId == user_id, false)
                .Include(x => x.ChatRoom)
                .ThenInclude(x => x.Messages)
                .ThenInclude(x => x.User)
                .OrderByDescending(x => x.ChatRoom.UpdateTime)
                .ToListAsync();

            var res = new List<JoinedChatRoom>();
            if (joinedRoom.Count() > 0)
            {
                res = joinedRoom.Select(x => new JoinedChatRoom
                {
                    ChatTitle = x.ChatRoom.Name,
                    CoverImg = x.ChatRoom.CoverImage,
                    LastSendMsg = x.ChatRoom.Messages.Count > 0 ? x.ChatRoom.Messages.Select(y => $"{y.User.FullName}: {y.Message}").Last() : string.Empty,
                    LastSendTime = x.ChatRoom.Messages.Count > 0 ? x.ChatRoom.Messages.Select(y => y.SendTime).Last().ToString("dd/MM/yyyy HH:mm") : string.Empty,
                    RoomId = x.RoomId.Value,
                    TransactionId = x.ChatRoom.Code.Contains("Transaction") ? int.Parse(x.ChatRoom.Code.Substring(x.ChatRoom.Code.IndexOf("-"))+1) : null
                }).ToList();
            }
            return res;
        }

        public async Task<bool> JoinRoom(int user_id, int room_id)
        {
            
            var user = await _repositoryManager.User
                            .FindByCondition(x => x.Id == user_id ,false)
                            .FirstOrDefaultAsync();
            var userJoined = await _repositoryManager.ChatRoomUser
                           .FindByCondition(x => x.UserId == user_id && x.RoomId == room_id, false)
                           .FirstOrDefaultAsync();
            if (userJoined == null)
            {
                
                _repositoryManager.ChatRoomUser.Create(new Entities.Models.UserChatRoom
                {
                    RoomId = room_id,
                    UserId = user_id
                });
                await _repositoryManager.SaveAsync();
            }

            return true;
        }

        public async Task<bool> LeaveRoom(int user_id, int room_id)
        {
            var userJoined = await _repositoryManager.ChatRoomUser
                .FindByCondition(x => x.UserId == user_id && x.RoomId == room_id, false)
                .FirstOrDefaultAsync();
            var user = await _repositoryManager.User
                           .FindByCondition(x => x.Id == user_id, false)
                           .FirstOrDefaultAsync();
            if (userJoined != null)
            {
                var msg = new SendMessageRequest
                {
                    Message = user.FullName + " đã rời hội thoại.",
                    RoomId = room_id,
                };
                await JoinOrleaveChat(msg);
                _repositoryManager.ChatRoomUser.Delete(userJoined);
                await _repositoryManager.SaveAsync();
            }

            return true;
        }

        public async Task<User> SendMessage(int user_id, SendMessageRequest info)
        {
            var room = await _repositoryManager.ChatRoom.FindByCondition(x => x.Id == info.RoomId, true).FirstOrDefaultAsync();

            if (room == null)
                return null;

            var msg = new Messages
            {
                Message = info.Message,
                RoomId = info.RoomId,
                SendTime = DateTime.UtcNow.AddHours(7),
                UserId = user_id
            };
            room.UpdateTime = DateTime.UtcNow.AddHours(7);
            _repositoryManager.Message.Create(msg);
            await _repositoryManager.SaveAsync();
            var res = await _repositoryManager.User.FindByCondition(x => x.Id == user_id, false).FirstOrDefaultAsync();
            return res;
        }
        public async Task JoinOrleaveChat(SendMessageRequest info)
        {
            var room = await _repositoryManager.ChatRoom.FindByCondition(x => x.Id == info.RoomId, true).FirstOrDefaultAsync();      
            var msg = new Messages
            {
                Message = info.Message,
                RoomId = info.RoomId,
                SendTime = DateTime.UtcNow.AddHours(7),
                UserId = 65
            };
            room.UpdateTime = DateTime.UtcNow.AddHours(7);
            _repositoryManager.Message.Create(msg);
            await _repositoryManager.SaveAsync();
           
        }
    }

    
}

