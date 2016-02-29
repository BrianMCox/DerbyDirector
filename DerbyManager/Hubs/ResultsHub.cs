using FastTrack;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Hubs
{
    public class ResultsHub : Hub
    {
        public void Send(RaceResult results)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<ResultsHub>();
            context.Clients.All.broadcastResults(results);
        }
    }
}