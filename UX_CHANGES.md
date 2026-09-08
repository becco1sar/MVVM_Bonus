# Quality Bonus — UX redesign

Baseline: `a806747` (snapshot taken before any change).
Four commits follow it. Every change is reversible with `git revert`.

## Bugs fixed

| What happened | Where |
|---|---|
| **Log Out** threw `NotImplementedException` | `MainViewModel.LogOut` |
| **Manage Staff** and **Configure Default Bonuses** did nothing — they fired Mediator tokens nobody subscribed to | `HrDashboardViewModel` → `MainViewModel` |
| **Finish and Preview** wrote the bonus to the database *before* showing the sheet, so a mistake was already recorded and the duplicate-period check then blocked a corrected re-entry | `InsertViewModel` |
| The signature sheet printed **"CLEARCHANNEL BELGIUM"** | `PrintingView.xaml` |
| The printed sheet listed rows as **"Item #1"…"Item #10"** instead of the evaluation point text | `ReportService`, `BonusModel` |
| Staff and bonus-config saves ran inside `catch { }` — a failed save looked identical to a successful one | `StaffManagementViewModel`, `BonusConfigViewModel` |
| **Add as New** inserted a worker with no `Teamleader_id`, making them invisible to every team leader | `StaffManagementViewModel` |
| The HR dashboard queried the database at app startup, before anyone had logged in | `MainViewModel` (pages are now created on first navigation) |
| The window could not be resized from its edges, could not be snapped, and covered the taskbar when maximised | `MainWindow.xaml` (now uses `WindowChrome`) |
| **Refresh Preview** on the history screen was bound to a command whose body was commented out | `BonusViewModel` |

## The two changes that matter most

**Review before saving.** Recording a bonus is now Edit → Review → Confirm.
Review renders the sheet and writes nothing; Confirm saves, re-checking the
period in case it was taken meanwhile.

**The unlock rule is visible.** Awarding all three base points unlocks the
additional ones. That was enforced in code and explained nowhere — seven rows
at 40% opacity with nothing to say why. There is now a progress bar against the
three, a plain statement of what is still needed, and a locked notice.

## Design system

`Resources/Styles.xaml` already defined a full token set that almost no view
referenced: 233 hardcoded hex values across nine views, eleven font sizes, and
one button style for every action. Now 15 hex values remain, all on the
deliberately white printable sheet.

Added: line icons replacing emoji; five text styles; four button roles with a
visible keyboard focus ring; dark `ComboBox`, `TextBox`, `ProgressBar`,
`ListBox`, `ListView`. The HR period picker was an unstyled `ComboBox` — a
light-grey Windows control with black selection text on a dark purple card.

## Verifying changes without a build

`Tools/validate-xaml.py` checks XAML well-formedness, properties set twice,
unresolved `StaticResource` keys, keys used before definition, missing event
handlers, and binding paths that exist on no member of the bound view model:

```
python Tools\validate-xaml.py .
```

It is not a compiler. It catches the mistakes a compiler would otherwise be the
first to find, which is useful when editing XAML in bulk.

## Known, deliberately left alone

- `net5.0-windows` has been out of support since May 2022. Retargeting to
  `net8.0-windows` is likely a one-line change but was not attempted without a
  build to verify it.
- `PrintingView` is unreachable: printing now happens inline on the history and
  new-bonus screens. It was restyled rather than deleted — removing a screen is
  a decision for the team.
- A profile with more evaluation points plus extra categories than the ten
  amount columns in `Bonus_General` overwrites its last points. That was
  already true and silent; the Insert screen now warns when it applies.
- Bonus amounts still live in ten fixed columns `AmountA`…`AmountJ`. Normalising
  that into rows would remove the ten-point ceiling and the index arithmetic
  around it, but it is a schema migration, not a UI change.
