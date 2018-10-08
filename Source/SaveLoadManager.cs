using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace UPDialogTool
{
    class SaveLoadManager
    {
	    private Dictionary<int, UPNodeBase> loadedNodes = new Dictionary<int, UPNodeBase>();
		private LinkedList<UPNodeConnector> loadedEdges = new LinkedList<UPNodeConnector>();

	    public void SaveNodes(HashSet<UPNodeBase> nodeList, JsonWriter writer)
	    {
		    writer.WritePropertyName("Nodes");
		    writer.WriteStartArray();
		    foreach (UPNodeBase node in nodeList)
		    {
			    SaveNodeData(node, writer);
		    }
		    writer.WriteEndArray();
	    }

	    public void SaveEdges(LinkedList<UPNodeConnector> edgeList, JsonWriter writer)
	    {
		    writer.WritePropertyName("Edges");
		    writer.WriteStartArray();
		    foreach (UPNodeConnector edge in edgeList)
		    {
			    SaveEdgeData(edge, writer);
		    }
		    writer.WriteEndArray();
	    }

	    public void SaveNodeData(UPNodeBase node, JsonWriter writer)
		{
			//Save node type information
			writer.WriteValue(node.GetNodeType());

			//Save node id
			writer.WriteValue(node.nodeID);

			//Save Dialog
			writer.WriteValue(node.DialogText);

			////Save Speaker Name
			writer.WriteValue(node.Actor);

			////Save Node Position
			writer.WriteValue(node.Translate.X);
			writer.WriteValue(node.Translate.Y);
		}

		public void SaveEdgeData(UPNodeConnector edge, JsonWriter writer)
		{
			writer.WriteValue(edge.fromNodeRef.nodeID);
			writer.WriteValue(edge.toNodeRef.nodeID);			
		}

		public List<UPNodeBase> LoadNodeData(JsonTextReader reader)
		{
			loadedNodes.Clear();
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				UPNodeBase node = null;
				NodeType nodeType = (NodeType)(long)reader.Value;
				switch (nodeType)
				{
					case NodeType.Root: 
						node = new RootNode();
						break;
					case NodeType.Dialog: 
						node = new UPNode();
						break;
					case NodeType.Condition:
						node = new UPNodeCondition();
						break;
					case NodeType.Trigger:
						node = new UPNodeTrigger();
						break;
					default:
						node = new UPNode();
						break;
				}
				reader.Read();

				node.nodeID = (int) (long) reader.Value;
				reader.Read();

				node.DialogText = (string) reader.Value;
				reader.Read();

				node.Actor = (string) reader.Value;
				reader.Read();

				node.Translate.X = (double) reader.Value;
				reader.Read();

				node.Translate.Y = (double) reader.Value;

				loadedNodes.Add(node.nodeID, node);		
			}
			return loadedNodes.Values.ToList();
		}

		public LinkedList<UPNodeConnector> LoadEdges(JsonTextReader reader, HashSet<UPNodeBase> nodeList)
		{
			loadedEdges.Clear();
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				UPNodeConnector edge = new UPNodeConnector();
				int fromNodeID = (int) (long) reader.Value;
				edge.fromNodeRef = loadedNodes[fromNodeID];
				edge.fromNodeRef.fromLines.AddLast(edge);
				reader.Read();

				int toNodeID = (int) (long) reader.Value;
				edge.toNodeRef = loadedNodes[toNodeID];
				edge.toNodeRef.toLines.AddLast(edge);
				loadedEdges.AddLast(edge);
			}
			return loadedEdges;
		}
					
	    public void WriteProperty<T>(JsonWriter writer, string propertyName, T propertyValue)
	    {
		    writer.WritePropertyName(propertyName);
		    writer.WriteValue(propertyValue);
	    }
    }
}
