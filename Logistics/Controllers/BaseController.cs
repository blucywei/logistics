using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Logistics.Models;
using Logistics.Util;
using System.Configuration;

namespace Logistics.Controllers
{
    public class BaseController : Controller
    {
        private LogisticsEntities _db = null;
        private UserInfo _userInfo = null;
        private UserInfoDetail _userInfoDetail = null;
        protected LogisticsEntities db
        {
            get
            {
                if (_db == null)
                {
                    _db = new LogisticsEntities();
                }
                return _db;
            }
        }

        
        protected UserInfo userInfo
        {
            get
            {
                _userInfo = (UserInfo)Session["userInfo"];
                if (_userInfo == null)
                {
                    var cookie = Request.Cookies[ConfigurationManager.AppSettings["cookieName"]];
                    if (cookie != null)
                    {
                        _userInfo = new UserInfo();
                        _userInfo.id = Int32.Parse(cookie.Values.Get("userid"));
                        _userInfo.name = MyUtils.DecodeToUTF8(cookie.Values.Get("username"));
                        _userInfo.cardNo = cookie.Values.Get("cardno");
                        Session["userInfo"] = _userInfo;
                    }
                }

                return _userInfo;
            }
        }

        protected UserInfoDetail userInfoDetail
        {
            get
            {
                _userInfoDetail = (UserInfoDetail)Session["userInfoDetail"];
                if (_userInfoDetail == null)
                {
                    if (userInfo != null)
                    {
                        _userInfoDetail = new UserInfoDetail();
                        var driver = db.vw_lg_users.Where(v => v.id == userInfo.id).First();
                        _userInfoDetail.email = driver.email;
                        _userInfoDetail.phone = driver.phone_no;
                        _userInfoDetail.carNo = driver.car_no;
                        _userInfoDetail.duty = driver.duty;
                        _userInfoDetail.platform = driver.platform;
                        _userInfoDetail.shortPhone = driver.short_phone_no;
                        _userInfoDetail.defaultLoc = driver.default_loc;
                        _userInfoDetail.defaultLocPriority = driver.default_loc_priority;
                        _userInfoDetail.maxCardsNumber = driver.limit_qty == null ? 0 : (int)driver.limit_qty;
                        Session["userInfoDetail"] = _userInfoDetail;
                    }
                }
                return _userInfoDetail;
            }
        }

        //清空用户详细，用于刷新用户信息
        protected void ClearUserInfoDetail()
        {
            if (_userInfo != null)
            {
                Session["userInfo"] = null;
                _userInfo = null;
            }
            if (_userInfoDetail != null)
            {
                Session["userInfoDetail"] = null;
                _userInfoDetail = null;
            }
        }

        protected string GetUserIP()
        {
            string ip;
            if (Request.ServerVariables["HTTP_VIA"] != null) // using proxy
            {
                ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();  // Return real client IP.
            }
            else// not using proxy or can't get the Client IP
            {
                ip = Request.ServerVariables["REMOTE_ADDR"].ToString(); //While it can't get the Client IP, it will return proxy IP.
            }
            return ip;
        }

        //记录操作日志
        protected void WriteEventLog(string model, string doWhat, int isNomal = 0)
        {
            var log = new lg_event_log();
            log.ip = GetUserIP();
            log.do_what = doWhat;
            log.is_normal = isNomal;
            log.model = model;
            log.op_date = DateTime.Now;
            log.user_id = userInfo.id;
            log.user_name = userInfo.name;
            db.lg_event_log.Add(log);
            db.SaveChanges();
        }

        //未登陆时的记录日志方法
        protected void WriteEventLogWithoutLogin(string cardNo, string msg, int isNomal = 0)
        {
            var log = new lg_event_log();
            log.ip = GetUserIP();
            log.user_name = cardNo;
            log.do_what = msg;
            log.is_normal = isNomal;
            log.op_date = DateTime.Now;
            log.model = "登陆注册模块";
            db.lg_event_log.Add(log);
            db.SaveChanges();
        }

        //设置cookie
        protected void setcookie(vw_lg_users user,int days)
        {
            var cookie = new HttpCookie(ConfigurationManager.AppSettings["cookieName"]);
            cookie.Expires = DateTime.Now.AddDays(days);
            cookie.Values.Add("userid", user.id.ToString());
            cookie.Values.Add("cardno", user.card_number);
            cookie.Values.Add("code", MyUtils.getMD5(user.id.ToString()));
            cookie.Values.Add("username", MyUtils.EncodeToUTF8(user.name));//用于记录日志
            Response.AppendCookie(cookie);
        }

    }


}
