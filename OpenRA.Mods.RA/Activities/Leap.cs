#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class Leap : Activity
	{
		Mobile mobile;
		WeaponInfo weapon;

		WPos from;
		WPos to;
		int ticks;
		int length;
		WAngle angle;

		public Leap(Actor self, Actor target, WeaponInfo weapon, WRange speed, WAngle angle)
		{
			var targetMobile = target.TraitOrDefault<Mobile>();
			if (targetMobile == null)
				throw new InvalidOperationException("Leap requires a target actor with the Mobile trait");

			this.weapon = weapon;
			this.angle = angle;
			mobile = self.Trait<Mobile>();
			mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, targetMobile.FromCell, targetMobile.FromSubCell);
			mobile.IsMoving = true;

			from = self.CenterPosition;
			to = self.World.Map.CenterOfSubCell(targetMobile.FromCell, targetMobile.FromSubCell);
			length = Math.Max((to - from).Length / speed.Range, 1);

			// HACK: why isn't this using the interface?
			self.Trait<WithInfantryBody>().Attacking(self, Target.FromActor(target));

			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
		}

		public override Activity Tick(Actor self)
		{
			if (ticks == 0 && IsCanceled)
				return NextActivity;

			mobile.SetVisualPosition(self, WPos.LerpQuadratic(from, to, angle, ++ticks, length));
			if (ticks >= length)
			{
				mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, mobile.ToCell, mobile.ToSubCell);
				mobile.FinishedMoving(self);
				mobile.IsMoving = false;

				self.World.ActorMap.GetUnitsAt(mobile.ToCell, mobile.ToSubCell)
					.Except(new[] { self }).Where(t => weapon.IsValidAgainst(t, self))
					.Do(t => t.Kill(self));

				return NextActivity;
			}

			return this;
		}
	}
}
