using Entities.Models;
using Entities.ResponseObject;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;


namespace BadmintonMatching.Controllers
{

    [ApiController]
    [Route("api/Settings")]
    public class SettingController : ControllerBase
    {
        private readonly ISettingServices _SettingService;
        public SettingController(ISettingServices SettingService)
        {
            _SettingService = SettingService;
        }



        [HttpPut]
        [Route("{SettingId}&{SettingAmount}/set_Setting")]
        public IActionResult SetSetting(int SettingId, decimal SettingAmount)
        {
            try
            {
              bool isSuccess=  _SettingService.UpdateSetting(SettingId, SettingAmount);
                if (isSuccess)
                {
                    return Ok(new SuccessObject<object> { Data = true, Message = "Cập nhật thành công !" });
                }
                else
                {
                    return Ok(new SuccessObject<object> { Data = true, Message = "Cập nhật thất bại" });
                }
                
            }
            catch (Exception ex)
            {
                return Ok(new SuccessObject<object> { Message = ex.Message });
            }
        }

        [HttpGet]
        [Route("get_listSetting")]
        [ProducesResponseType(typeof(SuccessObject<List<Setting>>), 200)]
        public async Task<IActionResult> GetListSetting()
        {

            var listSetting = await _SettingService.GetAllFree();

            foreach (var Setting in listSetting)
            {
                Console.WriteLine(Setting.SettingName);
            }
            return Ok(new SuccessObject<List<Setting>> { Data = listSetting, Message = Message.SuccessMsg });

        }
    }
}
