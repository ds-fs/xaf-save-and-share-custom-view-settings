using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using ViewSettings.Module.BusinessObjects;
using ViewSettingsSolution.Module.BusinessObjects;

namespace ViewSettingsSolution.Module.Controllers;

public class ViewVariantsController : ViewController
{

    private SingleChoiceAction ViewVariantsCommandsAction;

    private PopupWindowShowAction CreateViewVariantAction;
    private SingleChoiceAction SelectViewVariantAction;
    private string defaultUserSettings;
    private string currentUserId;
    private bool isLayoutProcessed = false;
    private bool isDefaultViewSelected = false;
    private ChoiceActionItem lastSelectedItem = null;
    private ChoiceActionItem createViewVariant;
    private ChoiceActionItem updateViewVariant;
    private ChoiceActionItem editViewVariantItem;
    private ChoiceActionItem deleteViewVariant;
    private ChoiceActionItem addWithCurrentSelectionViewVariantItem;
    private ChoiceActionItem editAddItemsViewVariantItem;
    private ChoiceActionItem editExecludeItemsViewVariantItem;



    public ViewVariantsController()
    {
        TargetViewNesting = Nesting.Root;


        ViewVariantsCommandsAction = new SingleChoiceAction(this, $"{GetType().Name}.{nameof(ViewVariantsCommandsAction)}", PredefinedCategory.View)
        { Caption = "Persönliche Ansichten", ImageName = "Navigation_Item_ViewVariant", ItemType = SingleChoiceActionItemType.ItemIsOperation, ShowItemsOnClick = true }; // , ConfirmationMessage = "Möchten Sie diese Aktion ausführen?" };
        ViewVariantsCommandsAction.Execute += ViewVariantsCommandsAction_Execute;


        createViewVariant = new ChoiceActionItem("Als neue Ansicht speichern", null) { ImageName = "Action_New" };
        updateViewVariant = new ChoiceActionItem("Ansicht speichern", null) { ImageName = "Action_Save", BeginGroup = true };
        editViewVariantItem = new ChoiceActionItem("Ansicht umbenennen", null) { ImageName = "Action_Edit" };
        deleteViewVariant = new ChoiceActionItem("Ansicht löschen", null) { ImageName = "Action_Delete", BeginGroup = true };


        addWithCurrentSelectionViewVariantItem = new ChoiceActionItem("Neue Ansicht aus aktueller Markierung", null) { BeginGroup = true };
        editAddItemsViewVariantItem = new ChoiceActionItem("Markierung in Ansicht einschließen", null);
        editExecludeItemsViewVariantItem = new ChoiceActionItem("Markierung aus aktueller Ansicht ausschließen", null);

        ViewVariantsCommandsAction.Items.Add(createViewVariant);
        ViewVariantsCommandsAction.Items.Add(updateViewVariant);
        ViewVariantsCommandsAction.Items.Add(editViewVariantItem);
        ViewVariantsCommandsAction.Items.Add(deleteViewVariant);

        ViewVariantsCommandsAction.Items.Add(addWithCurrentSelectionViewVariantItem);
        ViewVariantsCommandsAction.Items.Add(editAddItemsViewVariantItem);
        ViewVariantsCommandsAction.Items.Add(editExecludeItemsViewVariantItem);

        SelectViewVariantAction = new SingleChoiceAction(this, $"{GetType().Name}.{nameof(SelectViewVariantAction)}", PredefinedCategory.View) { PaintStyle = ActionItemPaintStyle.Image };
        SelectViewVariantAction.Execute += SelectViewVariantAction_Execute;

        CreateViewVariantAction = new PopupWindowShowAction(this, $"{GetType().Name}.{nameof(CreateViewVariantAction)}", nameof(ViewVariantsController)) { ImageName = "Action_New" };
        CreateViewVariantAction.Execute += CreateViewVariantAction_Execute;

        Actions.Add(ViewVariantsCommandsAction);
        Actions.Add(SelectViewVariantAction);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        currentUserId = SecuritySystem.CurrentUserId?.ToString();
        View.ModelSaving += View_ModelSaving;
        if (!isLayoutProcessed)
        {
            UpdateDefaultSettings(View.Model);
            isDefaultViewSelected = true;
            UpdateActions(null);
        }
    }
    protected override void OnDeactivated()
    {
        View?.ModelSaving -= View_ModelSaving;
        if (!isLayoutProcessed)
        {
            ViewVariantsCommandsAction?.Execute -= ViewVariantsCommandsAction_Execute;
            SelectViewVariantAction?.Execute -= SelectViewVariantAction_Execute;
            CreateViewVariantAction?.Execute -= CreateViewVariantAction_Execute;
        }
        base.OnDeactivated();
    }

