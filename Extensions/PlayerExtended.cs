using UnityEngine;

namespace ModTeleporter
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModTeleporter) }__").AddComponent<ModTeleporter>();
        }
    }
}
