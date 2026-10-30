using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

public class DatabaseBinder
{
    public IDisposable BindToFile<T>(IFileAccess fileAccess, string path, Action invalidateAction)
    {
        return fileAccess.WatchFileChanges(path)
            .Buffer(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => invalidateAction());
    }

    public IDisposable BindObject(ObservableObject obj, Action invalidateAction)
    {
        CompositeDisposable disposable = new();
        RecursiveBind(obj, invalidateAction, disposable, new HashSet<object>());
        return disposable;
    }

    private void RecursiveBind(ObservableObject obj, Action invalidateAction, CompositeDisposable disposable,
        HashSet<object> visitedObjects)
    {
        if (!visitedObjects.Add(obj))
        {
            // already bound this object
            return;
        }

        PropertyChangedEventHandler eventHandler = (_, _) => invalidateAction();
        obj.PropertyChanged += eventHandler;
        disposable.Add(Disposable.Create((obj, eventHandler),
            state => state.obj.PropertyChanged -= state.eventHandler));

        foreach (var propertyInfo in obj.GetType().GetProperties())
        {
            // todo: filter out based on attribute so we can ignore some properties
            if (!propertyInfo.GetType().IsAssignableTo(typeof(ObservableObject)))
            {
                continue;
            }

            var value = (ObservableObject?)propertyInfo.GetValue(obj);
            if (value != null)
            {
                RecursiveBind(value, invalidateAction, disposable, visitedObjects);
            }
        }
    }
}