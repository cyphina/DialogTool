# DialogTool
First attempt at making my own tool to create dialog for my game.  Made using WPF.  This is my first project using WPF

The tool has similar controls to the UE4 node editor.  I would've tried to extend the editor but both my editor extensions in my UnboundedPerceptions repository took me a long painful run to get them exactly how I wanted since the 
tutorials I followed were outdated/a bit off for my purposes.  

Instructions:
To setup, download the files, and add them to a WPF c# project.  

The tools controls are based off of the UE4 node editor.  Left click to select nodes, CTRL click to toggle selection on and off for a node, and left click drag to select multiple nodes with the selection rectangle.  Right click pans
around the view, and clicking on selected nodes drags all the nodes selected.  Edges aren't selectable yet, they will be soon.  Individual nodes and edges aren't deletable yet, so press the clear button to clear everything until I promptly update that feature.

The save button saves a copy of the graph so that it can be reloaded in the editor, and it saves in the debug folder.  The export button exports a JSON file that can be added to the dialog table of my game Unbounded Perception.  To do so,just export the data table file inside the game to JSON, and manually add the JSON to the table.  The load button loads any save file (not export file).  

Right now the export button and save buttons save to the same file so using either will overwrite the other one so beware of that.  I'll change that very soon.


