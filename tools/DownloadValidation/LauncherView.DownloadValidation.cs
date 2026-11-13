using System;
using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void WireValidationDownloadEvents(Action download, Action pause)
    {
        Download.DownloadRequested += download;
        PauseDownloadRequested += pause;
    }

    internal object ValidationUiSnapshot()
    {
        var buttons = new List<object>();
        void Walk(Node node)
        {
            if (node is Button button && button.IsVisibleInTree())
            {
                var rect = button.GetGlobalRect();
                buttons.Add(new
                {
                    name = button.Name.ToString(),
                    button.Text,
                    button.Disabled,
                    x = rect.Position.X,
                    y = rect.Position.Y,
                    width = rect.Size.X,
                    height = rect.Size.Y
                });
            }
            foreach (var child in node.GetChildren()) Walk(child);
        }
        Walk(_parent);
        var viewport = _parent.GetViewport().GetVisibleRect();
        return new { width = viewport.Size.X, height = viewport.Size.Y, buttons, status = _statusLabel.Text };
    }
}
