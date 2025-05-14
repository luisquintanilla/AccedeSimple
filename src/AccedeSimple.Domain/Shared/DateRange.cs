using System;

namespace AccedeSimple.Domain
{
    public record DateRange
    {
        public DateTime Start { get; init; }
        public DateTime End { get; init; }

        public DateRange(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentException("End date must be after start date");

            Start = start;
            End = end;
        }

        public bool Contains(DateTime date) =>
            date >= Start && date <= End;

        public int Days =>
            (End - Start).Days + 1;
    }
}