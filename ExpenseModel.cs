using System;

namespace PersonalExpenseTracker
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = "รายจ่าย"; // รายรับ หรือ รายจ่าย
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
