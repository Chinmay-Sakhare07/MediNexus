namespace HospitalManagement.API.Models.DTOs
{
    public class BillItemDTO
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public string ItemDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddBillItemRequest
    {
        public int BillId { get; set; }
        public string ItemDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}