using Logistics.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Logistics.Models;

namespace Logistics.Filter
{

    public class SessionTimeOutFilterAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            if (ctx.Session != null)
            {
                var cookie = ctx.Request.Cookies[ConfigurationManager.AppSettings["cookieName"]];
                if (cookie != null)
                {
                    var id = cookie.Values.Get("userid");
                    var code = cookie.Values.Get("code");
                    if (code.Equals(MyUtils.getMD5(id)))
                    {
                        base.OnActionExecuting(filterContext);
                        return;
                    }
                }
            }            
            filterContext.Result = new RedirectResult("~/Account/Login");
            //ctx.Response.Redirect("~/Account/Login");--虽可正常运行，但在调试模式下回出错，因为还是会在Action里面继续执行。
        }
    }

    public class SessionTimeOutJsonFilterAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            if (ctx.Session != null)
            {
                var cookie = ctx.Request.Cookies[ConfigurationManager.AppSettings["cookieName"]];
                if (cookie != null)
                {
                    var id = cookie.Values.Get("userid");
                    var code = cookie.Values.Get("code");
                    if (code.Equals(MyUtils.getMD5(id)))
                    {
                        base.OnActionExecuting(filterContext);
                        return;
                    }
                }
            }
            
            filterContext.Result = new JsonResult()
            {
                Data = new SimpleResultModel() { suc = false, msg = "操作失败！原因：会话已过期，请重新登陆系统" }
            };
            //filterContext.Result = new RedirectResult("~/Account/Login");
            //ctx.Response.Redirect("~/Account/Login");--虽可正常运行，但在调试模式下回出错，因为还是会在Action里面继续执行。
        }
    }

}