# CircBurn
CircBurn is a KSP mod to help plan celestial encounters.

While the Oberth effect is very real, it turns out that having your
periapsis as close to your target body is not always the most efficient in
terms of delta-V cost: for a given hyperbolic excess velocity (Vinf), there
is an optimal distance from the body where the cost to circularize your
orbit is minimized. Note that this is the optimal distance of the periapsis
of the hyperbola (ie, it is always most efficient to circularize at either
your periapsis or apoapsis (hyperbolic trajectories do not have an
apoapsis, though)).

The problems come with knowing what your Vinf is, and the fact that
altering your encounter periapsis is more than likely to alter your Vinf.
Also, for low Vinf, the optimal distance might well be outside the target's
sphere of influence. For these reasons, CircBurn is and advisory mod and
only displays the information: it does not alter or create any maneuver
nodes or your current trajectory.
