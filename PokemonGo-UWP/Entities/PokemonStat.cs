using Newtonsoft.Json;
using PokemonGo_UWP.Utils;
using PokemonGo_UWP.Utils.Helpers;
using PokemonGo_UWP.Views;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using Template10.Common;
using Template10.Mvvm;
using System.ComponentModel;
using POGOProtos.Settings.Master;
using System.Linq;
using System;
using System.Collections.Generic;

namespace PokemonGo_UWP.Entities
{
    public class AnyPokemonStat : IDisposable
    {
        public PokemonDataWrapper Data { get; set; }
        public MoveSettings Attack { get; set; }
        public MoveSettings SpecialAttack { get; set; }
        public PokemonType MainType { get; set; }
        public PokemonType ExtraType { get; set; }
        public AnyPokemonStat(PokemonDataWrapper pokemon)
        {
            Data = pokemon;

            MainType = GameClient.PokemonSettings.Where(f => f.PokemonId == Data.PokemonId).Select(s => s.Type).FirstOrDefault();
            ExtraType = GameClient.PokemonSettings.Where(f => f.PokemonId == Data.PokemonId).Select(s => s.Type2).FirstOrDefault();

            Attack = GameClient.MoveSettings.Where(f => f.MovementId == Data.Move1).FirstOrDefault();
            SpecialAttack = GameClient.MoveSettings.Where(f => f.MovementId == Data.Move2).FirstOrDefault();
        }

        public void Dispose()
        {

        }
    }

    public class MyPokemonStat : AnyPokemonStat
    {
        public Dictionary<PokemonType, int> TypeFactor { get; private set; }

        public MyPokemonStat(PokemonDataWrapper pokemon) : base(pokemon)
        {
            TypeFactor = new Dictionary<PokemonType, int>();

            foreach (var type in Enum.GetValues(typeof(PokemonType)))
            {
                GetFactorAgainst((PokemonType)type);
            }
        }

        private int GetFactorAgainst(PokemonType type)
        {
            if (TypeFactor.Keys.Contains(type))
            {
                return TypeFactor[type];
            }

            int factor = 0;
            if (GetBestTypes(type).Any(a => a == Attack.PokemonType))
            {
                factor += 2;
                if (MainType == Attack.PokemonType || ExtraType == Attack.PokemonType)
                    factor += 1;
            }
            if (GetWorstTypes(type).Any(a => a == Attack.PokemonType)) factor -= 2;

            if (GetBestTypes(type).Any(a => a == SpecialAttack.PokemonType))
            {
                factor += 2;
                if (MainType == SpecialAttack.PokemonType || ExtraType == SpecialAttack.PokemonType)
                    factor += 1;
            }
            if (GetWorstTypes(type).Any(a => a == SpecialAttack.PokemonType)) factor -= 2;

            TypeFactor.Add(type, factor);

            return factor;
        }

        public int GetFactorAgainst(int cp, bool isTraining)
        {
            decimal percent = 0.0M;
            if (cp > Data.Cp)
                percent = (decimal)Data.Cp / (decimal)cp * -100.0M;
            else
                percent = (decimal)cp / (decimal)Data.Cp * 100.0M;

            int factor = (int)((100.0M - Math.Abs(percent)) / 5.0M) * Math.Sign(percent);

            if (isTraining && cp <= Data.Cp)
                factor -= 100;

            return factor;
        }

        private int GetFactorAgainst(PokemonSettings pokemon)
        {
            int factor = GetFactorAgainst(pokemon.Type);
            factor += GetFactorAgainst(pokemon.Type2);
            return factor;
        }

        public void Dispose()
        {
            if (TypeFactor != null)
            {
                TypeFactor.Clear();
            }
        }

