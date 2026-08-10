using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;

namespace ST.Library.UI.NodeEditor;

public sealed class STNodeEditHistoryEntry
{
	internal ISTNodeEditOperation Operation { get; }

	internal long BeforeStateId { get; }

	internal long AfterStateId { get; set; }

	internal DateTime LastChangedUtc { get; set; }

	public string Description { get; }

	internal STNodeEditHistoryEntry(string description, ISTNodeEditOperation operation, long beforeStateId, long afterStateId)
	{
		Description = string.IsNullOrWhiteSpace(description) ? "编辑流程" : description;
		Operation = operation;
		BeforeStateId = beforeStateId;
		AfterStateId = afterStateId;
		LastChangedUtc = DateTime.UtcNow;
	}

	public override string ToString()
	{
		return Description;
	}
}

public sealed class STNodeEditTransaction : IDisposable
{
	private STNodeEditor _editor;
	private readonly bool _active;
	private bool _cancelled;

	internal STNodeEditTransaction(STNodeEditor editor, bool active)
	{
		_editor = editor;
		_active = active;
	}

	public void Cancel()
	{
		_cancelled = true;
	}

	public void Dispose()
	{
		STNodeEditor editor = _editor;
		_editor = null;
		if (_active && editor != null)
		{
			editor.EndEditTransaction(_cancelled);
		}
	}
}

internal interface ISTNodeEditOperation
{
	void Undo(STNodeEditor editor);

	void Redo(STNodeEditor editor);

	bool TryMerge(ISTNodeEditOperation operation);
}

internal sealed class STNodeCompositeEditOperation : ISTNodeEditOperation
{
	private readonly IReadOnlyList<ISTNodeEditOperation> _operations;

	public STNodeCompositeEditOperation(IReadOnlyList<ISTNodeEditOperation> operations)
	{
		_operations = operations;
	}

	public void Undo(STNodeEditor editor)
	{
		for (int i = _operations.Count - 1; i >= 0; i--)
		{
			_operations[i].Undo(editor);
		}
	}

	public void Redo(STNodeEditor editor)
	{
		for (int i = 0; i < _operations.Count; i++)
		{
			_operations[i].Redo(editor);
		}
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		return false;
	}
}

internal sealed class STNodeAddedEditOperation : ISTNodeEditOperation
{
	private readonly STNode _node;
	private readonly int _index;

	public STNodeAddedEditOperation(STNode node, int index)
	{
		_node = node;
		_index = index;
	}

	public void Undo(STNodeEditor editor)
	{
		editor.Nodes.Remove(_node);
	}

	public void Redo(STNodeEditor editor)
	{
		editor.Nodes.Insert(Math.Min(_index, editor.Nodes.Count), _node);
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		return false;
	}
}

internal sealed class STNodeRemovedEditOperation : ISTNodeEditOperation
{
	private readonly STNode _node;
	private readonly int _index;
	private readonly Dictionary<string, byte[]> _state;

	public STNodeRemovedEditOperation(STNode node, int index, Dictionary<string, byte[]> state)
	{
		_node = node;
		_index = index;
		_state = state;
	}

	public void Undo(STNodeEditor editor)
	{
		_node.OnLoadNode(CloneState(_state));
		editor.Nodes.Insert(Math.Min(_index, editor.Nodes.Count), _node);
	}

	public void Redo(STNodeEditor editor)
	{
		editor.Nodes.Remove(_node);
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		return false;
	}

	private static Dictionary<string, byte[]> CloneState(Dictionary<string, byte[]> state)
	{
		return state.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
	}
}

internal sealed class STNodeOptionReference
{
	private readonly STNode _node;
	private readonly bool _isInput;
	private readonly int _index;
	private readonly STNodeOption _option;
	private readonly int _optionCount;

	private STNodeOptionReference(STNode node, bool isInput, int index, STNodeOption option, int optionCount)
	{
		_node = node;
		_isInput = isInput;
		_index = index;
		_option = option;
		_optionCount = optionCount;
	}

