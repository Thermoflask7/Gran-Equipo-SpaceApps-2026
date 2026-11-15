// What the 3D view (right half) of 02_Lanzamiento shows. See LaunchScreen.
//   Liftoff: close-up, the camera follows the rocket lifting off and leaving the atmosphere (start of the Departure phase).
//   Transit: overview of the Earth -> Moon -> Earth route (rest of Departure, Lunar orbit and the first part of Return).
//   Landing: close-up, the camera follows the capsule through reentry to splashdown (end of the Return phase).
public enum FlightStage
{
    Liftoff,
    Transit,
    Landing
}
