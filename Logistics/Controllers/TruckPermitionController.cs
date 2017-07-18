using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Logistics.Models;
using Logistics.Filter;
using Logistics.Util;

namespace Logistics.Controllers
{
    public class TruckPermitionController : BaseController
    {

        //未审批界面
        [SessionTimeOutFilter]
        public ActionResult TIndex(string searchContent = "")
        {
            WriteEventLog("未审批放行申请", "进入界面查询");
            var result = (from t in db.vw_lg_truckPermition
                          where t.FBillNo.Contains(searchContent)
                          && t.FStatus == 1
                          select new SimpleTruckModels
                          {
                              account = t.account,
                              id = t.FInterID,
                              billNo = t.FBillNo,
                              submitter = t.FSubmiter,
                              submitDate = t.FSubDate
                          }).Distinct().ToList();
            ViewData["model"] = result;
            ViewData["searchContent"] = searchContent;
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult TDetails(string billNo)
        {
            WriteEventLog("查看批放行申请明细", "billNo:" + billNo);
            var result = (from t in db.vw_lg_truckPermition
                          where t.FBillNo==billNo
                          select new TruckModels
                          {
                              simpleTruckModel = new SimpleTruckModels()
                              {
                                  account = t.account,
                                  id = t.FInterID,
                                  billNo = t.FBillNo,
                                  submitter = t.FSubmiter,
                                  submitDate = t.FSubDate,
                                  status = t.FStatus == 1 ? "待审核" : (t.FStatus == 0 ? "已NG" : "审核通过")
                              },
                              //itemName = t.FItemName,
                              model = t.FModel,
                              qty = t.FQty,
                              //customer = t.FShipToName
                          }).ToList();
            ViewData["model"] = result;
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult TFinishBills()
        {
            WriteEventLog("已审批放行申请", "进入界面");
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult GetFinishBills(string fd, string td, string searchContent,int page = 1)
        {
            WriteEventLog("已审批放行申请", "查询参数：" + fd + "~" + td + ",search:" + searchContent + ",page:" + page);
            int pageNum = 30;
            DateTime fDate,tDate;
            
            if(!DateTime.TryParse(fd,out fDate)){
                fDate = DateTime.Parse("2016-11-1");
            }
            if(!DateTime.TryParse(td,out tDate)){
                tDate = DateTime.Parse("2080-9-9");
            }
            tDate = tDate.AddDays(1);

            //不能用视图查询，总是报光电数据库的查询参数错误，最后没办法用存储过程实现查询

            var res = db.GetLGFinishPermition(fDate, tDate, searchContent, userInfo.name).ToList();
            List<SimpleTruckModels> result = new List<SimpleTruckModels>();
            foreach (var r in res) {
                result.Add(new SimpleTruckModels()
                {
                    account = r.account,
                    id = r.FInterID,
                    billNo = r.FBillNo,
                    submitDate = r.FSubDate,
                    submitter = r.FSubmiter,
                    status = r.FStatus <= 1 ? "NG" : "OK"
                });
            }
            //var result = (from t in db.vw_lg_truckPermition
            //              where t.FBillNo.Contains(searchContent)
            //              && t.FStatus != 1
            //              && t.FChecker == userInfo.name
            //              && t.FCheckDate >= fDate  //视图是这行和下一行出错，日期的比较不能在光电数据库用视图实现？？
            //              && t.FCheckDate <= tDate
            //              orderby t.FCheckDate descending
            //              select new SimpleTruckModels
            //              {
            //                  account = t.account,
            //                  id = t.FInterID,
            //                  billNo = t.FBillNo,
            //                  submitter = t.FSubmiter,
            //                  submitDate = t.FSubDate
            //              }).Distinct().ToList();
            ViewData["fromDate"] = fd;
            ViewData["toDate"] = td;
            ViewData["searchContent"] = searchContent;
            ViewData["pageCount"] = Math.Ceiling(res.Count() * 1.0 / pageNum);
            ViewData["currentPage"] = page;
            ViewData["model"] = result.Skip((page - 1) * pageNum).Take(pageNum).ToList();
            return View("TFinishBills");
        }

        [SessionTimeOutJsonFilter]
        public JsonResult AuditPermition(string billNo, bool isOK, string opinion)
        {
            WriteEventLog("审批放行申请", "billNo:" + billNo + ";isOK:" + isOK.ToString());
            var bills = db.vw_lg_truckPermition.Where(t => t.FBillNo == billNo).ToList();
            if (bills.Count() == 0) {
                return Json(new SimpleResultModel() { suc = false, msg = "此单已不存在，审批失败" });
            }

            if (bills.Where(t => t.FStatus == 1).Count() == 0) {
                return Json(new SimpleResultModel() { suc = false, msg = "此单已被审批，不能再重复处理" });
            }

            try {
                db.AuditLGTruckPermition(bills.First().account, bills.First().FInterID, userInfo.name, isOK ? 2 : 0, opinion);
            }
            catch (Exception ex) {
                return Json(new SimpleResultModel() { suc = false, msg = "审批失败：" + ex.Message });
            }

            //发送邮件通知
            MyEmail.PermitionNotify(bills.First().FBillNo, isOK,opinion);
            return Json(new SimpleResultModel() { suc = true, msg = "审批成功" });
        }

    }
}
