using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;
using AppGame.App_Code;
using AppGame.Models;
using Newtonsoft.Json;

namespace AppGame.Controllers
{
    public class MemberGameGiftController : ApiController
    {
        MemberGameGift _MemberGameGift = new MemberGameGift();
        MemberGameGiftResult _MemberGameGiftResult = new MemberGameGiftResult();
        GiftsEntities _Gifts = new GiftsEntities();
        FEDSMBREntities _FEDSMBR = new FEDSMBREntities();
        OnlineEventsEntities _OnlineEvents = new OnlineEventsEntities();
        Func_EAN13 _FEAN = new Func_EAN13();
        Func_Coupon _FCoupon = new Func_Coupon();
        Func_CouponEP _FCouponEP = new Func_CouponEP();
        Activity _Activity = new Activity();
        CommonUtility commonUtility = new CommonUtility();

        public IHttpActionResult Get()
        {
            try
            {
                return Ok("成功");
            }
            catch (Exception ex)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace);
                commonUtility.Txt($"呼叫MemberGameGift_Get，錯誤，原因：{ex.Source}；{ex.Message}；{ex.StackTrace}");
                return Ok(_MemberGameGiftResult);
            }
        }

        [HttpPost]
        public IHttpActionResult Post(MemberGameGift Props)
        {
            try
            {
                commonUtility.Txt($"呼叫MemberGameGift_Post，{JsonConvert.SerializeObject(Props)}");
                return Ok(_Activity.App_202601(Props));
            }
            catch (Exception ex)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace);
                commonUtility.Txt($"呼叫MemberGameGift_Post，錯誤，會員編號：{Props.MemberId}，原因：{ex.Source}；{ex.Message}；{ex.StackTrace}");
                return Ok(_MemberGameGiftResult);
            }
        }
    }
}
