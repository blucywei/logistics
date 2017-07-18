using Logistics.Models;
using Logistics.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Logistics.Filter;

namespace Logistics.Controllers
{
    public class AccountController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult getImage()
        {
            string code = MyUtils.CreateValidateNumber(4);
            Session["code"] = code.ToLower();
            byte[] bytes = MyUtils.CreateValidateGraphic(code);
            return File(bytes, @"image/jpeg");
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult Login(FormCollection fc)
        {
            LoginModel model = new LoginModel()
            {
                UserName = fc.Get("username"),
                Password = fc.Get("password"),
                VailidateCode = fc.Get("code"),
                rememberDay = Int32.Parse(fc.Get("rememberDay"))
            };
            var result = StartLogin(model);
            return Json(result);
        }

        [AllowAnonymous]
        private SimpleResultModel StartLogin(LoginModel model)
        {
            if (!model.VailidateCode.ToLower().Equals((string)Session["code"]))
                return new SimpleResultModel() { suc = false, msg = "验证码错误" };
            int maxFailTimes = 5;
            string md5Password = MyUtils.getMD5(model.Password);
            string pureMd5Password = MyUtils.getPureMd5(model.Password);
            bool suc = false;
            string msg = "";
            string errorMsg = "";
            bool forbitFlag = false;
            int failTimes = 0;
            bool failSave = true;
            DateTime lastSixMonth = DateTime.Now.AddDays(-180);

            var user = db.vw_lg_users.Where(u => u.card_number == model.UserName).ToList();
            if (user.Count() < 1)
            {
                failSave = false;
                msg = "用户名不存在，登陆失败!";
            }
            else if (user.Where(u => u.forbit_flag == 1).Count() > 0)
            {
                failSave = false;
                msg = "用户名已被禁用，请联系管理员，登陆失败!";
            }
            else if (db.GetHREmpStatus(model.UserName).ToList().Count() > 0 && db.GetHREmpStatus(model.UserName).ToList().First() != "在职")
            {
                    forbitFlag = true;
                    msg = "你在人事系统不是【在职】状态，不能登陆";                
            }
            else if (user.Where(u => u.last_login_date != null && u.last_login_date < lastSixMonth).Count() > 0)
            {
                forbitFlag = true;
                msg = "连续六个月未登录，被系统禁用。请联系管理员";
            }
            else if (user.Where(u => u.password == md5Password || u.password==pureMd5Password).Count() < 1)
            {
                var thisUser = user.First();
                if (thisUser.fail_times == null)
                    failTimes = 1;
                else
                    failTimes = (int)thisUser.fail_times + 1;

                if (thisUser.fail_times >= maxFailTimes)
                {
                    forbitFlag = true;
                    failTimes = 0;
                    msg = "连续" + maxFailTimes + "次密码错误，用户被禁用!";
                }
                else
                {
                    msg = "已连续" + failTimes + "次密码错误，你还剩下" + (maxFailTimes - failTimes) + "次尝试机会。";
                }
                errorMsg = "密码错误：" + model.Password + ";" + msg;
            }
            else
            {
                //成功登录

                //写入cookie
                setcookie(user.First(), model.rememberDay);

                msg = "登陆成功";
                suc = true;
            }

            if (suc)
            {
                if (user.First().platform.Equals("empInfo"))
                {
                    //从员工信息表过来的
                    var empUser = db.ei_users.Single(u => u.card_number == model.UserName);
                    empUser.fail_times = 0;
                    empUser.last_login_date = DateTime.Now;
                }
                else
                {
                    //从pda表过来
                    db.UpdateLgPDAUser(model.UserName, failTimes, DateTime.Now, true);
                }
                WriteEventLog("用户登录", msg);
            }
            else
            {
                if (failSave)
                {
                    if (user.First().platform.Equals("empInfo"))
                    {
                        //从员工信息表过来的
                        var empUser = db.ei_users.Single(u => u.card_number == model.UserName);
                        empUser.fail_times = failTimes;
                        empUser.forbit_flag = forbitFlag;
                    }
                    else
                    {
                        //从pda表过来
                        db.UpdateLgPDAUser(model.UserName, failTimes, DateTime.Now, !forbitFlag);
                    }
                }
                WriteEventLogWithoutLogin(model.UserName, string.IsNullOrEmpty(errorMsg) ? msg : errorMsg, -10);
            }
            return new SimpleResultModel() { suc = suc, msg = msg };
        }

        public ActionResult LogOut()
        {
            var cookie = new HttpCookie(ConfigurationManager.AppSettings["cookieName"]);
            cookie.Expires = DateTime.Now.AddDays(-1);
            Response.AppendCookie(cookie);
            Session.Clear();
            return RedirectToAction("Login");
        }

        [SessionTimeOutJsonFilterAttribute]
        public JsonResult ChangePassword(string old_pass,string new_pass)
        {
            var us = db.vw_lg_users.Where(u => u.card_number == userInfo.cardNo).First();
            string vPass = us.password;
            if (userInfoDetail.platform.Equals("empInfo"))
            {
                if (!vPass.Equals(MyUtils.getMD5(old_pass)))
                {
                    return Json(new SimpleResultModel() { suc = false, msg = "旧密码不正确" });
                }
                else
                {
                    var user = db.ei_users.Single(u => u.id == userInfo.id);
                    user.password = MyUtils.getMD5(new_pass);
                    db.SaveChanges();
                }
            }
            else
            {
                if (!vPass.Equals(MyUtils.getPureMd5(old_pass)))
                {
                    return Json(new SimpleResultModel() { suc = false, msg = "旧密码不正确" });
                }
                else
                {
                    db.UpdatePDAPassword(userInfo.cardNo, MyUtils.getPureMd5(new_pass));
                }
            }
            return Json(new SimpleResultModel() { suc = true, msg = "密码更新成功" });
        }

        public ActionResult Test()
        {
            return View();
        }

        //跳转到哪个主页
        [SessionTimeOutFilter]
        public ActionResult JumpIndex()
        {
            string permitionAuditors = ConfigurationManager.AppSettings["permitionAuditor"];
            if (permitionAuditors.IndexOf(userInfo.cardNo) >= 0) {
                return RedirectToAction("TIndex", "TruckPermition");
            }
            else {
                return RedirectToAction("Index", "CardBoard");
            }
        }

    }
}
