using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public class NavigationPanelVE : VisualElement {
    public enum NavigationType { Automatic, Horizontal, Vertical }
    public enum OverflowBehaviour { none, loop, elevate };
    public enum EntryBehaviour { starting, lastIndex, closest }
    public enum SideNavigation { elevate, block }
    public class NavigationPanelFactory : UxmlFactory<NavigationPanelVE, NavigationPanelTraits> { }
    public class NavigationPanelTraits : UxmlTraits {
        UxmlEnumAttributeDescription<OverflowBehaviour> Overflow = new UxmlEnumAttributeDescription<OverflowBehaviour>() { name = "Overflow" };
        UxmlEnumAttributeDescription<EntryBehaviour> Entry = new UxmlEnumAttributeDescription<EntryBehaviour>() { name = "Entry" };
        UxmlEnumAttributeDescription<SideNavigation> Side = new UxmlEnumAttributeDescription<SideNavigation>() { name = "Side" };
        UxmlEnumAttributeDescription<NavigationType> Navigation = new UxmlEnumAttributeDescription<NavigationType>() { name = "Navigation", defaultValue = NavigationType.Automatic };

        UxmlBoolAttributeDescription requireAccept = new UxmlBoolAttributeDescription() { name = "RequiresAccept" };
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var target = ve.Q<NavigationPanelVE>();
            target.Overflow = Overflow.GetValueFromBag(bag, cc);
            target.Entry = Entry.GetValueFromBag(bag, cc);
            target.Side = Side.GetValueFromBag(bag, cc);
            target.RequiresAccept = requireAccept.GetValueFromBag(bag, cc);
            target.Navigation = Navigation.GetValueFromBag(bag, cc);

            target.focusable = true;
        }
    }
    public OverflowBehaviour Overflow { get; set; }
    public EntryBehaviour Entry { get; set; }
    public SideNavigation Side { get; set; }
    public NavigationType Navigation { get; private set; }

    NavigationType navType;
    public bool RequiresAccept { get; set; }

    FocusRing ring;
    bool reversedDirection;
    bool IsNavigating = false;


    public NavigationPanelVE() {
        this.RegisterCallback<NavigationMoveEvent>(Navigate);
        this.RegisterCallback<NavigationSubmitEvent>(NavigateIn);
        this.RegisterCallback<NavigationCancelEvent>(NavigateOut);

        this.RegisterCallback<FocusEvent>(FocusGained);
        this.RegisterCallback<GeometryChangedEvent>(GeometryChanged);
        this.RegisterCallback<AttachToPanelEvent>(Attached);
    }

    static NavigationMoveEvent.Direction lastNav;
    static VisualElement lastFocuable;

    private void FocusGained(FocusEvent evt) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown)
            return;
        if (evt.target != this) {
            IsNavigating = true;
            return;
        }

        IsNavigating = !RequiresAccept;

        if (IsNavigating) {
            Enter();
        }
        lastFocuable = this;
    }

    private void NavigateOut(NavigationCancelEvent evt = null) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown)
            return;

        IsNavigating = false;
        evt.PreventDefault();
        evt.StopPropagation();
        var ve = this.GetFirstAncestorOfType<NavigationPanelVE>();
        if (ve != null)
            ve.Focus();

    }
    private void NavigateIn(NavigationSubmitEvent evt) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown)
            return;

        IsNavigating = true;
        Enter();
        evt.PreventDefault();
        evt.StopPropagation();

    }
    private void Attached(AttachToPanelEvent evt) {
        ring = new FocusRing(this, Overflow, Entry, Side);
    }

    private void Navigate(NavigationMoveEvent evt) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown)
            return;
        if (!IsNavigating)
            return;

        VisualElement target = null;

        lastNav = evt.direction;

        switch (evt.direction) {
            case NavigationMoveEvent.Direction.None:
                break;
            case NavigationMoveEvent.Direction.Left:
                if (navType == NavigationType.Horizontal)
                    target = ring.Get(IsNext(false));
                else
                    SideNavigationCheck(evt);
                break;
            case NavigationMoveEvent.Direction.Right:
                if (navType == NavigationType.Horizontal)
                    target = ring.Get(IsNext(true));
                else
                    SideNavigationCheck(evt);
                break;

            case NavigationMoveEvent.Direction.Up:
                if (navType == NavigationType.Vertical)
                    target = ring.Get(IsNext(false));
                else
                    SideNavigationCheck(evt);
                break;

            case NavigationMoveEvent.Direction.Down:
                if (navType == NavigationType.Vertical)
                    target = ring.Get(IsNext(true));
                else
                    SideNavigationCheck(evt);
                break;
        }

        if (target != null) {
            evt.StopImmediatePropagation();
            evt.PreventDefault();

            Debug.Log($"{this.name} Focusing:  {target.name}");


            target.Focus();

        }

    }


    void SideNavigationCheck(NavigationMoveEvent evt) {
        if (Side == SideNavigation.block) {
            evt.PreventDefault();
            evt.StopPropagation();
        }

    }

    private void GeometryChanged(GeometryChangedEvent evt) {
        if (ring == null)
            ring = new FocusRing(this, Overflow, Entry, Side);
        else
            ring.Rebuild();

        if (Navigation == NavigationType.Automatic) {
            switch (this.resolvedStyle.flexDirection) {
                case FlexDirection.RowReverse:
                    this.navType = NavigationType.Horizontal;
                    reversedDirection = true;
                    break;
                case FlexDirection.Row:
                    this.navType = NavigationType.Horizontal;
                    break;
                case FlexDirection.ColumnReverse:
                    reversedDirection = true;
                    this.navType = NavigationType.Vertical;
                    break;
                case FlexDirection.Column:
                    this.navType = NavigationType.Vertical;
                    break;
            }
        } else {
            navType = Navigation;
        }
    }

    void Enter() {
        switch (this.Entry) {
            case EntryBehaviour.starting:
                ring.GetFirst().Focus();
                break;

            case EntryBehaviour.lastIndex:
                ring.GetLastFocused().Focus();
                break;

            case EntryBehaviour.closest:
                ring.FocusOnClosest(lastNav, lastFocuable);
                break;
        }
    }

    bool IsNext(bool nextIsPositive) {
        return nextIsPositive ^ reversedDirection;
    }
}