	public static STNodeOptionReference Create(STNodeOption option)
	{
		if (option == null || option.Owner == null)
		{
			return null;
		}
		STNodeOption[] options = option.IsInput
			? option.Owner.GetAllInputOptions()
			: option.Owner.GetAllOutputOptions();
		int index = Array.IndexOf(options, option);
		return index < 0 ? null : new STNodeOptionReference(option.Owner, option.IsInput, index, option, options.Length);
	}

	public STNodeOption Resolve(Dictionary<string, byte[]> state = null)
	{
		STNodeOption[] options = _isInput ? _node.GetAllInputOptions() : _node.GetAllOutputOptions();
		int currentIndex = Array.IndexOf(options, _option);
		if (currentIndex >= 0)
		{
			return options[currentIndex];
		}
		if (state != null && _option.Owner == null)
		{
			STNodeOptionCollection collection = _isInput ? _node.InputOptions : _node.OutputOptions;
			if (collection.Count >= _optionCount && _index < collection.Count)
			{
				STNodeOption replacement = collection[_index];
				if (replacement.ConnectionCount == 0)
				{
					collection.RemoveAt(_index);
				}
			}
			collection.Insert(Math.Min(_index, collection.Count), _option);
			return _option;
		}
		if ((_index < 0 || _index >= options.Length) && state != null)
		{
			_node.OnLoadNode(state.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone()));
			options = _isInput ? _node.GetAllInputOptions() : _node.GetAllOutputOptions();
		}
		if (_index < 0 || _index >= options.Length)
		{
			throw new InvalidOperationException("无法恢复节点端口，端口结构已发生变化");
		}
		return options[_index];
	}
}

internal sealed class STNodeConnectionEditOperation : ISTNodeEditOperation
{
	private readonly STNodeOptionReference _output;
	private readonly STNodeOptionReference _input;
	private readonly bool _connected;
	private readonly Dictionary<string, byte[]> _outputStateBefore;
	private readonly Dictionary<string, byte[]> _inputStateBefore;

	public STNodeConnectionEditOperation(
		STNodeOptionReference output,
		STNodeOptionReference input,
		bool connected,
		Dictionary<string, byte[]> outputStateBefore,
		Dictionary<string, byte[]> inputStateBefore)
	{
		_output = output;
		_input = input;
		_connected = connected;
		_outputStateBefore = outputStateBefore;
		_inputStateBefore = inputStateBefore;
	}

	public void Undo(STNodeEditor editor)
	{
		SetConnected(!_connected);
	}

	public void Redo(STNodeEditor editor)
	{
		SetConnected(_connected);
	}

	private void SetConnected(bool connected)
	{
		STNodeOption output = _output.Resolve(connected ? _outputStateBefore : null);
		STNodeOption input = _input.Resolve(connected ? _inputStateBefore : null);
		bool currentlyConnected = output.ConnectedOption.Contains(input) && input.ConnectedOption.Contains(output);
		if (currentlyConnected == connected)
		{
			return;
		}

		STNode outputNode = output.Owner;
		STNode inputNode = input.Owner;
		bool outputLocked = outputNode.LockOption;
		bool inputLocked = inputNode.LockOption;
		outputNode.LockOption = false;
		inputNode.LockOption = false;
		try
		{
			ConnectionStatus status = connected
				? output.ConnectOption(input)
				: output.DisConnectOption(input);
			ConnectionStatus expected = connected ? ConnectionStatus.Connected : ConnectionStatus.DisConnected;
			if (status != expected)
			{
				throw new InvalidOperationException($"无法{(connected ? "恢复" : "撤销")}节点连接：{status}");
			}
		}
		finally
		{
			outputNode.LockOption = outputLocked;
			inputNode.LockOption = inputLocked;
		}
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		return false;
	}
}

internal sealed class STNodeMoveEditOperation : ISTNodeEditOperation
{
	private readonly Dictionary<STNode, Point> _before;
	private Dictionary<STNode, Point> _after;

