import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/auth/auth.guard';
import { ShellLayout } from './layout/shell.layout';
import { LoginPage } from './pages/login/login.page';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { UsersPage } from './pages/users/users.page';
import { RolesPage } from './pages/roles/roles.page';
import { AuditPage } from './pages/audit/audit.page';
import { ClassesPage } from './pages/classes/classes.page';
import { StaffPage } from './pages/staff/staff.page';
import { ChildrenPage } from './pages/children/children.page';
import { ChildrenFormPage } from './pages/children/children-form.page';
import { ChildrenEntryHistoryPage } from './pages/children/children-entry-history.page';
import { EntryDeskPage } from './pages/entry/entry-desk.page';
import { EntryHistoryPage } from './pages/entry/entry-history.page';
import { PricePlansPage } from './pages/price-plans/price-plans.page';
import { PaymentsPage } from './pages/payments/payments.page';
import { ExpensesPage } from './pages/expenses/expenses.page';
import { AttendancePage } from './pages/attendance/attendance.page';
import { SalariesPage } from './pages/salaries/salaries.page';

import { UsersFormPage } from './pages/users/users-form.page';
import { UsersPasswordPage } from './pages/users/users-password.page';
import { RolesFormPage } from './pages/roles/roles-form.page';
import { ClassesFormPage } from './pages/classes/classes-form.page';
import { StaffFormPage } from './pages/staff/staff-form.page';
import { PricePlansFormPage } from './pages/price-plans/price-plans-form.page';
import { PaymentsFormPage } from './pages/payments/payments-form.page';
import { PaymentsPayPage } from './pages/payments/payments-pay.page';
import { ExpensesFormPage } from './pages/expenses/expenses-form.page';
import { ExpenseCategoryFormPage } from './pages/expenses/expense-category-form.page';
import { SettingsPage } from './pages/settings/settings.page';
import { LogsPage } from './pages/logs/logs.page';
import { ProfilePage } from './pages/profile/profile.page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: ShellLayout,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardPage },
      { path: 'profile', component: ProfilePage },
      {
        path: 'entry/history',
        component: EntryHistoryPage,
        canActivate: [permissionGuard('entry.scan')]
      },
      {
        path: 'entry',
        component: EntryDeskPage,
        canActivate: [permissionGuard('entry.scan')]
      },
      {
        path: 'children/new',
        component: ChildrenFormPage,
        canActivate: [permissionGuard('children.write')]
      },
      {
        path: 'children/:id/edit',
        component: ChildrenFormPage,
        canActivate: [permissionGuard('children.write')]
      },
      {
        path: 'children/:id/entry-history',
        component: ChildrenEntryHistoryPage,
        canActivate: [permissionGuard('entry.scan')]
      },
      {
        path: 'children',
        component: ChildrenPage,
        canActivate: [permissionGuard('children.read')]
      },
      {
        path: 'classes/new',
        component: ClassesFormPage,
        canActivate: [permissionGuard('classes.write')]
      },
      {
        path: 'classes/:id/edit',
        component: ClassesFormPage,
        canActivate: [permissionGuard('classes.write')]
      },
      {
        path: 'classes',
        component: ClassesPage,
        canActivate: [permissionGuard('classes.read')]
      },
      {
        path: 'staff/new',
        component: StaffFormPage,
        canActivate: [permissionGuard('staff.write')]
      },
      {
        path: 'staff/:id/edit',
        component: StaffFormPage,
        canActivate: [permissionGuard('staff.write')]
      },
      {
        path: 'staff',
        component: StaffPage,
        canActivate: [permissionGuard('staff.read')]
      },
      {
        path: 'price-plans/new',
        component: PricePlansFormPage,
        canActivate: [permissionGuard('priceplans.write')]
      },
      {
        path: 'price-plans/:id/edit',
        component: PricePlansFormPage,
        canActivate: [permissionGuard('priceplans.write')]
      },
      {
        path: 'price-plans',
        component: PricePlansPage,
        canActivate: [permissionGuard('priceplans.read')]
      },
      {
        path: 'payments/new',
        component: PaymentsFormPage,
        canActivate: [permissionGuard('payments.write')]
      },
      {
        path: 'payments/:id/pay',
        component: PaymentsPayPage,
        canActivate: [permissionGuard('payments.write')]
      },
      {
        path: 'payments',
        component: PaymentsPage,
        canActivate: [permissionGuard('payments.read')]
      },
      {
        path: 'expenses/categories/new',
        component: ExpenseCategoryFormPage,
        canActivate: [permissionGuard('expenses.write')]
      },
      {
        path: 'expenses/categories/:id/edit',
        component: ExpenseCategoryFormPage,
        canActivate: [permissionGuard('expenses.write')]
      },
      {
        path: 'expenses/new',
        component: ExpensesFormPage,
        canActivate: [permissionGuard('expenses.write')]
      },
      {
        path: 'expenses/:id/edit',
        component: ExpensesFormPage,
        canActivate: [permissionGuard('expenses.write')]
      },
      {
        path: 'expenses',
        component: ExpensesPage,
        canActivate: [permissionGuard('expenses.read')]
      },
      {
        path: 'attendance',
        component: AttendancePage,
        canActivate: [permissionGuard('salaries.read')]
      },
      {
        path: 'salaries',
        component: SalariesPage,
        canActivate: [permissionGuard('salaries.read')]
      },
      {
        path: 'users/new',
        component: UsersFormPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'users/:id/edit',
        component: UsersFormPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'users/:id/password',
        component: UsersPasswordPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'users',
        component: UsersPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'roles/new',
        component: RolesFormPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'roles/:id/edit',
        component: RolesFormPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'roles',
        component: RolesPage,
        canActivate: [permissionGuard('roles.manage')]
      },
      {
        path: 'settings',
        component: SettingsPage,
        canActivate: [permissionGuard('settings.read')]
      },
      {
        path: 'logs',
        component: LogsPage,
        canActivate: [permissionGuard('logs.read')]
      },
      {
        path: 'audit',
        component: AuditPage,
        canActivate: [permissionGuard('audit.read')]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];

