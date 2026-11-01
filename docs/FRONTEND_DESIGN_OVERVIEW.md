# UI/UX Design Specification: Universal Meal Manager (Web)

## 1. Visual Language & Design Tokens
* **Aesthetic**: Clean, utilitarian SaaS interface (inspired by Linear and Stripe) with high contrast and minimal visual clutter.
* **Typography**: `Inter` or `Geist Sans` for UI text; `Geist Mono` with `tabular-nums` enforced on all currency, meal counts, and rate calculations so numbers align vertically in tables.
* **Semantic Color Palette**:
  * **Primary (Brand/Action)**: Indigo (`indigo-600`) for primary buttons, active states, and navigation highlights.
  * **Surplus / Positive**: Emerald (`emerald-600` text, `emerald-50` badge background) when a member's deposit exceeds their total cost.
  * **Arrear / Due**: Rose (`rose-600` text, `rose-50` badge background) when a member owes money to the mess.
  * **Warning / Pending**: Amber (`amber-600`) for unapproved join requests or unverified bazar entries.
* **Theme Support**: First-class Light and Dark mode (`slate-950` background in dark mode, `white`/`slate-50` in light mode).

## 2. Global App Shell & Navigation
* **Desktop Sidebar (Left, Collapsible)**:
  * **Top**: **Mess Switcher Dropdown** displaying the active mess name, current user's role badge, and a "+ Create / Join Mess" action.
  * **Middle**: Primary links for Overview, Meal Sheet, Expenses & Deposits, Members, and Financial Reports.
  * **Bottom**: Mess Settings (visible to Meal Managers/Super Admins) and User Profile menu.
* **Top Utility Bar**:
  * **Active Month Switcher**: A prominent month/year selector (e.g., `November 2026`) that globally scopes all dashboard queries and calculations.
  * **Quick Action Button**: A primary `+ Quick Entry` dropdown allowing Managers to log today's meals or add a bazar expense from any screen.
  * **Notification Popover**: Alerts for new member join requests, role changes, and completed RabbitMQ monthly report exports.
* **Mobile Web Adaptation**: The sidebar collapses into a fixed **Bottom Navigation Bar** with 4 core tabs (Overview, Meals, Expenses, Summary) and a slide-out drawer for mess switching.

## 3. Core Screen Layouts & Interactive Workflows

### 3.1 Mess Overview Dashboard (`/dashboard/[messId]`)
* **Top KPI Row (4 Metric Cards)**:
  1. **Live Meal Rate**: Current rate calculated to two decimal places with a subtle trend indicator.
  2. **On-Hand Balance**: Total deposits collected minus total expenses spent.
  3. **Total Mess Expenditure**: Combined Bazar and Utility costs for the active month, plus total meal units consumed.
  4. **My Personal Status**: The logged-in member's personal meal count, total cost, and highlighted **Surplus/Arrear** badge.
* **Middle Split Section**:
  * **Left (65% width)**: Daily Meal & Bazar Trend chart (Recharts bar/line combo showing daily expenditure vs. meal volume).
  * **Right (35% width)**: Today's Meal Snapshot showing total Breakfast, Lunch, and Dinner counts to help the cook or manager plan quantities.
* **Bottom Section**: Recent Activity Feed listing the latest 5 bazar purchases, utility payments, and member deposits.

### 3.2 Interactive Meal Sheet (`/dashboard/[messId]/meals`)
* **Dual-View Toggle**:
  * **Daily Entry View (Default for Mobile & Quick Logging)**: Lists all active members vertically for a selected date. Each row features quick stepper controls (`-`, `0`, `0.5`, `1`, `2`, `+`) for Breakfast, Lunch, and Dinner, plus a "Guest Meal" counter.
  * **Monthly Matrix Grid (Desktop Spreadsheet View)**: Sticky left column for Member Names, horizontally scrollable date columns (`Day 1` to `Day 31`), and a sticky right column showing each member's monthly total.
* **UX Polish**: Toggling or incrementing a meal count triggers an optimistic UI update with a subtle pulse animation; read-only cells are locked automatically for regular Members.

### 3.3 Expenses & Deposits Hub (`/dashboard/[messId]/expenses`)
* **Three-Tab Layout**:
  1. **Bazar Expenses**: Filterable table showing Date, Buyer (Member avatar + name), Item summary, and Total Cost. Clicking `+ Add Bazar` opens a slide-over **Sheet (Drawer)** with dynamic row inputs (Item Name, Quantity, Price) that auto-sum the total before submission.
  2. **Utility Bills**: Grid of cards representing custom `UtilityTypes` (Electricity, Wi-Fi, Cook Salary, Gas) showing paid vs. pending status for the month.
  3. **Member Deposits**: Ledger of cash/digital contributions added by members toward their monthly mess balance, with Manager approval toggles.

### 3.4 Financial Settlement & Reports (`/dashboard/[messId]/reports`)
* **Master Accounting Table**: High-density table displaying Member Name, Total Meals, Meal Cost, Utility Share, Total Cost, Total Deposited, and Final Balance (color-coded green for Surplus, red for Arrear).
* **Export & Close Bar**: Action buttons to export the monthly statement as a PDF and a "Lock Month" toggle for the Meal Manager to freeze edits once accounts are settled.

### 3.5 Member Directory & Invites (`/dashboard/[messId]/members`)
* **Pending Approvals Banner**: Appears at the top for Managers when users request to join via an invite link, featuring one-click `Approve` and `Reject` buttons.
* **Invite Modal**: Generates a shareable invite link or QR code with an expiration timer and copy-to-clipboard button.
