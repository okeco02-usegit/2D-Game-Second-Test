namespace SchoolHorror
{
    public sealed class BatteryPickup : Pickup
    {
        protected override void OnCollected()
        {
            Player.Flashlight.Recharge(GameConfig.BatteryPickupAmount);
            Sound.PlayPickup();
        }
    }
}
