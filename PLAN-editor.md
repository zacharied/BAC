# Editor

This game mode needs an editor. See the `Mania` gamemode's editor for the basics.

The editor is to be presented as a vertically scrolling timeline. The y-axis represents time like in mania, and the x-axis is a horizontal mapping of the circular space in which the game takes place. The leftmost column should be tied to the West, continuing counter-clockwise (increasing in radians) around the circle, so the rightmost column would be tied to the North.

## Hit objects

The editor must support the following hit objects:
* CardinalButton objects, which in-game flow towards cardinal directions, and are shown as the four main vertical lanes of the editor.
* ShoulderButton objects, which appear on the left and right sides of the screen ingame. They are tied to the LEFT and RIGHT directions. They should be shown in their own lane between the WEST-SOUTH boundary and and EAST-NORTH boundary lanes. They should be oriented such that they are wider than they are tall in the editor.
* CenterSlam objects, which appear as arrows headed out to an arbitrary angle of the circle. These have an integer angle between 0 and 360. In the editor, they are positioned along the horizontal axis like the buttons, but can be positioned on any integer degree offset. They should be angled such that they are always pointing downward in the editor.
* EdgeSlam objects, which behave identically to CenterSlam objects but face left or right based on their RotationalDirection.
* Slider objects, which are a polyline made up of: (1) a SliderBody which serves as the start point and container for children, and (2) at least 1 SliderChild objects that define time and angle offsets from the path. These should be drawn as a series of lines joining each node, overlain on top of all the other objects with their angle converted to horizontal space.

## Interaction

The Toolbox component in Mania has buttons entering Select mode, as well as Taps and Holds. We need that, but our objects intead (as well as Select).

