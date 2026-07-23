using Vintagestory.API.Common;

namespace FeedOrFallow
{
    public class FeedOrFallowModSystem : ModSystem
    {
        private const string ConfigFile = "feedorfallow.json";

        public FeedOrFallowConfig Config { get; private set; }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);

            try
            {
                Config = api.LoadModConfig<FeedOrFallowConfig>(ConfigFile);
            }
            catch (System.Exception e)
            {
                api.Logger.Error("[feedorfallow] Failed to read {0}, falling back to defaults: {1}", ConfigFile, e);
                Config = null;
            }

            if (Config == null)
            {
                Config = new FeedOrFallowConfig();
                // Only the server should own the canonical config file.
                if (api.Side == EnumAppSide.Server)
                {
                    api.StoreModConfig(Config, ConfigFile);
                }
            }

            // Guard against typos in a hand-edited config (negative damage, zero intervals, ...).
            foreach (string note in Config.Sanitize())
            {
                api.Logger.Warning("[feedorfallow] Config adjusted: {0}", note);
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntityBehaviorClass("feedorfallow", typeof(EntityBehaviorFeedOrFallow));
        }

        public override void StartServerSide(Vintagestory.API.Server.ICoreServerAPI api)
        {
            base.StartServerSide(api);

            // One-line summary so a server admin (or tester filing a bug) can see the mod is live and
            // what settings are in effect, straight from server-main.log.
            api.Logger.Notification(
                "[feedorfallow] Active. GraceHours={0}, DamageInterval={1}h, Damage={2}/tick, MinGeneration={3}, GrazingCountsAsFeeding={4}, ConsumeFedItem={5}",
                Config.GraceHours, Config.DamageIntervalHours, Config.DamagePerInterval,
                Config.MinGeneration, Config.GrazingCountsAsFeeding, Config.ConsumeFedItem
            );
        }
    }
}
