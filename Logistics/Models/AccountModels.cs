using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Logistics.Models
{

    public class SimpleResultModel
    {
        public bool suc { get; set; }
        public string msg { get; set; }
        public string extra { get; set; }
    }

    public class UserInfo
    {
        public int id { get; set; }
        public string cardNo { get; set; }
        public string name { get; set; }

    }

    public class UserInfoDetail
    {
        public string platform { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string shortPhone { get; set; }
        public string carNo { get; set; }
        public string duty { get; set; }
        public string defaultLoc { get; set; }
        public int? defaultLocPriority { get; set; }
        public int maxCardsNumber { get; set; }

    }
    public class LoginModel
    {
        [Required]
        [Display(Name = "用户名")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [Display(Name = "记住我的天数")]
        public int rememberDay { get; set; }

        [Required]
        [Display(Name = "验证码")]
        public string VailidateCode { get; set; }
    }

}