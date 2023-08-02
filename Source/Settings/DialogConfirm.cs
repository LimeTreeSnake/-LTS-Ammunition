using System;
using UnityEngine;
using Verse;

namespace Ammunition.Settings
{
	public class DialogConfirm : Window
	{
		public override Vector2 InitialSize => new Vector2(400f, 140f);
		private Action _act;

		public DialogConfirm(Action action)
		{
			this.doCloseX = true;
			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = true;
			_act = action;
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);
			
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Rect headerRect = list.GetRect(40f);
			Widgets.Label(headerRect, Language.Translate.SettingsResetAll);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			list.Gap(20);
			
			Rect optionsRect = list.GetRect(40f);		
			optionsRect.SplitVertically(inRect.width / 2f,
				out Rect optionsRectLeft,
				out Rect optionsRectRight);
			if (Widgets.ButtonText(optionsRectLeft.ContractedBy(1,0), Language.Translate.Ok))
			{
				_act.Invoke();
				list.End();
				this.Close();
				return;
			}
			if (Widgets.ButtonText(optionsRectRight.ContractedBy(1,0), Language.Translate.Cancel))
			{
				list.End();
				this.Close();
				return;
			}
			list.End();
		}
	}
}