using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models
{

    public class ElCalculationNotes
    {
        public Guid NoteId { get; set; }
        public Guid RunId { get; set; }
        public string? Note { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public ElCalculation? Calculation { get; set; }

    }
}