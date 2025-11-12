using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace AppGame
{
    public class Func_EAN13
    {
        string _SQL = string.Empty;
        List<SqlParameter> _Parameter = new List<SqlParameter>();
        DB_Connection _DB = new DB_Connection();

        /// <summary>
        /// 取得券號流水號起始值(已更新)
        /// </summary>
        /// <param name="GId">贈品編號</param>
        /// <param name="Quantity">數量</param>
        /// <returns>StartNo</returns>
        public int GetStartNo(int GId, int Quantity, string MallId)
        {
            _SQL = @"SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                                        BEGIN TRAN
                                        Update UsedRule Set StartNo += @StartNo
                                        OUTPUT inserted.StartNo 
                                        Where GId = @GId AND MallId = @MallId AND IsUse = 1
                                        COMMIT TRAN";

            _Parameter.Add(new SqlParameter("StartNo", Quantity));
            _Parameter.Add(new SqlParameter("GId", GId));
            _Parameter.Add(new SqlParameter("MallId", MallId));

            int _StartNo = Convert.ToInt32(_DB.GetValue(_SQL, _Parameter));

            return _StartNo;
        }
        
        /// <summary>
        /// 生成贈品券號 (for C & L)
        /// </summary>
        /// <param name="MallId">分公司</param>
        /// <param name="GiftNo">券代號</param>
        /// <param name="StartNo">目前流水號</param>
        /// <returns>贈品券號</returns>
        public string GenerateCouponNo(string MallId, string GiftNo, string StartNo)
        {
            try
            {
                // 流水號補足6碼
                string SerialNo = StartNo.PadLeft(6, '0');
                string CouponNo = string.Format(@"{0}{1}{2}", MallId, GiftNo, SerialNo);
                CouponNo = GenerateEAN13(CouponNo);

                return CouponNo;
            }
            catch(Exception ex)
            {
                string _Error = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace);
                return _Error;
            }
        }

        /// <summary>
        /// 生成APP來店禮券號 (for EP)
        /// </summary>
        /// <param name="MallId">分公司</param>
        /// <param name="GiftNo">券代號</param>
        /// <param name="StartNo">目前流水號</param>
        /// <returns>贈品券號</returns>
        public string GenerateCouponNoByTypeEP(string MallId, string GiftNo, string StartNo)
        {
            try
            {
                // 流水號補足7碼
                string SerialNo = StartNo.PadLeft(6, '0');
                string CouponNo = string.Format(@"{0}{1}{2}", MallId, GiftNo, SerialNo);                

                return CouponNo;
            }
            catch (Exception ex)
            {
                string _Error = string.Format("錯誤，原因：{0}；{1}；{2}", ex.Source, ex.Message, ex.StackTrace);
                return _Error;
            }
        }

        /// <summary>
        /// 產生EAN13碼
        /// </summary>
        /// <param name="_couponno">欲編碼的內容(需12碼)</param>
        /// <returns>EAN13 Code</returns>
        public string GenerateEAN13(string _couponNo)
        {
            string _CouponNumber = string.Empty;
            var _cArr = _couponNo.ToCharArray();

            int o = 0; //奇數總和
            int e = 0; //偶數總和

            for (int i = 0; i < _cArr.Length; i++)
            {
                //奇數
                if (i % 2 == 0) o = o + Convert.ToInt32(_cArr[i].ToString());
                //偶數
                if (i % 2 == 1) e = e + Convert.ToInt32(_cArr[i].ToString());
            }

            string _c = (e * 3 + o).ToString(); //偶之合 *3 + 奇之合
            int right = Convert.ToInt32(_c.Substring(_c.Length - 1)); //個位數
            int checkNo = 0;

            if (right != 0) checkNo = 10 - right;
            else checkNo = 0;

            _CouponNumber = _couponNo + checkNo;

            return _CouponNumber;

        }

        /// <summary>
        /// 取得會員編號檢查碼(EAN10)
        /// </summary>
        /// <param name="Body">前9碼</param>
        /// <returns>完整會員編號</returns>
        public string GenerateMemberNo(string Body)
        {
            //位數

            int sum = 0;

            //字串轉陣列
            char[] _Char = Body.ToCharArray();

            //奇數位 加權 *1
            for (int i = 0; i < _Char.Length; i += 2)
            {
                sum += Convert.ToInt32(_Char[i].ToString()) * 1;
            }
            //偶數位 加權 *3
            for (int i = 1; i < _Char.Length; i += 2)
            {
                sum += Convert.ToInt32(_Char[i].ToString()) * 3;
            }

            //Sum長度
            int sumLength = sum.ToString().Count();

            // 10 - (Sum的個位數)
            string res = (10 - Convert.ToInt32(sum.ToString().Substring(sumLength - 1, 1))).ToString();

            //return 個位數
            return res.Substring(res.Length - 1, 1);
        }

    }
}