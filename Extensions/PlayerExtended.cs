using ModTeleporter.Managers;
using UnityEngine;

namespace ModTeleporter.Extensions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModTeleporter) }__").AddComponent<ModTeleporter>();
            new GameObject($"__{nameof(StylingManager)}__").AddComponent<StylingManager>();
        }
    }
}
