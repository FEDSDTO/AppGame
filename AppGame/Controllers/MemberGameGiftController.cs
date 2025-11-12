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
    public class MemberGameGiftController : ApiController
    {
        MemberGameGift _MemberGameGift = new MemberGameGift();
        MemberGameGiftResult _MemberGameGiftResult = new MemberGameGiftResult();
        GiftsEntities _Gifts = new GiftsEntities();
        FEDSMBREntities _FEDSMBR = new FEDSMBREntities();
        OnlineEventsEntities _OnlineEvents = new OnlineEventsEntities();
        Func_EAN13 _FEAN = new Func_EAN13();

        public IHttpActionResult Get()
        {
            try
            {
                return Ok("成功");
            }
            catch(Exception ex)
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
        public IHttpActionResult Post(MemberGameGift Props)
        {
            try
            {
                // step1. Validate the value of ApToken 
                var _ApToken = _Gifts.PosToken.Where(g => g.Token == Props.ApToken).ToList();
                if (_ApToken.Count() == 0)
                {
                    _MemberGameGiftResult.Status = false;
                    _MemberGameGiftResult.Remark = "ApToken錯誤";
                    return Ok(_MemberGameGiftResult);
                }

                // step2. Bring the value of『GiftId』into the Database『Gifts』- Datatable『Gifts』 ，and validate the value of『Id』
                long _Gid = 0;
                bool _GiftFlag = long.TryParse(Props.GiftId, out _Gid);
                
                if(_GiftFlag)
                {
                   Gifts Gifts = _Gifts.Gifts.Where(g => g.Id == _Gid).Where(g => g.IsUse == true).FirstOrDefault();

                    if(Gifts != null)
                    {
                        // step3. 將GiftId帶到OnlineEvents資料庫 - GiftsComparison資料表看是否存在資料
                        // if exists，表示此贈品為主贈品，需再根據資料表對應到的子贈品random出最後贈送的贈品
                        // if not，則直接贈送此贈品
                        List<GiftsComparison> _liGiftsComparison = _OnlineEvents.GiftsComparison.Where(o => o.MainGiftId == _Gid).Where(o=>o.Status==true).ToList();

                        if(_liGiftsComparison.Count > 0)
                        {
                            // step3-1-1. 根據對應到的子贈品，Random一個贈品Id，並回填至Gid
                            Random _rnGifts = new Random(Guid.NewGuid().GetHashCode());
                            int _RandomResult = _rnGifts.Next(1, _liGiftsComparison.Count);

                            GiftsComparison _RealGiftInfo = _liGiftsComparison[_RandomResult];
                            _Gid = (long)_RealGiftInfo.ComparisonGiftId;

                            // step3-1-2. According to the result of random，bring into the Datatable『Gifts』, then validate
                            // if illegal，return & end
                            // if legal，與原本無需random程式一同往下執行
                            Gifts = _Gifts.Gifts.Where(g => g.Id == _Gid).Where(g => g.IsUse == true).FirstOrDefault();

                            if(Gifts == null)
                            {
                                _MemberGameGiftResult.Status = false;
                                _MemberGameGiftResult.Remark = "GiftId的值非有效贈品編號";

                                _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                                {
                                    Controller = "APPGameGift",
                                    CreateDate = DateTime.Now,
                                    Message = "錯誤，GiftId的值非有效贈品編號"
                                });
                                _Gifts.SaveChanges();

                                return Ok(_MemberGameGiftResult);
                            }
                        }

                        // step 3-2. By the type of 『Gift』，將之塞到對應資料表
                        // Type = C (抵用券)，Insert into Coupon & CouponLog
                        // Type = L (摸彩券)，Insert into Coupon & CouponLog
                        // Type = EP (來店禮)，Insert into EntityPresent & EntityPresentLog

                        // Find GiftNo
                        string GiftNo = Gifts.GiftsNo;
                        string MallId = "10";

                        // step 3-2-1. find StartNo, UsedStart & UsedEnd from DataTable UsedRule 
                        UsedRule _UsedRule = _Gifts.UsedRule.Where(r => r.GId == _Gid).Where(r => r.MallId == MallId).Where(r => r.IsUse == true).FirstOrDefault();                        
                        DateTime UsedStart = (DateTime)_UsedRule.UsedStart;
                        DateTime UsedEnd = (DateTime)_UsedRule.UsedEnd;

                        // 取得StartNo (從資料庫撈出這批券號新增後最後的StartNo，再減掉贈送數量，取得初始值)
                        int _StartNo = _FEAN.GetStartNo(Gifts.Id, Props.Amount, MallId) - Props.Amount;
                        string CouponNo = string.Empty;

                        for (int i = 0; i < Props.Amount; i++)
                        {    
                            // step 3-2-2 generate CouponNo
                            // 抵用券 & 摸彩券
                            if(Gifts.Type == "C" || Gifts.Type == "L")
                            {
                                CouponNo = _FEAN.GenerateCouponNo(MallId, GiftNo, _StartNo.ToString());
                            }
                            else
                            {
                                // 來店禮
                                CouponNo = _FEAN.GenerateCouponNoByTypeEP(MallId, GiftNo, _StartNo.ToString());
                            }

                            DateTime Ahora = DateTime.Now;

                            if (CouponNo.Length > 13)
                            {
                                _MemberGameGiftResult.Status = false;
                                _MemberGameGiftResult.Remark = "贈品券號產生有誤";

                                _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                                {
                                    Controller = "APPGameGift",
                                    CreateDate = DateTime.Now,
                                    Message = CouponNo
                                });
                                _Gifts.SaveChanges();

                                return Ok(_MemberGameGiftResult);
                            }

                            // step 3-2-3 insert  
                            switch (Gifts.Type)
                            {
                                case "C":
                                case "L":
                                    // insert into Coupon & CouponLog
                                    {
                                        _Gifts.Coupon.Add(new Coupon
                                        {
                                            MemberId = Props.MemberId,
                                            GId = (int)_Gid,
                                            MallId = MallId,
                                            CouponNo = CouponNo,
                                            Type = Gifts.Type,
                                            Source = "E",
                                            UsedStart = UsedStart,
                                            UsedEnd = UsedEnd,
                                            CreateOn = Ahora,
                                            Status = "N"
                                        });

                                        _Gifts.Coupon_Log.Add(new Coupon_Log
                                        {
                                            MallId = MallId,
                                            MemberId = Props.MemberId,
                                            GId = (int)_Gid,
                                            CouponNo = CouponNo,
                                            CreateOn = Ahora,
                                            Status = "N"
                                        });

                                        _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                                        {
                                            Controller = "APPGameGift",
                                            CreateDate = Ahora,
                                            Message = string.Format(@"MemberId : {0}, GId : {1}, MallId : {2}, CouponNo : {3}",Props.MemberId, _Gid.ToString(), MallId, CouponNo)
                                        });
                                    }
                                    break;
                                case "EP":
                                    // insert into EntityPresent & EntityPresentLog
                                    _Gifts.EntityPresent.Add(new EntityPresent
                                    {
                                        MemberId = Props.MemberId,
                                        MallId = MallId,
                                        GId = (int)_Gid,
                                        CouponNo = CouponNo,
                                        Status = "N",
                                        UsedStart = UsedStart,
                                        UsedEnd = UsedEnd,
                                        CreateOn = Ahora
                                    });

                                    _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                                    {
                                        Controller = "APPGameGift",
                                        CreateDate = Ahora,
                                        Message = string.Format(@"MemberId : {0}, GId : {1}, MallId : {2}, CouponNo : {3}", Props.MemberId, _Gid.ToString(), MallId, CouponNo)
                                    });
                                    break;
                            }

                            _Gifts.SaveChanges();
                            _StartNo++;
                        }
                        _MemberGameGiftResult.Status = true;
                        _MemberGameGiftResult.Remark = string.Format(@"贈品新增成功 - MemberId : {0}, GId : {1}, MallId : {2}, Amount : {3}", Props.MemberId, _Gid.ToString(), MallId, Props.Amount);
                        return Ok(_MemberGameGiftResult);
                    }
                    else
                    {
                        _MemberGameGiftResult.Status = false;
                        _MemberGameGiftResult.Remark = "GiftId的值非有效贈品編號";

                        _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                        {
                            Controller = "APPGameGift",
                            CreateDate = DateTime.Now,
                            Message = "錯誤，GiftId的值非有效贈品編號"
                        });
                        _Gifts.SaveChanges();

                        return Ok(_MemberGameGiftResult);
                    }
                }
                else
                {
                    _MemberGameGiftResult.Status = false;
                    _MemberGameGiftResult.Remark = "GiftId的值非有效正整數";

                    _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                    {
                        Controller = "APPGameGift",
                        CreateDate = DateTime.Now,
                        Message = "錯誤，GiftId的值非有效正整數"
                    });
                    _Gifts.SaveChanges();

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
                    Message = string.Format("錯誤，會員編號：{0}，原因：{1}；{2}；{3}", Props.MemberId, ex.Source, ex.Message, ex.StackTrace)
                });
                _Gifts.SaveChanges();
                return Ok(_MemberGameGiftResult);
            }
        }
    }
}
