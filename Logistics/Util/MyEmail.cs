using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using Logistics.Models;
using System.Configuration;

namespace Logistics.Util
{
    public class MyEmail
    {
        static string semiServer = "smtp.truly.cn";
        //static string adminEmail = "liyihan.ic@truly.com.cn";

        //发送邮件的包装方法，因为集团邮箱经常发送不出邮件，所以以后默认使用半导体邮箱。2013-6-7
        public static bool SendEmail(string subject, string content, string emailAddress)
        {
            try
            {
                WebMail.SmtpServer = semiServer;
                WebMail.SmtpPort = 25;
                WebMail.UserName = "crm";
                WebMail.From = "\"信利厂内物流平台\"<crm@truly.cn>";
                WebMail.Password = "tic3006";
                WebMail.Send(
                to: emailAddress,
                    //bcc: semiBcc,
                subject: subject,
                body: content + "<br /><div style='clear:both'><hr />来自:信利厂内物流平台<br />注意:此邮件是系统自动发送，请不要直接回复此邮件</div>",
                isBodyHtml: true
                );
            }
            catch
            {
                //发送失败          
                return false;
            }
            return true;
        }

        public static bool NotifyUser(vw_lg_cardBord item,string driver, string status) {
            string subject="", content="";
            string contacts = "", contactEmails = "";
            if (!string.IsNullOrEmpty(item.department_contact_email))
            {
                contacts = item.department_contacts;
                contactEmails = item.department_contact_email;
            }
            if (!string.IsNullOrEmpty(item.destination_contact_email))
            {
                contacts += contacts == "" ? "" : ",";
                contactEmails += contactEmails == "" ? "" : ",";
                contacts += item.destination_contacts;
                contactEmails += item.destination_contact_email;
            }
            content = "<div>" + contacts + ",你好：</div>";
            if (status.Equals("卸货"))
            {
                subject = "卡板已收货通知";
                content += string.Format("<div style='margin-left:30px;'>来自{0}的卡板号为【{1}】的货物已送达{2},承运司机是：{3},请知悉！</div>", item.department, item.card_no, item.destination, driver);
            }
            else if (status.Equals("接单"))
            {
                subject = "卡板已被接单通知";
                content += string.Format("<div style='margin-left:30px;'>来自{0},即将送达{2}的卡板号为【{1}】的货物已被接单,承运司机是：{3},请知悉！</div>", item.department, item.card_no, item.destination, driver);
            }

            return SendEmail(subject, content, contactEmails);
        }

        public static bool PermitionNotify(string billNo, bool isOK,string opinion="")
        {
            string name = ConfigurationManager.AppSettings["permitionSubmitName"];
            string email = ConfigurationManager.AppSettings["permitionSubmitEmail"];
            string auditResult = isOK ? "OK" : "NG";
            string subject = "放行单申请已" + auditResult + "通知";
            string content = "<div>" + name + ",你好：</div>";
            content += string.Format("<div style='margin-left:30px;'>你提交的单号为【{0}】的放行单申请已审核,审核结果为：{1},审核意见：{2},请知悉！</div>", billNo, auditResult, string.IsNullOrEmpty(opinion) ? "无" : opinion);

            return SendEmail(subject, content, email);
        }

    }
}