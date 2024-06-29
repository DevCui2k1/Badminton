using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Intefaces;
using Services.Interfaces;

namespace Services.Implements
{
  
    public class SettingService : ISettingServices
    {
        private readonly IRepositoryManager _repositoryManager;

        public SettingService(IRepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
        }

        public bool UpdateSetting(int SettingId, decimal SettingAmount)
        {
            var Setting = _repositoryManager.Setting.FindByCondition(f => f.SettingId == SettingId, false).FirstOrDefault(); ;
            if (Setting!=null)
            {
                Setting.SettingAmount= SettingAmount;
                _repositoryManager.Setting.Update(Setting);
                _repositoryManager.SaveAsync().Wait();
                return true;
            }
            
           return false;
        }

        public async Task<List<Setting>> GetAllFree()
        {
            var Setting =await _repositoryManager.Setting.FindAll(true).ToListAsync();         
            return Setting;          
        }
    }
}
