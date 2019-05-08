﻿using NetMud.Communication.Messaging;
using NetMud.DataAccess.Cache;
using NetMud.DataStructure.Combat;
using NetMud.DataStructure.Player;
using System;
using System.Linq;

namespace NetMud.Combat
{
    public static class Round
    {
        /// <summary>
        /// Execute an attack round
        /// </summary>
        /// <param name="actor">the person attacking</param>
        /// <param name="target">the person defending</param>
        public static bool ExecuteRound(IPlayer actor, IPlayer target)
        {
            //Stagger clearance comes before ending a fight
            if (actor.Stagger > 0)
            {
                actor.Stagger -= 1;
                //Send a ui update
                actor.Descriptor.SendWrapper();
                return true;
            }

            if (!actor.IsFighting())
            {
                return false;
            }

            if (actor == target)
            {
                target = null;
            }

            IFightingArt attack = actor.LastAttack;

            //If we lack an attack or we're on the tail end of the attack just find a new one and start it
            if (attack == null || !actor.Executing)
            {
                IFightingArtCombination myCombo = actor.LastCombo;
                if (myCombo == null)
                {
                    ulong distance = 0;
                    if (target != null)
                    {
                        distance = (ulong)Math.Abs((double)(actor.CurrentLocation.CurrentSection - target.CurrentLocation.CurrentSection));
                    }

                    var validCombos = actor.Combos.Where(combo => combo.IsValid(actor, target, distance));

                    if (validCombos.Count() == 0)
                    {
                        myCombo = actor.Combos.FirstOrDefault();
                    }
                    else
                    {
                        myCombo = validCombos.FirstOrDefault();
                    }
                }

                //uhh k we need to use a fake combo logic to get a random attack
                if (myCombo == null)
                {
                    var attacks = TemplateCache.GetAll<IFightingArt>(true);

                    attack = attacks.FirstOrDefault(atk => atk.IsValid(actor, target, (ulong)Math.Abs(actor.Balance)));
                }
                else
                {
                    attack = myCombo.GetNext(actor.LastAttack);
                }

                actor.Stagger = attack.Setup;
                actor.Sturdy = attack.Armor;
                actor.LastCombo = myCombo;
                actor.LastAttack = attack;
                actor.Executing = true;

                if (actor.Stagger > 0)
                {
                    actor.Stagger -= 1;

                    //Send a ui update
                    actor.Descriptor.SendWrapper();
                    return true;
                }
                //else we just run right into the combo if there's no setup
            }

            //execute the attack
            actor.Executing = false;

            //basics
            actor.Stagger = attack.Recovery;
            actor.LastAttack = attack;
            actor.LastCombo = null;
            actor.Balance = attack.DistanceChange;

            //numbers damage
            if (attack.Health.Actor > 0)
            {
                actor.Harm((ulong)attack.Health.Actor);
            }

            if (attack.Stamina.Actor > 0)
            {
                actor.Exhaust(attack.Stamina.Actor);
            }

            Tuple<string, string, string> messaging = null;

            if (target != null)
            {
                var rand = new Random();
                target.Balance = -1 * attack.DistanceChange;

                var targetReadiness = ReadinessState.Offensive;
                if (target.LastAttack != null)
                {
                    targetReadiness = target.LastAttack.Readiness;
                }

                //TODO: for now we're just doing flat chances for avoidance states since we have no stats to base anything on
                var avoided = false;
                var blocked = false;
                double impact = attack.Impact;
                double damage = attack.Health.Victim;
                double staminaDrain = attack.Stamina.Victim;

                switch (targetReadiness)
                {
                    case ReadinessState.Circle: //circle is dodge essentially
                        avoided = rand.Next(0, 100) >= Math.Abs(target.Balance) / 4 * 100;
                        break;
                    case ReadinessState.Block:
                        blocked = rand.Next(0, 100) >= Math.Abs(target.Balance) / 2 * 100;

                        if(blocked)
                        {
                            impact *= .25;
                            damage *= .50;
                            staminaDrain *= .50;
                        };
                        break;
                    case ReadinessState.Deflect:
                        blocked = rand.Next(0, 100) >= Math.Abs(target.Balance) / 2 * 100;

                        if(blocked)
                        {
                            impact *= .50;
                            damage *= .25;
                            staminaDrain *= .25;
                        }
                        break;
                    case ReadinessState.Redirect:
                        avoided = rand.Next(0, 100) >= Math.Abs(target.Balance) / 20 * 100;
                        break;
                }

                messaging = GetOutputStrings(attack, target, avoided || blocked, targetReadiness);

                if (!avoided)
                {
                    var victStagger = target.Sturdy - (int)Math.Truncate(impact);

                    //Affect the victim, sturdy/armor absorbs stagger impact and the remainder gets added to victim stagger
                    target.Sturdy = Math.Max(0, victStagger);
                    target.Stagger += Math.Abs(Math.Min(0, victStagger));

                    if (damage > 0)
                    {
                        target.Harm((ulong)Math.Truncate(damage));
                    }

                    if (staminaDrain > 0)
                    {
                        target.Exhaust((int)Math.Truncate(staminaDrain));
                    }

                    //affect qualities
                    if (attack.QualityValue != 0 && !string.IsNullOrWhiteSpace(attack.ResultQuality))
                    {
                        target.SetQuality(attack.QualityValue, attack.ResultQuality, attack.AdditiveQuality);
                    }
                }
            }
            else
            {
                messaging = GetOutputStrings(attack, target, false, ReadinessState.Offensive);
            }

            var msg = new Message(messaging.Item1)
            {
                ToTarget = new string[] { messaging.Item2 },
                ToOrigin = new string[] { messaging.Item3 }
            };


            msg.ExecuteMessaging(actor, null, target, actor.CurrentLocation, null, 1000, true);

            return true;
        }

