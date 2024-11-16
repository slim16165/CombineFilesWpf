using System;
using System.Collections.Generic;

namespace TreeViewFileExplorer.Events;

/// <summary>
/// Defines methods for subscribing and publishing events.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Subscribes to a specific event type.
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> action);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    void Publish<TEvent>(TEvent eventToPublish);
}

public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<Action<object>>> _subscribers = new Dictionary<Type, List<Action<object>>>();

    public void Subscribe<TEvent>(Action<TEvent> action)
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Action<object>>();
        }
        _subscribers[eventType].Add(e => action((TEvent)e));
    }

    public void Publish<TEvent>(TEvent eventToPublish)
    {
        var eventType = typeof(TEvent);
        if (_subscribers.ContainsKey(eventType))
        {
            foreach (var action in _subscribers[eventType])
            {
                action(eventToPublish);
            }
        }
    }
}