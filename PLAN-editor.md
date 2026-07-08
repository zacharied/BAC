# Editor

This game mode needs an editor. See the `Mania` gamemode's editor for our template.

The editor is to be presented as a vertically scrolling timeline. The y-axis represents time like in mania, and the x-axis is a horizontal mapping of the circular space in which the game takes place. The leftmost of the timeline should be tied to the West, continuing counter-clockwise (increasing in radians) around the circle, so the rightmost column would be tied to the North. Objects can be placed in any of 360 possible angles (degrees) so the timeline x-axis can theoretically be divided into a grid of 360.

## Presentation

The grid should be presented to the user with configurable x-axis snapping settings that default to 45 degree increments. Defer to how Mania mode does it for time (vertical axis) snapping.

On both horizontal sides of the grid should be a "fake" view of the adjacent 30 degrees that would be beyond that x-coordinate. This should be slightly darkened, and should show clones of the objects in the actual vertical slice being cloned. These clones should be interactable. Please ask about this, it will be hard to implement!

## Hit objects

The editor must support the following hit objects:
* CardinalButton, CenterSlam, EdgeSlam: these are singular objects that can be placed at any angle. CardinalButton is represented by its square sprite, CenterSlam is represented by the arrow sprite rotated downward, and EdgeSlam is the arrow sprite rotated left or right (based on its RotationalDirection).
* HoldCardinalNote: identical in behavior to CardinalNote, but also has a duration. Defer to Mania implementation (HoldNote) for all interaction and behavior details.
* ShoulderButton objects, which appear on the left and right sides of the screen ingame. They are tied to the LEFT and RIGHT directions. They should be shown in their own lane between the WEST-SOUTH boundary and and EAST-NORTH boundary lanes. In the editor, they can be represented as a purple version of the CardinalButton sprite.
* Slider objects, which are a polyline made up of: (1) a SliderBody which serves as the start point and container for children, and (2) at least 1 SliderChild objects that define time and angle offsets from the path. These should be drawn as a series of lines joining each node, overlain on top of all the other objects with their angle converted to horizontal space.

## Interaction

The Toolbox component in Mania has buttons entering Select mode, as well as Taps and Holds. We need that, but our objects intead (as well as Select). 

We will have to make some decisions for how the user can interact with slider objects, since they can have an arbitrary number of children added to them.