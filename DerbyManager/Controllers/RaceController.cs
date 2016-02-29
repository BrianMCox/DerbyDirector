using DerbyManager.Models;
using FastTrack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DerbyManager.Controllers
{
    [RoutePrefix("api/race")]
    public class RaceController : ApiController
    {
        FileService service = new FileService();

        [HttpGet]
        public IEnumerable<Heat> GetRace()
        {
            IEnumerable<Heat> result;
            var fileName = string.Format("{0}\\data\\race-2016 Pinewood Derby.json", System.Web.Hosting.HostingEnvironment.MapPath("~/"));

            try
            {
                var json = System.IO.File.ReadAllText(fileName);
                result = JsonConvert.DeserializeObject<IEnumerable<Heat>>(json);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                result = null;
            }

            return result;
        }

        [HttpPut]
        public void UpdateRace(IEnumerable<Heat> race)
        {
            var file = string.Format("{0}\\data\\race-2016 Pinewood Derby.json", System.Web.Hosting.HostingEnvironment.MapPath("~/"));
            service.Save(file, race);
        }

        [HttpGet]
        [Route("results")]
        public List<GroupResults> GetResults()
        {
            var fileName = string.Format("{0}\\data\\race-2016 Pinewood Derby.json", System.Web.Hosting.HostingEnvironment.MapPath("~/"));
            var results = new List<GroupResults>();

            try
            {
                var evt = (Event)service.Load(string.Format("{0}\\data\\event-2016 Pinewood Derby.json", System.Web.Hosting.HostingEnvironment.MapPath("~/")));

                var json = System.IO.File.ReadAllText(fileName);
                var raceData = JsonConvert.DeserializeObject<IEnumerable<Heat>>(json);
                var groupedData = raceData.GroupBy(d => d.groupName);
                
                foreach (var g in groupedData)
                {
                    var awdCount = GetAwardCount(evt, g.Key);
                    var temp = GetGroupResults(g);
                    var indResults = temp.Select(i => new IndividualResults { Name = i.Key, Times = i.Value.ToList(), AverageTime = i.Value.Average(), TotalTime = i.Value.Sum(), Visible = false }).ToList();
                    var group = new GroupResults { GroupName = g.Key, IndividualResults = indResults.OrderBy(i => i.TotalTime).Take(awdCount).ToList() };
                    results.Add(group);
                }
                
            }
            catch (System.IO.FileNotFoundException ex)
            {
                return null;
            }

            return results;
        }

        private int GetAwardCount(Event evt, string groupName)
        {
            foreach (var group in evt.Groups)
            {
                if (group.Name != groupName)
                {
                    foreach(var division in group.Divisions)
                    {
                        if (division.Name == groupName)
                        {
                            return division.Awards;
                        }
                    }
                }
                else
                {
                    return group.Awards;
                }
            }

            return 0;
        }

        private Dictionary<string, IList<decimal>> GetGroupResults (IGrouping<string, Heat> groupHeats)
        {
            var results = new Dictionary<string, IList<decimal>>();
            foreach (var heat in groupHeats)
            {
                foreach (var lane in heat.laneAssignments)
                {
                    if (!results.ContainsKey(lane.Driver))
                    {
                        results.Add(lane.Driver, new List<decimal> { lane.Time });
                    }
                    else
                    {
                        results[lane.Driver].Add(lane.Time);
                    }
                }
            }

            return results;
        }

        [HttpGet]
        [Route("timer/init")]
        public ComPortInfo TimerInit()
        {
            return TrackMonitor.Initialize();
        }

        [HttpGet]
        [Route("timer/test")]
        public bool TestConnection()
        {
            return TrackMonitor.TestConnection();
        }

        [HttpGet]
        [Route("timer/newConnection")]
        public ComPortInfo NewConnection()
        {
            return TrackMonitor.NewConnection();
        }

        [HttpGet]
        [Route("timer/endRace")]
        public bool EndRace()
        {
            return TrackMonitor.EndRace();
        }

        [HttpGet]
        [Route("timer/clearRace")]
        public bool ClearRace()
        {
            return TrackMonitor.ClearRace();
        }
    }
}
