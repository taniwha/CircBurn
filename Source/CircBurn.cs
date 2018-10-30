/*
	CircBurn.cs

	Optimal circularization burn periapsis from hyperbolic trajectory.

	Copyright (C) 2018 Bill Currie <bill@taniwha.org>

	Author: Bill Currie <bill@taniwha.org>
	Date: 2018/10/30

	This program is free software; you can redistribute it and/or
	modify it under the terms of the GNU General Public License
	as published by the Free Software Foundation; either version 2
	of the License, or (at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	See the GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to:

		Free Software Foundation, Inc.
		59 Temple Place - Suite 330
		Boston, MA  02111-1307, USA

*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KSP.UI;

namespace CircBurn {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class CircBurn : MonoBehaviour
	{
		static Rect windowpos;
		private static bool gui_enabled;
		private static bool hide_ui;

		internal static CircBurn instance;

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void ShowGUI ()
		{
			gui_enabled = true;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}
		void UpdateGUIState ()
		{
			enabled = !hide_ui && gui_enabled;
		}

		void onHideUI ()
		{
			hide_ui = true;
			UpdateGUIState ();
		}

		void onShowUI ()
		{
			hide_ui = false;
			UpdateGUIState ();
		}

		void Awake ()
		{
			instance = this;
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
		}

		void Start ()
		{
		}

		void WindowGUI (int windowID)
		{
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel.patchedConicSolver == null) {
				GUILayout.BeginVertical ();
				GUILayout.Label ("Patched conics not available.");
				GUILayout.Label ("Encounter information not available.");
				GUILayout.EndVertical ();
				return;
			}
			GUILayout.BeginVertical ();
			var patches = vessel.patchedConicSolver.patches;
			for (int i = 0; i < patches.Count; i++) {
				Orbit patch = patches[i];
				if (!patch.activePatch) {
					break;
				}
				if (patch.eccentricity <= 1) {
					// when the trajectory is closed, the optimal
					// circularization altitude is at the apoapsis, but that's
					// probably not what the user is interested in, so just
					// skip the patch
					continue;
				}
				var body = patch.referenceBody;
				var mu = body.gravParameter;
				var e = patch.eccentricity;
				var a = patch.semiMajorAxis;
				string name = body.name;
				double curpe = a * (1 - e);
				double vinf = Math.Sqrt(-mu / a);
				double optpe = -2 * a;
				double vpe = Math.Sqrt(mu*(2/curpe - 1/a));
				double vopt = Math.Sqrt(mu*(2/optpe - 1/a));
				double vp = Math.Sqrt(mu/curpe);
				double vo = Math.Sqrt(mu/optpe);
				curpe -= body.Radius;
				optpe -= body.Radius;

				GUILayout.Label (name + " " + vinf + " " + curpe + " " + vpe + " " + (vpe - vp));
				GUILayout.Label ("    " + optpe + " " + vopt + " " + (vopt - vo));
			}
			GUILayout.EndVertical ();
		}

		void OnGUI ()
		{
			if (gui_enabled) { // don't create windows unless we're going to show them
				GUI.skin = HighLogic.Skin;
				if (windowpos.x == 0) {
					windowpos = new Rect (Screen.width / 2 - 250,
						Screen.height / 2 - 30, 0, 0);
				}
				windowpos = GUILayout.Window (GetInstanceID (),
					windowpos, WindowGUI,
					CircBurnVersionReport.GetVersion (),
					GUILayout.Width (500));
				if (windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
					InputLockManager.SetControlLock ("CB_Flight_window_lock");
				} else {
					InputLockManager.RemoveControlLock ("CB_Flight_window_lock");
				}
			} else {
				InputLockManager.RemoveControlLock ("CB_Flight_window_lock");
			}
		}
	}
}
