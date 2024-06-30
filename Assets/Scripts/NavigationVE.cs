using System;
using UnityEngine;
using UnityEngine.UIElements;

public class NavigationVE : VisualElement {

    public const string SELECTED_STATE = "navigation-panel__active";

    public enum NavigationType { Horizontal = 2, Vertical = 4 }
    public enum OverflowBehaviour { none, loop, elevate };
    public enum EntryBehaviour { closest, starting, lastItem, lastIndex }
    public enum SideNavigation { elevate, block }

    public class NavigationFactory : UxmlFactory<NavigationVE, NavigationTraits> { }
    public class NavigationTraits : UxmlTraits {
        UxmlIntAttributeDescription startingFocus = new UxmlIntAttributeDescription() { name = "StartingElement" };

        UxmlEnumAttributeDescription<OverflowBehaviour> overflow = new UxmlEnumAttributeDescription<OverflowBehaviour>() { name = "Overflow" };
        UxmlEnumAttributeDescription<EntryBehaviour> Entry = new UxmlEnumAttributeDescription<EntryBehaviour>() { name = "EntryBehaviour" };
        UxmlEnumAttributeDescription<SideNavigation> Side = new UxmlEnumAttributeDescription<SideNavigation>() { name = "SideNavigation" };

        UxmlBoolAttributeDescription requireAccept = new UxmlBoolAttributeDescription() { name = "RequireAccept" };
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var target = ve.Q<NavigationVE>();
            target.StartingElement = startingFocus.GetValueFromBag(bag, cc);
            target.Overflow = overflow.GetValueFromBag(bag, cc);
            target.currentIndex = target.StartingElement;
            target.Entry = Entry.GetValueFromBag(bag, cc);
            target.RequireAccept = requireAccept.GetValueFromBag(bag, cc);
            target.Side = Side.GetValueFromBag(bag, cc);
            target.focusable = true;
        }
    }

    public int StartingElement { get; set; }
    public NavigationType Navigation { get; set; }
    public OverflowBehaviour Overflow { get; set; }
    public EntryBehaviour Entry { get; set; }
    public SideNavigation Side { get; set; }

    public bool RequireAccept { get; set; }

    VisualElement selectedItem;
    int currentIndex;

    bool IsSelected { get { return _isSelected; } set { _isSelected = value; this.EnableInClassList(SELECTED_STATE, value); } }
    bool _isSelected;

    public NavigationVE() {
        this.RegisterCallback<NavigationMoveEvent>(Navigate);
        this.RegisterCallback<NavigationSubmitEvent>(NavigateIn);
        this.RegisterCallback<NavigationCancelEvent>(NavigateOut);

        this.RegisterCallback<FocusEvent>(FocusGained);
        this.RegisterCallback<FocusOutEvent>(FocusLost);
        this.RegisterCallback<FocusInEvent>(FocusIn);
        this.RegisterCallback<GeometryChangedEvent>(StyleResolved);
        this.focusable = true;
        currentIndex = StartingElement;
    }

    private void StyleResolved(GeometryChangedEvent evt) {
        switch (this.resolvedStyle.flexDirection) {
            case FlexDirection.RowReverse:
            case FlexDirection.Row:
                this.Navigation = NavigationType.Horizontal;
                break;
            case FlexDirection.ColumnReverse:
            case FlexDirection.Column:
                this.Navigation = NavigationType.Vertical;
                break;
        }
    }

    private void FocusIn(FocusInEvent evt) {
        if (evt.propagationPhase != PropagationPhase.BubbleUp) return;
        IsSelected = true;
        var ve = this.GetFirstAncestorOfType<NavigationVE>();
        if (ve != null) {
            ve.currentIndex = ve.hierarchy.IndexOf(this);
        }
    }

    private void FocusLost(FocusOutEvent evt) {
        if (evt.target != this) return;
    }

    private void NavigateOut(NavigationCancelEvent evt = null) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown) return;
        if (!IsSelected) {
            return;
        }

        evt.PreventDefault();
        evt.StopPropagation();
        IsSelected = false;
        var ve = this.GetFirstAncestorOfType<NavigationVE>();
        if (ve != null)
            ve.Focus();
    }

    private void NavigateIn(NavigationSubmitEvent evt) {
        if (evt.propagationPhase == PropagationPhase.TrickleDown) return;

        IsSelected = true;

        Select();

        evt.PreventDefault();
        evt.StopPropagation();
    }

    private void FocusGained(FocusEvent evt) {

        if (evt.propagationPhase == PropagationPhase.TrickleDown) return;
        if (evt.target != this) {
            IsSelected = true;
            return;
        }

        if (!RequireAccept) {
            IsSelected = true;
            Select();
        }
        if (IsSelected) {
            Select();
        }
    }

    private void Navigate(NavigationMoveEvent evt) {
        if (!IsSelected) return;
        if (evt.propagationPhase == PropagationPhase.TrickleDown) return;

        int target = -1;
        bool overflown = false;
        switch (evt.direction) {
            case NavigationMoveEvent.Direction.Up:
                if (Navigation.HasFlag(NavigationType.Vertical)) {
                    target = NavigateBackward(out overflown);
                } else SideNavigationCheck(evt);
                break;
            case NavigationMoveEvent.Direction.Down:
                if (Navigation.HasFlag(NavigationType.Vertical)) {
                    target = NavigateForward(out overflown);
                } else SideNavigationCheck(evt);
                break;
            case NavigationMoveEvent.Direction.Left:
                if (Navigation.HasFlag(NavigationType.Horizontal)) {
                    target = NavigateBackward(out overflown);
                } else SideNavigationCheck(evt);
                break;
            case NavigationMoveEvent.Direction.Right:
                if (Navigation.HasFlag(NavigationType.Horizontal)) {
                    target = NavigateForward(out overflown);
                } else SideNavigationCheck(evt);
                break;
        }
        if (target < 0) {
            if (RequireAccept) {
                evt.PreventDefault();
                evt.StopPropagation();
            }
            return;
        }

        if (overflown) {
            switch (Overflow) {
                case OverflowBehaviour.loop: {
                        evt.PreventDefault();
                        evt.StopPropagation();
                        this.hierarchy.ElementAt(target).Focus();
                        currentIndex = target;
                        break;
                    }
                case OverflowBehaviour.none: {
                        evt.PreventDefault();
                        evt.StopPropagation();
                        break;
                    }
                case OverflowBehaviour.elevate: {
                        IsSelected = false;
                        break;
                    }
            }
        } else {
            evt.PreventDefault();
            evt.StopPropagation();
            this.hierarchy.ElementAt(target).Focus();
            currentIndex = target;
        }

    }
    void SideNavigationCheck(NavigationMoveEvent evt) {
        if (Side == SideNavigation.block) {
            evt.PreventDefault();
            evt.StopPropagation();
        }
    }

    int NavigateForward(out bool overflown) {
        for (int i = 1; i < this.hierarchy.childCount; i++) {
            int target = (currentIndex + i) % this.hierarchy.childCount;
            var ve = this.hierarchy.ElementAt(target);
            if (ve.focusable) {
                overflown = currentIndex > target;
                return target;
            }
        }

        overflown = false;
        //fallback
        return currentIndex;
    }

    int NavigateBackward(out bool overflown) {
        for (int i = 1; i < this.hierarchy.childCount; i++) {
            int target = (currentIndex + this.hierarchy.childCount - i) % this.hierarchy.childCount;
            var ve = this.hierarchy.ElementAt(target);
            if (ve.focusable) {
                overflown = currentIndex < target;
                return target;
            }
        }


        overflown = false;
        //fallback
        return currentIndex;
    }

    void Select() {
        switch (Entry) {
            
            case EntryBehaviour.lastIndex:
                break;
            case EntryBehaviour.lastItem:
                var itemIndex = this.IndexOf(selectedItem);
                if (itemIndex > 0)
                    currentIndex = itemIndex;
                break;
            case EntryBehaviour.starting:
                currentIndex = StartingElement;
                break;

        }

        currentIndex = Mathf.Clamp(currentIndex, 0, this.childCount);

        if (!this.hierarchy.ElementAt(currentIndex).focusable)
            currentIndex = NavigateForward(out bool overflown);
        this.hierarchy.ElementAt(currentIndex).Focus();
    }
}

