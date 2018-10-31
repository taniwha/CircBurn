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

		ScrollView patchscroll = new ScrollView (680, 300);

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

		struct PatchLine {
			public string cur1;
			public string cur2;
			public string cur3;
			public string cur4;
			public bool curFlag;	// optimal pe is outside SoI
			public string plan1;
			public string plan2;
			public string plan3;
			public string plan4;
			public bool planFlag;	// optimal pe is outside SoI
		}
		PatchLine []patchLines;

		void CalcVelocities (double mu, double r, double a, out double Vpe, out double Vcirc)
		{
			Vpe = Math.Sqrt (mu * (2/r - 1/a));
			Vcirc = Math.Sqrt (mu / r);
		}

		int WritePatchLines (int line, Orbit patch)
		{
			var body = patch.referenceBody;
			patchLines[line].cur1 = body.name;
			patchLines[line].cur3 = "";
			patchLines[line].cur4 = "";
			patchLines[line].curFlag = false;
			if (patch.eccentricity <= 1) {
				// when the trajectory is closed, the optimal
				// circularization altitude is not valid and may be at
				// either the apopapsis or the periapsis, depending on
				// the orbit.
				patchLines[line].cur2 = "N/A";
				// ensure the other lines are blank in case the flight plane
				// is hyperbolic
				patchLines[line+1].cur1 = "";
				patchLines[line+1].cur2 = "";
				patchLines[line+1].cur3 = "";
				patchLines[line+1].cur4 = "";
				patchLines[line+1].curFlag = false;
				patchLines[line+2].cur1 = "";
				patchLines[line+2].cur2 = "";
				patchLines[line+2].cur3 = "";
				patchLines[line+2].cur4 = "";
				patchLines[line+2].curFlag = false;
				return 0;
			}
			double mu = body.gravParameter;
			double a = patch.semiMajorAxis;
			double e = patch.eccentricity;
			double Vpe, Vcirc;
			double Vinf = Math.Sqrt(-mu / a);
			double pe = a * (1 - e);
			CalcVelocities (mu, pe, a, out Vpe, out Vcirc);
			double Ope = -2 * a;
			double oVpe, oVcirc;
			CalcVelocities (mu, Ope, a, out oVpe, out oVcirc);
			patchLines[line].cur2 = Vinf.ToString("F1");
			patchLines[line+1].cur1 = "";
			patchLines[line+1].cur2 = pe.ToString("F1");
			patchLines[line+1].cur3 = Vpe.ToString("F1");
			patchLines[line+1].cur4 = (Vpe - Vcirc).ToString("F1");
			patchLines[line+1].curFlag = false;
			patchLines[line+2].cur1 = "";
			patchLines[line+2].cur2 = Ope.ToString("F1");
			patchLines[line+2].cur3 = oVpe.ToString("F1");
			patchLines[line+2].cur4 = (oVpe - oVcirc).ToString("F1");
			patchLines[line+2].curFlag = Ope >= body.sphereOfInfluence;
			return 1;
		}

		int WritePlannedLines (int line, Orbit patch)
		{
			var body = patch.referenceBody;
			patchLines[line].plan1 = body.name;
			patchLines[line].plan3 = "";
			patchLines[line].plan4 = "";
			patchLines[line].planFlag = false;
			if (patch.eccentricity <= 1) {
				// when the trajectory is closed, the optimal
				// circularization altitude is not valid and may be at
				// either the apopapsis or the periapsis, depending on
				// the orbit.
				patchLines[line].plan2 = "N/A";
				// ensure the other lines are blank in case the flight plane
				// is hyperbolic
				patchLines[line+1].plan1 = "";
				patchLines[line+1].plan2 = "";
				patchLines[line+1].plan3 = "";
				patchLines[line+1].plan4 = "";
				patchLines[line+1].planFlag = false;
				patchLines[line+2].plan1 = "";
				patchLines[line+2].plan2 = "";
				patchLines[line+2].plan3 = "";
				patchLines[line+2].plan4 = "";
				patchLines[line+2].planFlag = false;
				return 0;
			}
			double mu = body.gravParameter;
			double a = patch.semiMajorAxis;
			double e = patch.eccentricity;
			double Vpe, Vcirc;
			double Vinf = Math.Sqrt(-mu / a);
			double pe = a * (1 - e);
			CalcVelocities (mu, pe, a, out Vpe, out Vcirc);
			double Ope = -2 * a;
			double oVpe, oVcirc;
			CalcVelocities (mu, Ope, a, out oVpe, out oVcirc);
			patchLines[line].plan2 = Vinf.ToString("F1");
			patchLines[line+1].plan1 = "";
			patchLines[line+1].plan2 = pe.ToString("F1");
			patchLines[line+1].plan3 = Vpe.ToString("F1");
			patchLines[line+1].plan4 = (Vpe - Vcirc).ToString("F1");
			patchLines[line+1].planFlag = false;
			patchLines[line+2].plan1 = "";
			patchLines[line+2].plan2 = Ope.ToString("F1");
			patchLines[line+2].plan3 = oVpe.ToString("F1");
			patchLines[line+2].plan4 = (oVpe - oVcirc).ToString("F1");
			patchLines[line+2].planFlag = Ope >= body.sphereOfInfluence;
			return 1;
		}

		void ScanPatches (Vessel vessel, bool highlight)
		{
			// patches is the list of trajectories the vessel will follow if
			// no maneuvers are performed.
			// flightPlan includes the trajectories of patches up to the time
			// of the first maneuver node, and then is the trajectories the
			// vessel will follow if that maneuver is performed (and any
			// subsequent maneuvers as well)
			var patches = vessel.patchedConicSolver.patches;
			var flightPlan = vessel.patchedConicSolver.flightPlan;
			int numPatches = 0;
			int numPlanned = flightPlan.Count;

			for (int i = 0; i < patches.Count; i++) {
				if (!patches[i].activePatch) {
					numPatches = i;
					break;
				}
			}

			int count = Math.Max(numPlanned, numPatches);
			if (patchLines == null || patchLines.Length < 3 * count) {
				// each patch needs up to 3 lines:
				// name Vinf ____ ___
				// ____ pe   vpe  dV	current info
				// ____ Ope  Ovpe OdV   optimal info
				// with the set of columns repeated for the flight plan
				patchLines = new PatchLine[count * 3];
			}

			int line = 0;
			for (int i = 0; i < count; i++) {
				int full = 0;
				if (i < numPatches) {
					full |= WritePatchLines(line, patches[i]);
				}
				if (i < numPlanned) {
					full |= WritePlannedLines(line, flightPlan[i]);
				}
				line += full == 1 ? 3 : 1;
			}

			GUILayout.BeginHorizontal ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].cur2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].curFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].cur1, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].cur2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].curFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].cur2, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].cur2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].curFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].cur3, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].cur2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].curFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].cur4, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].plan2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].planFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].plan1, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].plan2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].planFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].plan2, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].plan2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].planFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].plan3, style);
			}
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			for (int i = 0; i < line; i++) {
				GUIStyle style = CBStyles.white;
				if (patchLines[i].plan2 == "N/A") {
					style = CBStyles.red;
				}
				if (patchLines[i].planFlag) {
					style = CBStyles.yellow;
				}
				GUILayout.Label (patchLines[i].plan4, style);
			}
			GUILayout.EndVertical ();

			GUILayout.EndHorizontal ();
		}

		void WindowGUI (int windowID)
		{
			CBStyles.Init();

			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel.patchedConicSolver == null) {
				GUILayout.BeginVertical ();
				GUILayout.Label ("Patched conics not available.");
				GUILayout.Label ("Encounter information not available.");
				GUILayout.EndVertical ();
				return;
			}

			GUILayout.BeginVertical ();

			patchscroll.Begin ();
			ScanPatches (vessel, patchscroll.mouseOver);
			patchscroll.End ();

			GUILayout.EndVertical ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));
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
