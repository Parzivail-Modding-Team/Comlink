using System.Collections;
using System.Collections.Generic;
using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class PlayerDialogueNode : ComlinkNode, IList<string>
	{
		private readonly IList<string> _listImplementation = new List<string>();

		/// <inheritdoc />
		public int Count => _listImplementation.Count;

		/// <inheritdoc />
		public bool IsReadOnly => _listImplementation.IsReadOnly;

		/// <inheritdoc />
		public string this[int index]
		{
			get => _listImplementation[index];
			set
			{
				_listImplementation[index] = value;
				UpdateOutputPins();
			}
		}

		public PlayerDialogueNode() : base(NodeType.PlayerDialogue, NodeId.NewId())
		{
			Name = "Player Dialogue";
			Color = 0xFF_87cefa;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
		}

		/// <inheritdoc />
		public IEnumerator<string> GetEnumerator()
		{
			return _listImplementation.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable) _listImplementation).GetEnumerator();
		}

		/// <inheritdoc />
		public void Add(string item)
		{
			_listImplementation.Add(item);
			UpdateOutputPins();
		}

		/// <inheritdoc />
		public void Clear()
		{
			_listImplementation.Clear();
			UpdateOutputPins();
		}

		/// <inheritdoc />
		public bool Contains(string item)
		{
			return _listImplementation.Contains(item);
		}

		/// <inheritdoc />
		public void CopyTo(string[] array, int arrayIndex)
		{
			_listImplementation.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc />
		public bool Remove(string item)
		{
			var r = _listImplementation.Remove(item);
			UpdateOutputPins();
			return r;
		}

		/// <inheritdoc />
		public int IndexOf(string item)
		{
			return _listImplementation.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, string item)
		{
			_listImplementation.Insert(index, item);
			UpdateOutputPins();
		}

		/// <inheritdoc />
		public void RemoveAt(int index)
		{
			_listImplementation.RemoveAt(index);
			UpdateOutputPins();
		}

		private void UpdateOutputPins()
		{
			OutputPins.Clear();
			for (var i = 0; i < Count; i++)
			{
				var option = this[i];
				OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, (short) i), option));
			}
		}
	}
}