using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.PowerUps
{
    public class PowerHelpers
    {
        private readonly ModConfig Config;

        public int TotalFriendship { get; private set; }
        public int Power { get; private set; }
        public float ReturnPercent { get; private set; }
        public float ShieldPercent { get; private set; }
        public int DailyReturnFee { get; private set; }
        public int DailyShieldFee { get; private set; }
        public int DailyTimeReductionFee { get; private set; }
        public static Color GetChestColor(int index)
        {
            Color[] colors = new Color[]
            {
                Color.White,
                Color.Orange,
                Color.Red,
                Color.Pink,
                Color.Purple,
                Color.Blue,
                Color.Cyan,
                Color.Green,
                Color.Lime
            };

            if (index < 0 || index >= colors.Length)
                return Color.White;

            return colors[index];
        }
        public PowerHelpers(ModConfig config)
        {
            this.Config = config;
        }
        public void Recalculate()
        {
            ComputeTotalFriendship();
            ComputePower();
            ComputeReturnPercent();
            ComputeShieldPercent();
            ComputeFees();
            ComputeTimeReductionFee();
        }
        private void ComputeTotalFriendship()
        {
            TotalFriendship = 0;

            foreach (var entry in Game1.player.friendshipData.Values)
                TotalFriendship += entry.Points;
        }
        private void ComputePower()
        {
            Power = TotalFriendship / 100;
        }

        private void ComputeReturnPercent()
        {
            ReturnPercent = Math.Min(0.95f, Power / 1000f);
        }

        private void ComputeShieldPercent()
        {
            ShieldPercent = Math.Min(0.95f, Power / 1000f);
        }
        private void ComputeFees()
        {
            DailyReturnFee = (int)(ReturnPercent * 100);
            DailyShieldFee = (int)(ShieldPercent * 100);
        }
        public static void ApplyProductionTimeReduction(ModConfig Config, IMonitor Monitor)
        {
            if (!Config.EnableProcessTimeReduction)
                return;

            int power = ModEntry.FriendPower.Power;

            const int MaxTime = 8640;
            const int MinTime = 10;
            float pFriend = Math.Clamp(power / 1000f, 0f, 1f);
            float pPrime = (float)Math.Pow(pFriend, 0.85f);

            int effectiveTime = (int)(MinTime + (MaxTime - MinTime) * (1f - pPrime));
            float scale = (float)effectiveTime / MaxTime;

            Monitor.Log($"[Production Accelerator] Scale: {scale:F2}", LogLevel.Info);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs)
                {
                    if (pair.Value is SObject obj && obj.bigCraftable.Value && obj.minutesUntilReady.Value > 0)
                    {
                        int currentTime = obj.minutesUntilReady.Value;
                        int newTime = Math.Max(MinTime, (int)(currentTime * scale));
                        obj.minutesUntilReady.Value = newTime;

                        Monitor.Log($"[Production Accelerator] {obj.Name} at {location.Name} tile {pair.Key}: {currentTime} → {newTime}", LogLevel.Trace);
                    }
                }
            }
        }
        private void ComputeTimeReductionFee()
        {
            float pFriend = Math.Clamp(Power / 1000f, 0f, 1f);
            float pPrime = (float)Math.Pow(pFriend, 0.85f);
            float reductionPercent = pPrime * 100f;

            DailyTimeReductionFee = (int)(reductionPercent);
        }

        public static void TriggerReturnDamageExplosions(int damageTaken)
        {
            var location = Game1.currentLocation;
            var farmer = Game1.player;
            int reflectedDamage = damageTaken;
            int radius = (int)(ModEntry.FriendPower.ReturnPercent * 24);
            radius = Math.Clamp(radius, 2, 24);

            var monsters = location.characters
                .OfType<Monster>()
                .ToList();

            foreach (var monster in monsters)
            {
                float dist = Vector2.Distance(monster.Tile, farmer.Tile);
                if (dist <= radius)
                {
                    TriggerReturnExplosion(
                        monster,
                        reflectedDamage,
                        ModEntry.Instance.Monitor
                    );
                }
            }
        }
        public static void TriggerReturnExplosion(Monster damager, int reflectedDamage, IMonitor monitor)
        {
            if (damager == null || reflectedDamage <= 0)
                return;

            int radius = Math.Clamp(2 + (reflectedDamage / 10), 2, 18);

            Vector2 tile = damager.Tile;

            int beforeHP = damager.Health;

            Game1.currentLocation.explode(
                tile,
                radius,
                Game1.player,
                false,
                reflectedDamage
            );

            int afterHP = damager.Health;
        }
    }
}
