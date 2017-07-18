using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Logistics.Models
{
    public class FinishCardsModel
    {
        public string cardNo { get; set; }
        public string dep { get; set; }
        public string depPos { get; set; }
        public string des { get; set; }

        public DateTime? date{get;set;}

        public string dateStr
        {
            get
            {
                if (date == null)
                {
                    return "";
                }
                else
                {
                    return ((DateTime)date).ToString("yyyy-MM-dd HH:mm");
                }
            }
        }

        public int? packNum { get; set; }

        public int? isEmergency { get; set; }

        public string mobilePhone { get; set; }
    }

    public class carLocationModel
    {
        public string location { get; set; }
        public int? priority { get; set; }
        public DateTime? dtime { get; set; }
    }

}