using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Logistics.Models
{
    public class SimpleTruckModels
    {
        public string account { get; set; }
        public int id { get; set; }
        public string billNo { get; set; }
        public string submitter { get; set; }
        public DateTime? submitDate { get; set; }

        public string submitDateStr
        {
            get
            {
                if (submitDate != null) {
                    return ((DateTime)submitDate).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else {
                    return "";
                }
            }
        }

        public string status { get; set; }
        public string accountName
        {
            get
            {
                if (account.Equals("SEMI")) {
                    return "半导体";
                }
                else {
                    return "光电";
                }
            }
        }

    }

    public class TruckModels
    {
        public SimpleTruckModels simpleTruckModel { get; set; }
        public string itemName { get; set; }
        public string model { get; set; }
        public decimal? qty { get; set; }
        public string customer { get; set; }
    }

}