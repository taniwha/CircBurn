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

		ScrollView patchesScroll = new ScrollView (420, 200);
		ScrollView plannedScroll = new ScrollView (420, 200);

		static GUILayoutOption numberWidth = GUILayout.Width (120);
		static GUILayoutOption plannedWidth = GUILayout.Width (120);

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

		Orbit highlightedPatch;
		bool highlightedPlanned;
		// KSP creates new patches every time a maneuver node updates :(
		// (is that my fault?)
		int selectedIndex;
		Orbit selectedPatch;
		bool selectedPlanned;

		void SelectPatch (Orbit patch, bool planned, int index)
		{
			bool curState = selectedPatch == patch;
			bool newState = GUILayout.Toggle (curState, "");

			if (newState != curState) {
				selectedPatch = newState ? patch : null;
				selectedPlanned = planned;
				selectedIndex = index;
			}
		}

		bool ScanPatches (List<Orbit> patches, bool highlight, bool planned)
		{
			bool foundSelected = false;

			for (int i = 0; i < patches.Count; i++) {
				var patch = patches[i];
				if (!patch.activePatch) {
					break;
				}
				GUIStyle style = CBStyles.white;
				double Vinf = double.NaN;
				double Vsoi = double.NaN;
				var body = patch.referenceBody;
				double mu = body.gravParameter;
				double Rsoi = body.sphereOfInfluence;
				double a = patch.semiMajorAxis;
				double e = patch.eccentricity;

				if (e <= 1) {
					style = CBStyles.red;
					if (a * (1 + e) >= Rsoi) {
						Vsoi = Math.Sqrt (mu * (2/Rsoi - 1/a));
					}
				} else {
					Vsoi = Vinf = Math.Sqrt (-mu / a);
					if (Rsoi < double.PositiveInfinity) {
						Vsoi = Math.Sqrt (mu * (2/Rsoi - 1/a));
					}
					if (-2 * a >= Rsoi) {
						style = CBStyles.yellow;
					}
				}
				GUILayout.BeginHorizontal();
				SelectPatch (patch, planned, i);
				GUILayout.Label (patch.referenceBody.name, style);
				GUILayout.FlexibleSpace ();
				GUILayout.Label (Vinf.ToString("F1"), style, numberWidth);
				GUILayout.Label (Vsoi.ToString("F1"), style, numberWidth);
				GUILayout.EndHorizontal ();

				Event evt = Event.current;
				if (evt.type == EventType.Repaint) {
					var rect = GUILayoutUtility.GetLastRect();
					if (highlight && rect.Contains(evt.mousePosition)) {
						highlightedPatch = patch;
						highlightedPlanned = planned;
					}
				}
				if (patch == selectedPatch) {
					foundSelected = true;
				}
			}
			return foundSelected;
		}

		void DataLine (string name, double val, GUIStyle style)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (name);
			GUILayout.FlexibleSpace ();
			GUILayout.Label (val.ToString ("F1"), style, numberWidth);
			GUILayout.EndHorizontal ();
		}

		void DataBlock (string type, double pe, double vpe, double vcirc,
						GUIStyle style)
		{
			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label (type);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			DataLine ("Pe:", pe, style);
			DataLine ("VPe:", vpe, style);
			DataLine ("VCirc:", vcirc, style);
			DataLine ("dV:", vpe - vcirc, style);

			GUILayout.EndVertical ();
		}

		void DisplayPatch (Orbit patch, bool planned)
		{
			if (patch == null) {
				double nan = double.NaN;
				GUILayout.BeginHorizontal ();
				GUILayout.Label (" ", plannedWidth);
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				DataBlock ("Actual", nan, nan, nan, CBStyles.white);
				DataBlock ("Optimal", nan, nan, nan, CBStyles.white);
				GUILayout.EndHorizontal ();
				return;
			}

			var body = patch.referenceBody;
			double R = body.Radius;
			double Rsoi = body.sphereOfInfluence;
			double mu = body.gravParameter;
			double a = patch.semiMajorAxis;
			double e = patch.eccentricity;

			double pe = a * (1 - e);
			double vpe = Math.Sqrt (mu * (2/pe - 1/a));
			double vcirc = Math.Sqrt (mu / pe);

			double ope = -2 * a;
			double ovpe = Math.Sqrt (mu * (2/ope - 1/a));
			double ovcirc = Math.Sqrt (mu / ope);

			GUIStyle style = CBStyles.white;
			if (e <= 1) {
				style = CBStyles.red;
				ope = ovpe = ovcirc = double.NaN;
			} else {
				if (ope >= Rsoi) {
					style = CBStyles.yellow;
				}
			}

			GUILayout.BeginHorizontal ();
			GUILayout.Label (planned ? "Planned" : "Current", plannedWidth);
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			DataBlock ("Actual", pe - R, vpe, vcirc, CBStyles.white);
			DataBlock ("Optimal", ope - R, ovpe, ovcirc, style);
			GUILayout.EndHorizontal ();
		}

		void PatchLists (List<Orbit> patches, List<Orbit> flightPlan)
		{
			if (selectedPatch != null && selectedPlanned
				&& selectedIndex < flightPlan.Count) {
				selectedPatch = flightPlan[selectedIndex];
			}
			highlightedPatch = null;

			GUILayout.BeginHorizontal ();

			bool selOK = false;
			GUILayout.BeginVertical ();
			GUILayout.Label ("Current Trajectory");
			patchesScroll.Begin ();
			selOK |= ScanPatches (patches, patchesScroll.mouseOver, false);
			patchesScroll.End ();
			GUILayout.EndVertical ();

			GUILayout.BeginVertical ();
			GUILayout.Label ("Planned Trajectory");
			plannedScroll.Begin ();
			selOK |= ScanPatches (flightPlan, plannedScroll.mouseOver, true);
			plannedScroll.End ();
			GUILayout.EndVertical ();

			if (!selOK) {
				selectedPatch = null;
			}

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

			// patches is the list of trajectories the vessel will follow if
			// no maneuvers are performed.
			// flightPlan includes the trajectories of patches up to the time
			// of the first maneuver node, and then is the trajectories the
			// vessel will follow if that maneuver is performed (and any
			// subsequent maneuvers as well)
			var patches = vessel.patchedConicSolver.patches;
			var flightPlan = vessel.patchedConicSolver.flightPlan;

			GUILayout.BeginVertical ();

			PatchLists (patches, flightPlan);

			if (selectedPatch != null) {
				DisplayPatch (selectedPatch, selectedPlanned);
			} else {
				DisplayPatch (highlightedPatch, highlightedPlanned);
			}

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
			}
		}
	}
}
