using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Logistics.Filter;
using Logistics.Models;
using Logistics.Util;
using System.Configuration;

namespace Logistics.Controllers
{

    public class CardBoardController : BaseController
    {
        public const int pageNumbers = 20;

        [SessionTimeOutFilter]
        public ActionResult Index()
        {
            ViewData["userName"] = userInfo.name;
            ViewData["carNo"] = userInfoDetail.carNo;
            ViewData["defaultLoc"] = userInfoDetail.defaultLoc;
            ViewData["currentLoc"] = GetCarLoc().location;
            ViewData["fromDate"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["toDate"] = DateTime.Now.ToString("yyyy-MM-dd");
            ViewData["locList"] = db.vw_lg_localInfo.Select(v => v.position).Distinct().ToList();
            ViewData["maxNum"] = userInfoDetail.maxCardsNumber;
            WriteEventLog("卡板主界面", "打开主界面");
            return View();
        }

        [SessionTimeOutFilter]
        public ActionResult KIndex()
        {
            ViewData["userName"] = userInfo.name;
            ViewData["carNo"] = userInfoDetail.carNo;
            ViewData["defaultLoc"] = userInfoDetail.defaultLoc;
            ViewData["currentLoc"] = GetCarLoc().location;
            ViewData["fromDate"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["toDate"] = DateTime.Now.ToString("yyyy-MM-dd");
            ViewData["locList"] = db.vw_lg_localInfo.Select(v => v.position).Distinct().ToList();            
            return View("Index.knockout");
        }

        [SessionTimeOutFilter]
        public ActionResult BIndex()
        {
            ViewData["userName"] = userInfo.name;
            ViewData["carNo"] = userInfoDetail.carNo;
            ViewData["defaultLoc"] = userInfoDetail.defaultLoc;
            ViewData["currentLoc"] = GetCarLoc().location;
            ViewData["fromDate"] = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
            ViewData["toDate"] = DateTime.Now.ToString("yyyy-MM-dd");
            ViewData["locList"] = db.vw_lg_localInfo.Select(v => v.position).Distinct().ToList();
            return View("Index.barcode");
        }

        //获取待处理列表
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult GetWaitingDealCards(string searchContent = "")
        {
            DateTime halfHourAgo = DateTime.Now.AddMinutes(-30);

            int loadNum = Int32.Parse(ConfigurationManager.AppSettings["loadNum"]); //抢单页面显示的条数

            //首先取消超过30分钟未装货的单
            
            var toDelete = (from v in db.vw_lg_cardBord
                            where v.status == "派单"
                            && v.loader_number != null
                            && v.loader_date <= halfHourAgo
                            select v).ToList();
            if (toDelete.Count() > 0)
            {
                foreach (var t in toDelete)
                {
                    if (t.return_status != "待处理") {
                        WriteEventLog("取消承载", "name:" + t.loader_name + ";card_no:" + t.card_no);
                        db.UpdateLgCardInfo(t.id, "TIC", "System", "取消", "");
                    }
                    else {
                        //已抢单未装货的，如果有返运申请的，超过30分钟未处理的，自动默认同意申请 
                        WriteEventLog("自动返运", "name:" + t.loader_name + ";card_no:" + t.card_no);
                        db.AuditLgReturnCard(t.card_no, "TIC", "SYSTEM", "OK");
                        db.UpdateLgCardInfo(t.id, "TIC", "System", "返运", "");
                    }
                }
            }   

            //司机当前位置
            int? currentLocPriority = GetCarLoc().priority;

            //优先级还不完善，暂时不作处理
            var items = (from v in db.vw_lg_cardBord
                         where (v.loader_number == null || v.loader_number == "")
                         && v.status == "派单"
                         && (v.department_priority >= currentLocPriority)
                         && v.card_no.Contains(searchContent)
                         orderby v.is_emergency descending, v.department_priority, v.card_no
                         select new
                         {
                             id = v.id,
                             cardNo = v.card_no,
                             dep = v.department,
                             depPos = v.department_pos,
                             des = v.destination,
                             packNum = v.pack_num,
                             isEmergency = v.is_emergency,
                             mobilePhone = v.mobile_phone
                         }).ToList().Union(
                             (from v in db.vw_lg_cardBord
                              where (v.loader_number == null || v.loader_number == "")
                              && v.status == "派单"
                              && (v.department_priority == null || v.department_priority < currentLocPriority)
                              && v.card_no.Contains(searchContent)
                              orderby v.is_emergency descending, v.department_priority, v.card_no
                              select new
                              {
                                  id = v.id,
                                  cardNo = v.card_no,
                                  dep = v.department,
                                  depPos = v.department_pos,
                                  des = v.destination,
                                  packNum = v.pack_num,
                                  isEmergency = v.is_emergency,
                                  mobilePhone = v.mobile_phone
                              }).ToList()
                         ).Take(loadNum).ToList();
            WriteEventLog("卡板", "刷新待处理列表,searchContent:" + searchContent);
            return Json(new { suc = true, items = items });
        }

        //接单，或者承载
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult AcceptCards(string ids)
        {
            var arr = ids.Split(',');
            int currentNumber=0;
            //承载前先判断是否超出卡板数限制,0表示不限制
            if (userInfoDetail.maxCardsNumber != 0)
            {
                currentNumber = db.vw_lg_cardBord.Where(v => v.loader_number == userInfo.cardNo && v.status != "结束" && v.status != "卸货" & v.status!= "作废").Count();
                int maxNumberNow = userInfoDetail.maxCardsNumber - currentNumber;
                if (arr.Count() > maxNumberNow)
                {
                    return Json(new SimpleResultModel() { suc = false, msg = "超出可承载卡板数限制，最多还可承载" + maxNumberNow + "件" });
                }
            }
            int failCount = 0;
            foreach (var a in arr) {
                int id = Int32.Parse(a);
                var item = db.vw_lg_cardBord.Where(v => v.id == id).First();
                if (item.status != "派单" || !string.IsNullOrEmpty(item.loader_number)) {
                    failCount++;
                }
                else {
                    db.UpdateLgCardInfo(id, userInfo.cardNo, userInfo.name, "承载", "");
                    //发送邮件通知部门联系人和目的地联系人
                    if (MyEmail.NotifyUser(item, userInfo.name, "接单")) {
                        WriteEventLog("卡板", "接单确认邮件发送成功");
                    }
                    else {
                        WriteEventLog("卡板", "接单确认邮件发送失败", -100);
                    }
                }
            }

            WriteEventLog("卡板", string.Format("承载成功：{0};失败:{1};最多:{2};已载:{3}",(arr.Count() - failCount),failCount,userInfoDetail.maxCardsNumber,currentNumber));

            if (failCount == arr.Count())
            {
                return Json(new SimpleResultModel() { suc = false, msg = "承载失败，请等待刷新后再试" });
            }
            else if (failCount > 0)
            {
                return Json(new SimpleResultModel() { suc = true, msg = string.Format("成功承载卡板{0}件,失败{1}件", arr.Count() - failCount, failCount) });
            }
            else
            {
                return Json(new SimpleResultModel() { suc = true, msg = "成功承载" + arr.Count() + "件卡板" });
            }
        }

        //处理中的卡板列表
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult GetDealingCards()
        {
            DateTime yesterday = DateTime.Now.AddDays(-1);
            var items = (from v in db.vw_lg_cardBord
                         where (v.loader_number == userInfo.cardNo
                         || v.transfer_num == userInfo.cardNo)
                         && v.status != "结束"
                         && v.status != "卸货"
                         && (v.status != "作废" || (v.status == "作废" && v.loader_date > yesterday))
                         orderby v.loader_date
                         select new
                         {
                             id = v.id,
                             cardNo = v.card_no,
                             dep = v.department,
                             depPos = v.department_pos,
                             des = v.destination,
                             status = v.status == "派单" ? "未装货" : (v.status == "作废" ? "作废" : "转运中"),
                             locked = v.return_status == "待处理" ? true : false,
                             returnApplier = v.return_apply_name,
                             returnApplyDate = v.return_apply_date,
                             returnReason = v.return_reason,
                             packNum = v.pack_num,
                             isEmergency = v.is_emergency,
                             mobilePhone = v.mobile_phone
                         }).ToList();
            
            WriteEventLog("卡板", "处理中的卡板列表");
            return Json(new { suc = true, items = items });
        }

        //开始装货
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult UploadingCards(string ids)
        {
            var arr = ids.Split(',');
            int failCount = 0;
            string lastLoc = "";
            int? lastPriority = 0;
            foreach (var a in arr)
            {
                int id = Int32.Parse(a);
                var item = db.vw_lg_cardBord.Where(v => v.id == id).First();
                if (item.status != "派单" || item.loader_number != userInfo.cardNo || item.return_status == "待处理")
                {
                    failCount++;
                }
                else
                {
                    db.UpdateLgCardInfo(id, userInfo.cardNo, userInfo.name, "装货","");
                    lastLoc = item.department_pos;
                    lastPriority = item.department_priority;
                }
            }

            //保存装货地点到车辆当前位置表
            if (!string.IsNullOrEmpty(lastLoc))
            {
                AddNewCarLocation(lastLoc, lastPriority);
            }

            WriteEventLog("卡板", "开始装货,成功：" + (arr.Count() - failCount) + ";失败：" + failCount);
            if (failCount == arr.Count())
            {
                return Json(new { suc = false, msg = "装货失败，请检查卡板当前状态", loc = lastLoc });
            }
            else if (failCount > 0)
            {
                return Json(new { suc = true, msg = string.Format("成功装货卡板{0}件,失败{1}件", arr.Count() - failCount, failCount), loc = lastLoc });
            }
            else
            {
                return Json(new { suc = true, msg = "成功装货" + arr.Count() + "件卡板", loc = lastLoc });
            }
        }

        [SessionTimeOutJsonFilter]
        public JsonResult UploadingCarsByNo(string cardNo)
        {
            string lastLoc = "";
            int? lastPriority = 0;

            var items = db.vw_lg_cardBord.Where(v => v.card_no == cardNo).ToList();
            if (items.Count() == 0) {
                WriteEventLog("装货", "失败，不存在此卡板：" + cardNo);
                return Json(new SimpleResultModel() { suc = false, msg = "不存在此卡板编号，请重试" });
            }
            var item = items.First();
            if (item.status != "派单" || item.loader_number != userInfo.cardNo || item.return_status == "待处理") {
                WriteEventLog("装货", "失败，请检查卡板状态：" + cardNo,-10);
                return Json(new SimpleResultModel() { suc = false, msg = "装货失败，请检查卡板状态" });
            }

            db.UpdateLgCardInfo(item.id, userInfo.cardNo, userInfo.name, "装货", "");
            lastLoc = item.department_pos;
            lastPriority = item.department_priority;

            //保存装货地点到车辆当前位置表
            if (!string.IsNullOrEmpty(lastLoc)) {
                AddNewCarLocation(lastLoc, lastPriority);
            }
            WriteEventLog("装货", "成功：" + cardNo);
            return Json(new { suc = true, msg = "装货成功：" + cardNo, loc = lastLoc });
        }

        //开始卸货
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult ReceivingCards(string ids)
        {
            var arr = ids.Split(',');
            int failCount = 0;
            string lastLoc = "";
            int? lastPriority = 0;
            foreach (var a in arr)
            {
                int id = Int32.Parse(a);
                var item = db.vw_lg_cardBord.Where(v => v.id == id).First();
                if (item.status != "转运中" || item.transfer_num != userInfo.cardNo || item.return_status == "待处理")
                {
                    failCount++;
                }
                else
                {
                    lastLoc = item.destination;
                    lastPriority = Int32.Parse(item.destination_priority);
                    db.UpdateLgCardInfo(id, userInfo.cardNo, userInfo.name, "卸货",userInfoDetail.carNo);
                    //发送邮件通知部门联系人和目的地联系人
                    if (MyEmail.NotifyUser(item, userInfo.name, "卸货"))
                    {
                        WriteEventLog("卡板", "卸货确认邮件发送成功");
                    }
                    else
                    {
                        WriteEventLog("卡板", "卸货确认邮件发送失败", -100);
                    }
                }
            }            

            //保存卸货地点到车辆当前位置表
            if (!string.IsNullOrEmpty(lastLoc))
            {
                AddNewCarLocation(lastLoc, lastPriority);
            }
            WriteEventLog("卡板", "开始卸货,成功：" + (arr.Count() - failCount) + ";失败：" + failCount);
            if (failCount == arr.Count())
            {
                return Json(new { suc = false, msg = "卸货失败，请检查卡板当前状态" , loc = lastLoc });
            }
            else if (failCount > 0)
            {
                return Json(new {  suc = true, msg = string.Format("成功卸货卡板{0}件,失败{1}件", arr.Count() - failCount, failCount) , loc = lastLoc });
            }
            else
            {
                return Json(new { suc = true, msg = "成功卸货" + arr.Count() + "件卡板" , loc = lastLoc });
            }
        }


        //弃单
        //[SessionTimeOutJsonFilterAttribute]
        //public JsonResult GiveUpbills(string ids)
        //{
        //    var arr = ids.Split(',');
        //    try {
        //        foreach (var ar in arr) {
        //            int id = Int32.Parse(ar);
        //            db.UpdateLgCardInfo(id, userInfo.cardNo, userInfo.name, "取消", "");
        //        }
        //    }
        //    catch (Exception ex) {
        //        return Json(new SimpleResultModel() { suc = false, msg = "操作失败：" + ex.Message });
        //    }
        //    return Json(new SimpleResultModel() { suc = true, msg = "操作成功" });
        //}

        //查询已结束申请
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult GetFinishCards(string fr_date, string to_date, string dep, string des)
        {
            DateTime frDate, toDate;
            if (!DateTime.TryParse(fr_date, out frDate))
            {
                return Json(new SimpleResultModel() { suc = false, msg = "起始日期不能为空" });
            }
            if (!DateTime.TryParse(to_date, out toDate))
            {
                return Json(new SimpleResultModel() { suc = false, msg = "结束日期不能为空" });
            }
            toDate = toDate.AddDays(1);

            var items = (from v in db.vw_lg_cardBord
                         where v.receiver_num == userInfo.cardNo
                         && (v.status == "结束" || v.status == "卸货")
                         && v.receiver_date >= frDate
                         && v.receiver_date <= toDate
                         && v.department_pos.Contains(dep)
                         && v.destination.Contains(des)
                         orderby v.receiver_date descending
                         select new FinishCardsModel
                         {
                             cardNo = v.card_no,
                             dep = v.department,
                             depPos = v.department_pos,
                             des = v.destination,
                             date = v.receiver_date,
                             packNum = v.pack_num,
                             isEmergency = v.is_emergency,
                             mobilePhone = v.mobile_phone
                         }).ToList();
            var total = items.Count();
            WriteEventLog("卡板", string.Format("查询已结束申请,{0}~{1},dep:{2};des:{3}", fr_date, to_date, dep, des));
            if (total == 0)
            {
                return Json(new SimpleResultModel() { suc = false, msg = "查询不到符合条件的记录" });
            }
            Session["finishRecords"] = items;
            return Json(new { suc = true, rows = items.Take(pageNumbers), pages = Math.Ceiling((total * 1.0) / pageNumbers) });
        }

        [SessionTimeOutJsonFilterAttribute]
        public JsonResult GetFinishCardsPage(int page)
        {
            if (Session["finishRecords"] == null)
            {
                return Json(new SimpleResultModel() { suc = false, msg = "当前没有数据，请重新查询" });
            }
            List<FinishCardsModel> list = (List<FinishCardsModel>)Session["finishRecords"];
            WriteEventLog("卡板", "翻页：" + page);
            return Json(new { suc = true, rows = list.Skip((page - 1) * pageNumbers).Take(pageNumbers).ToList() });
        }

        //修改默认车辆位置
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult UpdateDefaultLoc(string newLoc)
        {
            try
            {
                db.UpdateLgDefaultLoc(userInfo.cardNo, newLoc);
                ClearUserInfoDetail();
            }
            catch (Exception ex)
            {
                WriteEventLog("更新默认始发地", ex.Message, -100);
                return Json(new SimpleResultModel() { suc = false, msg = ex.Message });
            }
            WriteEventLog("更新默认始发地", "成功更改为：" + newLoc);
            return Json(new SimpleResultModel() { suc = true, msg = "操作成功" });
        }
        
        //修改车辆当前位置
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult UpdateCurrentLoc(string newLoc)
        {
            int? newPriority = db.vw_lg_localInfo.Where(l => l.position == newLoc).First().priority;
            db.lg_carLocation.Add(new lg_carLocation()
            {
                loc = newLoc,
                priority = newPriority,
                car_no = userInfoDetail.carNo,
                dtime = DateTime.Now
            });
            db.SaveChanges();
            return Json(new SimpleResultModel() { suc = true, msg = "修改成功" });
        }

        //当前车辆位置
        private carLocationModel GetCarLoc()
        {
            string carNo = userInfoDetail.carNo;
            DateTime beginOfToday = DateTime.Parse(DateTime.Now.ToShortDateString());
            var carLocs = (from c in db.lg_carLocation
                           where c.dtime > beginOfToday
                           && c.car_no == carNo
                           orderby c.dtime descending
                           select c).ToList();
            if (carLocs.Count() > 0)
            {
                var carLoc = carLocs.First();
                return new carLocationModel()
                {
                    location = carLoc.loc,
                    priority = carLoc.priority,
                    dtime = carLoc.dtime
                };
            }
            else
            {
                return new carLocationModel()
                {
                    location = userInfoDetail.defaultLoc,
                    priority = userInfoDetail.defaultLocPriority == null ? 0 : userInfoDetail.defaultLocPriority,
                    dtime = beginOfToday
                };
            }
        }

        //新增一条车辆位置记录
        private void AddNewCarLocation(string loc, int? priority)
        {
            var lo = new lg_carLocation();
            lo.loc = loc;
            lo.priority = priority;
            lo.car_no = userInfoDetail.carNo;
            lo.dtime = DateTime.Now;
            db.lg_carLocation.Add(lo);
            db.SaveChanges();
        }

        //处理返运申请
        [SessionTimeOutJsonFilterAttribute]
        public JsonResult HandelReturnApply(string cardNo,string result){
            try {
                db.AuditLgReturnCard(cardNo, userInfo.cardNo, userInfo.name, result);
                if (result.Equals("OK")) {
                    //OK的话，要将卡板状态设置为上货状态
                    int id = db.vw_lg_cardBord.Single(c => c.card_no == cardNo).id;
                    db.UpdateLgCardInfo(id, userInfo.cardNo, userInfo.name, "返运", "");
                }
            }
            catch (Exception ex) {
                WriteEventLog("返运", "卡板号：" + cardNo + ";处理失败：" + ex.Message, -100);
                return Json(new SimpleResultModel() { suc = false, msg = "处理失败" });
            }
            WriteEventLog("返运", "卡板号：" + cardNo + ";处理成功");
            return Json(new SimpleResultModel() { suc = true, msg = "处理成功" });
        }
                
    }
}
