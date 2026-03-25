using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using ViewSettingsSolution.Module.BusinessObjects;

namespace ViewSettingsSolution.Module.Controllers;

public class ViewVariantsController : ViewController
{
    private SimpleAction DeleteViewVariantAction;
    private PopupWindowShowAction CreateViewVariantAction;
    private SingleChoiceAction SelectViewVariantAction;
    private SimpleAction UpdateCurrentViewVariantAction;
    private SimpleAction UpdateDefaultSettingsWithSelectedVariantAction;
    private string defaultUserSettings;
    private string currentUserId;
    private bool isLayoutProcessed = false;
    private bool isDefaultViewSelected = false;
    private ChoiceActionItem lastSelectedItem = null;
   


    public ViewVariantsController()
    {
        this.TargetViewNesting = Nesting.Root;

        this.SelectViewVariantAction = new SingleChoiceAction(this, "Ansicht auswählen", PredefinedCategory.View) { PaintStyle = ActionItemPaintStyle.Image };
        this.SelectViewVariantAction.Execute += new SingleChoiceActionExecuteEventHandler(this.SelectViewVariantAction_Execute);

        this.CreateViewVariantAction = new PopupWindowShowAction(this, "Als neue Ansicht speichern", PredefinedCategory.View) { ImageName = "Action_New" };
        this.CreateViewVariantAction.CustomizePopupWindowParams += new CustomizePopupWindowParamsEventHandler(this.CreateViewVariantAction_CustomizePopupWindowParams);
        this.CreateViewVariantAction.Execute += new PopupWindowShowActionExecuteEventHandler(this.CreateViewVariantAction_Execute);


        this.UpdateCurrentViewVariantAction = new SimpleAction(this, "Aktuelle Ansicht aktualisieren", PredefinedCategory.View) { ImageName = "Update" };
        this.UpdateCurrentViewVariantAction.Execute += new SimpleActionExecuteEventHandler(this.UpdateCurrentViewVariantAction_Execute);

        this.UpdateDefaultSettingsWithSelectedVariantAction = new SimpleAction(this, "Standardansicht mit ausgewählter Ansicht aktualisieren", PredefinedCategory.View) { ImageName = "Action_Copy" };
        UpdateDefaultSettingsWithSelectedVariantAction.Execute += UpdateDefaultSettingsWithSelectedVariantAction_Execute;

        this.DeleteViewVariantAction = new SimpleAction(this, "Ansicht löschen", PredefinedCategory.View) { ImageName = "Action_Delete" };
        this.DeleteViewVariantAction.Execute += new SimpleActionExecuteEventHandler(this.DeleteViewVariantAction_Execute);


        this.Actions.Add(this.SelectViewVariantAction);
        this.Actions.Add(this.CreateViewVariantAction);
        this.Actions.Add(this.UpdateCurrentViewVariantAction);
        this.Actions.Add(this.UpdateDefaultSettingsWithSelectedVariantAction);
        this.Actions.Add(this.DeleteViewVariantAction);
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
        base.OnDeactivated();
    }


    private void CreateViewVariantAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var objectSpace = e.Application.CreateObjectSpace<ViewSettingsStore>();
        e.View = e.Application.CreateDetailView(objectSpace, objectSpace.CreateObject<ViewSettingsStore>());
    }
    private void CreateViewVariantAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var store = (ViewSettingsStore)e.PopupWindowViewCurrentObject;
        store.OwnerId = currentUserId;
        store.ViewId = View.Id;
        store.TargetObjectType = this.TargetObjectType ?? View.ObjectTypeInfo.Type;
        SaveViewVariantToXML(store);
        isDefaultViewSelected = false;
        UpdateActions(store.Name);
    }
    private void DeleteViewVariantAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var currentLayoutItem = SelectViewVariantAction.SelectedItem.Data as ViewSettingsStore;
        if (TryLoadViewVariantFromXML(defaultUserSettings))
        {
            using var os = Application.CreateObjectSpace<ViewSettingsStore>();
            var currentLayoutItemInOs = os.GetObject(currentLayoutItem);
            os.Delete(currentLayoutItemInOs);
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
    private void UpdateCurrentViewVariantAction_Execute(object sender, SimpleActionExecuteEventArgs e) => SaveViewVariantToXML(SelectViewVariantAction.SelectedItem.Data as ViewSettingsStore);
    private void UpdateDefaultSettingsWithSelectedVariantAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        SaveViewVariantToXML(SelectViewVariantAction.SelectedItem.Data as ViewSettingsStore);
        UpdateDefaultSettings(View.Model);
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
        store.Xml = UserDifferencesHelper.GetUserDifferences(View.Model)[""];
        ((UnitOfWork)store.Session).CommitChanges();
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
            SelectViewVariantAction.Items.Insert(0,defaultItem);
            var itemToSelect = SelectViewVariantAction.Items.FindItemByID(itemToSelectCaption);
            SelectViewVariantAction.SelectedItem = (itemToSelect != null) ? itemToSelect : defaultItem;
            lastSelectedItem = SelectViewVariantAction.SelectedItem;
        }
        UpdateActionsActive();
    }
    private void UpdateActionsActive()
    {
        var isActive = SelectViewVariantAction.Items.Count > 0 && SelectViewVariantAction.SelectedItem.Data != null;
        DeleteViewVariantAction.Active["HasVariants"] = isActive;
        UpdateCurrentViewVariantAction.Active["HasVariants"] = isActive;
        UpdateDefaultSettingsWithSelectedVariantAction.Active["HasVariants"] = isActive;
    }
}
