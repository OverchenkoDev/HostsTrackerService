using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HostTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace HostTracker.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HostsController : ControllerBase
    {
        private HostTrackerContext _db;

        public HostsController(HostTrackerContext context)
        {
            _db = context;
        }

        [HttpPost("add")]
        public IActionResult Add(Hosts host)
        {
            if (string.IsNullOrEmpty(host.ServiceName) || string.IsNullOrEmpty(host.CheckDomain))
                return BadRequest();
            else
            {
                try
                {
                    Hosts curHost = _db.Hosts.Where(x => x.ServiceName == host.ServiceName && x.CheckDomain == host.CheckDomain).FirstOrDefault();
                    if (curHost == null)
                    {
                        _db.Hosts.Add(host);
                        _db.SaveChanges();
                        return Ok();
                    }
                    else
                        return UnprocessableEntity("Сервис уже занесён");
                }
                catch(Exception ex)
                {
                    EventLog error = new EventLog();
                    error.Message = ex.Message;
                    error.Details = ex.StackTrace;
                    error.Type = $@"Ошибка во время добавления сервиса {host.ServiceName} с адресом {host.CheckDomain}";
                    error.DateTime = DateTime.UtcNow;
                    _db.EventLog.Add(error);
                    _db.SaveChanges();
                    return StatusCode(500);
                }
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete(Hosts host)
        {
            if (string.IsNullOrEmpty(host.ServiceName) || string.IsNullOrEmpty(host.CheckDomain))
                return BadRequest();
            else
            {
                try
                {
                    Hosts curHost = _db.Hosts.Where(x => x.ServiceName == host.ServiceName && x.CheckDomain == host.CheckDomain).FirstOrDefault();
                    if (curHost != null)
                    {
                        _db.Hosts.Remove(curHost);
                        _db.SaveChanges();
                        return Ok();
                    }
                    else
                        return NotFound();
                }
                catch (Exception ex)
                {
                    EventLog error = new EventLog();
                    error.Message = ex.Message;
                    error.Details = ex.StackTrace;
                    error.Type = $@"Ошибка во время удаления сервиса {host.ServiceName} с адресом {host.CheckDomain}";
                    error.DateTime = DateTime.UtcNow;
                    _db.EventLog.Add(error);
                    _db.SaveChanges();
                    return StatusCode(500);
                }
            }
        }
    }
}
