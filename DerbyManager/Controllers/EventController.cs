using DerbyManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DerbyManager.Controllers
{
    [RoutePrefix("api/event")]
    public class EventController : ApiController
    {
        FileService service = new FileService();

        [HttpGet]
        public Event GetEvent()
        {
            return (Event)service.Load(string.Format("{0}\\data\\event-2016 Pinewood Derby.json", System.Web.Hosting.HostingEnvironment.MapPath("~/")));
        }

        [HttpPut]
        public void UpdateEvent(Event e)
        {
            
            var file = string.Format("{0}\\data\\event-{1}.json", System.Web.Hosting.HostingEnvironment.MapPath("~/"), e.Name);
            service.Save(file, e);
        }
    }
}
