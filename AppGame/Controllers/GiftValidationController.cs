using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;
using AppGame.Models;

namespace AppGame.Controllers
{
    public class GiftValidationController : ApiController
    {
        GiftValidation _GiftValidation = new GiftValidation();
        MemberGameGiftResult _MemberGameGiftResult = new MemberGameGiftResult();
        GiftsEntities _Gifts = new GiftsEntities();

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
                _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                {
                    Controller = "APPGameGift",
                    CreateDate = DateTime.Now,
                    Message = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace)
                });
                _Gifts.SaveChanges();
                return Ok(_MemberGameGiftResult);
            }
        }

        [HttpPost]
        public IHttpActionResult Post(GiftValidation Props)
        {
            try
            {
                // validation 1 - APToken
                var _ApToken = _Gifts.PosToken.Where(g => g.Token == Props.ApToken).ToList();
                if (_ApToken.Count() == 0)
                {
                    _MemberGameGiftResult.Status = false;
                    _MemberGameGiftResult.Remark = "ApToken錯誤";
                    return Ok(_MemberGameGiftResult);
                }

                // validation 2 - GiftId
                int _Gid = 0;
                bool _GidFlag = int.TryParse(Props.GiftId, out _Gid);

                if (_GidFlag)
                {
                    Gifts Gifts = _Gifts.Gifts.Where(g => g.Id == _Gid).Where(g => g.IsUse == true).FirstOrDefault();

                    if (Gifts != null)
                    {
                        _MemberGameGiftResult.Status = true;
                        _MemberGameGiftResult.Remark = "驗證成功";
                        return Ok(_MemberGameGiftResult);
                    }
                    else
                    {
                        _MemberGameGiftResult.Status = false;
                        _MemberGameGiftResult.Remark = "GiftId的值非有效贈品編號";
                        return Ok(_MemberGameGiftResult);
                    }
                }
                else
                {
                    _MemberGameGiftResult.Status = false;
                    _MemberGameGiftResult.Remark = "GiftId的值非有效正整數";
                    return Ok(_MemberGameGiftResult);
                }
            }
            catch (Exception ex)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace);
                _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                {
                    Controller = "APPGameGift",
                    CreateDate = DateTime.Now,
                    Message = string.Format("錯誤，Gid：{0}，原因：{1}；{2}；{3}", Props.GiftId, ex.Source, ex.Message, ex.StackTrace)
                });
                _Gifts.SaveChanges();
                return Ok(_MemberGameGiftResult);
            }
        }
    }
}
