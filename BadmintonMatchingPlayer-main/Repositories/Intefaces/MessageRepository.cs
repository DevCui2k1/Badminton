using Entities;
using Entities.Models;
using Repositories.Implements;

namespace Repositories.Intefaces
{
    public class MessageRepository : RepositoryBase<Messages>, IMessageRepository
    {
        public MessageRepository(DataContext context) : base(context)
        {

        }
    }
}
