# Jonas Wombacher - Research Project Telecooperation
This repository contains the Unity project I built for a university course called "Research Project Telecooperation". The resulting app was developed for the Meta Quest Pro. If you want to know more about this project, you can check out the [documentation on my homepage](https://www.jonaswombacher.de/research-project-telecooperation).

https://github.com/jjoonnaasss/research-project-telecooperation/assets/33218099/ba4c2c3d-43e0-42de-84f0-2b81c5ec8b8a

## Resources used in the project
- Fire particles: [Unity Particle Pack](https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325)
- Arcade model: [AurynSky Arcade Machines Pack 02](https://assetstore.unity.com/packages/3d/props/arcade-machines-pack-02-lowpoly-pack-79442)
- Models and animations: [mixamo.com](https://www.mixamo.com/)
- Occlusion shader: https://github.com/quill18/UnityMiniGolf/tree/master/Assets/FakeHoleProject

## App controls
### Controls for setting up the environment
Holding down either of the two grip/grab buttons on the controllers brings up the setup UI, attached to the left controller. It is used for setting up the environment, characters, and goals and it won't be visible in the simulation mode.

![Setup UI window](https://github.com/jjoonnaasss/research-project-telecooperation/assets/33218099/44a25baf-3140-4d7b-b098-1543178317ca)

At the top of this window, you can switch between creating/editing obstacles and characters.
Below that, you can toggle the creation mode. In the creation mode, you can create and edit obstacles or characters. Outside the creation mode, you can place goals by pointing at the floor with the right controller and pressing the A button.
Below the creation mode checkbox, you have access to all the visualization settings, including the visualization modes of the characters themselves (humanoid or sphere) and of their traces (trail, ghost humanoid or ghost sphere).
The buttons at the bottom of this window allow you to quit the app and to open the dialog for saving or loading environments.

The simulation mode can be activated by pressing and releasing both joysticks while the creation mode is disabled. Deactivating it requires the user to press and release both joysticks again. This input combination is rather hidden on purpose to prevent participants from leaving the simulation mode by accident.
### Controls in the simulation mode
Once the simulation mode is active, all functionality is mapped directly to the controllers without the need to interact with any UI elements like buttons.
Arcades and fire sources can be placed by pointing at the floor with the right controller and pressing the left or right controller's trigger button, respectively.
After this, the simulation can be started by pressing the A button and reset by pressing the B button.
After all characters have reached their goal, you can select individual characters for rewinding by pointing at their rewind trail with the right controller and pressing the grip/grab button. With the left joystick, you can then rewind the selected character forward or backward in time. Pressing the A button lets him start to walk again from his current position.
