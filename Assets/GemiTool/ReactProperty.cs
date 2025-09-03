using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ReactPropertyConverter<T> : JsonConverter<ReactProperty<T>>
{
    public override ReactProperty<T> ReadJson(JsonReader reader, Type objectType, ReactProperty<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        T value = serializer.Deserialize<T>(reader);
        return new ReactProperty<T> (value);
    }

    public override void WriteJson(JsonWriter writer, ReactProperty<T> value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }
}

public class ReactProperty<T>
{
    private T _value;
    private event Action<T> _callbacks;

    public ReactProperty()
    {

    }

    public ReactProperty(T value)
    {
        _value = value;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (Equals(value, _value))
                return;

            _value = value;
            _callbacks?.Invoke(value);
        }
    }

    public IDisposable Subscription(Action<T> callback)
    {
        _callbacks += callback;
        return new ServiceDisposable(() => _callbacks -= callback);
    }
}
