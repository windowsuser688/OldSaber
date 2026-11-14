# OldSaber

OldSaber is a debut of the scrapped mod "LegacySaber" by user [Kaitlyn.](https://github.com/meowybnuuy)

This mod (almost) accurately recreates old Beat Saber. (Specifically 0.10.1).

Features that we need tweaking:
1. Old Bombs
2. idk lmao
# Installation
1. Beat Saber 1.40.8
2. BSIPA
3. BSML
4. SiraUtil

# Preview
To see OldSaber in action, see the [preview.](https://www.youtube.com/watch?v=SP-lQd-KsDY)

# Features
1. Old Notes
2. Old Environment
3. Old Obstacles
4. Old HUD
5. Old Sabers + Trails

# Installing the mod
1. Download the latest release
2. Extract it
3. Open the Plugins Folder
4. Drag the Plugins folder's content into your Beat Saber's Plugin folder
5. Enjoy!
# Building the mod
To build the mod, you will need:
1. Python 3.15
2. UnityPy (At least version 1.25.4)
3. TypeTreeGeneratorAPI (At least version 0.0.10)
4. dnfile (At least version 0.18.0)

Steps to building (Skip to step 3 if you have everything installed):
1. Install Python 3.15 (if you havent, dumbass)
2. Run this in the command line to install the dependencies:
```bash
pip install UnityPy TypeTreeGeneratorAPI dnfile
```
Or:
```bash
python -m pip install UnityPy TypeTreeGeneratorAPI dnfile
```
3. Go to the directory (skip this if you want to manually double-click the python file and run it in Windows):
```bash
cd C:\source\to\the\project\project-name\tools
```
(replace "C:\source\to\the\project and project-name to the path and the name of the project)

Or, on linux:
```bash
cd /source/to/the/project/project-name/tools
```
(replace "/source/to/the/project and project-name to the path and the name of the project)

4. Run the Python file (skip this too if you double-click the python file if you are on windows, you can also don't skip this)
```bash
python3 build_mod.py
```
This will build the mod, into a Plugins folder, or a zip file.

5. Open the Plugins Folder (in the dist folder of the project folder)
6. Drag the Plugins folder's content into your Beat Saber's Plugin folder
7. Enjoy!
