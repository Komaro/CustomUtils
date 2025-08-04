using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

public struct Selector<T> {

    private readonly T[] _values;
    public string[] Values { get; }
    
    public delegate void OnChanged(int oldIndex);
    public SafeDelegate<OnChanged> ChangeHandler;

    private int _selectIndex;
    
    public int SelectIndex {
        get => _selectIndex;
        set {
            if (_selectIndex == value) {
                return;
            }
    
            if (_values.IsValidIndex(value)) {
                var oldIndex = _selectIndex;
                _selectIndex = value;
                ChangeHandler.Handler?.Invoke(oldIndex);
            }
        }
    }
    
    public Selector(T[] values, string[] valueTexts) {
        _values = values;
        Values = valueTexts;
        _selectIndex = 0;
        ChangeHandler = default;    
    }

    public Selector(T[] values) {
        _values = values;
        Values = values.Select(value => value.ToString()).ToArray();
        _selectIndex = 0;
        ChangeHandler = default;
    }
    
    public Selector(T[] values, Func<T, string> textSelector) {
        _values = values;
        Values = values.Select(textSelector).ToArray();
        _selectIndex = 0;
        ChangeHandler = default;
    }
    
    public static implicit operator T(in Selector<T> selector) => selector.Get();

    public readonly T Get() => _values[_selectIndex];
    public readonly T Get(int index) => _values.IsValidIndex(index) ? _values[index] : _values[0];
    public readonly T Get(string text) => Values.TryFindIndex(text, out var index) ? _values[index] : _values[0];

    public void Clear() => ChangeHandler.Clear();
}

public class EditorTodoService : EditorService<EditorTodoService> {

    private readonly Dictionary<MemberInfo, List<TodoRequiredAttribute>> _todoAttributeDic = new();
    
    private readonly Dictionary<Type, EditorDrawer> _drawerDic = new();

    private int _selectIndex;
    private Selector<Type> _menuSelector;

    private EditorAsyncOperation _operation; 

    private const string SELECT_MENU_SAVE_KEY = nameof(EditorTodoService);

    [MenuItem("Service/Todo Service")]
    private static void OpenWindow() => Window.Open();
    
    protected override void OnDisable() {
        base.OnDisable();
        _operation?.Clear();
        _menuSelector.Clear();
    }

    protected override void Refresh() => _operation = AsyncRefresh();

    protected override async Task AsyncRefresh(EditorAsyncOperation operation) {
        operation.Init();
        if (HasOpenInstances()) {
            _todoAttributeDic.Clear();
            operation.Report(0.5f);
            
            var types = ReflectionProvider.GetSubTypesOfTypeDefinition(typeof(EditorTodoDrawer<>)).ToArray();
            foreach (var type in types) {
                if (type.TryGetCustomAttribute<EditorTodoDrawerAttribute>(out var attribute) && (_drawerDic.TryGetValue(attribute.attributeType, out var drawer) == false || drawer == null)) {
                    if (SystemUtil.TrySafeCreateInstance(out drawer, type, this)) {
                        _drawerDic.Add(attribute.attributeType, drawer);
                    }
                }
            }
            
            _menuSelector.Clear();
            _menuSelector = new Selector<Type>(_drawerDic.Keys.ToArray(), type => type.GetAlias());
            _menuSelector.ChangeHandler += ChangeMenu;

            if (_drawerDic.TryGetValue(_menuSelector.Get(), out var selectDrawer)) {
                selectDrawer.CacheRefresh();
            }
        }
        
        operation.Done();
        await Task.CompletedTask;
    }

    private void OnGUI() {
        if (_operation is { IsDone: false }) {
            EditorCommon.DrawProgressBar(_operation, "데이터 캐싱 중...");
            Repaint();
            return;
        }
        
        _menuSelector.SelectIndex = EditorCommon.DrawTopToolbar(_menuSelector.SelectIndex, index => EditorCommon.Set(SELECT_MENU_SAVE_KEY, index), _menuSelector.Values);
        if (_drawerDic.TryGetValue(_menuSelector, out var drawer)) {
            drawer.Draw();
        }
    }

    private void ChangeMenu(int oldIndex) {
        if (_drawerDic.TryGetValue(_menuSelector.Get(oldIndex), out var drawer)) {
            drawer.Close();
        }

        if (_drawerDic.TryGetValue(_menuSelector.Get(), out drawer)) {
            drawer.CacheRefresh();
        }
    }
}