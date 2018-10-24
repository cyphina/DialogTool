# DialogTool
First attempt at making my own tool to create dialog for my game.  Made using WPF.  This is my first project using WPF

The tool has similar controls to the UE4 node editor.  I would've tried to extend the editor but both my editor extensions in my UnboundedPerceptions repository took me a long painful run to get them exactly how I wanted since the 
tutorials I followed were outdated/a bit off for my purposes.  

Instructions:
To setup, download the files, and add them to a WPF c# project.  

The tools controls are based off of the UE4 node editor.  Left click to select nodes, CTRL click to toggle selection on and off for a node, and left click drag to select multiple nodes with the selection rectangle.  Right click pans around the view, and clicking on selected nodes drags all the nodes selected.  Edges can be delete by alt clicking on them.  Press delete to delete the selected nodes and all of their edges.

To create a new node hold either F (Basic dialog node), C (Conditional node which leads to node A if the condition is true, or B if the condition is false), or T (Trigger node that causes in game effects to happen)and then click on the spot where you want to create a node.  If you have any questions about the parameters to the conditions or trigger, hover over the condition or trigger item inside the node's comboxes and a tooltip will appear.

Click on the dark blue button on a node and drag off it and drop on another node to create an edge between the nodes.

Double click the red box at the top to change the title of the dialog since that's what all the node names  will be based off of.  

The save button saves a copy of the graph so that it can be reloaded in the editor, and it saves in the debug folder.  The export button exports a JSON file that can be added to the dialog table of my game Unbounded Perception.  To do so,just export the data table file inside the game to JSON, and manually add the JSON to the table.  The load button loads any save file (not export file).  



