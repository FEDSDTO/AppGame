using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using AppGame.App_Code;
using AppGame.Controllers;
using AppGame.Models;
using Newtonsoft.Json;

namespace AppGame.App_Code
{
    public class Activity : ApiController
    {
        MemberGameGift _MemberGameGift = new MemberGameGift();
        MemberGameGiftResult _MemberGameGiftResult = new MemberGameGiftResult();
        GiftsEntities _Gifts = new GiftsEntities();
        FEDSMBREntities _FEDSMBR = new FEDSMBREntities();
        OnlineEventsEntities _OnlineEvents = new OnlineEventsEntities();
        FEDSPOINTSHOPEntities _FEDSPOINTSHOP = new FEDSPOINTSHOPEntities();
        Func_EAN13 _FEAN = new Func_EAN13();
        Func_Coupon _FCoupon = new Func_Coupon();
        Func_CouponEP _FCouponEP = new Func_CouponEP();
        CommonUtility commonUtility = new CommonUtility();
        public MemberGameGiftResult App_20240916(MemberGameGift Props)
        {
            // step1. Validate the value of ApToken 
            var _ApToken = _Gifts.PosToken.Where(g => g.Token == Props.ApToken).ToList();
            if (_ApToken.Count() == 0)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = "ApToken錯誤";
                return _MemberGameGiftResult;
            }

            // step2. Bring the value of『GiftId』into the Database『Gifts』- Datatable『Gifts』 ，and validate the value of『Id』
            long _Gid = 0;
            bool _GiftFlag = long.TryParse(Props.GiftId, out _Gid);
            if (!_GiftFlag)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = "GiftId的值非有效正整數";

                commonUtility.Txt("");
                _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                {
                    Controller = "APPGameGift",
                    CreateDate = DateTime.Now,
                    Message = "錯誤，GiftId的值非有效正整數"
                });
                _Gifts.SaveChanges();

