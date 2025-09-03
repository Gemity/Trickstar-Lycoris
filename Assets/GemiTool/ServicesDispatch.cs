using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ServiceDisposable : IDisposable
{
    private Action action;
    public ServiceDisposable(Action action)
    {
        this.action = action;
    }

    public void Dispose()
    {
        action();
    }
}

[Serializable]
public abstract class BaseService
{
}

public abstract class BaseService<T> : BaseService where T : BaseService<T>
{
    protected event Action<T> action;
    public virtual IDisposable Add(Action<T> action)
    {
        this.action += action;
        return CreateDisposable(action);
    }

    public virtual IDisposable Override(Action<T> action)
    {
        this.action = action;
        return CreateDisposable(action);
    }

    public virtual void Execute()
    {
        action?.Invoke(this as T);
    }

    public virtual void Execute(T t)
    {
        action?.Invoke(t);
    }

    private IDisposable CreateDisposable(Action<T> action)
    {
        return new ServiceDisposable(() => this.action -= action);
    }
}

public class GizmosService : BaseService<GizmosService>
{
}

public class HandleService : BaseService<HandleService>
{
}

public class CallbackService : BaseService<CallbackService>
{
}

public class ExitPlayModeService : BaseService<ExitPlayModeService>
{
    public ExitPlayModeService() : base()
    {
#if UNITY_EDITOR

#endif
    }
}

public class ServicesDispatch : MonoBehaviour
{
    private static Dictionary<Type, Dictionary<string, BaseService>> _services;
    public static IReadOnlyDictionary<Type, Dictionary<string, BaseService>> Services => _services;

    static ServicesDispatch()
    {
        _services = new();
    }

    public static IDisposable Add<T>(string name, Action<T> action) where T : BaseService<T>, new()
    {
        T e = FindOrCreate<T>(name);
        return e.Add(action);
    }

    public static IDisposable Add<T>(Action<T> action) where T : BaseService<T>, new()
    {
        string name = typeof(T).Name;
        T e = FindOrCreate<T>(name);
        return e.Add(action);
    }

    public static IDisposable Override<T>(string name, Action<T> action) where T : BaseService<T>, new()
    {
        T e = FindOrCreate<T>(name);
        return e.Override(action);
    }

    public static void Remove<T>(string name) where T : BaseService<T>
    {
        T e = Find<T>(name);
        if (e != null)
        {
            Type t = typeof(T);
            _services[t].Remove(name);
        }
    }

    public static void Remove(Type t, string name)
    {
        if(_services.ContainsKey(t))
        {
            _services[t].Remove(name);
        }
    }

    public static void Execute<T>(string name) where T : BaseService<T>
    {
        Find<T>(name)?.Execute();
    }

    public static void Execute<T>(T t) where T : BaseService<T>
    {
        string name = typeof(T).Name;
        Find<T>(name)?.Execute(t);
    }

    public static void Execute(Type t, string name)
    {
        var service = Find(t, name);
        if (service == null)
            return;

        MethodInfo method = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if(method != null)
            _ = method.Invoke(service, null);
    }

    public static void ExecuteAndRemove<T>(string name) where T : BaseService<T>
    {
        Execute<T>(name);
        Remove<T>(name);
    }

    public static void ExecuteAndRemove(Type t, string name)
    {
        Execute(t, name);
        Remove(t, name);
    }

    public static void Execute<T>(string name, T t) where T : BaseService<T>
    {
        Find<T>(name)?.Execute(t);
    }

    public static void Execute(Type t, string name, object sender)
    {
        var service = Find(t, name);
        if(service == null) 
            return;

        MethodInfo method = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { t }, null);
        if (method != null)
            _ = method.Invoke(service, new[] { sender });
    }

    public static void ExecuteAndRemove<T>(string name, T t) where T : BaseService<T>
    {
        Execute(name, t);
        Remove<T>(name);
    }

    private static T FindOrCreate<T>(string name) where T : BaseService<T>, new()
    {
        T service = Find<T>(name);

        if (service == null)
        {
            Type t = typeof(T);
            if (!_services.ContainsKey(t))
                _services.Add(t, new());

            service = new T();
            _services[t].Add(name,service);
        }

        return service;
    }

    private static BaseService FindOrCreate(Type t, string name)
    {
        if (!t.IsSubclassOf(typeof(BaseService<>)))
            return null;

        var service = Find(t, name);

        if (service == null)
        {
            if (!_services.ContainsKey(t))
                _services.Add(t, new());

            service = (BaseService)Activator.CreateInstance(t);
            _services[t].Add(name, service);
        }

        return service;
    }

    private static T Find<T>(string name) where T : BaseService<T>
    {
        Type t = typeof(T);
        if (!_services.ContainsKey(t) || !_services[t].ContainsKey(name))
            return null;

        return _services[t][name] as T;
    }

    private static BaseService Find(Type t, string name)
    {
        if (!_services.ContainsKey(t) || !_services[t].ContainsKey(name))
            return null;

        return _services[t][name];
    }

    public static bool HasService<T>(string name) where T : BaseService<T>
    {
        return Find<T>(name) != null;
    }

    public static bool HasService(Type t, string name)
    {
        return Find(t, name) != null;
    }

    public static void RemoveAll()
    {
        _services.Clear();
    }

    private void OnDrawGizmos()
    {
        Type type = typeof(GizmosService);
        if (!_services.ContainsKey(type))
            return;

        foreach (var i in _services[type])
            (i.Value as GizmosService)?.Execute();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ServicesDispatch))]
public class ServiceDispatchEditor : Editor
{
    private void OnSceneGUI()
    {
        Type t = typeof(HandleService);
        if(!ServicesDispatch.Services.ContainsKey(t))
            return;

        foreach (var s in ServicesDispatch.Services[t])
            (s.Value as HandleService)?.Execute();
    }
}
#endif

