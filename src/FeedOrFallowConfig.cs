namespace FeedOrFallow
{
    /// <summary>
    /// User-editable settings. Written to (and read from) ModConfig/feedorfallow.json
    /// in the game/server data folder on first launch.
    /// </summary>
    public class FeedOrFallowConfig
    {
        /// <summary>In-game hours an animal can go without food before it starts taking damage.
        /// One in-game day is 24 hours, so the default 72 = three days.</summary>
        public double GraceHours = 72;

        /// <summary>Once starving, how many in-game hours between each tick of hunger damage.</summary>
        public double DamageIntervalHours = 6;

        /// <summary>Hit points lost per damage tick. Most livestock have well under 20 HP,
        /// so a starving animal dies over a day or two of neglect.</summary>
        public float DamagePerInterval = 2f;

        /// <summary>Only animals at or above this generation are affected. Wild-spawned animals are
        /// generation 0; anything bred or raised in captivity climbs from there. Default 1 means
        /// "only your domesticated/captive-bred stock starve" — set to 0 to affect every animal
        /// that carries the behavior.</summary>
        public int MinGeneration = 1;

        /// <summary>Whether feeding consumes one of the held item.</summary>
        public bool ConsumeFedItem = true;

        /// <summary>When true, an animal eating on its own — grazing grass/crops or feeding from a
        /// trough — counts as feeding, so animals with forage keep themselves fed while animals shut
        /// in a barren pen still starve unless you hand-feed them. This works by reading the vanilla
        /// "lastMealEatenTotalHours" timestamp the game already stamps when an animal eats. Set false
        /// for a hardcore "must hand-feed everything" mode, where only your own feeding counts.</summary>
        public bool GrazingCountsAsFeeding = true;

        /// <summary>Babies are exempt: any animal whose entity code contains one of these substrings
        /// is skipped. Lets the behavior sit on a whole entity file (which usually covers adult and
        /// juvenile variants) without starving the young.</summary>
        public string[] ExemptBabyCodeParts = new string[] { "lamb", "chick", "piglet", "calf", "baby", "cub", "kid" };

        /// <summary>Real-time seconds between starvation checks. Coarse on purpose — starvation is
        /// measured in in-game hours, so there's no reason to evaluate it every tick.</summary>
        public double CheckIntervalSeconds = 10;

        /// <summary>
        /// Clamp hand-edited values into sane ranges so a typo in the config can't produce absurd or
        /// harmful behavior (negative damage would <em>heal</em> starving animals; a negative or zero
        /// damage interval would apply damage on every check; a tiny check interval hammers the
        /// server). Returns human-readable messages for any value that had to be corrected, so the
        /// mod can log them on startup.
        /// </summary>
        public System.Collections.Generic.List<string> Sanitize()
        {
            var notes = new System.Collections.Generic.List<string>();

            if (GraceHours < 0)
            {
                notes.Add($"GraceHours {GraceHours} < 0, clamped to 0");
                GraceHours = 0;
            }
            if (DamageIntervalHours < 0.1)
            {
                notes.Add($"DamageIntervalHours {DamageIntervalHours} too low, clamped to 0.1");
                DamageIntervalHours = 0.1;
            }
            if (DamagePerInterval < 0)
            {
                notes.Add($"DamagePerInterval {DamagePerInterval} < 0 (would heal), clamped to 0");
                DamagePerInterval = 0;
            }
            if (MinGeneration < 0)
            {
                notes.Add($"MinGeneration {MinGeneration} < 0, clamped to 0");
                MinGeneration = 0;
            }
            if (CheckIntervalSeconds < 0.5)
            {
                notes.Add($"CheckIntervalSeconds {CheckIntervalSeconds} too low, clamped to 0.5");
                CheckIntervalSeconds = 0.5;
            }
            if (ExemptBabyCodeParts == null)
            {
                ExemptBabyCodeParts = new string[0];
            }

            return notes;
        }
    }
}
