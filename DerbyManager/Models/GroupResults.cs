using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public class GroupResults
    {
        public string GroupName { get; set; }
        public List<IndividualResults> IndividualResults { get; set; }
    }
}