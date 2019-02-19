# DialogTool #
First attempt at making my own tool to create dialog for my game made in Unreal Engine 4 https://github.com/cyphina/UnboundedPerceptions.  This is my first project using WPF.

The tool has similar controls to the UE4 node editor.  I would've tried to extend the editor but both my editor extensions in my UnboundedPerceptions repository took me a long painful run to get them exactly how I wanted since the 
tutorials I followed were outdated/a bit off for my purposes.  I should probably should've looked at some of the source code for some plugins to get it right instead of following the outdated tutorials.

## Instructions ##
To setup, download the files, and add them to a WPF c# project. Or use the exe I added.  

Navigating the tool has as similar control scheme to that used in the UE4 node editor.  

* Left click to select nodes 
* CTRL click to toggle selection on and off for a node 
* Left click drag to select multiple nodes with the selection rectangle. 
* Right click pans around the view, and clicking on selected nodes drags all the nodes selected.  
* Edges can be deleted by alt clicking on them.  
* Press delete to delete the selected nodes and all of their edges.

To create a new node hold either: 
* F (Basic dialog node)
* C (Conditional node which leads to node A if the condition is true, or B if the condition is false)
* T (Trigger node that causes in game effects to happen)
Then click on the spot where you want to create a node.  

If you have any questions about the parameters to the conditions or trigger, hover over the condition or trigger item inside the node's comboxes and a tooltip will appear.

Click on the dark blue button on a node and drag off it and drop on another node to create an edge between the nodes.

Double click the red box at the top to change the title of the dialog since that's what all the node names  will be based off of.  

## Buttons ##
* The save button saves a copy of the graph so that it can be reloaded in the editor, and it saves in the debug folder. 
* The export button exports a JSON file that can be added to the dialog table of my game Unbounded Perception.  
    * To add the dialog to my game, just right click and export the data table file inside the editor to JSON (see https://docs.unrealengine.com/en-us/Gameplay/DataDriven), and add the JSON generated by this tool to the JSON exported from the data table.  Then right click the data table and reimport the combined data.
* The load button loads any save file (not export file).  
* Zoom reset resets the zoom value to the default
* The clear button deletes all the nodes on the screen
* The combine button combines all the JSON files recursively.  It starts from the folder you select, and returns a JSON object which you can import as a data table in the UE4 editor.  
    * If you want to add it to an existing data table delete the brackets around the JSON object and follow the instructions I wrote for the export button 


