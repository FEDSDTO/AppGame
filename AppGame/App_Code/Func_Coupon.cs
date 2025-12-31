using AppGame.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AppGame.App_Code
{
    public class Func_Coupon
    {
        string _SQL = string.Empty;
        List<SqlParameter> _Parameter = new List<SqlParameter>();
        DB_Connection _DB = new DB_Connection();
        GiftsEntities _Gifts = new GiftsEntities();

        public void Insert(string MemberId, int Gid, string MallId, string CouponNo, string Type, DateTime UsedStart, DateTime UsedEnd, DateTime Ahora)
        {
            try
            {
                _Gifts.Coupon.Add(new Coupon
                {
                    MemberId = MemberId,
                    GId = Gid,
                    MallId = MallId,
                    CouponNo = CouponNo,
                    Type = Type,
                    Source = "E",
                    UsedStart = UsedStart,
                    UsedEnd = UsedEnd,
                    CreateOn = Ahora,
                    Status = "N"
                });

                _Gifts.Coupon_Log.Add(new Coupon_Log
                {
                    MallId = MallId,
                    MemberId = MemberId,
                    GId = Gid,
                    CouponNo = CouponNo,
                    CreateOn = Ahora,
                    Status = "N"
                });
                _Gifts.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Func_Coupon_Insert",ex);
            }
        }
    }
}