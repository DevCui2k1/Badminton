using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Entities;
using Entities.Models;
using Repositories.Intefaces;

namespace Repositories.Implements
{
    public class WithdrawDetailRepository : RepositoryBase<WithdrawDetail>, IWithdrawDetailRepository
    {
        public WithdrawDetailRepository(DataContext dataContext) : base(dataContext)
        {

        }
    }
}