	public STNodeMoveEditOperation(Dictionary<STNode, Point> before, Dictionary<STNode, Point> after)
	{
		_before = before;
		_after = after;
	}

	public void Undo(STNodeEditor editor)
	{
		Apply(editor, _before);
	}

	public void Redo(STNodeEditor editor)
	{
		Apply(editor, _after);
	}

	private static void Apply(STNodeEditor editor, Dictionary<STNode, Point> locations)
	{
		foreach (KeyValuePair<STNode, Point> pair in locations)
		{
			if (!editor.Nodes.Contains(pair.Key))
			{
				continue;
			}
			bool locked = pair.Key.LockLocation;
			pair.Key.LockLocation = false;
			pair.Key.Location = pair.Value;
			pair.Key.LockLocation = locked;
		}
		editor.BuildBounds();
		editor.BuildLinePath();
		editor.Invalidate();
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		STNodeMoveEditOperation other = operation as STNodeMoveEditOperation;
		if (other == null || _before.Count != other._before.Count || _before.Keys.Any(node => !other._before.ContainsKey(node)))
		{
			return false;
		}
		_after = new Dictionary<STNode, Point>(other._after);
		return true;
	}
}

internal sealed class STNodeStateEditOperation : ISTNodeEditOperation
{
	private readonly STNode _node;
	private readonly Dictionary<string, byte[]> _before;
	private Dictionary<string, byte[]> _after;
	private readonly string _propertyName;

	public STNodeStateEditOperation(STNode node, Dictionary<string, byte[]> before, Dictionary<string, byte[]> after, string propertyName)
	{
		_node = node;
		_before = before;
		_after = after;
		_propertyName = propertyName ?? string.Empty;
	}

	public void Undo(STNodeEditor editor)
	{
		Apply(editor, _before);
	}

	public void Redo(STNodeEditor editor)
	{
		Apply(editor, _after);
	}

	private void Apply(STNodeEditor editor, Dictionary<string, byte[]> state)
	{
		if (!editor.Nodes.Contains(_node))
		{
			return;
		}
		_node.OnLoadNode(CloneState(state));
		editor.BuildBounds();
		editor.BuildLinePath();
		editor.Invalidate();
	}

	public bool TryMerge(ISTNodeEditOperation operation)
	{
		STNodeStateEditOperation other = operation as STNodeStateEditOperation;
		if (other == null || !ReferenceEquals(_node, other._node) || !string.Equals(_propertyName, other._propertyName, StringComparison.Ordinal))
		{
			return false;
		}
		_after = CloneState(other._after);
		return true;
	}

	private static Dictionary<string, byte[]> CloneState(Dictionary<string, byte[]> state)
	{
		return state.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
	}
}

public partial class STNodeEditor
{
	private const int DefaultMaximumHistoryEntries = 100;
	private readonly ObservableCollection<STNodeEditHistoryEntry> _undoHistory = new ObservableCollection<STNodeEditHistoryEntry>();
	private readonly ObservableCollection<STNodeEditHistoryEntry> _redoHistory = new ObservableCollection<STNodeEditHistoryEntry>();
	private readonly Dictionary<STNode, Dictionary<string, byte[]>> _nodeStateCache = new Dictionary<STNode, Dictionary<string, byte[]>>();
	private ReadOnlyObservableCollection<STNodeEditHistoryEntry> _readOnlyUndoHistory;
	private ReadOnlyObservableCollection<STNodeEditHistoryEntry> _readOnlyRedoHistory;
	private Dictionary<STNode, NodeEditSnapshot> _transactionSnapshots;
	private List<ISTNodeEditOperation> _transactionOperations;
	private string _transactionDescription;
	private int _transactionDepth;
	private int _historySuppressionDepth;
	private int _historyReplayDepth;
	private bool _transactionCancelled;
	private bool _enableHistory;
	private long _nextStateId;
	private long _currentStateId;
	private long _savedStateId;
	private STNodeEditTransaction _pointerEditTransaction;
	private STNodeOption _pendingConnectionFirst;
	private STNodeOption _pendingConnectionSecond;
	private STNodeOptionReference _pendingConnectionOutput;
	private STNodeOptionReference _pendingConnectionInput;
	private Dictionary<string, byte[]> _pendingConnectionOutputState;
	private Dictionary<string, byte[]> _pendingConnectionInputState;

