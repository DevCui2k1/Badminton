using Entities;
using Entities.Models;
using Repositories.Intefaces;

namespace Repositories.Implements
{
    public class UserChatRoomRepository : RepositoryBase<UserChatRoom>, IUserChatRoomRepository
    {
        public UserChatRoomRepository(DataContext context) : base(context)
        {

        }
    }
}