        public static IEnumerable<PokemonType> GetBestTypes(PokemonType defenceType)
        {
            switch (defenceType)
            {
                case PokemonType.Bug:
                    return new PokemonType[] { PokemonType.Rock, PokemonType.Fire, PokemonType.Flying };
                case PokemonType.Dark:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Fairy, PokemonType.Fighting };
                case PokemonType.Dragon:
                    return new PokemonType[] { PokemonType.Dragon, PokemonType.Fire, PokemonType.Ice };
                case PokemonType.Electric:
                    return new PokemonType[] { PokemonType.Ground };
                case PokemonType.Fairy:
                    return new PokemonType[] { PokemonType.Poison, PokemonType.Steel };
                case PokemonType.Fighting:
                    return new PokemonType[] { PokemonType.Fairy, PokemonType.Flying, PokemonType.Psychic };
                case PokemonType.Fire:
                    return new PokemonType[] { PokemonType.Ground, PokemonType.Rock, PokemonType.Water };
                case PokemonType.Flying:
                    return new PokemonType[] { PokemonType.Electric, PokemonType.Ice, PokemonType.Rock };
                case PokemonType.Ghost:
                    return new PokemonType[] { PokemonType.Dark, PokemonType.Ghost };
                case PokemonType.Grass:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Fire, PokemonType.Flying, PokemonType.Ice, PokemonType.Poison };
                case PokemonType.Ground:
                    return new PokemonType[] { PokemonType.Grass, PokemonType.Ice, PokemonType.Water };
                case PokemonType.Ice:
                    return new PokemonType[] { PokemonType.Fighting, PokemonType.Fire, PokemonType.Rock, PokemonType.Steel };
                case PokemonType.None:
                    return new PokemonType[] { };
                case PokemonType.Normal:
                    return new PokemonType[] { PokemonType.Fighting };
                case PokemonType.Poison:
                    return new PokemonType[] { PokemonType.Ground, PokemonType.Psychic };
                case PokemonType.Psychic:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Dark, PokemonType.Ghost };
                case PokemonType.Rock:
                    return new PokemonType[] { PokemonType.Fighting, PokemonType.Grass, PokemonType.Ground, PokemonType.Steel, PokemonType.Water };
                case PokemonType.Steel:
                    return new PokemonType[] { PokemonType.Fighting, PokemonType.Fire, PokemonType.Ground };
                case PokemonType.Water:
                    return new PokemonType[] { PokemonType.Electric, PokemonType.Grass };

                default:
                    return null;
            }
        }

        public static IEnumerable<PokemonType> GetWorstTypes(PokemonType defenceType)
        {
            switch (defenceType)
            {
                case PokemonType.Bug:
                    return new PokemonType[] { PokemonType.Fighting, PokemonType.Grass, PokemonType.Ground };
                case PokemonType.Dark:
                    return new PokemonType[] { PokemonType.Dark, PokemonType.Ghost };
                case PokemonType.Dragon:
                    return new PokemonType[] { PokemonType.Electric, PokemonType.Fire, PokemonType.Grass, PokemonType.Water };
                case PokemonType.Electric:
                    return new PokemonType[] { PokemonType.Electric, PokemonType.Flying, PokemonType.Steel };
                case PokemonType.Fairy:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Dark, PokemonType.Dragon, PokemonType.Fighting };
                case PokemonType.Fighting:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Dark, PokemonType.Rock };
                case PokemonType.Fire:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Fire, PokemonType.Fairy, PokemonType.Grass, PokemonType.Ice, PokemonType.Steel };
                case PokemonType.Flying:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Fighting, PokemonType.Grass };
                case PokemonType.Ghost:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Poison };
                case PokemonType.Grass:
                    return new PokemonType[] { PokemonType.Electric, PokemonType.Grass, PokemonType.Ground, PokemonType.Water };
                case PokemonType.Ground:
                    return new PokemonType[] { PokemonType.Poison, PokemonType.Rock };
                case PokemonType.Ice:
                    return new PokemonType[] { PokemonType.Ice };
                case PokemonType.None:
                    return new PokemonType[] { };
                case PokemonType.Normal:
                    return new PokemonType[] { };
                case PokemonType.Poison:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Fairy, PokemonType.Fighting, PokemonType.Grass, PokemonType.Poison };
                case PokemonType.Psychic:
                    return new PokemonType[] { PokemonType.Fighting, PokemonType.Psychic };
                case PokemonType.Rock:
                    return new PokemonType[] { PokemonType.Fire, PokemonType.Flying, PokemonType.Normal, PokemonType.Poison };
                case PokemonType.Steel:
                    return new PokemonType[] { PokemonType.Bug, PokemonType.Dragon, PokemonType.Fairy, PokemonType.Flying, PokemonType.Grass, PokemonType.Ice, PokemonType.Normal, PokemonType.Psychic, PokemonType.Rock, PokemonType.Steel };
                case PokemonType.Water:
                    return new PokemonType[] { PokemonType.Fire, PokemonType.Ice, PokemonType.Steel, PokemonType.Water };

                default:
                    return null;
            }
        }
    }
}