/*
This file is part of Early Bird.

Early Bird is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Early Bird is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Early Bird.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;

namespace CircBurn {

	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class CB_AppButton : MonoBehaviour
	{
		const ApplicationLauncher.AppScenes buttonScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;
		private static ApplicationLauncherButton button = null;

		public static Callback Toggle = delegate {};

		static bool buttonVisible
		{
			get {
				return true;
			}
		}

		public static void UpdateVisibility ()
		{
			if (button != null) {
				button.VisibleInScenes = buttonVisible ? buttonScenes : 0;
			}
		}

		private void onToggle ()
		{
			Toggle();
		}

		private void onHover()
		{
		}

		private void onHoverOut()
		{
		}

		private void onEnable()
		{
		}

		private void onDisable()
		{
		}

		public void Start()
		{
			GameObject.DontDestroyOnLoad(this);
			GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
			GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
		}

		void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
			GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
			button = null;
		}

		void OnGUIAppLauncherReady ()
		{
			if (ApplicationLauncher.Ready && button == null) {
				var tex = GameDatabase.Instance.GetTexture("CircBurn/Textures/CircBurn_icon", false);
				button = ApplicationLauncher.Instance.AddModApplication(onToggle, onToggle, onHover, onHoverOut, onEnable, onDisable, buttonScenes, tex);
				UpdateVisibility ();
			}
			if (button != null
				&& !ApplicationLauncher.Instance.ShouldBeVisible(button)) {
				button.gameObject.SetActive(false);
			}
		}

		void OnGUIAppLauncherDestroyed ()
		{
			if (button != null) {
				Destroy(button);
			}
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class CBToolbar_FlightWindow : MonoBehaviour
	{
		public void Awake ()
		{
			CB_AppButton.Toggle += CircBurn.ToggleGUI;
		}

		void OnDestroy()
		{
			CB_AppButton.Toggle -= CircBurn.ToggleGUI;
		}
	}
}
