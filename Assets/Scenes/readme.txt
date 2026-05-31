Lesson1-Just an basic scene to move around
Lesson2-Making use of two types of teleportation systems. Teleportation Area & Teleportation Anchor

--------------
Lesson3

We've got two red cubes. Grab Interactable 1 and Grab Interactable 2.

Grab Interactable 1 has an Interactable Event to change colours.
Grab Interactable 2 is there for the 'Direct Interactor' we have under the 'Right Controller'. We have an Interection Layer Mask called 'CloseInteractable' which is assigned to the 'Direct Interactor' & 'Grab Interactor 2' 

--------------


--------------
Lesson4

We've got a table with multiple objects on it. 3 Books, 1 Red Cup and 1 Green Cup. We also have multiple Sockets on the Bookshelf. The book sockets are making use of 'Interaction Layer Mask' but the 'GeneralSockTagFilter' is making use
of the tag 'Cup' which the tag is also assigned on the red cup. Basically this will allow only cups with tag 'Cup' to be placed in the socket with that tag.

We are also making use of a custom script 'XR Socket Audio Feedback', which is making use of events to play sounds on interection of elements. We could have made use of 'Interactor Events' directly from the GUI but decided to create a dedicated script.

--------------

--------------

Lesson 5

Mela here we have TV_Interactable, Chair_Grab_Interactable, Bottle_ToggleVisibility, MilkCartonMoveOnActivate


I suggest you start with the TV_Interactable - This has a button (green box) that changes colour on hovering and rotates the TV when turned.

Then you go do the MilkCartonMoveOnActivate  - This explains the Activate and Deactivated. You have to press the trigger and grip button at the same time.

Finally the Chair_Grab_Interactable - this is the most complicated of them all. Restriction in the Z axis, check the settings in the gameobject scripts. And also the Bottle shows when the chair moves closely to the table (you can restrict by tag). 

--------------

Lesson 6

The TV has a button that when pressed shows a bottle on the Table.

There is also the ready made UI prefab by Unity.

--------------

Lesson 7

Joystick that moves a car. in the XR Origin there is the 'Direct Interector' under 'Right Controller' and the layer CloseInterecable is appliedl. Also on the joystick under GrabHandle there is the XR Grab Interactable (not visible because mesh is disabled).

--------------


Lesson 8

In the simple hands folder there are two prefabs WhiteHand and BlackHand. in our case we are making use of the WhiteHand and we have same script (just different controller (left vs right)) under XR Origin (XR RIG). Press 3 for the joystick click in the emulator

----------------

Lesson 9

In this lesson we have a Whack a Bottle game. You have to grab the hammer and hit the bottles.

It is important that first we develop for the PC, infact we have a TestCamera and in Grab_SledgeHammer we have LineRenderer and script PC_Hammer_Controller (use wasd qe and shift to smack the bottles). The rigidbody is set to kinematic and disabled gravity (like when we grab with device controller)
So to test for XR make sure that you disable PC Hammer Controller and enable XR Grab Interactable on the Hammer.



----------------