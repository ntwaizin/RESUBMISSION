namespace PROG_P2.Models
{
    /// <summary>The race result for one enrolment (one result per enrolment).</summary>
    public class Result
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string ResultStatus { get; set; } = "Pending";

        public Enrolment Enrolment { get; set; } = null!;
    }
}
