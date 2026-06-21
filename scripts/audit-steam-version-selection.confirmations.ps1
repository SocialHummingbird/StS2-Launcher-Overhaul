function Add-SteamVersionSelectionConfirmationChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Dialog.Buttons.cs" `
        "accepts contextual confirmation button labels" `
        @(
            "confirmText",
            "cancelText",
            "DialogButtonText",
            "BuildDialogButtons",
            "TryGetPressedPointerPosition"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Dialog.Message.cs" `
        "keeps compact confirmation warning messages scroll-safe" `
        @(
            "CompactDialogWidthRatio = 0\.9f",
            "CompactDialogMaxMessageHeightRatio = 0\.44f",
            "CompactDialogMessageMinScrollHeight = 96",
            "BuildDialogMessageArea",
            "ShouldScrollDialogMessage",
            "DialogMessageScrollHeight",
            "new ScrollContainer",
            "DialogMessageWidth\(profile\)",
            "profile\.ViewportSize\.Y \* CompactDialogMaxMessageHeightRatio"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Dialog.ButtonFactory.cs" `
        "keeps compact confirmation button sizing isolated below scroll-safe warnings" `
        @(
            "BuildDialogButton",
            "ApplyDialogButtonLayout",
            "DialogMessageWidth\(profile\)",
            "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "LauncherComponentTheme\.DialogButtonWidth",
            "button\.Pressed \+= callback"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Dialog.Pointer.cs" `
        "keeps touch and mouse confirmation dialog pointer forwarding isolated" `
        @(
            "TryGetPressedPointerPosition",
            "InputEventScreenTouch",
            "InputEventMouseButton",
            "Pressed: true",
            "position = touch\.Position",
            "position = mouse\.Position"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Confirmation.cs" `
        "exposes contextual confirmation button label overloads" `
        @(
            "confirmText",
            "cancelText",
            "BuildConfirmationDialog"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Confirmation.cs" `
        "sizes confirmation dialogs from the current visible viewport" `
        @(
            "CurrentConfirmationProfile",
            "GetVisibleRect\(\)\.Size",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "BuildConfirmationDialog\(message,\s*CurrentConfirmationProfile\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.Startup.BranchSwitch.cs" `
        "labels branch-switch confirmation with explicit compact actions" `
        @(
            "Switch Version",
            "Keep Current"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Factory.cs" `
        "labels final cloud confirmation with explicit compact actions" `
        @(
            "Push to Cloud",
            "Cancel Push",
            "Pull from Cloud",
            "Cancel Pull"
        )
}