    protected bool ShowDeleteConfirmationMessage(string message = "Möchten Sie diese Ansicht wirklich löschen?")
    {
        return true;
    }

    private void ViewVariantsCommandsAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
    {
        switch (e.SelectedChoiceActionItem)
        {
            case var _ when e.SelectedChoiceActionItem == createViewVariant:
                ShowPopupWindow(e);
                break;
            case var _ when e.SelectedChoiceActionItem == updateViewVariant:
                UpdateCurrentView();
                break;
            case var _ when e.SelectedChoiceActionItem == editViewVariantItem:
                ShowPopupWindow(e);
                break;
            case var _ when e.SelectedChoiceActionItem == deleteViewVariant:
                if (ShowDeleteConfirmationMessage())
                    DeleteViewVariant();
                break;
            case var _ when e.SelectedChoiceActionItem == addWithCurrentSelectionViewVariantItem:
                // Neue Ansicht aus aktueller Markierung erstellen
                break;
            case var _ when e.SelectedChoiceActionItem == editAddItemsViewVariantItem:
                // Markierung in Ansicht einschließen
                break;
            case var _ when e.SelectedChoiceActionItem == editExecludeItemsViewVariantItem:
                // Markierung aus aktueller Ansicht ausschließen
                break;
        }
    }
    private void ShowPopupWindow(SingleChoiceActionExecuteEventArgs e)
    {
        var os = Application.CreateObjectSpace<ViewSettingsStore>();
        var param = CreateViewVariantAction.GetPopupWindowParams();
        if (e.SelectedChoiceActionItem == editViewVariantItem)
        {
            var store = os.GetObject(SelectViewVariantAction.SelectedItem?.Data as ViewSettingsStore);
            param.View = Application.CreateDetailView(os, store);
        }
        else
            param.View = Application.CreateDetailView(os, os.CreateObject<ViewSettingsStore>());
        e.ShowViewParameters.CreatedView = param.View;
        e.ShowViewParameters.Controllers.Add(param.DialogController);
        e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
    }
    private void CreateViewVariantAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var objectSpace = e.Application.CreateObjectSpace<ViewSettingsStore>();
        e.View = e.Application.CreateDetailView(objectSpace, objectSpace.CreateObject<ViewSettingsStore>());
    }
    private void CreateViewVariantAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var store = (ViewSettingsStore)e.PopupWindowViewCurrentObject;
        if (store.Session.IsNewObject(store))
        {
            store.OwnerId = currentUserId;
            store.ViewId = View.Id;
            store.TargetObjectType = this.TargetObjectType ?? View.ObjectTypeInfo.Type;
        }
        SaveViewVariantToXML(store);
        isDefaultViewSelected = false;
        UpdateActions(store.Name);
    }
    private void DeleteViewVariant()
    {
        var store = SelectViewVariantAction.SelectedItem.Data as ViewSettingsStore;
        if (store != null && TryLoadViewVariantFromXML(defaultUserSettings))
        {
            using var os = Application.CreateObjectSpace<ViewSettingsStore>();
            var storeToDelete = os.GetObject(store);
            os.Delete(storeToDelete);
            os.CommitChanges();
            isDefaultViewSelected = true;
            UpdateActions(null);
        }
    }
    private void SelectViewVariantAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
    {
        var isVariantChanged = false;
        var currentItem = SelectViewVariantAction.SelectedItem;
        if (currentItem.Data != null)
        {
            isVariantChanged = TryLoadViewVariantFromXML(((ViewSettingsStore)currentItem.Data).Xml);
        }
        else
        {
            if (TryLoadViewVariantFromXML(defaultUserSettings))
            {
                isDefaultViewSelected = true;
                isVariantChanged = true;
            }
        }
        SelectViewVariantAction.SelectedItem = isVariantChanged ? currentItem : lastSelectedItem;
        UpdateActionsActive();
    }
    private void UpdateCurrentView()
    {
        SaveViewVariantToXML(SelectViewVariantAction.SelectedItem?.Data as ViewSettingsStore);
        if (SelectViewVariantAction.SelectedItem.Data as ViewSettingsStore == null) // Standard Ansicht speichern
        {
            UpdateDefaultSettings(View.Model);
        }
    }
    private void View_ModelSaving(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!isLayoutProcessed && !isDefaultViewSelected)
        {
            SetDifferences(defaultUserSettings, View.Model);
            e.Cancel = true;
        }
    }
    private void SetDifferences(string xml, IModelView model)
    {
        var differences = new Dictionary<string, string>();
        differences.Add("", xml);
        UserDifferencesHelper.SetUserDifferences(model, differences);
    }
    private bool TryLoadViewVariantFromXML(string xml)
    {
        var result = false;
        var savedView = Frame.View;
        isLayoutProcessed = true;
        foreach (ISupportUpdate controller in Frame.Controllers)
        {
            controller.BeginUpdate();
        }
        try
        {
            if (Frame.SetView(null, true, null, false))
            {
                if (isDefaultViewSelected)
                {
                    UpdateDefaultSettings(savedView.Model);
                    isDefaultViewSelected = false;
                }
                SetDifferences(xml, savedView.Model);
                savedView.LoadModel(false);
                Frame.SetView(savedView);
                result = true;
            }
        }
        finally
        {
            foreach (ISupportUpdate controller in Frame.Controllers)
            {
                controller.EndUpdate();
            }
        }
        isLayoutProcessed = false;
        return result;
    }
    private void SaveViewVariantToXML(ViewSettingsStore store)
    {
        isLayoutProcessed = true;
        View.SaveModel();
        isLayoutProcessed = false;
        if (store != null)
        {
            store.Xml = UserDifferencesHelper.GetUserDifferences(View.Model)[""];
            ((UnitOfWork)store.Session).CommitChanges();
        }
    }
    private void UpdateDefaultSettings(IModelView model) => defaultUserSettings = UserDifferencesHelper.GetUserDifferences(model)[""];
    private void UpdateActions(string itemToSelectCaption)
    {
        SelectViewVariantAction.Items.Clear();
        lastSelectedItem = null;
        var criteria = CriteriaOperator.FromLambda<ViewSettingsStore>(x => x.TargetObjectType == (this.TargetObjectType ?? this.View.ObjectTypeInfo.Type) && (x.IsShared || x.OwnerId == null || x.OwnerId == currentUserId));
        var objectSpace = Application.CreateObjectSpace<ViewSettingsStore>();
        foreach (var item in objectSpace.GetObjects<ViewSettingsStore>(criteria).OrderBy(x => x.Name))
        {
            SelectViewVariantAction.Items.Add(new ChoiceActionItem(item.Name, item));
        }
        if (SelectViewVariantAction.Items.Count > 0)
        {
            var defaultItem = new ChoiceActionItem("Standard", null);
            SelectViewVariantAction.Items.Insert(0, defaultItem);
            var itemToSelect = SelectViewVariantAction.Items.FindItemByID(itemToSelectCaption);
            SelectViewVariantAction.SelectedItem = (itemToSelect != null) ? itemToSelect : defaultItem;
            lastSelectedItem = SelectViewVariantAction.SelectedItem;
        }
        UpdateActionsActive();
    }
    private void UpdateActionsActive()
    {
        var store = SelectViewVariantAction.SelectedItem?.Data as ViewSettingsStore;
        var isActive = SelectViewVariantAction.Items.Count > 0 && store != null && (store.OwnerId == currentUserId || (SecuritySystem.CurrentUser != null && (SecuritySystem.CurrentUser as ApplicationUser).IsUserInRole("Administrators")));
        editViewVariantItem.Enabled["HasVariants"] = isActive;
        deleteViewVariant.Enabled["HasVariants"] = isActive;
    }
}
