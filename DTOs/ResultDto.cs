using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Organiser records a result for an enrolment.</summary>
    public class ResultCreateDTO
    {
        [Range(1, int.MaxValue)]
        public int EnrolmentID { get; set; }

        /// <summary>Finish time as hh:mm:ss, e.g. "00:28:35".</summary>
        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? Position { get; set; }

        /// <summary>Finished, DNF, DNS or Pending.</summary>
        [Required]
        public string ResultStatus { get; set; } = "Finished";
    }

    /// <summary>Organiser updates an existing result.</summary>
    public class ResultUpdateDTO
    {
        public TimeSpan? FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int? Position { get; set; }

        [Required]
        public string ResultStatus { get; set; } = "Finished";
    }

    public class ResultResponseDTO
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = "";
        public int EventID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = "";
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string ResultStatus { get; set; } = "";
    }
}
