using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AppGame.Models
{
    public class MemberGameGift
    {
        public string ApToken { get; set; }
        public string MemberId { get; set; }
        public string GiftId { get; set; }
        public int Amount { get; set; }
    }
}