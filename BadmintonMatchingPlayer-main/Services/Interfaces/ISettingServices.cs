using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;

namespace Services.Interfaces
{
    public interface ISettingServices
    {
        bool UpdateSetting(int SettingId, decimal SettingAmount);
        Task<List<Setting>> GetAllFree();
    }
}
