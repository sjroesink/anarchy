using System;
using System.Linq;

namespace Reborn
{
    public static class VitalModifierValidation
    {
        public static void Run(DistrictGame game,Action<bool,string> check)
        {
            var build=new CharacterBuild();int baseline=build.MaxNano;
            var item=build.inventory.At(6);var definition=item.Definition;
            var effect=AoSelfEffects.Get(26370);
            var oldItem=definition.modifiers;var oldEffect=effect.modifiers;
            var oldBuild=game.Build;float oldHealth=game.Health,oldNano=game.Nano;
            try
            {
                // Synthetic modifier fixtures, never a claim that the solar rifle grants nano.
                definition.modifiers=oldItem.Concat(new[]{new AoStatModifier{statId=221,amount=40}}).ToArray();
                check(build.MaxNano==baseline+40,"equipped direct MaxNanoEnergy modifier contributes once");
                effect.modifiers=oldEffect.Concat(new[]{new AoStatModifier{statId=221,amount=25}}).ToArray();
                build.Effects.Apply(effect.id,build.MaxNcu);
                check(build.MaxNano==baseline+65,"item and effect MaxNanoEnergy modifiers combine");
                build.Effects.Cancel(effect.id);item.slot=0;
                check(build.MaxNano==baseline,"removing item and effect removes their maximum nano bonuses");
                game.Build=build;game.Nano=baseline-5;
                build.Effects.Apply(effect.id,build.MaxNcu);game.ClampResources();
                check(game.Nano==baseline-5,"increasing nano capacity does not refill current nano");
                game.Nano=build.MaxNano;game.CancelNano(effect.id);
                check(game.Nano==baseline,"cancelling max-nano effect immediately clamps current energy");
                build.Effects.Apply(effect.id,build.MaxNcu);game.Nano=build.MaxNano;
                build.Effects.Tick(effect.duration+.1f);game.ClampResources();
                check(game.Nano==baseline,"expired max-nano effect cannot leave energy above capacity");
            }
            finally{definition.modifiers=oldItem;effect.modifiers=oldEffect;game.Build=oldBuild;game.Health=oldHealth;game.Nano=oldNano;}
        }
    }
}
