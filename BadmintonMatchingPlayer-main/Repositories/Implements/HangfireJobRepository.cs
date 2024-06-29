using Entities;
using Entities.Models;
using Repositories.Intefaces;

namespace Repositories.Implements
{
    internal class HangfireJobRepository : RepositoryBase<HangfireJob>, IHangfireJobRepository
    {
        public HangfireJobRepository(DataContext context) : base(context)
        {

        }
    }
}