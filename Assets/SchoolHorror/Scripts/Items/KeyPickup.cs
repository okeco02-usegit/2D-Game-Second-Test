using System.Collections.Generic;

namespace SchoolHorror
{
    public sealed class KeyPickup : Pickup
    {
        public static readonly List<KeyPickup> Active = new List<KeyPickup>();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        protected override void OnCollected()
        {
            Player.AddKey();
            Sound.PlayKeyPickup();
        }
    }
}
