using AppGame.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AppGame.App_Code
{
    public class Func_CouponEP
    {
        string _SQL = string.Empty;
        List<SqlParameter> _Parameter = new List<SqlParameter>();
        DB_Connection _DB = new DB_Connection();
        GiftsEntities _Gifts = new GiftsEntities();
        public bool Insert(string MemberId, int Gid, string MallId, string CouponNo,  DateTime UsedStart, DateTime UsedEnd, DateTime Ahora)
        {
            try
            {
                _Gifts.EntityPresent.Add(new EntityPresent
                {
                    MemberId = MemberId,
                    MallId = MallId,
                    GId = Gid,
                    CouponNo = CouponNo,
                    Status = "N",
                    UsedStart = UsedStart,
                    UsedEnd = UsedEnd,
                    CreateOn = Ahora
                });
                _Gifts.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Func_CouponEP_Insert", ex);
            }
        }
    }
}