        /// <summary>
        /// Make the output messaging
        /// </summary>
        /// <returns>actor, target, room</returns>
        private static Tuple<string, string, string> GetOutputStrings(IFightingArt attack, IPlayer target, bool blocked, ReadinessState avoidanceType)
        {
            var targetGlyph = target == null ? "a shadow" : "$T$";
            var actorTargetGlyph = target == null ? "your shadow" : "$T$";

            var verb = !string.IsNullOrWhiteSpace(attack.ActionVerb) ? attack.ActionVerb : attack.Name;
            var obj = !string.IsNullOrWhiteSpace(attack.ActionObject) ? "and " + attack.ActionObject + " " : string.Empty;

            string toOrigin = string.Format("$A$ {0}s {2}{1}.", verb, targetGlyph, obj);
            string toActor = string.Format("You {0} {2}{1}.", verb, actorTargetGlyph, obj);
            string toTarget = "";

            if (target != null)
            {
                toTarget = string.Format("$A$ {0}s {1}you.", verb, obj);

                if (blocked)
                {
                    switch (avoidanceType)
                    {
                        case ReadinessState.Circle: //circle is dodge essentially
                            toActor = string.Format("{0} dodges your attack!", actorTargetGlyph);
                            toTarget = string.Format("You dodge $A$s {0}.", verb);
                            toOrigin = string.Format("$A$ {0}s {1} but they dodge!.", verb, targetGlyph);
                            break;
                        case ReadinessState.Block:
                            toActor = string.Format("You {0} {1} but they block!", verb, actorTargetGlyph);
                            toTarget = string.Format("$A$ {0}s you but you block it.", verb);
                            toOrigin = string.Format("$A$ {0}s {1} but they block.", verb, targetGlyph);
                            break;
                        case ReadinessState.Deflect:
                            toActor = string.Format("You {0} {1} but they deflect it!", verb, actorTargetGlyph);
                            toTarget = string.Format("$A$ {0}s you but you deflect it.", verb);
                            toOrigin = string.Format("$A$ {0}s {1} but they deflect the blow.", verb, targetGlyph);
                            break;
                        case ReadinessState.Redirect:
                            toActor = string.Format("{0} redirects your attack back to you!", actorTargetGlyph);
                            toTarget = string.Format("You redirect $A$s {0} back at them.", verb);
                            toOrigin = string.Format("$A$ {0}s {1} but they redirect the attack back at them!.", verb, targetGlyph);
                            break;
                    }
                }
            }

            return new Tuple<string, string, string>(toActor, toTarget, toOrigin);
        }
    }
}