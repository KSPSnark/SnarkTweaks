using System;

namespace OrbitalSurveyScanning
{
    /// <summary>
    /// The stock ModuleOrbitalSurveyor has a problem.  It requires your orbit
    /// to be between some minimum and some maximum altitude.  The minimum
    /// altitude is either 1/10 of the body's radius, or 25 km, whichever is
    /// bigger.  That's usually OK... unless you have a body whose SoI is
    /// actually *smaller* than 25 km, in which case it becomes impossible
    /// to scan.  This module is a drop-in replacement.
    /// </summary>
    public class ModuleOrbitalSurveyorBugfix : ModuleOrbitalSurveyor
    {
        public override void PerformSurvey()
        {
            double minPe = PreferredMinimumAltitude;
            double maxAp = PreferredMaximumAltitude;
            double soi = vessel.mainBody.sphereOfInfluence;
            double planetRadius = vessel.mainBody.Radius;

            // How much "head room" do we have in the body's SoI?
            double soiAltitude = soi - planetRadius;
            if (soiAltitude <= 0)
            {
                // The SoI is actually smaller than the planet!
                PostScreenMessage(string.Format("{0} has no SoI! It's impossible to orbit, so you can't take a scan.", vessel.mainBody.name));
                return;
            }

            // Make sure the max Ap isn't outside the SoI.
            double soiScale = 1.0;
            if (maxAp > soiAltitude)
            {
                soiScale = Math.Sqrt(soiAltitude / maxAp); // will be between 0 and 1
                maxAp = soiAltitude;
            }

            // Pe can't be above Ap
            if (minPe > maxAp) minPe = maxAp;

            // Scale the min Pe down, if needed, but never to less than 3 km
            minPe = Math.Max(3000f, minPe * soiScale);

            // The minimum Pe can never be above 95% of the maximum Ap
            double maxAllowedPe = 0.95 * maxAp;
            if (minPe > maxAllowedPe) minPe = maxAllowedPe;

            bool isInAltitudeBounds = (vessel.orbit.PeA > minPe) && (vessel.orbit.ApA < maxAp);

            if (!isInAltitudeBounds || (vessel.situation != Vessel.Situations.ORBITING) || (vessel.orbit.inclination < 80))
            {
                PostScreenMessage(string.Format(
                    "You must be in a stable polar orbit between {0} and {1} to perform an orbital survey.",
                    FormatMetersToKilometers(minPe),
                    FormatMetersToKilometers(maxAp)));

            }
            else
            {
                sendDataToComms();
            }
        }

        /// <summary>
        /// Gets the minimum allowable altitude to do a scan, if SoI size isn't a factor.
        /// </summary>
        protected virtual double PreferredMinimumAltitude
        {
            get
            {
                return Math.Max(vessel.mainBody.Radius / 10.0, (double)minThreshold);
            }
        }

        /// <summary>
        /// Gets the maximum allowable altitude to do a scan, if SoI size isn't a factor.
        /// </summary>
        protected virtual double PreferredMaximumAltitude
        {
            get
            {
                return Math.Min(vessel.mainBody.Radius * 5.0, (double)maxThreshold);
            }
        }

        protected virtual void PostScreenMessage(string message)
        {
            ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
        }

        private static string FormatMetersToKilometers(double meters)
        {
            double km = meters / 1000.0;
            if (km < 11.0)
            {
                return string.Format("{0:0.0}km", km);
            }
            else
            {
                return string.Format("{0:0}km", km);
            }
        }
    }
}
