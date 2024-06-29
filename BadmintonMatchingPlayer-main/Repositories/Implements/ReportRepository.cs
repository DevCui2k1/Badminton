using Entities;
using Entities.Models;
using Repositories.Intefaces;

namespace Repositories.Implements
{
    internal class ReportRepository : RepositoryBase<Report>, IReportRepository
    {
        public ReportRepository(DataContext dataContext) : base(dataContext)
        {

        }
    }
}
