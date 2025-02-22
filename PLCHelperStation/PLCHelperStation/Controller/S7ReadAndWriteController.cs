﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PLCHelperStation.DB;
using PLCHelperStation.Modbel;
using PLCHelperStation.ResultObj;
using S7.Net;

namespace PLCHelperStation.Controller
{
    enum Cpu
    {
        s71200,
        s71500
    }


    [Route("api/[controller]")]
    [ApiController]
    public class S7ReadAndWriteController : ControllerBase
    {
        protected readonly ILogger<S7ReadAndWriteController> _logger;

        public S7ReadAndWriteController(ILogger<S7ReadAndWriteController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// DB块读
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("ReadDBPoint")]
        [EnableCors("AllowSpecificOrigins")] // 应用 CORS 策略
        public async Task<ActionResult<Result>> ReadDBPointAsync(int id)
        {

            try
            {
                //1、连接PLC，需要CPU类型、IP、Rack、Slot
                using (var ctx = new TestDbContext())
                {
                    var dbconfig = ctx.DBConfigs.Find(id);
                    if (dbconfig == null)
                    {
                        _logger.LogError("读取的DB块不存在");
                        return new Result { Code = 404, Message = "读取的DB块不存在" };
                    }
                    var s7Name = dbconfig.S7Name;
                    var s7 = ctx.S7Configs.FirstOrDefault(x => x.S7Name == s7Name);
                    string cpu = s7.CPUType;

                    Cpu cpuType;
                    if (cpu == "S7-1200")
                    {
                        cpuType = Cpu.s71200;
                    }
                    else if (cpu == "S7-1500")
                    {
                        cpuType = Cpu.s71500;
                    }
                    else
                    {
                        _logger.LogError("S7数据点读取，读取的CPU类型未知");
                        return new Result { Code = 400, Message = "未知的CPU类型" };
                    }
                    var ip = s7.IP;
                    var rack = Convert.ToInt16(s7.Rack);
                    var slot = Convert.ToInt16(s7.Slot);

                    //2、使用
                    using (Plc plc = new Plc((CpuType)cpuType, ip, rack, slot))
                    {
                        
                        await plc.OpenAsync();

                        if (!plc.IsConnected)
                        {
                            return new Result { Code = 402, Message = "连接PLC失败！"};
                        }
                       

                        //DBX读取位，比如bool类型；DBW读取字；DBD读取数值
                        var dbType = dbconfig.DBType;
                        var dbpoint = dbconfig.DBOffset;
                        if (dbType == "Int")
                        {
                            var addrlast = "D";
                            var addragain = dbconfig.DBAddress.ToString();
                            var addr = $"{addragain}" + ".DBD" + $"{dbpoint}";
                            var res = plc.Read(addr);

                            if (dbconfig.DBName != null && res != null)
                            {
                                var result = res.ToString();
                                if (result != null)
                                {
                                    var dBRWRecord = new S7DBRWRecord(dbconfig.DBName, dbconfig.Remark, result, null, DateTime.Now);

                                    ctx.S7DBRWRecords.Add(dBRWRecord);
                                    ctx.SaveChanges();
                                    _logger.LogWarning($"保存DB点名称为{dbconfig.DBName},备注为：{dbconfig.Remark}的一条读记录");
                                }
                            }

                            _logger.LogWarning($"用户对{dbconfig.DBName}执行了数据读取,采集的结果为{res}");
                            return new Result { Code = 200, Message = "查询成功", Data = res };
                        }
                        else if (dbType == "bool")
                        {
                            var addrlast = "D";
                            var addragain = dbconfig.DBAddress.ToString();
                            var addr = $"{addragain}" + ".DBX" + $"{dbpoint}";
                            var res = plc.Read(addr);
                            if (dbconfig.DBName != null && res != null)
                            {
                                var result = res.ToString();
                                if (result != null)
                                {
                                    var dBRWRecord = new S7DBRWRecord(dbconfig.DBName, dbconfig.Remark, result, null, DateTime.Now);

                                    ctx.S7DBRWRecords.Add(dBRWRecord);
                                    ctx.SaveChanges();
                                    _logger.LogWarning($"保存DB点名称为{dbconfig.DBName},备注为：{dbconfig.Remark}的一条读记录");
                                }
                            }
                            _logger.LogWarning($"用户对{dbconfig.DBName}执行了数据读取,采集的结果为{res}");
                            return new Result { Code = 200, Message = "查询成功", Data = res };
                        }
                        return new Result { Code = 400, Message = "查询失败" };
                    }
                }
            }
            catch (Exception ex)
            {

                _logger.LogError($"系统在读取DB块时发生异常，异常信息为：{ex.Message}");
                return new Result { Code = 400, Message = $"系统在读取DB块时发生异常，异常信息为：{ex.Message}" };
            }


        }



