using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Ablinger.MyAiHarness.Core.Harness.FileAccess;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Microsoft.VisualBasic;

namespace Ablinger.MyAiHarness.Core.Harness.Database;

public class DatabaseBinder
{
    public IDisposable BindToFile(IFileAccess fileAccess, string path, Action invalidateAction,
        IFileAccess.FileWatchOptions? fileWatchOptions = null)
    {
        return fileAccess.WatchFileChanges(path, fileWatchOptions)
            .Buffer(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => invalidateAction());
    }

    public IDisposable BindToDirectory(IFileAccess fileAccess, string path,
        Action<Dictionary<string, WatcherChangeTypes>> invalidateAction,
        IFileAccess.FileWatchOptions? fileWatchOptions = null)
    {
        return fileAccess.WatchFileChanges(path, fileWatchOptions)
            .Buffer(TimeSpan.FromMilliseconds(200))
            .Subscribe(events =>
            {
                Dictionary<string, WatcherChangeTypes> changes = new();
                foreach (var eventPattern in events)
                {
                    changes[eventPattern.EventArgs.FullPath] = eventPattern.EventArgs.ChangeType;
                }

                invalidateAction(changes);
            });
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