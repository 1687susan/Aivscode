using System;
using System.ComponentModel;

namespace day1
{
    public class Order
    {
        [Description("訂單編號")]
        public string OrderId { get; set; } = "";
        [Description("客戶名稱")]
        public string CustomerName { get; set; } = "";
        [Description("訂單狀態")]
        public string Status { get; set; } = "";
        [Description("訂單日期")]
        public DateTime Date { get; set; }
        [Description("退換貨原因")]
        public string RefundReason { get; set; } = "";
        // 可依需求擴充其他欄位
    }
}
