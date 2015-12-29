# Ground Commander for IL-2: Battle of Stalingrad #

This repository contains tools to generate special missions that can be run in IL-2: Battle of Stalingrad multiplayer death-match.
These missions are special because they allow players to give orders to AI-controlled units through a web-browser.

### Repository content ###

* BuildMission.fsx Script to build a mission. Edit the bottom rows to provide the path to the input files.
* Lib/ Groups describing the composition of vehicle platoons and static defenses.
* Commander/ Source code for the web application
* Oblivskaya/ Small sample mission on the Stalingrad summer map. Focused on tank battles.
* Stalingrad/ Large mission. Possibly too big, distances are too long for tanks.
* Kalach, VLuki: Deprecated examples.

### How to build a mission ###

A mission is assembled from several mission files. All the following files must be provided:

* Static.Mission, Static.eng: The content of these files is copied to the generated mission. It's regular mission content, not controlled by Ground Commander.
* Platoons.Group: Start locations, types and coalition of vehicle platoons controlled by Ground Commander. The names are important, as they decide the type of platoon. Follow the convention for the prefixes (Pz3, Pz4, 1c, szf, T34, T70, ba10m, bm13). They must have entities and have their country set to Germany or Russia.
* Waypoints.Group: Destinations and directions of vehicle platoons controlled by Ground Commander.
* Defenses.Group: Locations of static defenses (anti-tank canons, anti-air). Each unit is replaced by a small group (3 units) of the same type. Saves some copy-pasting. Not affected by GroundCommander.
* Airfields.Group: Copied to the generated mission. Meant for the airfields where players spawn. Note that you can have airfields in Static.Mission instead, it works too.

Look at BuildMission.fsx, edit the bottom lines to point to the directory containing the files listed above, run in fsi. If you are using Visual Studio, Ctrl-A followed by Ctrl-Enter (select all, run in fsharp interactive). This can take a few minutes, depending primarily on the size of Static.Mission.

Load the produced file in the mission editor (this will take some time too), validate it, save it to a multiplayer folder. Before running in DServer, delete the generated .Mission file (e.g. GroundCommanderOblivskaya.Mission). It's better to let the server and clients load the mission from the msnbin file instead, it's much faster.

Sometimes the editor crashes while loading the mission. That happens when there are too many platoons or waypoints in Platoons.Group and Waypoints.Group, respectively. Sorry not my fault... You'll have to remove some waypoints or platoons.

### How to run the command interface

Enable the remote console in your DServer config.

The file SampleConfig.json contains two examples of configurations. The first is to be used to command platoons, the second to directly trigger ServerInput MCUs in the mission. Copy-paste the relevant entry (which starts and ends with curly braces) to a new file named Configuration.json.

Edit Configuration.json. It needs the IP and console password to DServer. It also needs the path to Platoon.Group and Waypoints.Group.
Here is a short description of each configuration item:

* rconUsername (string): username to use for the remote console. Note that this has nothing to do with your player or server account name.
* rconPassword (string): password to use for the remote console. This is not your player or server account password.
* rconAddress (string, ip address): IP of the remote console. Typically I use 127.0.0.1 and run the program on the same PC as DServer. This will limit access to the remote console to the PC itself.
* rconPort (integer): port of the remote console.
* webListeningAddresses (array of strings): URL at which players will access the Ground Commander web app.
* bindings (array of strings, ip address/port pair): IPs of the network interfaces on which the Ground Commander web app will receive requests.
* armies (optional record): Controls the functionality to give commands to troops.
* waypointsFilename (array of strings), under armies: Path to the group file containing the waypoints.
* platoonsFilename (array of strings), under armies: Path to the group file containing the platoons start positions and names.
* events (optional record): Controls the functionality to trigger ServerInput MCUs directly.
* password (string), under events: Password to access the events page.
* items (record array), under events: Array or records describing the ServerInput MCUs. Each record has a field "name", which is the name of the MCU in the mission file, and a field "label", which is a human-readable label displayed in the web interface.
* logging (optional boolean): Set to true to debug connection problems with players.

You will need to restart the program every time the server loads a new mission file (because the waypoints and platoons groups are not the same). You do not need to restart it when the server reloads the same mission file.

You can kill it and restart it during a mission. This will confuse players, as their interface in the web browser no longer is in sync with the server's data (and error reporting about that is currently non-existent). Players will have to reload the page and log in again.