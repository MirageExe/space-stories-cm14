using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Telephone;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Telephone;

public sealed class RMCTelephoneBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly List<string> TabOrder = new() { "MP Dept.", "Almayer", "Command", "Offices", "ARES", "Dropship", "Marine" };

    private TelephoneWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<TelephoneWindow>();

        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (State is not RMCTelephoneBuiState state)
            return;

        _window.Tabs.DisposeAllChildren();
        var tabs = new Dictionary<string, BoxContainer>();
        foreach (var phone in state.Phones)
        {
            if (!tabs.TryGetValue(phone.Category, out var tab))
            {
                tab = new BoxContainer { Orientation = LayoutOrientation.Vertical };
                tabs[phone.Category] = tab;

                var scroll = new ScrollContainer
                {
                    HScrollEnabled = false,
                    VScrollEnabled = true,
                    VerticalExpand = true,
                };

                var category = new BoxContainer { Orientation = LayoutOrientation.Vertical };
                scroll.AddChild(category);

                var searchBar = new LineEdit();
                tab.AddChild(searchBar);
                tab.AddChild(scroll);

                searchBar.OnTextChanged += args =>
                {
                    foreach (var scroll in _window.Tabs.GetControlOfType<ScrollContainer>())
                    {
                        foreach (var container in scroll.GetControlOfType<BoxContainer>())
                        {
                            foreach (var child in container.Children)
                            {
                                if (child is LineEdit otherBar)
                                {
                                    otherBar.SetText(args.Text, false);
                                }
                                else if (child is Button button)
                                {
                                    button.Visible = button.Text?.Contains(args.Text, StringComparison.OrdinalIgnoreCase) ?? false;
                                }
                            }
                        }
                    }
                };
            }

            foreach (var child in tab.Children)
            {
                if (child is not ScrollContainer scroll)
                    continue;

                foreach (var scrollChild in scroll.Children)
                {
                    if (scrollChild is not BoxContainer category)
                        continue;

                    var phoneButton = new Button
                    {
                        Text = phone.Name,
                        StyleClasses = { "OpenBoth" },
                    };
                    phoneButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneCallBuiMsg(phone.Id));
                    category.AddChild(phoneButton);
                    break;
                }
            }
        }

        foreach (var categoryName in TabOrder)
        {
            if (tabs.Remove(categoryName, out var category))
            {
                _window.Tabs.AddChild(category);
                TabContainer.SetTabTitle(category, categoryName);
            }
        }

        foreach (var (categoryName, category) in tabs)
        {
            _window.Tabs.AddChild(category);
            TabContainer.SetTabTitle(category, categoryName);
        }

        // SSCM start
        var historyTab = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        var historyScroll = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true,
            VerticalExpand = true,
        };
        var historyList = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        historyScroll.AddChild(historyList);
        historyTab.AddChild(historyScroll);

        if (state.CallLog.Count == 0)
        {
            historyList.AddChild(new Label { Text = Loc.GetString("phone-call-log-empty") });
        }
        else
        {
            foreach (var entry in state.CallLog)
            {
                var entryBox = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
                var timeLabel = new Label
                {
                    Text = $"[{entry.TimeString}]",
                    MinWidth = 60,
                };
                var nameLabel = new Label
                {
                    Text = $"{entry.CallerName}  ({entry.DeviceName})",
                    HorizontalExpand = true,
                };
                entryBox.AddChild(timeLabel);
                entryBox.AddChild(nameLabel);
                historyList.AddChild(entryBox);
            }
        }

        _window.Tabs.AddChild(historyTab);
        TabContainer.SetTabTitle(historyTab, Loc.GetString("phone-call-log-tab"));

        var blockTab = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        var blockScroll = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true,
            VerticalExpand = true,
        };
        var blockList = new BoxContainer { Orientation = LayoutOrientation.Vertical };
        blockScroll.AddChild(blockList);
        blockTab.AddChild(blockScroll);

        if (state.BlockedPhones.Count == 0)
        {
            blockList.AddChild(new Label { Text = Loc.GetString("phone-block-list-empty") });
        }
        else
        {
            foreach (var blockedId in state.BlockedPhones)
            {
                var matchedPhone = state.Phones.Find(p => p.Id == blockedId);
                var blockedName = string.IsNullOrEmpty(matchedPhone.Name) ? blockedId.ToString() : matchedPhone.Name; // SSCM edit

                var row = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
                var nameLabel = new Label
                {
                    Text = blockedName,
                    HorizontalExpand = true,
                };
                var unblockBtn = new Button
                {
                    Text = Loc.GetString("phone-block-unblock-button"),
                    StyleClasses = { "OpenBoth", "Caution" },
                };
                var capturedId = blockedId;
                unblockBtn.OnPressed += _ => SendPredictedMessage(new RMCTelephoneBlockBuiMsg(capturedId, false));
                row.AddChild(nameLabel);
                row.AddChild(unblockBtn);
                blockList.AddChild(row);
            }
        }

        var separator = new Label { Text = "─────────────────────────────" };
        blockList.AddChild(separator);
        blockList.AddChild(new Label { Text = Loc.GetString("phone-block-add-header") });
        foreach (var phone in state.Phones)
        {
            if (state.BlockedPhones.Contains(phone.Id))
                continue;

            var row = new BoxContainer { Orientation = LayoutOrientation.Horizontal };
            var nameLabel = new Label
            {
                Text = phone.Name,
                HorizontalExpand = true,
            };
            var blockBtn = new Button
            {
                Text = Loc.GetString("phone-block-button"),
                StyleClasses = { "OpenBoth" },
            };
            var capturedId = phone.Id;
            blockBtn.OnPressed += _ => SendPredictedMessage(new RMCTelephoneBlockBuiMsg(capturedId, true));
            row.AddChild(nameLabel);
            row.AddChild(blockBtn);
            blockList.AddChild(row);
        }

        _window.Tabs.AddChild(blockTab);
        TabContainer.SetTabTitle(blockTab, Loc.GetString("phone-block-tab"));
        // SSCM end

        _window.Buttons.DisposeAllChildren();
        if (state.Dnd)
        {
            var disableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth", "Caution" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-enabled"),
            };
            disableDndButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneDndBuiMsg(false));
            _window.Buttons.AddChild(disableDndButton);
        }
        else if (state.CanDnd)
        {
            var enableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-disabled"),
            };
            enableDndButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneDndBuiMsg(true));
            _window.Buttons.AddChild(enableDndButton);
        }
        else
        {
            var enableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-locked"),
                Disabled = true,
            };
            _window.Buttons.AddChild(enableDndButton);
        }
    }
}
