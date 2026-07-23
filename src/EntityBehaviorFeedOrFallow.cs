using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace FeedOrFallow
{
    /// <summary>
    /// Kills an animal that goes too long without eating. "Eating" means either hand-feeding (our
    /// OnInteract) or the animal feeding itself — grazing grass/crops or eating from a trough. We
    /// detect self-feeding by reading <c>lastMealEatenTotalHours</c>, the timestamp the vanilla
    /// "seekfoodandeat" AI stamps on the entity every time it consumes a food source (the same value
    /// EntityBehaviorHarvestable already uses for body-condition drops). After a grace period the
    /// animal takes periodic hunger damage until it eats again — or dies.
    ///
    /// All state lives in WatchedAttributes so it survives save/reload. Runs server-side only.
    /// </summary>
    public class EntityBehaviorFeedOrFallow : EntityBehavior
    {
        private const string LastFedKey = "ffLastFedHours";
        private const string LastDamageKey = "ffLastDamageHours";

        // Written by the vanilla eating AI (VSEssentials AiTaskSeekFoodAndEat) whenever the animal
        // grazes or feeds from a trough. Reading it is how grazing counts as feeding.
        private const string VanillaLastMealKey = "lastMealEatenTotalHours";

        private FeedOrFallowConfig config;
        private float secondsSinceCheck;

        // Effective minimum generation for this animal. Defaults to the global config value but can be
        // overridden per entity in the patch, e.g. tamed mounts that are "tamed" regardless of breeding:
        //   { "code": "feedorfallow", "minGeneration": 0 }
        private int minGeneration;

        public EntityBehaviorFeedOrFallow(Entity entity) : base(entity) { }

        public override string PropertyName() => "feedorfallow";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            config = entity.Api.ModLoader.GetModSystem<FeedOrFallowModSystem>()?.Config;

            minGeneration = config?.MinGeneration ?? 1;
            if (attributes != null && attributes["minGeneration"].Exists)
            {
                minGeneration = attributes["minGeneration"].AsInt(minGeneration);
            }
        }

        /// <summary>Does the starvation rule apply to this particular animal?</summary>
        private bool Applies()
        {
            if (config == null) return false;

            int generation = entity.WatchedAttributes.GetInt("generation", 0);
            if (generation < minGeneration) return false;

            string[] exempt = config.ExemptBabyCodeParts;
            if (exempt != null && entity.Code != null)
            {
                string path = entity.Code.Path;
                for (int i = 0; i < exempt.Length; i++)
                {
                    if (!string.IsNullOrEmpty(exempt[i]) && path.Contains(exempt[i])) return false;
                }
            }

            return true;
        }

        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            // Authoritative starvation runs server-side only.
            if (entity.World.Side != EnumAppSide.Server) return;
            if (config == null || !entity.Alive) return;

            secondsSinceCheck += deltaTime;
            if (secondsSinceCheck < config.CheckIntervalSeconds) return;
            secondsSinceCheck = 0;

            if (!Applies()) return;

            double now = entity.World.Calendar.TotalHours;

            // First time we see this animal: start its clock now rather than instantly starving
            // something that existed before the mod (or this behavior) was added.
            if (!entity.WatchedAttributes.HasAttribute(LastFedKey))
            {
                entity.WatchedAttributes.SetDouble(LastFedKey, now);
                return;
            }

            double lastFed = EffectiveLastFedHours(now);
            if (now - lastFed <= config.GraceHours)
            {
                // Well-fed (grazed recently or hand-fed) — clear any starvation-damage marker.
                if (entity.WatchedAttributes.HasAttribute(LastDamageKey))
                {
                    entity.WatchedAttributes.RemoveAttribute(LastDamageKey);
                }
                return;
            }

            // Starving. Throttle damage to one tick per DamageIntervalHours.
            double lastDamage = entity.WatchedAttributes.GetDouble(LastDamageKey, lastFed + config.GraceHours);
            if (now - lastDamage < config.DamageIntervalHours) return;

            entity.WatchedAttributes.SetDouble(LastDamageKey, now);
            entity.ReceiveDamage(
                new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Hunger },
                config.DamagePerInterval
            );
        }

        /// <summary>
        /// The most recent in-game hour at which this animal ate — the later of our own hand-feed
        /// stamp and (when grazing counts) the vanilla self-feeding stamp.
        /// </summary>
        private double EffectiveLastFedHours(double now)
        {
            double handFed = entity.WatchedAttributes.GetDouble(LastFedKey, now);

            if (config.GrazingCountsAsFeeding)
            {
                double lastMeal = entity.WatchedAttributes.GetDouble(VanillaLastMealKey, handFed);
                if (lastMeal > handFed) return lastMeal;
            }

            return handFed;
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (mode != EnumInteractMode.Interact || config == null) return;
            if (!Applies()) return;
            if (itemslot == null || itemslot.Empty) return;

            // Anything with food nutrition counts as feed (grain, vegetables, fruit, bread, ...).
            CollectibleObject collectible = itemslot.Itemstack?.Collectible;
            if (collectible == null || collectible.NutritionProps == null) return;

            if (entity.World.Side == EnumAppSide.Server)
            {
                ResetFeedTimer();

                if (config.ConsumeFedItem)
                {
                    itemslot.TakeOut(1);
                    itemslot.MarkDirty();
                }

                // Confirm to the player who fed it.
                IServerPlayer serverPlayer = (byEntity as EntityPlayer)?.Player as IServerPlayer;
                serverPlayer?.SendMessage(
                    GlobalConstants.InfoLogChatGroup,
                    Lang.Get("feedorfallow:fed", entity.GetName()),
                    EnumChatType.Notification,
                    null
                );
            }

            // Swallow the interaction so we don't also mount, harvest, or otherwise act on the animal.
            handled = EnumHandling.PreventSubsequent;
        }

        private void ResetFeedTimer()
        {
            entity.WatchedAttributes.SetDouble(LastFedKey, entity.World.Calendar.TotalHours);
            entity.WatchedAttributes.RemoveAttribute(LastDamageKey);
        }
    }
}
