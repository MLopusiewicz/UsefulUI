using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static NavigationPanelVE;


public class FocusRing {
    VisualElement root;
    List<VisualElement> ring = new();
    int currentIndex = 0;
    public OverflowBehaviour Overflow { get; }
    public EntryBehaviour Entry { get; }
    public SideNavigation Side { get; }

    Func<VisualElement, bool> yield;
    Func<VisualElement, bool> accept;

    public FocusRing(VisualElement root, OverflowBehaviour overflow, EntryBehaviour entry, SideNavigation side) {
        this.root = root;
        Overflow = overflow;
        Entry = entry;
        Side = side;
        yield = (ve) => {
            if (ve is NavigationPanelVE)
                return true;
            if (ve.delegatesFocus)
                return true;

            return false;
        };
        accept = (ve) => { return ve.canGrabFocus && ve.focusable; };
        Rebuild();
    }

    public void Rebuild() {
        ring.Clear();
        Query(ref ring, root);
        ring.Sort(IndexSort);

    }

    public void Query(ref List<VisualElement> list, VisualElement target) {
        foreach (var a in target.Children()) {
            if (accept(a))
                list.Add(a);
            if (yield(a))
                continue;
            Query(ref list, a);
        }
    }

    public VisualElement GetNext() {
        currentIndex++;
        if (currentIndex == ring.Count) {
            switch (Overflow) {
                case OverflowBehaviour.none:
                    currentIndex = ring.Count - 1;
                    break;
                case OverflowBehaviour.loop:
                    currentIndex = 0;
                    break;
                case OverflowBehaviour.elevate:
                    currentIndex = ring.Count - 1;
                    return null;
            }
        }
        return ring[currentIndex];
    }
    public VisualElement GetPrevious() {
        currentIndex--;
        if (currentIndex < 0) {
            switch (Overflow) {
                case OverflowBehaviour.none:
                    currentIndex = 0;
                    break;
                case OverflowBehaviour.loop:
                    currentIndex = ring.Count - 1;
                    break;
                case OverflowBehaviour.elevate:
                    currentIndex = 0;
                    return null;
            }
        }
        return ring[currentIndex];
    }

    public VisualElement Get(bool positiveDirection) {
        if (positiveDirection)
            return GetNext();
        else
            return GetPrevious();
    }

    public VisualElement GetNext(VisualElement target) {
        currentIndex = ring.IndexOf(target);
        return GetNext();
    }
    public VisualElement GetPrevious(VisualElement target) {
        currentIndex = ring.IndexOf(target);
        return GetPrevious();
    }
    public VisualElement GetFirst() {
        currentIndex = 0;
        return ring[0];
    }
    public VisualElement GetLastFocused() {
        return ring[currentIndex];
    }

    public int IndexSort(VisualElement a, VisualElement b) {
        return b.tabIndex - a.tabIndex;
    }

    public void FocusOnClosest(NavigationMoveEvent.Direction lastNav, VisualElement lastFocus) {
        Vector3 dir = Vector3.zero;
        switch (lastNav) {
            case NavigationMoveEvent.Direction.Left:
                dir = Vector3.left;
                break;
            case NavigationMoveEvent.Direction.Right:
                dir = Vector3.right;
                break;
            case NavigationMoveEvent.Direction.Up:
                dir = Vector3.up;
                break;
            case NavigationMoveEvent.Direction.Down:
                dir = Vector3.down;
                break;
        }
        float lastDistance = float.MaxValue;
        VisualElement e = null;

        foreach (var a in ring) {

            var p = a.worldBound.center - lastFocus.worldBound.center;
            var f = Vector3.Project(p, dir).magnitude;
            if (f > 0 && p.magnitude < lastDistance) {
                e = a;
                lastDistance = p.magnitude;
            }
        }

        if (e != null) {
            e.Focus();
            currentIndex = ring.IndexOf(e);
        }
    }
}



