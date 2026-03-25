using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace ViewSettingsSolution.Module.BusinessObjects;

public class ViewSettingsStore : BaseObject
{
    Type _targetObjectType;
    private string _xml;
    private string _name;
    private string _ownerId;
    private string _viewId;
    private bool _isShared;

    public ViewSettingsStore(Session session) : base(session)
    {
    }

    [VisibleInDashboards(false), VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false), VisibleInReports(false)]
    [Size(SizeAttribute.Unlimited)]
    public string Xml
    {
        get => _xml;
        set => SetPropertyValue(nameof(Xml), ref _xml, value);
    }
    public string Name
    {
        get => _name;
        set => SetPropertyValue(nameof(Name), ref _name, value);
    }
    [VisibleInDashboards(false), VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false), VisibleInReports(false)]
    public string OwnerId
    {
        get => _ownerId;
        set => SetPropertyValue(nameof(OwnerId), ref _ownerId, value);
    }
    public bool IsShared
    {
        get => _isShared;
        set => SetPropertyValue(nameof(IsShared), ref _isShared, value);
    }
    [VisibleInDashboards(false), VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false), VisibleInReports(false)]
    public string ViewId
    {
        get => _viewId;
        set => SetPropertyValue<string>(nameof(ViewId), ref _viewId, value);
    }
    [ValueConverter(typeof(TypeToStringConverter))]
    [VisibleInDashboards(false), VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false), VisibleInReports(false)]
    public Type TargetObjectType
    {
        get => _targetObjectType;
        set => SetPropertyValue(nameof(TargetObjectType), ref _targetObjectType, value);
    }
}
