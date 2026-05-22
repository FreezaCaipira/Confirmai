using Confirmai.Enums;

namespace Confirmai.Shared.Helpers
{
    /// <summary>
    /// Minimum player requirements per sport modality.
    /// Minimum = minimum viable game per side × 2 sides.
    /// </summary>
    public static class EventMinimums
    {
        public record Minimums(int MinOutfield, int MinGoalkeepers)
        {
            public int Total => MinOutfield + MinGoalkeepers;
        }

        private static readonly Dictionary<Sport, Minimums> Defaults = new()
        {
            // 4 linha + 1 goleiro por lado × 2 = 8 linha + 2 goleiros = 10 total
            [Sport.Futsal] = new(8, 2),

            // Poker não tem quórum numérico de jogadores de campo
            [Sport.Poker]  = new(0, 0),

            // Future sports (add enum values when implemented):
            // Society: 6 linha + 1 goleiro × 2 = 12 + 2 = 14
            // Campo:   10 linha + 1 goleiro × 2 = 20 + 2 = 22
        };

        public static Minimums Get(Sport sport) =>
            Defaults.TryGetValue(sport, out var m) ? m : new(0, 0);

        /// <summary>Returns true when the confirmed counts meet or exceed the minimums.</summary>
        public static bool HasQuorum(Sport sport, int confirmedOutfield, int confirmedGoalkeepers)
        {
            var m = Get(sport);
            if (m.Total == 0) return true; // no minimum defined
            return confirmedOutfield >= m.MinOutfield && confirmedGoalkeepers >= m.MinGoalkeepers;
        }

        /// <summary>How many players are still needed to reach quorum (0 if already met).</summary>
        public static int Lacking(Sport sport, int confirmedOutfield, int confirmedGoalkeepers)
        {
            var m = Get(sport);
            int lackOut = Math.Max(0, m.MinOutfield - confirmedOutfield);
            int lackGk  = Math.Max(0, m.MinGoalkeepers - confirmedGoalkeepers);
            return lackOut + lackGk;
        }
    }
}