	private sealed class NodeEditSnapshot
	{
		public Point Location { get; }

		public Dictionary<string, byte[]> State { get; }

		public NodeEditSnapshot(Point location, Dictionary<string, byte[]> state)
		{
			Location = location;
			State = state;
		}
	}

	public bool EnableHistory
	{
		get => _enableHistory;
		set
		{
			if (_enableHistory == value)
			{
				return;
			}
			_enableHistory = value;
			ClearHistory();
			RefreshNodeStateCache();
		}
	}

	public int MaximumHistoryEntries { get; set; } = DefaultMaximumHistoryEntries;

	public bool CanUndo => _undoHistory.Count > 0 && _transactionDepth == 0;

	public bool CanRedo => _redoHistory.Count > 0 && _transactionDepth == 0;

	public bool IsModified => _currentStateId != _savedStateId;

	public bool IsReplayingHistory => _historyReplayDepth > 0;

	public ReadOnlyObservableCollection<STNodeEditHistoryEntry> UndoHistory => _readOnlyUndoHistory;

	public ReadOnlyObservableCollection<STNodeEditHistoryEntry> RedoHistory => _readOnlyRedoHistory;

	public event EventHandler HistoryChanged;

	public event EventHandler NodeLocationChanged;

	private void InitializeEditing()
	{
		_readOnlyUndoHistory = new ReadOnlyObservableCollection<STNodeEditHistoryEntry>(_undoHistory);
		_readOnlyRedoHistory = new ReadOnlyObservableCollection<STNodeEditHistoryEntry>(_redoHistory);
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (_, _) => Undo(), (_, e) => e.CanExecute = CanUndo));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (_, _) => Redo(), (_, e) => e.CanExecute = CanRedo));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, (_, _) => CutSelectionToClipboard(), (_, e) => e.CanExecute = EnableEdit && GetSelectedNode().Length > 0));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, _) => CopySelectionToClipboard(), (_, e) => e.CanExecute = GetSelectedNode().Length > 0));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (_, _) => ExecutePasteCommand(), (_, e) => e.CanExecute = EnableEdit && ClipboardContainsGraph()));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (_, _) => DeleteSelectedNodes(), (_, e) => e.CanExecute = EnableEdit && GetSelectedNode().Length > 0));
		CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_, _) => SelectAllNodes(), (_, e) => e.CanExecute = Nodes.Count > 0));
		ClearHistory();
	}

	public STNodeEditTransaction BeginEditTransaction(string description)
	{
		if (!_enableHistory || _historySuppressionDepth > 0)
		{
			return new STNodeEditTransaction(this, active: false);
		}
		if (_transactionDepth == 0)
		{
			_transactionDescription = description;
			_transactionOperations = new List<ISTNodeEditOperation>();
			_transactionSnapshots = CaptureNodeSnapshots();
			_transactionCancelled = false;
		}
		_transactionDepth++;
		return new STNodeEditTransaction(this, active: true);
	}

	public void ExecuteEditTransaction(string description, Action action)
	{
		if (action == null)
		{
			throw new ArgumentNullException(nameof(action));
		}
		using STNodeEditTransaction transaction = BeginEditTransaction(description);
		try
		{
			action();
		}
		catch
		{
			transaction.Cancel();
			throw;
		}
	}

	internal void EndEditTransaction(bool cancel)
	{
		if (_transactionDepth <= 0)
		{
			return;
		}
		_transactionCancelled |= cancel;
		_transactionDepth--;
		if (_transactionDepth != 0)
		{
			return;
		}

		try
		{
			if (_transactionCancelled)
			{
				RollbackTransaction();
			}
			else
			{
				AppendSnapshotChanges();
				if (_transactionOperations.Count > 0)
				{
					ISTNodeEditOperation operation = _transactionOperations.Count == 1
						? _transactionOperations[0]
						: new STNodeCompositeEditOperation(_transactionOperations.ToArray());
					AddHistoryEntry(_transactionDescription, operation, allowMerge: false);
				}
			}
		}
		finally
		{
			_transactionOperations = null;
			_transactionSnapshots = null;
			_transactionDescription = null;
			_transactionCancelled = false;
			RefreshNodeStateCache();
		}
	}

	private void RollbackTransaction()
	{
		ReplayHistory(() =>
		{
			for (int i = _transactionOperations.Count - 1; i >= 0; i--)
			{
				_transactionOperations[i].Undo(this);
			}
			foreach (KeyValuePair<STNode, NodeEditSnapshot> pair in _transactionSnapshots)
			{
				STNode node = pair.Key;
				if (!Nodes.Contains(node))
				{
					continue;
				}
				node.OnLoadNode(CloneState(pair.Value.State));
				bool locked = node.LockLocation;
				node.LockLocation = false;
				node.Location = pair.Value.Location;
				node.LockLocation = locked;
			}
			BuildBounds();
			BuildLinePath();
			Invalidate();
		});
	}

	public IDisposable SuspendHistoryRecording()
	{
		_historySuppressionDepth++;
		return new DelegateDisposable(() =>
		{
			_historySuppressionDepth--;
			if (_historySuppressionDepth == 0)
			{
				RefreshNodeStateCache();
			}
		});
	}

	public void Undo()
	{
		if (!CanUndo)
		{
			return;
		}
		STNodeEditHistoryEntry entry = _undoHistory[_undoHistory.Count - 1];
		ReplayHistory(() => entry.Operation.Undo(this));
		_undoHistory.RemoveAt(_undoHistory.Count - 1);
		_redoHistory.Add(entry);
		_currentStateId = entry.BeforeStateId;
		NotifyHistoryChanged();
	}

	public void Redo()
	{
		if (!CanRedo)
		{
			return;
		}
		STNodeEditHistoryEntry entry = _redoHistory[_redoHistory.Count - 1];
		ReplayHistory(() => entry.Operation.Redo(this));
		_redoHistory.RemoveAt(_redoHistory.Count - 1);
		_undoHistory.Add(entry);
		_currentStateId = entry.AfterStateId;
		NotifyHistoryChanged();
	}

	public void ClearHistory()
	{
		_undoHistory.Clear();
		_redoHistory.Clear();
		_currentStateId = ++_nextStateId;
		_savedStateId = _currentStateId;
		RefreshNodeStateCache();
		NotifyHistoryChanged();
	}

	public void MarkSaved()
	{
		_savedStateId = _currentStateId;
		NotifyHistoryChanged();
	}

	public bool DeleteSelectedNodes()
	{
		STNode[] nodes = GetSelectedNode()
			.OrderByDescending(node => Nodes.IndexOf(node))
			.ToArray();
		if (!EnableEdit || nodes.Length == 0)
		{
			return false;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("删除节点");
		foreach (STNode node in nodes)
		{
			Nodes.Remove(node);
		}
		return true;
	}

	public bool MoveSelectedNodes(int offsetX, int offsetY)
	{
		STNode[] nodes = GetSelectedNode();
		if (!EnableEdit || nodes.Length == 0 || offsetX == 0 && offsetY == 0)
		{
			return false;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("移动节点");
		bool moved = false;
		foreach (STNode node in nodes)
		{
			Point before = node.Location;
			node.Location = new Point(before.X + offsetX, before.Y + offsetY);
			moved |= node.Location != before;
		}
		return moved;
	}

	public void SelectAllNodes()
	{
		foreach (STNode node in Nodes)
		{
			AddSelectedNode(node);
		}
	}

	internal void RecordNodeAdded(STNode node, int index)
	{
		TrackNode(node);
		RecordOperation(new STNodeAddedEditOperation(node, index), "添加节点");
	}

	internal Dictionary<string, byte[]> CaptureNodeStateForRemoval(STNode node)
	{
		return !_enableHistory || _historySuppressionDepth > 0
			? null
			: CloneState(CapturePersistentState(node));
	}

	internal List<STNodeConnectionEditOperation> CaptureNodeConnectionsForRemoval(STNode node)
	{
		if (!_enableHistory || _historySuppressionDepth > 0)
		{
			return null;
		}
		return GetConnections()
			.Where(connection => ReferenceEquals(connection.Output.Owner, node) || ReferenceEquals(connection.Input.Owner, node))
			.Select(connection => new STNodeConnectionEditOperation(
				STNodeOptionReference.Create(connection.Output),
				STNodeOptionReference.Create(connection.Input),
				connected: false,
				CloneState(CapturePersistentState(connection.Output.Owner)),
				CloneState(CapturePersistentState(connection.Input.Owner))))
			.ToList();
	}

	internal void RecordNodeConnectionsRemoved(List<STNodeConnectionEditOperation> operations)
	{
		if (operations == null)
		{
			return;
		}
		foreach (STNodeConnectionEditOperation operation in operations)
		{
			RecordOperation(operation, "断开连接");
		}
	}

	internal void RecordNodeRemoved(STNode node, int index, Dictionary<string, byte[]> state)
	{
		if (state == null)
		{
			return;
		}
		RecordOperation(new STNodeRemovedEditOperation(node, index, state), "删除节点");
	}

	internal void PrepareConnectionChange(STNodeOption first, STNodeOption second)
	{
		_pendingConnectionFirst = first;
		_pendingConnectionSecond = second;
		_pendingConnectionOutput = null;
		_pendingConnectionInput = null;
		_pendingConnectionOutputState = null;
		_pendingConnectionInputState = null;
		if (!_enableHistory || _historySuppressionDepth > 0)
		{
			return;
		}
		if (first == null || second == null || first.IsInput == second.IsInput)
		{
			return;
		}
		STNodeOption output = first.IsInput ? second : first;
		STNodeOption input = first.IsInput ? first : second;
		_pendingConnectionOutput = STNodeOptionReference.Create(output);
		_pendingConnectionInput = STNodeOptionReference.Create(input);
		_pendingConnectionOutputState = CapturePersistentState(output.Owner);
		_pendingConnectionInputState = CapturePersistentState(input.Owner);
	}

	internal void CompleteConnectionChange(STNodeOption first, STNodeOption second, bool? connected)
	{
		STNodeOptionReference outputReference = _pendingConnectionOutput;
		STNodeOptionReference inputReference = _pendingConnectionInput;
		Dictionary<string, byte[]> outputState = _pendingConnectionOutputState;
		Dictionary<string, byte[]> inputState = _pendingConnectionInputState;
		bool matches = ReferenceEquals(first, _pendingConnectionFirst) && ReferenceEquals(second, _pendingConnectionSecond)
			|| ReferenceEquals(first, _pendingConnectionSecond) && ReferenceEquals(second, _pendingConnectionFirst);
		_pendingConnectionFirst = null;
		_pendingConnectionSecond = null;
		_pendingConnectionOutput = null;
		_pendingConnectionInput = null;
		_pendingConnectionOutputState = null;
		_pendingConnectionInputState = null;
		if (!matches || !connected.HasValue || outputReference == null || inputReference == null)
		{
			return;
		}
		STNodeOption output = first.IsInput ? second : first;
		STNodeOption input = first.IsInput ? first : second;
		bool exists = output.ConnectedOption.Contains(input) && input.ConnectedOption.Contains(output);
		if (exists != connected.Value)
		{
			return;
		}
		RecordOperation(
			new STNodeConnectionEditOperation(
				outputReference,
				inputReference,
				connected.Value,
				CloneState(outputState),
				CloneState(inputState)),
			connected.Value ? "连接节点" : "断开连接");
	}

	internal void OnNodeLocationChanged(STNode node, Point before, Point after)
	{
		if (before == after)
		{
			return;
		}
		NodeLocationChanged?.Invoke(this, EventArgs.Empty);
		if (_transactionDepth > 0)
		{
			return;
		}
		RecordOperation(
			new STNodeMoveEditOperation(
				new Dictionary<STNode, Point> { [node] = before },
				new Dictionary<STNode, Point> { [node] = after }),
			"移动节点",
			allowMerge: true);
	}

	internal void TrackNode(STNode node)
	{
		if (node == null || _nodeStateCache.ContainsKey(node))
		{
			return;
		}
		node.PropertyChanged += Node_PropertyChanged;
		_nodeStateCache[node] = CapturePersistentState(node);
	}

	internal void UntrackNode(STNode node)
	{
		if (node == null)
		{
			return;
		}
		node.PropertyChanged -= Node_PropertyChanged;
		_nodeStateCache.Remove(node);
	}

	private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		STNode node = sender as STNode;
		if (node == null)
		{
			return;
		}
		Dictionary<string, byte[]> after = CapturePersistentState(node);
		if (!_nodeStateCache.TryGetValue(node, out Dictionary<string, byte[]> before))
		{
			_nodeStateCache[node] = after;
			return;
		}
		if (_transactionDepth > 0 || !_enableHistory || _historySuppressionDepth > 0)
		{
			return;
		}
		if (StatesEqual(before, after))
		{
			return;
		}
		_nodeStateCache[node] = after;
		string propertyName = e.PropertyName ?? string.Empty;
		RecordOperation(
			new STNodeStateEditOperation(node, CloneState(before), CloneState(after), propertyName),
			string.IsNullOrEmpty(propertyName) ? "修改节点属性" : $"修改 {propertyName}",
			allowMerge: true);
	}

	private void BeginPointerEdit(string description)
	{
		if (_pointerEditTransaction == null)
		{
			_pointerEditTransaction = BeginEditTransaction(description);
		}
	}

	private void EndPointerEdit()
	{
		STNodeEditTransaction transaction = _pointerEditTransaction;
		_pointerEditTransaction = null;
		transaction?.Dispose();
	}

	private void DisposeEditing()
	{
		STNodeEditTransaction transaction = _pointerEditTransaction;
		_pointerEditTransaction = null;
		transaction?.Cancel();
		transaction?.Dispose();
		foreach (STNode node in _nodeStateCache.Keys.ToArray())
		{
			UntrackNode(node);
		}
		_undoHistory.Clear();
		_redoHistory.Clear();
		HistoryChanged = null;
		NodeLocationChanged = null;
	}

	private void RecordOperation(ISTNodeEditOperation operation, string description, bool allowMerge = false)
	{
		if (!_enableHistory || _historySuppressionDepth > 0 || operation == null)
		{
			return;
		}
		if (_transactionDepth > 0)
		{
			_transactionOperations.Add(operation);
			return;
		}
		AddHistoryEntry(description, operation, allowMerge);
		RefreshNodeStateCache();
	}

	private void AddHistoryEntry(string description, ISTNodeEditOperation operation, bool allowMerge)
	{
		_redoHistory.Clear();
		if (allowMerge && _undoHistory.Count > 0)
		{
			STNodeEditHistoryEntry previous = _undoHistory[_undoHistory.Count - 1];
			if (previous.AfterStateId != _savedStateId
				&& DateTime.UtcNow - previous.LastChangedUtc <= TimeSpan.FromMilliseconds(750)
				&& previous.Operation.TryMerge(operation))
			{
				previous.AfterStateId = ++_nextStateId;
				previous.LastChangedUtc = DateTime.UtcNow;
				_currentStateId = previous.AfterStateId;
				NotifyHistoryChanged();
				return;
			}
		}

		long beforeStateId = _currentStateId;
		long afterStateId = ++_nextStateId;
		_undoHistory.Add(new STNodeEditHistoryEntry(description, operation, beforeStateId, afterStateId));
		while (_undoHistory.Count > Math.Max(1, MaximumHistoryEntries))
		{
			_undoHistory.RemoveAt(0);
		}
		_currentStateId = afterStateId;
		NotifyHistoryChanged();
	}

	private void ReplayHistory(Action action)
	{
		_historyReplayDepth++;
		_historySuppressionDepth++;
		try
		{
			action();
		}
		finally
		{
			_historySuppressionDepth--;
			_historyReplayDepth--;
			RefreshNodeStateCache();
		}
	}

	private Dictionary<STNode, NodeEditSnapshot> CaptureNodeSnapshots()
	{
		Dictionary<STNode, NodeEditSnapshot> snapshots = new Dictionary<STNode, NodeEditSnapshot>();
		foreach (STNode node in Nodes)
		{
			snapshots[node] = new NodeEditSnapshot(node.Location, CapturePersistentState(node));
		}
		return snapshots;
	}

	private void AppendSnapshotChanges()
	{
		if (_transactionSnapshots == null)
		{
			return;
		}
		Dictionary<STNode, Point> beforeLocations = new Dictionary<STNode, Point>();
		Dictionary<STNode, Point> afterLocations = new Dictionary<STNode, Point>();
		foreach (KeyValuePair<STNode, NodeEditSnapshot> pair in _transactionSnapshots)
		{
			STNode node = pair.Key;
			if (!Nodes.Contains(node))
			{
				continue;
			}
			if (node.Location != pair.Value.Location)
			{
				beforeLocations[node] = pair.Value.Location;
				afterLocations[node] = node.Location;
			}
			Dictionary<string, byte[]> afterState = CapturePersistentState(node);
			if (!StatesEqual(pair.Value.State, afterState))
			{
				_transactionOperations.Add(new STNodeStateEditOperation(node, CloneState(pair.Value.State), CloneState(afterState), string.Empty));
			}
		}
		if (beforeLocations.Count > 0)
		{
			_transactionOperations.Add(new STNodeMoveEditOperation(beforeLocations, afterLocations));
		}
	}

	private static Dictionary<string, byte[]> CapturePersistentState(STNode node)
	{
		Dictionary<string, byte[]> state = node.OnSaveNode()
			.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
		state.Remove("Guid");
		state.Remove("Left");
		state.Remove("Top");
		return state;
	}

	private static Dictionary<string, byte[]> CloneState(Dictionary<string, byte[]> state)
	{
		return state.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
	}

	private static bool StatesEqual(Dictionary<string, byte[]> first, Dictionary<string, byte[]> second)
	{
		if (first.Count != second.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, byte[]> pair in first)
		{
			if (!second.TryGetValue(pair.Key, out byte[] value) || !pair.Value.SequenceEqual(value))
			{
				return false;
			}
		}
		return true;
	}

	private void RefreshNodeStateCache()
	{
		STNode[] staleNodes = _nodeStateCache.Keys.Where(node => !Nodes.Contains(node)).ToArray();
		foreach (STNode node in staleNodes)
		{
			UntrackNode(node);
		}
		foreach (STNode node in Nodes)
		{
			TrackNode(node);
			_nodeStateCache[node] = CapturePersistentState(node);
		}
	}

	private void NotifyHistoryChanged()
	{
		HistoryChanged?.Invoke(this, EventArgs.Empty);
		CommandManager.InvalidateRequerySuggested();
	}

	private sealed class DelegateDisposable : IDisposable
	{
		private Action _dispose;

		public DelegateDisposable(Action dispose)
		{
			_dispose = dispose;
		}

		public void Dispose()
		{
			Action dispose = _dispose;
			_dispose = null;
			dispose?.Invoke();
		}
	}
}