        /// <summary>
        /// DB块写
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("WriteDBPoint")]
        [EnableCors("AllowSpecificOrigins")] // 应用 CORS 策略
        public ActionResult<Result> WriteDBPoint(int id, string target)
        {
            //1、连接PLC，需要CPU类型、IP、Rack、Slot
            using (var ctx = new TestDbContext())
            {
                var dbconfig = ctx.DBConfigs.Find(id);
                if (dbconfig == null)
                {
                    _logger.LogError("将要写入的DB块不存在");
                    return new Result { Code = 404, Message = "将要写入的DB块不存在" };
                }
                var s7Name = dbconfig.S7Name;
                var s7 = ctx.S7Configs.FirstOrDefault(x => x.S7Name == s7Name);
                string cpu = s7.CPUType;

                Cpu cpuType;
                if (cpu == "S7-1200")
                {
                    cpuType = Cpu.s71200;
                }
                else if (cpu == "S7-1500")
                {
                    cpuType = Cpu.s71500;
                }
                else
                {
                    _logger.LogError("S7数据点读取，写入的CPU类型未知");
                    return new Result { Code = 400, Message = "未知的CPU类型" };
                }
                var ip = s7.IP;
                var rack = Convert.ToInt16(s7.Rack);
                var slot = Convert.ToInt16(s7.Slot);

                //2、使用
                using (Plc plc = new Plc((CpuType)cpuType, ip, rack, slot))
                {
                    plc.Open();
                    //DBX读取位，比如bool类型；DBW读取字；DBD读取数值
                    var dbType = dbconfig.DBType;
                    var dbpoint = dbconfig.DBOffset;
                    if (dbType == "Int")
                    {
                        var addrlast = "D";
                        var addragain = dbconfig.DBAddress.ToString();
                        var addr = $"{addragain}" + ".DBD" + $"{dbpoint}";
                        var targetint = Convert.ToInt32(target); 
                        plc.Write(addr, targetint);

                        if (dbconfig.DBName != null)
                        {

                            var dBRWRecord = new S7DBRWRecord(dbconfig.DBName, dbconfig.Remark, "success", null, DateTime.Now);

                            ctx.S7DBRWRecords.Add(dBRWRecord);
                            ctx.SaveChanges();
                            _logger.LogWarning($"保存DB点名称为{dbconfig.DBName},备注为：{dbconfig.Remark}的一条写记录");



                        }

                        _logger.LogWarning($"用户对{dbconfig.DBName}执行了写入,采集的结果为{target}");
                        return new Result { Code = 200, Message = "写入成功", Data = target };
                    }
                    else if (dbType == "bool")
                    {
                        var addrlast = "D";
                        var addragain = dbconfig.DBAddress.ToString();
                        var addr = $"{addragain}" + ".DBX" + $"{dbpoint}";

                        if (bool.TryParse(target, out bool boolValue))
                        {
                            plc.Write(addr, boolValue);
                        }
                        else
                        {
                            return new Result { Code = 400, Message = "Invalid boolean value" };
                        }
                        if (dbconfig.DBName != null)
                        {
                            //bool.TryParse(target, out bool boolValue);
                            var dBRWRecord = new S7DBRWRecord(dbconfig.DBName, dbconfig.Remark, "success", null, DateTime.Now);

                            ctx.S7DBRWRecords.Add(dBRWRecord);
                            ctx.SaveChanges();
                            _logger.LogWarning($"保存DB点名称为{dbconfig.DBName},备注为：{dbconfig.Remark}的一条写记录");



                        }
                        _logger.LogWarning($"用户对{dbconfig.DBName}执行了写入,写入的结果为{target}");
                        return new Result { Code = 200, Message = "写入成功", Data = target };
                    }
                    return new Result { Code = 400, Message = "查询失败" };
                }
            }
        }
    }
}
  
