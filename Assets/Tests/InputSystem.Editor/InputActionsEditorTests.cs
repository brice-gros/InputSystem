// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS && UNITY_6000_0_OR_NEWER

using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using System;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;
using UnityEditor;
using UnityEditor.PackageManager.UI;

public class InputActionsEditorTests
{
    EditorWindow m_Window;
    [SetUp]
    public void SetUp()
    {
        TestUtils.MockDialogs();
    }

    [TearDown]
    public void TearDown()
    {
        m_Window?.Close();
        AssetDatabaseUtils.Restore();
        TestUtils.RestoreDialogs();
    }

    void Click(VisualElement ve)
    {
        Event evtd = new Event();
        evtd.type = EventType.MouseDown;
        evtd.mousePosition = ve.worldBound.center;
        evtd.clickCount = 1;
        using var pde = PointerDownEvent.GetPooled(evtd);
        ve.SendEvent(pde);

        Event evtu = new Event();
        evtu.type = EventType.MouseUp;
        evtu.mousePosition = ve.worldBound.center;
        evtu.clickCount = 1;
        using var pue = PointerUpEvent.GetPooled(evtu);
        ve.SendEvent(pue);
    }

    void SendText(VisualElement ve, string text, bool sendReturn = true)
    {
        foreach (var character in text)
        {
            var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = character };
            using var kde = KeyDownEvent.GetPooled(evtd);
            ve.SendEvent(kde);

            var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.None, character = character };
            using var kue = KeyUpEvent.GetPooled(evtu);
            ve.SendEvent(kue);
        }
        if (sendReturn)
        {
            SendReturn(ve);
        }
    }

    void SendReturn(VisualElement ve)
    {
        var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.Return };
        using var kde = KeyDownEvent.GetPooled(evtd);
        var evtd2 = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = '\n' };
        using var kde2 = KeyDownEvent.GetPooled(evtd2);
        ve.SendEvent(kde);
        ve.SendEvent(kde2);

        var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.Return };
        using var kue = KeyUpEvent.GetPooled(evtu);
        ve.SendEvent(kue);
    }

    void SendDeleteCommand(VisualElement ve)
    {
        var evt = new Event() { type = EventType.ExecuteCommand, commandName = "Delete" };
        using var ce = ExecuteCommandEvent.GetPooled(evt);
        ve.SendEvent(ce);
    }

    IEnumerator WaitForFocusToChange(VisualElement ve, double timeoutSecs = 5.0)
    {
        var focusController = ve.focusController;
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            yield return null;
            if (focusController.focusedElement != ve && focusController.focusedElement != null) break;
            Debug.Log(focusController.focusedElement);
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(focusController.focusedElement, Is.Not.EqualTo(ve).And.Not.Null);
        Debug.Log(focusController.focusedElement);
    }

    IEnumerator WaitForTextFieldFocus(VisualElement ve, double timeoutSecs = 5.0)
    {
        var focusController = ve.focusController;
        var veText = ve.Q<TextField>();
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            yield return null;
            if (focusController.focusedElement == veText) break;
            //Debug.Log(focusController.focusedElement);
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(focusController.focusedElement, Is.EqualTo(veText));
        //Debug.Log(focusController.focusedElement);
    }

    IEnumerator WaitForTextFieldFocus2(VisualElement ve, double timeoutSecs = 5.0)
    {
        var focusController = ve.focusController;
        //var veText = ve.Q<TextField>();
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            //focusController.focusedElement.
            yield return null;
            if (focusController.focusedElement is TextField) break;
            Debug.Log(focusController.focusedElement);
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(focusController.focusedElement is TextField, Is.True);
        //Debug.Log(focusController.focusedElement);
    }

    IEnumerator WaitForFocus(VisualElement ve, double timeoutSecs = 5.0)
    {
        var focusController = ve.focusController;
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            yield return null;
            if (focusController.focusedElement == ve) break;
            Debug.Log(focusController.focusedElement);
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(focusController.focusedElement, Is.EqualTo(ve));
    }

    IEnumerator WaitForSecs(double timeoutSecs)
    {
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            yield return null;
        }
        while (endTime > EditorApplication.timeSinceStartup);
    }

    IEnumerator WaitForNotDirty(VisualElement ve, double timeoutSecs = 5.0)
    {
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            if (ve.panel.isDirty == false) break;
            yield return null;
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(ve.panel.isDirty, Is.False);
    }

    [UnityTest]
    public IEnumerator CreateActionMap()
    {
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        var editor = InputActionsEditorWindow.OpenEditor(asset);
        m_Window = editor;

        yield return WaitForNotDirty(editor.rootVisualElement);
        var button = editor.rootVisualElement.Q<UnityEngine.UIElements.Button>("add-new-action-map-button");
        Assume.That(button, Is.Not.Null);
        Click(button);

        yield return WaitForNotDirty(editor.rootVisualElement);

        // Wait for the focus to move out the button(should be on the new action map)
        yield return WaitForFocusToChange(button);

        // Rename the new action map
        SendText(editor.rootVisualElement, "New Name");

        yield return null;

        // Check on the UI side
        var actionMapsContainer = editor.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(1));
        Assert.That(actionMapItem[0].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(editor.currentAssetInEdition.actionMaps.Count, Is.EqualTo(1));
        Assert.That(editor.currentAssetInEdition.actionMaps[0].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    public IEnumerator RenameActionMap()
    {
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        asset.AddActionMap("First Name");
        asset.AddActionMap("Old Name");
        asset.AddActionMap("Third Name");
        var editor = InputActionsEditorWindow.OpenEditor(asset);
        m_Window = editor;

        yield return WaitForNotDirty(editor.rootVisualElement);

        var actionMapsContainer = editor.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem, Is.Not.Null);
        Assume.That(actionMapItem.Count, Is.EqualTo(3));
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Old Name"));
        // for the selection the prevent some instabilities with current ui intregration
        editor.rootVisualElement.Q<ListView>("action-maps-list-view").selectedIndex = 1;

        yield return WaitForNotDirty(actionMapsContainer);
        Click(actionMapItem[1]);

        yield return WaitForFocus(editor.rootVisualElement.Q("action-maps-list-view"));
        // refetch the action map item since the ui may have refreshed.
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();

        Click(actionMapItem[1]);

        // wait for 1 second to let the time of the async focus event to be proceeded
        yield return WaitForSecs(1);

        // Rename the new action map
        SendText(editor.rootVisualElement, "New Name");

        //yield return null;
        yield return WaitForNotDirty(editor.rootVisualElement);

        // Check on the UI side
        actionMapsContainer = editor.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(3));
        Assert.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(editor.currentAssetInEdition.actionMaps.Count, Is.EqualTo(3));
        Assert.That(editor.currentAssetInEdition.actionMaps[1].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    public IEnumerator DeleteActionMap()
    {
        var asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        asset.AddActionMap("First Name");
        asset.AddActionMap("To Delete Name");
        asset.AddActionMap("Third Name");
        var editor = InputActionsEditorWindow.OpenEditor(asset);
        m_Window = editor;

        yield return WaitForNotDirty(editor.rootVisualElement);

        var actionMapsContainer = editor.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem, Is.Not.Null);
        Assume.That(actionMapItem.Count, Is.EqualTo(3));
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("To Delete Name"));

        yield return WaitForNotDirty(actionMapsContainer);
        Click(actionMapItem[1]);

        yield return WaitForFocus(editor.rootVisualElement.Q("action-maps-list-view"));
        // refetch the action map item since the ui may have refreshed.
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();

        SendDeleteCommand(editor.rootVisualElement);
        //Click(actionMapItem[1]);

        //// wait for 1 second to let the time of the async focus event to be proceeded
        yield return WaitForSecs(1);

        //// Rename the new action map
        //SendText(editor.rootVisualElement, "New Name");

        yield return null;

        // Check on the UI side
        actionMapsContainer = editor.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(2));
        Assert.That(actionMapItem[0].Q<Label>("name").text, Is.EqualTo("First Name"));
        Assert.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Third Name"));

        // Check on the asset side
        Assert.That(editor.currentAssetInEdition.actionMaps.Count, Is.EqualTo(2));
        Assert.That(editor.currentAssetInEdition.actionMaps[0].name, Is.EqualTo("First Name"));
        Assert.That(editor.currentAssetInEdition.actionMaps[1].name, Is.EqualTo("Third Name"));
    }
}
#endif
