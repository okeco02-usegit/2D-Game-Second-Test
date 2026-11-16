namespace SchoolHorror
{
    public sealed class KeyPickup : Pickup
    {
        protected override void OnCollected()
        {
            Player.AddKey();
            Sound.PlayPickup();
        }
    }
}
