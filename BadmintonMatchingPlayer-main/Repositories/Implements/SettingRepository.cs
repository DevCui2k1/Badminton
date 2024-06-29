using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities;
using Entities.Models;
using Repositories.Intefaces;

namespace Repositories.Implements
{
    internal class SettingRepository : RepositoryBase<Setting>, ISettingRepository
    {
        public SettingRepository(DataContext context) : base(context)
        {
        }
    }

    
}