                return _MemberGameGiftResult;
            }

            Gifts Gifts = _Gifts.Gifts.Where(g => g.Id == _Gid).Where(g => g.IsUse == true).FirstOrDefault();

            if (Gifts == null)
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

                return _MemberGameGiftResult;
            }
            // step3. 將GiftId帶到OnlineEvents資料庫 - GiftsComparison資料表看是否存在資料
            // if exists，表示此贈品為主贈品，需再根據資料表對應到的子贈品random出最後贈送的贈品
            // if not，則直接贈送此贈品
            List<GiftsComparison> _liGiftsComparison = _OnlineEvents.GiftsComparison.Where(o => o.MainGiftId == _Gid).Where(o => o.Status == true).ToList();

            if (_liGiftsComparison.Count > 0)
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

                if (Gifts == null)
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

                    return _MemberGameGiftResult;
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
                if (Gifts.Type == "C" || Gifts.Type == "L")
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

                    return _MemberGameGiftResult;
                }

                // step 3-2-3 insert  
                switch (Gifts.Type)
                {
                    case "C":
                    case "L":
                        // insert into Coupon & CouponLog
                        {
                            _FCoupon.Insert(Props.MemberId, (int)_Gid, MallId, CouponNo, Gifts.Type, UsedStart, UsedEnd, Ahora);

                            _Gifts.SystemExchangeLog.Add(new SystemExchangeLog
                            {
                                Controller = "APPGameGift",
                                CreateDate = Ahora,
                                Message = string.Format(@"MemberId : {0}, GId : {1}, MallId : {2}, CouponNo : {3}", Props.MemberId, _Gid.ToString(), MallId, CouponNo)
                            });
                        }
                        break;
                    case "EP":
                        // insert into EntityPresent & EntityPresentLog
                        _FCouponEP.Insert(Props.MemberId, (int)_Gid, MallId, CouponNo, UsedStart, UsedEnd, Ahora);

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
            return _MemberGameGiftResult;
        }

        public MemberGameGiftResult App_202601(MemberGameGift Props)
        {
            #region 驗證輸入
            // step1. Validate the value of ApToken 
            var _ApToken = _Gifts.PosToken.Where(g => g.Token == Props.ApToken).ToList();
            if (_ApToken.Count() == 0)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = "ApToken錯誤";
                return _MemberGameGiftResult;
            }

            // step2. Bring the value of『GiftId』into the Database『Gifts』- Datatable『Gifts』 ，and validate the value of『Id』
            long _Gid = 0;
            bool _GiftFlag = long.TryParse(Props.GiftId, out _Gid);
            if (!_GiftFlag)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = "GiftId的值非有效正整數";
                commonUtility.Txt($"呼叫App_202601，錯誤，GiftId的值非有效正整數");

                return _MemberGameGiftResult;
            }

            Gifts Gifts = _Gifts.Gifts.Where(g => g.Id == _Gid).Where(g => g.IsUse == true).FirstOrDefault();

            if (Gifts == null)
            {
                _MemberGameGiftResult.Status = false;
                _MemberGameGiftResult.Remark = "GiftId的值非有效贈品編號";
                commonUtility.Txt($"呼叫App_202601，錯誤，GiftId的值非有效贈品編號");

                return _MemberGameGiftResult;
            }
            #endregion

            //****************寫死項目*****************
            //  1. 來店禮：未填住址改送抵用券(寫死Gid)
            //  2. 來店禮：Gid去對應點加金票券(寫死Gid)
            //  3. 來店禮：從指定會員(寫死)裡面抓出事先設定好的來店裡券
            //  4. 來店禮：點加金券 指定會員沒有對應分店的券，改送抵用券(寫死Gid)
            //*****************************************

            // step 3-2. By the type of 『Gift』，將之塞到對應資料表
            // Type = C (抵用券)，Insert into Coupon & CouponLog
            // Type = L (摸彩券)，Insert into Coupon & CouponLog
            // Type = EP (來店禮)，點加金票券 有填寫住址則送，沒填改送抵用券
            int _TargetMemberId = 1;        //指定會員ID
            //int _TargetGId = 11633;         // 正式 //改送抵用券(寫死Gid)
            int _TargetGId = 1688;         // 測試 //改送抵用券(寫死Gid)

            string _GiftsType = Gifts.Type; //紀錄更動後的GiftsType
            string MallId = "10";
            int _Memberid = Convert.ToInt32(Props.MemberId);
            int _FEDSVoucherId = 0;     //點加金券ID

            if (Gifts.Type == "EP")
            {
                Member _Member = _FEDSMBR.Member.Where(m => m.Id == _Memberid).FirstOrDefault();
                // 依據顧客個資 (FEDSMBR-Member)是否有填寫住址(City)判斷要送哪間分店

                // Gid去對應點加金票券(寫死)
                switch (Props.GiftId)
                {
                    //正式
                    //case "11678":    //MD甜甜圈券
                    //    _FEDSVoucherId = 409;
                    //測試
                    case "1695":    //MD甜甜圈券
                        _FEDSVoucherId = 90;
                        switch (_Member.City)
                        {
                            case "台北市":
                            case "基隆市":
                            case "宜蘭縣":
                                MallId = "55";
                                break;
                            case "新北市":
                            case "桃園市":
                                MallId = "54";
                                break;
                            case "新竹縣":
                                MallId = "72";
                                break;
                            case "新竹市":
                            case "苗栗縣":
                                MallId = "42";
                                break;
                            case "台中市":
                            case "彰化縣":
                            case "雲林縣":
                            case "嘉義市":
                            case "嘉義縣":
                                MallId = "53";
                                break;
                            case "台南市":
                            case "高雄市":
                            case "屏東縣":
                                MallId = "51";
                                break;
                            case "花蓮縣":
                            case "台東縣":
                                MallId = "52";
                                break;
                            default:
                                // 未填住址改送抵用券(寫死Gid)
                                commonUtility.Txt($"呼叫App_202601，未填住址改送抵用券");
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                        }
                        break;
                    //正式
                    //case "11680":    //NIKE 100元十足券
                    //    _FEDSVoucherId = 407;
                    //測試
                    case "1693":    //NIKE 100元十足券
                        _FEDSVoucherId = 92;
                        switch (_Member.City)
                        {
                            case "台北市":
                            case "基隆市":
                            case "宜蘭縣":
                                MallId = "55";
                                break;
                            case "新北市":
                                //板大、中山隨機分配
                                string[] options = { "54", "50" };
                                Random rnd = new Random();
                                // 根據陣列長度隨機取得索引值
                                string result = options[rnd.Next(options.Length)];

                                MallId = result;
                                //_MemberGameGiftResult.Remark = $"{MallId}";
                                //return _MemberGameGiftResult;
                                break;
                            case "桃園市":
                            case "新竹縣":
                            case "新竹市":
                            case "苗栗縣":
                                MallId = "40";
                                break;
                            case "台中市":
                            case "彰化縣":
                            case "雲林縣":
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                            case "嘉義市":
                            case "嘉義縣":
                            case "台南市":
                                MallId = "48";
                                break;
                            case "高雄市":
                            case "屏東縣":
                                MallId = "51";
                                break;
                            case "花蓮縣":
                            case "台東縣":
                                MallId = "52";
                                break;
                            default:
                                // 未填住址改送抵用券(寫死Gid)
                                commonUtility.Txt($"呼叫App_202601，未填住址改送抵用券");
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                        }
                        break;
                    //正式
                    //case "11679":    //台隆 100元十足券
                    //    _FEDSVoucherId = 408;
                    //測試
                    case "1694":    //台隆 100元十足券
                        _FEDSVoucherId = 91;
                        switch (_Member.City)
                        {
                            case "台北市":
                            case "基隆市":
                            case "宜蘭縣":
                            case "新北市":
                            case "桃園市":
                            case "新竹縣":
                                MallId = "54";
                                break;
                            case "新竹市":
                            case "苗栗縣":
                            case "台中市":
                            case "彰化縣":
                            case "雲林縣":
                            case "嘉義市":
                            case "嘉義縣":
                            case "台南市":
                            case "高雄市":
                            case "屏東縣":
                            case "花蓮縣":
                            case "台東縣":
                                MallId = "53";
                                break;
                            default:
                                // 未填住址改送抵用券(寫死Gid)
                                commonUtility.Txt($"呼叫App_202601，未填住址改送抵用券");
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                        }
                        break;
                    //正式
                    //case "11676":    //歐舒丹護手霜 30ml
                    //    _FEDSVoucherId = 411;
                    //測試
                    case "1691":    //歐舒丹護手霜 30ml
                        _FEDSVoucherId = 94;
                        switch (_Member.City)
                        {
                            case "台北市":
                            case "基隆市":
                            case "宜蘭縣":
                            case "新北市":
                                MallId = "54";
                                break;
                            case "桃園市":
                                MallId = "40";
                                break;
                            case "新竹縣":
                                MallId = "72";
                                break;
                            case "新竹市":
                            case "苗栗縣":
                                MallId = "42";
                                break;
                            case "台中市":
                            case "彰化縣":
                            case "雲林縣":
                                MallId = "53";
                                break;
                            case "嘉義市":
                            case "嘉義縣":
                                MallId = "37";
                                break;
                            case "台南市":
                            case "高雄市":
                            case "屏東縣":
                                MallId = "51";
                                break;
                            case "花蓮縣":
                            case "台東縣":
                                MallId = "52";
                                break;
                            default:
                                // 未填住址改送抵用券(寫死Gid)
                                commonUtility.Txt($"呼叫App_202601，未填住址改送抵用券");
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                        }
                        break;
                    //正式
                    //case "11677":    //商開 酪梨油
                    //    _FEDSVoucherId = 410;
                    //測試
                    case "1692":    //商開 酪梨油
                        _FEDSVoucherId = 93;
                        switch (_Member.City)
                        {
                            case "台北市":
                            case "基隆市":
                            case "宜蘭縣":
                                MallId = "55";
                                break;
                            case "新北市":
                                MallId = "50";
                                break;
                            case "桃園市":
                                MallId = "40";
                                break;
                            case "新竹縣":
                                MallId = "72";
                                break;
                            case "新竹市":
                            case "苗栗縣":
                                MallId = "42";
                                break;
                            case "台中市":
                            case "彰化縣":
                            case "雲林縣":
                                MallId = "53";
                                break;
                            case "嘉義市":
                            case "嘉義縣":
                            case "台南市":
                                MallId = "48";
                                break;
                            case "高雄市":
                            case "屏東縣":
                                MallId = "51";
                                break;
                            case "花蓮縣":
                            case "台東縣":
                                MallId = "52";
                                break;
                            default:
                                // 未填住址改送抵用券(寫死Gid)
                                commonUtility.Txt($"呼叫App_202601，未填住址改送抵用券");
                                _GiftsType = "C";
                                Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                                _Gid = Gifts.Id;
                                MallId = "10";
                                break;
                        }
                        break;
                    default:
                        commonUtility.Txt($"呼叫App_202601，錯誤，點加金券號不符合活動設定，改送抵用券");
                        // ※以防萬一 若Gid對應不到，改送抵用券(寫死Gid)
                        _GiftsType = "C";
                        Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                        _Gid = Gifts.Id;
                        MallId = "10";
                        break;
                }

                int _MallIdInt = Convert.ToInt32(MallId);
                // 從指定會員(寫死)裡面抓出事先設定好的來店裡券 →再轉到中獎會員身上
                var _mfv = (from mfv in _FEDSPOINTSHOP.MemberFEDSVoucher
                            join fvso in _FEDSPOINTSHOP.FEDSVoucherSalesOrder on mfv.FEDSVoucherSalesOrderNo equals fvso.SalesOrderNo
                            where mfv.MemberId == _TargetMemberId &&
                                  mfv.FEDSVoucherId == _FEDSVoucherId &&
                                  fvso.MallId == _MallIdInt
                            orderby mfv.Id ascending
                            select mfv).FirstOrDefault();

                // 點加金券 指定會員沒有對應分店的券，改送抵用券(寫死Gid)
                if (_mfv == null)
                {
                    commonUtility.Txt($"呼叫App_202601，指定會員沒有對應分店ID({MallId})的券，改送抵用券");
                    _GiftsType = "C";
                    Gifts = _Gifts.Gifts.Where(g => g.Id == _TargetGId).Where(g => g.IsUse == true).FirstOrDefault();
                    _Gid = Gifts.Id;
                    MallId = "10";
                }

                if (_GiftsType == "EP")
                {
                    _mfv.MemberId = _Memberid;  // 移轉點加金券

                    // MemberFEDSVoucherHistory 新增轉移紀錄
                    // 複製原有資料的紀錄，修改後新增
                    var _mfvh = (from mfvh in _FEDSPOINTSHOP.MemberFEDSVoucherHistory
                                 where mfvh.MemberFEDSVoucherId == _mfv.Id
                                 select mfvh).FirstOrDefault();

                    _FEDSPOINTSHOP.MemberFEDSVoucherHistory.Add(new MemberFEDSVoucherHistory
                    {
                        MemberId = _Memberid,
                        MemberFEDSVoucherId = _mfv.Id,
                        SalesOrderNo = _mfv.FEDSVoucherSalesOrderNo,
                        CreateDate = DateTime.Now,
                        Status = _mfvh.Status,
                        IsGenerateInvoice = _mfvh.IsGenerateInvoice,
                        Creator = "AppGameApi",
                        Remark = $"APP轉盤點加金票券從{_TargetMemberId}轉移給{_Memberid}"
                    });

                    _FEDSPOINTSHOP.SaveChanges();
                    commonUtility.Txt($"呼叫App_202601，贈品新增成功，MemberId : {Props.MemberId}, GId : {_Gid.ToString()}, MallId : {MallId}, FEDSVoucherId : {_FEDSVoucherId}");

                }
            }
            string GiftNo = Gifts.GiftsNo;

            if (_GiftsType == "C" || _GiftsType == "L")
            {
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
                    DateTime Ahora = DateTime.Now;
                    CouponNo = _FEAN.GenerateCouponNo(MallId, GiftNo, _StartNo.ToString());
                    if (CouponNo.Length > 13)
                    {
                        _MemberGameGiftResult.Status = false;
                        _MemberGameGiftResult.Remark = "贈品券號產生有誤";
                        commonUtility.Txt($"呼叫App_202601，錯誤，贈品券號產生有誤，{CouponNo}");

                        return _MemberGameGiftResult;
                    }

                    _FCoupon.Insert(Props.MemberId, (int)_Gid, MallId, CouponNo, Gifts.Type, UsedStart, UsedEnd, Ahora);

                    commonUtility.Txt($"呼叫App_202601，贈品新增成功，MemberId : {Props.MemberId}, GId : {_Gid.ToString()}, MallId : {MallId}, CouponNo : {CouponNo}");
                    _StartNo++;
                }
            }

            _MemberGameGiftResult.Status = true;
            _MemberGameGiftResult.Remark = string.Format(@"贈品新增成功 - MemberId : {0}, GId : {1}, MallId : {2}, Amount : {3}", Props.MemberId, _Gid.ToString(), MallId, Props.Amount);

            return _MemberGameGiftResult;
        }
    }
}